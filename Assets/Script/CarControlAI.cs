using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(CarControl))]
[RequireComponent(typeof(Rigidbody))]
public class CarControlAI : MonoBehaviour
{
    [Header("Waypoint Navigation")]
    public WaypointNavigator navigator;     // Assign in Inspector

    [Header("AI Tuning")]
    [Tooltip("How far ahead to look to modulate speed.")]
    public float lookAheadDist = 12f;
    [Tooltip("How strongly the AI tries to match desired speed.")]
    public float accelAggression = 0.2f;

    private CarControl carControl;
    private Rigidbody rb;

    private List<float> checkpointTimes = new List<float>();
    private string logPath;

    private void Awake()
    {
        carControl = GetComponent<CarControl>();
        rb = GetComponent<Rigidbody>();

        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string logDir = Path.Combine(projectRoot, "script", "log");
        Directory.CreateDirectory(logDir);
        logPath = Path.Combine(logDir, GetType().Name + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt");
    }

    private void FixedUpdate()
    {
        // If there's no navigator or it has no waypoints, do nothing
        if (navigator == null || navigator.GetWaypointCount() == 0)
        {
            carControl.SetInputs(0f, 0f);
            return;
        }

        int prevIndex = navigator.GetCurrentIndex();
        navigator.CheckAndAdvance(transform.position);
        int curIndex = navigator.GetCurrentIndex();
        if (curIndex != prevIndex)
        {
            checkpointTimes.Add(Time.time);
            if (curIndex == 0)
            {
                using (var w = new StreamWriter(logPath, true))
                {
                    w.WriteLine(string.Join(",", checkpointTimes.Select(t => t.ToString("F2"))));
                }
                checkpointTimes.Clear();
            }
        }

        // Compute the inputs (accel and steer) using the current waypoint
        float accelInput, steerInput;
        ComputeAIInputs(out accelInput, out steerInput);

        // Send the inputs to the CarControl (the core physics)
        carControl.SetInputs(accelInput, steerInput);
    }

    private void ComputeAIInputs(out float accel, out float steer)
    {
        accel = 0f;
        steer = 0f;

        // 1) Current waypoint
        Transform target = navigator.GetCurrentWaypoint();
        if (!target) return;

        // 2) Direction & distance to waypoint
        Vector3 toTgt = target.position - transform.position;
        float dist = toTgt.magnitude;
        Vector3 dir = toTgt.normalized;

        // 3) Steering from angle
        float angDeg = Vector3.SignedAngle(transform.forward, dir, Vector3.up);
        // Map ±45° => ±1 steering input
        steer = Mathf.Clamp(angDeg / 45f, -1f, 1f);

        // 4) Speed calculation
        float currentSpeed = rb.linearVelocity.magnitude; // m/s
        float facingFactor = Mathf.Clamp01((90f - Mathf.Abs(angDeg)) / 90f);
        float desiredSpeed = Mathf.Lerp(5f, carControl.maxSpeed, facingFactor)
                             * Mathf.Clamp01(dist / lookAheadDist);

        // 5) Accel / brake
        float speedError = desiredSpeed - currentSpeed;
        accel = Mathf.Clamp(speedError * accelAggression, -1f, 1f);
    }
}
