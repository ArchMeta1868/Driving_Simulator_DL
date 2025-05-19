using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class AntiRollBar : MonoBehaviour               //  ← attribute removed
{
    public WheelCollider left, right;
    public float stiffness = 20000f;

    Rigidbody rb;
    void Awake() => rb = GetComponentInParent<Rigidbody>();

    void FixedUpdate()
    {
        if (!left || !right) return;

        float compL = Compression(left);
        float compR = Compression(right);
        float force = (compL - compR) * stiffness;

        if (left.isGrounded)
            rb.AddForceAtPosition(left.transform.up * -force, left.transform.position);
        if (right.isGrounded)
            rb.AddForceAtPosition(right.transform.up *  force, right.transform.position);
    }

    static float Compression(WheelCollider w)
    {
        WheelHit hit;
        return w.GetGroundHit(out hit)
            ? (-w.transform.InverseTransformPoint(hit.point).y - w.radius) / w.suspensionDistance
            : 1f;
    }
}

#if UNITY_EDITOR
public static class AntiRollBarMenu
{
    [MenuItem("Tools/Vehicle Upgrades/Add Anti-Roll Bars %#r")]   // Ctrl+Shift+R
    public static void AddBars()
    {
        int ok = 0, skip = 0;

        foreach (var rb in Selection.GetFiltered<Rigidbody>(SelectionMode.Editable))
        {
            var wheels = rb.GetComponentsInChildren<WheelCollider>();
            if (wheels.Length < 4) { skip++; continue; }

            System.Array.Sort(wheels, (a,b)=>
            {
                int z = -a.transform.position.z.CompareTo(b.transform.position.z);
                return z!=0 ? z : a.transform.position.x.CompareTo(b.transform.position.x);
            });

            if (TryCreate(rb.gameObject, wheels[0], wheels[1])) ok++;  // front
            if (TryCreate(rb.gameObject, wheels[^2], wheels[^1])) ok++; // rear
        }

        EditorUtility.DisplayDialog("Anti-Roll Bar",
            $"Anti-roll bars added: {ok}\nAxles skipped: {skip}", "OK");
    }

    static bool TryCreate(GameObject car, WheelCollider L, WheelCollider R)
    {
        if (!L || !R) return false;

        // Skip if a bar for this pair already exists
        foreach (var bar in car.GetComponents<AntiRollBar>())
            if ((bar.left == L && bar.right == R) || (bar.left == R && bar.right == L))
                return false;

        var newBar = car.AddComponent<AntiRollBar>();
        if (newBar == null) return false;                     // safety: AddComponent failed

        newBar.left = L;  newBar.right = R;
        return true;
    }
}
#endif