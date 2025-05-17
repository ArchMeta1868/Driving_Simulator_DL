using UnityEngine;
using UnityEngine.AI;
public class PedestrianAI : MonoBehaviour
{
    public Transform[] points;
    private NavMeshAgent agent;
    private int destIndex = 0;
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (points.Length > 0)
        {
            agent.SetDestination(points[0].position);
        }
    }
    void Update()
    {
        if (agent.pathPending) return;
        if (agent.remainingDistance < 0.5f)
        {
            // Reached destination, choose next
            destIndex = (destIndex + 1) % points.Length;
            agent.SetDestination(points[destIndex].position);
        }
    }
}