using UnityEngine;

[AddComponentMenu("Vehicle/Limited-Slip Differential")]
public class LimitedSlipDifferential : MonoBehaviour
{
    public WheelCollider left;
    public WheelCollider right;
    [Tooltip("Torque bias ratio  (1 = open diff, 3 = sports LSD)")]
    public float torqueBias = 2.5f;

    void FixedUpdate()
    {
        if (!left || !right) return;

        float wL = Mathf.Abs(left.rpm);
        float wR = Mathf.Abs(right.rpm);
        if (wL < 1f && wR < 1f) return;   // near stand-still

        float faster = Mathf.Max(wL, wR);
        float slower = Mathf.Min(wL, wR);
        float ratio  = faster / Mathf.Max(slower, 1f);

        if (ratio > torqueBias)
        {
            WheelCollider give = wL > wR ? left : right;
            WheelCollider take = wL > wR ? right : left;

            // shift 10 % of current motorTorque from fast wheel to slow wheel
            float shift = give.motorTorque * 0.10f;
            give.motorTorque -= shift;
            take.motorTorque += shift;
        }
    }
}
