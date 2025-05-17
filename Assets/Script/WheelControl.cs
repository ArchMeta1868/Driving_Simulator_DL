using UnityEngine;
[RequireComponent(typeof(WheelCollider))]
public class WheelControl : MonoBehaviour
{
    public Transform wheelModel;  // Assign the visible wheel model (mesh)
    private WheelCollider wheelCol;
    private Vector3 wheelPos;
    private Quaternion wheelRot;

    void Start()
    {
        wheelCol = GetComponent<WheelCollider>();
    }

    void Update()
    {
        // Update wheel model position/rotation to match WheelCollider
        wheelCol.GetWorldPose(out wheelPos, out wheelRot);
        if (wheelModel != null)
        {
            wheelModel.position = wheelPos;
            wheelModel.rotation = wheelRot;
        }
    }
}