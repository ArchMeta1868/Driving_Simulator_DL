using UnityEngine;

/// <summary>
/// Holds a list of waypoints and manages which one is "current",
/// advancing to the next when close enough.
/// </summary>
public class WaypointNavigator : MonoBehaviour
{
    [Tooltip("List of waypoints in the order the AI should follow.")]
    public Transform[] waypoints;

    [Tooltip("Minimum distance to consider a waypoint 'reached'.")]
    public float waypointRadius = 3f;

    // Keeps track of the current waypoint index
    private int currentIndex = 0;

    /// <summary>
    /// Returns the currently targeted waypoint Transform,
    /// or null if waypoints are not set.
    /// </summary>
    public Transform GetCurrentWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0) return null;
        return waypoints[currentIndex];
    }

    /// <summary>
    /// Checks how close 'position' is to the current waypoint,
    /// and if within radius, advances to the next waypoint.
    /// </summary>
    public void CheckAndAdvance(Vector3 position)
    {
        Transform current = GetCurrentWaypoint();
        if (current == null) return;

        float dist = Vector3.Distance(position, current.position);
        if (dist < waypointRadius)
        {
            AdvanceToNextWaypoint();
        }
    }

    /// <summary>
    /// Moves the index forward by one, wrapping around at the end.
    /// </summary>
    public void AdvanceToNextWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        currentIndex = (currentIndex + 1) % waypoints.Length;
    }

    /// <summary>
    /// Returns how many waypoints are in the list.
    /// </summary>
    public int GetWaypointCount() => (waypoints == null) ? 0 : waypoints.Length;

    /// <summary>
    /// Returns the current waypoint index.
    /// </summary>
    public int GetCurrentIndex() => currentIndex;
}
