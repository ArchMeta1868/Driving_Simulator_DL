using UnityEngine;

[RequireComponent(typeof(CarControl))]
[RequireComponent(typeof(Rigidbody))]
public class PP : MonoBehaviour
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

    private const float wheelBase = 2.5f; // approximate wheel base for pure pursuit

    private void Awake()
    {
        carControl = GetComponent<CarControl>();
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        // If there's no navigator or it has no waypoints, do nothing
        if (navigator == null || navigator.GetWaypointCount() == 0)
        {
            carControl.SetInputs(0f, 0f);
            return;
        }

        // Check if we've reached the current waypoint; if so, advance
        navigator.CheckAndAdvance(transform.position);

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

        // 2) Determine lookahead point along the path
        Vector3 lookAheadPoint = ComputeLookAheadPoint(lookAheadDist);

        // 3) Pure pursuit steering based on lookahead point
        Vector3 localPoint = transform.InverseTransformPoint(lookAheadPoint);
        float ld = Mathf.Max(localPoint.magnitude, 0.001f);
        float curvature = (2f * localPoint.x) / (ld * ld);
        float steerAngle = Mathf.Atan(curvature * wheelBase) * Mathf.Rad2Deg;
        float maxSteer = Mathf.Max(1f, carControl.steeringAngle);
        steer = Mathf.Clamp(steerAngle / maxSteer, -1f, 1f);

        // 4) Speed calculation similar to CarControlAI
        float angDeg = Mathf.Abs(Mathf.Atan2(localPoint.x, localPoint.z) * Mathf.Rad2Deg);
        float dist = Vector3.Distance(transform.position, target.position);
        float currentSpeed = rb.linearVelocity.magnitude; // m/s
        float facingFactor = Mathf.Clamp01((90f - angDeg) / 90f);
        float desiredSpeed = Mathf.Lerp(5f, carControl.maxSpeed, facingFactor)
                             * Mathf.Clamp01(dist / lookAheadDist);

        // 5) Accel / brake
        float speedError = desiredSpeed - currentSpeed;
        accel = Mathf.Clamp(speedError * accelAggression, -1f, 1f);
    }

    private Vector3 ComputeLookAheadPoint(float distance)
    {
        var wps = navigator.waypoints;
        if (wps == null || wps.Length == 0)
            return transform.position;

        Vector3 currentPos = transform.position;
        int idx = navigator.GetCurrentIndex();
        float remaining = distance;

        while (true)
        {
            Vector3 nextPos = wps[idx].position;
            float segLen = Vector3.Distance(currentPos, nextPos);
            if (segLen >= remaining)
            {
                float t = Mathf.Clamp01(remaining / segLen);
                return Vector3.Lerp(currentPos, nextPos, t);
            }

            remaining -= segLen;
            currentPos = nextPos;
            idx = (idx + 1) % wps.Length;

            // Safety: if we've looped the entire path, break
            if (idx == navigator.GetCurrentIndex())
                return nextPos;
        }
    }
}
