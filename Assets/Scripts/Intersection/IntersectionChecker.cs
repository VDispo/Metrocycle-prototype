using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

public class IntersectionChecker : MonoBehaviour
{
    [Header("IMPORTANT: Add Lane Detect Objects in Counter Clockwise direction, left lane detects in even positions.")]
    public GameObject[] laneDetects;
    [Header("IMPORTANT: Add Green light objects in same order (and number) as lane detects.")]
    public GameObject[] greenLights = null;
    [TextArea(3, 10)] public string wrongWayText;
    [TextArea(3, 10)] public string redLightText;
    [TextArea(3, 10)] public string leftLaneLeftTurnText;
    [TextArea(3, 10)] public string leftLaneRightTurnText;
    [TextArea(3, 10)] public string leftLaneUTurnText;
    [TextArea(3, 10)] public string rightLaneLeftTurnText;
    [TextArea(3, 10)] public string rightLaneRightTurnText;
    [TextArea(3, 10)] public string rightLaneUTurnText;

    // Index Legend:
    //         13 12  10 11
    //        | R L || L R |
    // -------\   v    ^   /-----
    //   15 R   \        /  R 9
    //   14 L <   \    /  < L 8
    // --------     \/      -----
    // --------     /\      -----
    //    0 L >   /    \  > L 6
    //    1 R    /      \   R 7
    // --------/  v    ^  \ -----
    //        | R L || L R |
    //          3 2    4 5

    // Idea: In the local perspective of the driver, they are either in Idx 0 or 1
    // Hence, we only need to hardcode rules for this perspective
    // then simply "rotate" the rules if the absolute/global position of the driver is Idx 8 or 9, etc.

    // From Idx 0 or 1, crossing to the following lanes are invalid (wrong way)
    private int[] invalid_WrongWayIdx = {4,5 , 8,9 , 12,13};

    // From Idx 0 to the following Idx are bad right/left (u-)turns
    private int[] bad_LeftLaneRightTurnIdx = {2, 3};
    private int[] bad_LeftLaneLeftTurnIdx = {11};
    private int[] bad_LeftLaneUTurnIdx = {15};

    // From Idx 1 to the following Idx are bad right/left (u-)turns
    private int[] bad_RightLaneRightTurnIdx = {2};
    private int[] bad_RightLaneLeftTurnIdx = {10, 11};
    private int[] bad_RightLaneUTurnIdx = {14, 15};

    // Idx of lane where driver came from
    private int entryIdx;
    private float headCheckRefTime;

    void Start() {
        entryIdx = -1;

        GameManager.Instance.resetSignal.AddListener(() => {
            entryIdx = -1;
            headCheckRefTime = -1;
        });
    }

    int GetLaneDetectIndex(GameObject target) {
        for (int i = 0; i < laneDetects.Length; ++i) {
            if (laneDetects[i] == target)
                return i;
        }

        return -1;
    }

    public void resetEntry() {
        entryIdx = -1;
    }

    public void laneDetectEntered(GameObject laneDetect) {
        Debug.Log("Collision with" + laneDetect);
        int idx = GetLaneDetectIndex(laneDetect);
        bool isEntry = false;
        if (entryIdx == -1) {
            entryIdx = idx;
            Debug.Log("Entry:" + entryIdx);
            isEntry = true;

            // NOTE: If a user is turning (left/right/u-turn), technically head check
            // and blinker should be checked here at ENTRY. BUT we are not sure
            // if user will be performing a turn or going straight until EXIT
            // hence, we record ENTRY time for reference later
            headCheckRefTime = Time.time;
        }
        else {
            // "rotate" perspective to either Idx 0 or 1
            // e.g. if driver came from Idx 9 and drove to Idx 15,
            // that is equivalent to driving from Idx 1 to Idx 7
            // where 1 = 9-9 + (9%2), 7 = 15-9 + (9%2)
            Debug.Log("Exit:" + idx);
            idx = idx - entryIdx + (entryIdx % 2);
            if (idx < 0) {
                // e.g. from Idx 8 to Idx 0; 0 - 8 = -8
                // but this is just a 180 inversion of Idx 0 to 8
                // hence we expect exit idx to be 8 = -8 + 16
                idx += laneDetects.Length;
            }

            GameManager.Instance.startBlinkerCancelTimer();
        }

        PopupType type = PopupType.INFO;
        string popupText = "";

        if (isEntry) {
            if ((entryIdx % 4) >= 2) {
                // Refer to legend, every 3rd or 4th Idx is in reverse direction
                Debug.Log("Entered Wrong Way " + entryIdx);
                type = PopupType.ERROR;
                popupText = wrongWayText + " Entry";

                // "Forgive" driver, set entryIdx to adjacent valid entry lane
                // e.g. If they entered via Idx 2, correct to Idx 4
                // e.g. wrong entry at Idx 14; correctedt to 14+2 = Idx 16 = Idx 0
                entryIdx = (entryIdx+2) % laneDetects.Length;
            } else {
                if (greenLights != null) {
                    int trafficLightIdx = (int) (entryIdx / 2);
                    Debug.Log($"TRAFFIC LIGHT {greenLights} {trafficLightIdx} {entryIdx}");
                    Debug.Log(greenLights[trafficLightIdx] + " " + (greenLights[trafficLightIdx]?.activeSelf ?? false));
                    if (greenLights[trafficLightIdx] != null && !greenLights[trafficLightIdx].activeSelf) {
                        Debug.Log("Entered on Red Light " + entryIdx);
                        type = PopupType.ERROR;
                        popupText = redLightText;

                        GameManager.Instance.setErrorReason(Metrocycle.ErrorReason.INTERSECTION_REDLIGHT);
                    }
                }
            }
        }
        else {
            // Normalize entryIdx to either Idx 0 or 1
            entryIdx = entryIdx % 2;

            // Check blinker/head check for right turn
            if (bad_LeftLaneRightTurnIdx.Contains(idx)) {
                Debug.Log("Checking Proper RIGHT Turn");
                GameManager.Instance.checkProperTurnOrLaneChange(Direction.RIGHT, headCheckRefTime);
            }
            // Check blinker/head check for left turn/u-turn
            if (bad_RightLaneLeftTurnIdx.Contains(idx)
                || bad_RightLaneUTurnIdx.Contains(idx)) {
                Debug.Log("Checking Proper LEFT Turn");
                GameManager.Instance.checkProperTurnOrLaneChange(Direction.LEFT, headCheckRefTime);
            }

            if (idx == 0 || idx == 1) {
                // NOTE: SOFT ERROR. Driver back-pedalled in intersection. Assume they will exit intersection normally at some point
                // TODO: handle this situation better. For now, we set it internally as error but no popup prompt
                GameManager.Instance.setErrorReason(Metrocycle.ErrorReason.INTERSECTION_WRONGWAY);

                entryIdx = -1;
                return;
            }

            // TODO: use PopupType.WARNING for bad
            if (invalid_WrongWayIdx.Contains(idx)) {
                Debug.Log("Invalid Wrong Way " + idx);
                type = PopupType.ERROR;
                popupText = wrongWayText;

                GameManager.Instance.setErrorReason(Metrocycle.ErrorReason.INTERSECTION_WRONGWAY);
            }
            else if (entryIdx == 0){
                Debug.Log("From Left to " + idx);
                if (bad_LeftLaneRightTurnIdx.Contains(idx)) {
                    type = PopupType.ERROR;
                    popupText = leftLaneRightTurnText;

                    GameManager.Instance.setErrorReason(Metrocycle.ErrorReason.INTERSECTION_RIGHTTURN_FROM_OUTERLANE);
                }
                else if (bad_LeftLaneLeftTurnIdx.Contains(idx)) {
                    type = PopupType.ERROR;
                    popupText = leftLaneLeftTurnText;

                    GameManager.Instance.setErrorReason(Metrocycle.ErrorReason.INTERSECTION_LEFTTURN_TO_OUTERLANE);
                }
                else if (bad_LeftLaneUTurnIdx.Contains(idx)) {
                    type = PopupType.ERROR;
                    popupText = leftLaneUTurnText;

                    GameManager.Instance.setErrorReason(Metrocycle.ErrorReason.INTERSECTION_LEFT_UTURN_TO_OUTERLANE);
                }
            }
            else {
                Debug.Log("From Right to " + idx);
                if (bad_RightLaneRightTurnIdx.Contains(idx)) {
                    type = PopupType.ERROR;
                    popupText = rightLaneRightTurnText;

                    GameManager.Instance.setErrorReason(Metrocycle.ErrorReason.INTERSECTION_RIGHTTURN_TO_OUTERLANE);
                }
                else if (bad_RightLaneLeftTurnIdx.Contains(idx)) {
                    type = PopupType.ERROR;
                    popupText = rightLaneLeftTurnText;

                    GameManager.Instance.setErrorReason(Metrocycle.ErrorReason.INTERSECTION_LEFTTURN_FROM_OUTERLANE);
                }
                else if (bad_RightLaneUTurnIdx.Contains(idx)) {
                    type = PopupType.ERROR;
                    popupText = rightLaneUTurnText;

                    GameManager.Instance.setErrorReason(Metrocycle.ErrorReason.INTERSECTION_LEFT_UTURN_FROM_OUTERLANE);
                }
            }

            // reset entryIdx
            entryIdx = -1;
            headCheckRefTime = -1f;
        }

        Debug.Log(popupText);

        switch (type) {
        case PopupType.ERROR:
            GameManager.Instance.PopupSystem.popError(
                "You used the intersection incorrectly", popupText
            );
            break;
        default:
            break;
        }
    }
}
