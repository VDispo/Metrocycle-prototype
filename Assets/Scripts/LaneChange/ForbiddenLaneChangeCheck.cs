using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ForbiddenLaneChangeCheck : MonoBehaviour
{
    private bool hasCrossedLine;

    void Start() {
        hasCrossedLine = false;
    }

    public void enteredLane(GameObject lane) {
        if (hasCrossedLine) {
            Debug.Log("Illegal lane change!!!!");

            GameManager.Instance.PopupSystem.popError(
                "You used the intersection incorrectly",
                "You are not allowed to change lane anymore when the line is solid."
            );

            GameManager.Instance.setErrorReason(Metrocycle.ErrorReason.LANECHANGE_NOTALLOWED);
        }

        hasCrossedLine = false;
    }

    void OnTriggerEnter (Collider other) {
        Debug.Log($"Solid line crossed {other}");
        hasCrossedLine = true;
    }
}
