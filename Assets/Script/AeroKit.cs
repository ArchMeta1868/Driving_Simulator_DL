using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class AeroKit : MonoBehaviour
{
    [Header("Coefficients")]
    public float Cd    = 0.32f;   // drag coefficient
    public float Cl    = 0.80f;   // negative lift coeff (down-force)
    public float area  = 2.2f;    // m², auto-filled by menu
    public bool  showDebug;

    Rigidbody rb;
    void Awake() => rb = GetComponent<Rigidbody>();

    void FixedUpdate()
    {
        Vector3 v = rb.linearVelocity;
        float  v2 = v.sqrMagnitude;
        if (v2 < 1f) return;

        const float airDensity = 1.225f;  // kg/m³ at sea level
        float q = 0.5f * airDensity * v2; // dynamic pressure

        Vector3 drag  = -v.normalized * (q * Cd * area);
        Vector3 lift  =  transform.up * -(q * Cl * area); // down-force

        rb.AddForce(drag);
        rb.AddForce(lift);

        if (showDebug)
            Debug.DrawRay(rb.worldCenterOfMass, drag + lift, Color.cyan);
    }
}

#if UNITY_EDITOR
static class AeroKitMenu
{
    [MenuItem("Tools/Vehicle Upgrades/Add Aero Kit")]
    private static void AddAero()
    {
        int done = 0;
        foreach (var rb in Selection.GetFiltered<Rigidbody>(SelectionMode.Editable))
        {
            var kit = rb.gameObject.AddComponent<AeroKit>();
            kit.area = EstimateFrontalArea(rb);
            done++;
        }

        EditorUtility.DisplayDialog("Aero Kit",
            done == 0
              ? "Select a car’s Rigidbody first."
              : $"Aero Kit added to {done} car(s) (area auto-estimated).", "OK");
    }

    static float EstimateFrontalArea(Rigidbody rb)
    {
        Renderer[] rends = rb.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return 2.2f;

        Bounds b = rends[0].bounds;
        foreach (var r in rends) b.Encapsulate(r.bounds);

        // Frontal projection ≈ width × height
        return b.size.x * b.size.y;
    }
}
#endif
