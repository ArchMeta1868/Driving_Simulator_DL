using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class CarControl : MonoBehaviour
{
    [Header("Car Settings")]
    public float motorTorque = 3500f;
    public float brakeTorque = 10000f;
    public float maxSpeed = 80f; // m/s
    public float steeringAngle = 30f;
    public float steeringAngleAtMax = 10f;

    [Header("Wheels")]
    public List<WheelCollider> driveWheels = new List<WheelCollider>();
    public List<WheelCollider> steerWheels = new List<WheelCollider>();

    [Header("Aerodynamics")]
    public float dragCoefficient = 0f;
    public float downForceCoefficient = 0f;

    [Header("Engine & Gearbox")]
    public AnimationCurve torqueCurve = AnimationCurve.Linear(0, 1, 1, 1);
    public float idleRPM = 900f;
    public float maxRPM = 6500f;
    public float shiftUpRPM = 6000f;
    public float shiftDownRPM = 2500f;
    [Tooltip("Gear ratios including reverse as index 0.")]
    public float[] gearRatios = { -3.0f, 3.2f, 2.1f, 1.5f, 1.2f, 1.0f };

    [Header("Traction & Brakes")]
    [Range(0, 1)] public float slipLimit = 0.4f;
    public float tractionControlGain = 50f;
    public float absGain = 1000f;

    [Header("Steering Assist")]
    [Range(0, 1)] public float steerAssist = 0f;

    // ------------------- Internal State -------------------
    private Rigidbody rb;
    private float currentSpeed;       // magnitude in m/s
    private int currentGear = 1;      // 0 = reverse gear, 1+ = forward
    private float engineRPM;

    private float throttleFiltered;
    private float throttleVel;
    private const float smoothTime = 0.1f;

    // -1..+1 from outside (Player or AI)
    private float desiredAccelInput;
    private float desiredSteerInput;

    // Tweak these to taste
    private const float STATIONARY_THRESHOLD = 0.1f;  // when speed < 0.1 m/s => "at rest"
    private const float EPS = 0.01f;                  // crossing zero velocity

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass += new Vector3(0, -0.5f, 0);
    }

    private void FixedUpdate()
    {
        // 1) Basic speed & direction
        Vector3 velocity = rb.linearVelocity;
        currentSpeed = velocity.magnitude;
        float localZVel = Vector3.Dot(transform.forward, velocity); // + => forward, - => backward

        // 2) Steering
        float speedFactor = Mathf.InverseLerp(0f, maxSpeed, currentSpeed);
        float steerNow = Mathf.Lerp(steeringAngle, steeringAngleAtMax, speedFactor);
        foreach (var w in steerWheels)
        {
            w.steerAngle = desiredSteerInput * steerNow;
        }

        // 3) Aerodynamics
        if (dragCoefficient > 0f)
        {
            rb.AddForce(-velocity * currentSpeed * dragCoefficient);
        }
        if (downForceCoefficient > 0f)
        {
            rb.AddForce(Vector3.down * currentSpeed * currentSpeed * downForceCoefficient);
        }

        // 4) Handle "zero-speed" condition first.
        //    If near standstill (< STATIONARY_THRESHOLD), we let user pick gear:
        //    W => forward gear, S => reverse gear. 
        //    This ensures that at rest, pressing S always triggers reverse, pressing W always triggers forward.
        if (currentSpeed < STATIONARY_THRESHOLD)
        {
            if (desiredAccelInput > 0.1f)
            {
                // W => go forward
                currentGear = 1;
            }
            else if (desiredAccelInput < -0.1f)
            {
                // S => go reverse
                currentGear = 0;
            }
        }
        else
        {
            // 5) If not stationary, do symmetrical flip logic once crossing zero velocity
            if (currentGear == 0)
            {
                // In reverse gear, if velocity is actually positive => flip to forward
                if (localZVel > EPS)
                {
                    currentGear = 1;
                }
            }
            else
            {
                // In forward gear, if velocity is actually negative => flip to reverse
                if (localZVel < -EPS)
                {
                    currentGear = 0;
                }
            }
        }

        // 6) Forward gear auto up/down shifting (skip if gear=0)
        if (currentGear >= 1)
        {
            if (engineRPM > shiftUpRPM && currentGear < gearRatios.Length - 1 && desiredAccelInput > 0f)
            {
                currentGear++;
            }
            else if (engineRPM < shiftDownRPM && currentGear > 1)
            {
                currentGear--;
            }
        }

        // 7) Compute engine RPM from wheel RPM
        float wheelRPM = 0f;
        foreach (var w in driveWheels)
        {
            wheelRPM += w.rpm;
        }
        wheelRPM /= Mathf.Max(1, driveWheels.Count);

        engineRPM = Mathf.Abs(wheelRPM) * Mathf.Abs(gearRatios[currentGear]) + idleRPM;
        engineRPM = Mathf.Clamp(engineRPM, idleRPM, maxRPM);

        // 8) Determine throttle vs brake
        bool isBraking = false;
        float rawThrottle = 0f;

        if (currentGear == 0)
        {
            // Reverse gear => pressing S => reverse torque
            float reversedInput = -desiredAccelInput; // if S=-1 => reversedInput=+1
            if (reversedInput > 0.1f)
            {
                rawThrottle = Mathf.Clamp01(reversedInput);
            }
            // If pressing W in reverse gear => brake
            if (desiredAccelInput > 0.1f)
            {
                isBraking = true;
            }
        }
        else
        {
            // Forward gear => pressing W => forward torque
            float forwardInput = Mathf.Max(0f, desiredAccelInput);
            rawThrottle = Mathf.Clamp01(forwardInput);

            // If pressing S in forward gear => brake
            if (desiredAccelInput < -0.1f)
            {
                isBraking = true;
            }
        }

        // Smooth final throttle
        throttleFiltered = Mathf.SmoothDamp(throttleFiltered, rawThrottle, ref throttleVel, smoothTime);

        // 9) Apply torque or brake
        float gearRatio = gearRatios[currentGear];
        float availableTorque = motorTorque * gearRatio * torqueCurve.Evaluate(engineRPM / maxRPM);

        foreach (WheelCollider wheel in driveWheels)
        {
            wheel.GetGroundHit(out WheelHit hit);
            float slip = Mathf.Abs(hit.forwardSlip);

            // Traction Control if accelerating forward
            float tcFactor = 1f;
            if (!isBraking && currentGear > 0 && slip > slipLimit)
            {
                tcFactor = Mathf.Lerp(1f, 0f, (slip - slipLimit) * tractionControlGain);
            }

            // ABS if braking
            float absFactor = 1f;
            if (isBraking && slip > slipLimit)
            {
                absFactor = Mathf.Lerp(1f, 0f, (slip - slipLimit) * absGain);
            }

            if (isBraking)
            {
                wheel.motorTorque = 0f;
                wheel.brakeTorque = brakeTorque * absFactor;
            }
            else
            {
                wheel.brakeTorque = 0f;
                wheel.motorTorque = throttleFiltered * availableTorque * tcFactor;
            }
        }

        // 10) Steering assist
        if (steerAssist > 0f && rb.linearVelocity.sqrMagnitude > 1f)
        {
            Vector3 velDir = velocity.normalized;
            float angleDiff = Vector3.SignedAngle(transform.forward, velDir, Vector3.up);
            rb.AddTorque(Vector3.up * -angleDiff * steerAssist * 0.1f, ForceMode.VelocityChange);
        }

        // 11) Speed cap – 'maxSpeed' is stored in m/s so compare without
        // converting the current speed to km/h
        if (currentSpeed > maxSpeed)
        {
            foreach (var w in driveWheels)
            {
                w.motorTorque = 0f;
            }
        }
    }

    // ---------------- Public API ----------------
    public void SetInputs(float accelInput, float steerInput)
    {
        desiredAccelInput = Mathf.Clamp(accelInput, -1f, 1f);
        desiredSteerInput = Mathf.Clamp(steerInput, -1f, 1f);
    }

    public float GetSpeedKMH() => rb.linearVelocity.magnitude * 3.6f;
    public int GetCurrentGear() => currentGear;
    public float GetEngineRPM() => engineRPM;
}
