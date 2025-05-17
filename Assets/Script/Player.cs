using UnityEngine;

// Attach this to the same GameObject that has CarControl
[RequireComponent(typeof(CarControl))]
public class Player : MonoBehaviour
{
    private CarControl carControl;

    private void Awake()
    {
        carControl = GetComponent<CarControl>();
    }

    private void FixedUpdate()
    {
        // Gather input from Unity's Input Manager (keyboard, gamepad, etc.)
        float accelInput = Input.GetAxis("Vertical");   // -1..1
        float steerInput = Input.GetAxis("Horizontal"); // -1..1

        // Pass them to the shared CarControl
        carControl.SetInputs(accelInput, steerInput);
    }
}
