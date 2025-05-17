using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

[RequireComponent(typeof(CarControlRL))]
public class DriveAgent : Agent
{
    [Header("Track")]
    // REMOVED: public Transform[] waypoints; 
    public LayerMask trackMask;
    public float maxEpisodeTime = 90f;

    // CHANGED: Instead of storing raw waypoints,
    // we store a reference to WaypointNavigator:
    [SerializeField] private WaypointNavigator navigator;  // Assign in Inspector

    CarControlRL car;
    float timer;
    Vector3 startPos;
    Quaternion startRot;

    // REMOVED: int wpIndex;

    public override void Initialize()
    {
        car = GetComponent<CarControlRL>();
        startPos = transform.position;
        startRot = transform.rotation;
    }

    public override void OnEpisodeBegin()
    {
        // Reset the car’s position
        transform.SetPositionAndRotation(startPos, startRot);
        var r = car.GetComponent<Rigidbody>();
        r.linearVelocity = r.angularVelocity = Vector3.zero;

        // Reset timer
        timer = 0;

        // CHANGED: If we want to reset the navigator’s waypoints to start from 0,
        // we can do something like this. Since the original navigator code
        // doesn't have a direct "reset" method, we can do a loop or add a method:
        // But for minimal changes, we won't do that unless needed.
        // Example if needed:
        // if (navigator != null) navigator.ResetToFirstWaypoint(); 
        // (Or cycle until 'currentIndex == 0')
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Speed
        Vector3 lv = transform.InverseTransformDirection(
                         car.GetComponent<Rigidbody>().linearVelocity);
        sensor.AddObservation(lv.x / car.maxSpeed);
        sensor.AddObservation(lv.z / car.maxSpeed);

        // CHANGED: Instead of using waypoints[wpIndex],
        // we get the current waypoint from the navigator:
        Transform currentWaypoint = navigator ? navigator.GetCurrentWaypoint() : null;
        if (currentWaypoint)
        {
            Vector3 to = currentWaypoint.position - transform.position;
            Vector3 ld = transform.InverseTransformDirection(to.normalized);
            sensor.AddObservation(ld.x);
            sensor.AddObservation(ld.z);
        }
        else
        {
            // If no waypoint or no navigator, just add zeros:
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
        }

        // Rays left/center/right (unchanged)
        float[] angles = { -40, -20, 0, 20, 40 };
        foreach (float a in angles)
        {
            Vector3 d = Quaternion.Euler(0, a, 0) * transform.forward;
            bool hitSomething = Physics.Raycast(
                transform.position + Vector3.up * 0.5f,
                d, out var hit, 20f, trackMask
            );
            sensor.AddObservation(hitSomething ? hit.distance / 20f : 1f);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float gas = Mathf.Clamp01(actions.ContinuousActions[0]);
        float brake = Mathf.Clamp01(actions.ContinuousActions[1]);
        float steer = actions.ContinuousActions[2];

        // Apply actions to CarControlRL
        car.ApplyActions(gas, brake, steer);

        // Reward for moving forward
        Vector3 velocity = car.GetComponent<Rigidbody>().linearVelocity;
        float fwd = Vector3.Dot(velocity, transform.forward);
        if (fwd > 0) AddReward(0.002f * (fwd / car.maxSpeed));
        else AddReward(-0.002f);

        // CHANGED: Instead of manually checking wpIndex, we rely on the navigator:
        if (navigator)
        {
            // 1) Check if we're close enough to advance to the next waypoint
            navigator.CheckAndAdvance(transform.position);

            // 2) We'll compute distance to the *current* waypoint
            Transform currentWaypoint = navigator.GetCurrentWaypoint();
            if (currentWaypoint)
            {
                // If we are within some threshold, we add a reward & check if we wrapped around
                float distance = Vector3.Distance(transform.position, currentWaypoint.position);
                if (distance < 5f)
                {
                    AddReward(5f);
                    // navigator.AdvanceToNextWaypoint(); // (already called in CheckAndAdvance)

                    // If we just advanced, see if we've looped back to index 0
                    if (navigator.GetCurrentIndex() == 0)
                    {
                        AddReward(20f);
                        EndEpisode();
                    }
                }
            }
        }

        // Check if we are "off track"
        if (!Physics.Raycast(transform.position + Vector3.up * 0.2f,
                             Vector3.down, 0.3f, trackMask))
        {
            AddReward(-20f);
            EndEpisode();
        }

        // Check time limit
        timer += Time.fixedDeltaTime;
        if (timer > maxEpisodeTime) EndEpisode();
    }

    void FixedUpdate() => RequestDecision();

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var c = actionsOut.ContinuousActions;
        float v = Input.GetAxis("Vertical");
        c[0] = Mathf.Max(0, v);   // gas
        c[1] = Mathf.Max(0, -v);  // brake
        c[2] = Input.GetAxis("Horizontal");
    }
}
