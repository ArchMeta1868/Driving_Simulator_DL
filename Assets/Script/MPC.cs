using UnityEngine;

[RequireComponent(typeof(CarControl))]
[RequireComponent(typeof(Rigidbody))]
public class MPC : MonoBehaviour
{
    [Header("Waypoint Navigation")]
    public WaypointNavigator navigator;  // Assign in Inspector

    [Header("AI Tuning")]
    [Tooltip("How far ahead to look to modulate speed.")]
    public float lookAheadDist = 12f;
    [Tooltip("How strongly the AI tries to match desired speed.")]
    public float accelAggression = 0.2f;

    [Header("MPC Parameters")]
    [Tooltip("Number of prediction steps.")]
    public int predictionSteps = 10;
    [Tooltip("Timestep used for prediction.")]
    public float predictionDt = 0.2f;
    [Tooltip("Approximate wheel base length.")]
    public float wheelBase = 2.5f;

    private CarControl carControl;
    private Rigidbody rb;

    private void Awake()
    {
        carControl = GetComponent<CarControl>();
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (navigator == null || navigator.GetWaypointCount() == 0)
        {
            carControl.SetInputs(0f, 0f);
            return;
        }

        navigator.CheckAndAdvance(transform.position);

        float accelInput, steerInput;
        ComputeMPCInputs(out accelInput, out steerInput);
        carControl.SetInputs(accelInput, steerInput);
    }

    private void ComputeMPCInputs(out float accel, out float steer)
    {
        accel = 0f;
        steer = 0f;

        Transform target = navigator.GetCurrentWaypoint();
        if (!target) return;

        Vector3 targetPos = target.position;
        Vector3 pos = transform.position;
        float yaw = transform.eulerAngles.y * Mathf.Deg2Rad;
        float speed = rb.linearVelocity.magnitude;

        // candidate discrete inputs
        float[] accelOptions = { -1f, -0.5f, 0f, 0.5f, 1f };
        float[] steerOptions = { -1f, -0.5f, 0f, 0.5f, 1f };

        float bestCost = float.MaxValue;
        float bestAccel = 0f;
        float bestSteer = 0f;

        foreach (float a in accelOptions)
        {
            foreach (float s in steerOptions)
            {
                Vector3 p = pos;
                float v = speed;
                float h = yaw;
                int idx = navigator.GetCurrentIndex();
                Vector3 tgt = targetPos;

                for (int step = 0; step < predictionSteps; step++)
                {
                    float delta = s * carControl.steeringAngle * Mathf.Deg2Rad;
                    // simple kinematic bicycle model
                    p.x += v * Mathf.Cos(h) * predictionDt;
                    p.z += v * Mathf.Sin(h) * predictionDt;
                    h += v / wheelBase * Mathf.Tan(delta) * predictionDt;
                    v += a * carControl.maxSpeed * accelAggression * predictionDt;
                    v = Mathf.Clamp(v, 0f, carControl.maxSpeed);

                    // check if we would reach current waypoint
                    if (Vector3.Distance(p, tgt) < navigator.waypointRadius)
                    {
                        idx = (idx + 1) % navigator.GetWaypointCount();
                        tgt = navigator.waypoints[idx].position;
                    }
                }

                float distCost = Vector3.Distance(p, tgt);
                float dirCost = Mathf.Abs(Mathf.DeltaAngle(h * Mathf.Rad2Deg,
                           Mathf.Atan2(tgt.z - p.z, tgt.x - p.x) * Mathf.Rad2Deg));
                float cost = distCost + 0.1f * dirCost;

                if (cost < bestCost)
                {
                    bestCost = cost;
                    bestAccel = a;
                    bestSteer = s;
                }
            }
        }

        // optionally modulate speed toward lookAheadDist
        float facing = Vector3.SignedAngle(transform.forward, (targetPos - pos).normalized, Vector3.up);
        float facingFactor = Mathf.Clamp01((90f - Mathf.Abs(facing)) / 90f);
        float desiredSpeed = Mathf.Lerp(5f, carControl.maxSpeed, facingFactor)
                             * Mathf.Clamp01(Vector3.Distance(pos, targetPos) / lookAheadDist);
        float speedError = desiredSpeed - speed;
        bestAccel = Mathf.Clamp(bestAccel + speedError * accelAggression, -1f, 1f);

        accel = bestAccel;
        steer = Mathf.Clamp(bestSteer, -1f, 1f);
    }
}
