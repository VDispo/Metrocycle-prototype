﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Speedometer : MonoBehaviour {
    // private const float MAX_SPEED_ANGLE = -20;
    // private const float ZERO_SPEED_ANGLE = 230;

    // private Transform needleTranform;
    // private Transform speedLabelTemplateTransform;
    // public float speedLabelDistance = -1;

    private Text digitalSpeedometerText;

    private float speedMax;
    private float speed;

    [SerializeField] private static float runningAvgSpeed;
    [SerializeField] private static int numSpeedSamples;

    [SerializeField] private int updateEveryNthFrame = 5;
    private int frameCount;

    private void Awake() {
        // needleTranform = transform.Find("needle");
        // speedLabelTemplateTransform = transform.Find("speedLabelTemplate2");
        // speedLabelTemplateTransform.gameObject.SetActive(false);

        // if (speedLabelDistance == -1) {
        //     speedLabelDistance = (speedLabelTemplateTransform.Find("speedText").transform.position
        //     - speedLabelTemplateTransform.Find("dashImage").transform.position).magnitude;
        // }

        digitalSpeedometerText = transform.Find("digital Speedometer").GetComponent<Text>();

        speed = 0f;
        speedMax = 120f;
        runningAvgSpeed = 0f;
        numSpeedSamples = 0;

        // CreateSpeedLabels();
        frameCount = 0;
    }

    private void Update() {
        speed = GameManager.Instance.getBikeSpeed();
        if (speed > speedMax) speed = speedMax;

        // needleTranform.eulerAngles = new Vector3(0,0,GetSpeedRotation());

        if (++frameCount == updateEveryNthFrame) {
            if (speed > 1f) {
                ++numSpeedSamples;
                runningAvgSpeed = runningAvgSpeed*(numSpeedSamples-1) / numSpeedSamples + (speed / numSpeedSamples);
            }

            // Update digital Speedometer display
            digitalSpeedometerText.text = Mathf.RoundToInt(speed) + "";
            // reset frameCount
            frameCount = 0;
        }
    }

    // private void CreateSpeedLabels() {
    //     int labelAmount=6;
    //     float totalAngleSize = ZERO_SPEED_ANGLE - MAX_SPEED_ANGLE;

    //     for (int i=0;i <= labelAmount; i++){
    //         Transform speedLabelTransform = Instantiate(speedLabelTemplateTransform, transform);
    //         float labelSpeedNormalized = (float)i / labelAmount;
    //         float speedLabelAngle = ZERO_SPEED_ANGLE - labelSpeedNormalized * totalAngleSize;

    //         Transform speedText = speedLabelTransform.Find("speedText");

    //         speedLabelTransform.eulerAngles = new Vector3(0,0,speedLabelAngle);
    //         speedText.GetComponent<Text>().text = Mathf.RoundToInt(labelSpeedNormalized * speedMax).ToString();
    //         speedText.eulerAngles = Vector3.zero;
    //         speedLabelTransform.gameObject.SetActive(true);

    //         speedText.transform.position = Vector3.MoveTowards(speedText.transform.position, needleTranform.position, speedLabelDistance);
    //     }
    //     needleTranform.SetAsLastSibling();
    // }

    // private float GetSpeedRotation() {
    //     float totalAngleSize = ZERO_SPEED_ANGLE - MAX_SPEED_ANGLE;

    //     float speedNormalized = speed / speedMax;

    //     return ZERO_SPEED_ANGLE - speedNormalized * totalAngleSize;
    // }

    public static float GetAvgSpeed()
    {
        return runningAvgSpeed;
    }
}
