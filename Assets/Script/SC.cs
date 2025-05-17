using UnityEngine;

[RequireComponent(typeof(CarControl))]
[RequireComponent(typeof(Rigidbody))]
public class SC : MonoBehaviour
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

    private const float kStanleyGain = 1.0f;

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

        // Current waypoint
        Transform current = navigator.GetCurrentWaypoint();
        if (!current) return;

        int count = navigator.GetWaypointCount();
        int curIndex = navigator.GetCurrentIndex();
        Transform next = (count > 1) ? navigator.waypoints[(curIndex + 1) % count] : current;

        // Direction of path segment
        Vector3 pathDir = (next.position - current.position).normalized;
        if (pathDir.sqrMagnitude < 0.0001f)
        {
            pathDir = (current.position - transform.position).normalized;
        }

        // Vector toward the current waypoint
        Vector3 toCurrent = current.position - transform.position;
        float dist = toCurrent.magnitude;

        // When starting far away, steer directly toward the waypoint
        bool approachMode = dist > navigator.waypointRadius * 2f;
        Vector3 goalDir = approachMode ? toCurrent.normalized : pathDir;

        // Heading error
        float headingErrDeg = Vector3.SignedAngle(transform.forward, goalDir, Vector3.up);

        // Cross track only once we are near the path
        float cross = 0f;
        if (!approachMode)
        {
            Vector3 offset = transform.position - current.position;
            cross = Vector3.Cross(pathDir, offset).y;
        }

        // Stanley control law
        float vel = rb.linearVelocity.magnitude + 0.1f; // prevent div by zero
        float correctionDeg = Mathf.Atan2(kStanleyGain * cross, vel) * Mathf.Rad2Deg;
        float steerDeg = headingErrDeg + correctionDeg;
        steer = Mathf.Clamp(steerDeg / 45f, -1f, 1f);

        // Speed control similar to CarControlAI
        float facingFactor = Mathf.Clamp01((90f - Mathf.Abs(steerDeg)) / 90f);
        float desiredSpeed = Mathf.Lerp(5f, carControl.maxSpeed, facingFactor)
                             * Mathf.Clamp01(dist / lookAheadDist);

        float currentSpeed = rb.linearVelocity.magnitude; // m/s
        float speedError = desiredSpeed - currentSpeed;
        accel = Mathf.Clamp(speedError * accelAggression, -1f, 1f);
    }
}
