//======================================================================
//  CarControlRL.cs  – ML-Agents bridge: converts (gas, brake, steer)
//  to CarControl.SetInputs(accel, steer).    *No* physics duplicated.
//======================================================================
using UnityEngine;

[RequireComponent(typeof(CarControl))]
[RequireComponent(typeof(Rigidbody))]
public class CarControlRL : MonoBehaviour
{
    /* ---------- inputs written by DriveAgent ---------- */
    [HideInInspector] public float gasInput;    // 0‥1
    [HideInInspector] public float brakeInput;  // 0‥1
    [HideInInspector] public float steerInput;  // –1‥+1

    /* ---------- cached refs ---------- */
    CarControl core;      // shared physics script
    Rigidbody rb;

    void Awake()
    {
        core = GetComponent<CarControl>();   // has all wheel & tuning data
        rb = GetComponent<Rigidbody>();    // used only for SpeedKMH
    }

    /* ---------- forward inputs every physics step ---------- */
    void FixedUpdate()
    {
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
