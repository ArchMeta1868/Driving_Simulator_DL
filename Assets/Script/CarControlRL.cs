//======================================================================
//  CarControlRL.cs  – ML-Agents bridge: converts (gas, brake, steer)
//  to CarControl.SetInputs(accel, steer).    *No* physics duplicated.
//======================================================================
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(CarControl))]
[RequireComponent(typeof(Rigidbody))]
public class CarControlRL : MonoBehaviour
{
    [Header("Waypoint Navigation")]
    public WaypointNavigator navigator; // Optional navigator for logging
    /* ---------- inputs written by DriveAgent ---------- */
    [HideInInspector] public float gasInput;    // 0‥1
    [HideInInspector] public float brakeInput;  // 0‥1
    [HideInInspector] public float steerInput;  // –1‥+1

    /* ---------- cached refs ---------- */
    CarControl core;      // shared physics script
    Rigidbody rb;

    private List<float> checkpointTimes = new List<float>();
    private string logPath;

    void Awake()
    {
        core = GetComponent<CarControl>();   // has all wheel & tuning data
        rb = GetComponent<Rigidbody>();    // used only for SpeedKMH

        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string logDir = Path.Combine(projectRoot, "script", "log");
        Directory.CreateDirectory(logDir);
        logPath = Path.Combine(logDir, GetType().Name + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt");
    }

    /* ---------- forward inputs every physics step ---------- */
    void FixedUpdate()
    {
        if (navigator != null && navigator.GetWaypointCount() > 0)
        {
            int prev = navigator.GetCurrentIndex();
            navigator.CheckAndAdvance(transform.position);
            int cur = navigator.GetCurrentIndex();
            if (cur != prev)
            {
                checkpointTimes.Add(Time.time);
                if (cur == 0)
                {
                    using (var w = new StreamWriter(logPath, true))
                    {
                        w.WriteLine(string.Join(",", checkpointTimes.Select(t => t.ToString("F2"))));
                    }
                    checkpointTimes.Clear();
                }
            }
        }

        float accel = Mathf.Clamp01(gasInput) - Mathf.Clamp01(brakeInput); // + = throttle, – = brake
        float steer = Mathf.Clamp(steerInput, -1f, 1f);

        core.SetInputs(accel, steer);    // single call to the real drivetrain
    }

    /* ---------- ML-Agents helper methods ---------- */
    public void ApplyActions(float gas, float brake, float steer)
    {
        gasInput = gas;
        brakeInput = brake;
        steerInput = steer;
    }

    public float SpeedKMH => rb.linearVelocity.magnitude * 3.6f;

    // proxy so DriveAgent keeps using car.maxSpeed unchanged
    public float maxSpeed => core.maxSpeed;
}
