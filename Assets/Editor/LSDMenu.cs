#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

static class LSDMenu
{
    [MenuItem("Tools/Vehicle Upgrades/Add Limited-Slip Differential")]
    private static void AddLSD()
    {
        int carsDone = 0;

        foreach (Rigidbody rb in Selection.GetFiltered<Rigidbody>(SelectionMode.Editable))
        {
            WheelCollider[] wheels = rb.GetComponentsInChildren<WheelCollider>();
            if (wheels.Length < 2) continue;

            // ------- pick the axle with the smallest Z (rear-most) -------
            float rearZ = float.PositiveInfinity;
            foreach (var w in wheels) rearZ = Mathf.Min(rearZ, w.transform.position.z);

            WheelCollider left = null, right = null;
            foreach (var w in wheels)
                if (Mathf.Abs(w.transform.position.z - rearZ) < 0.05f)   // same axle
                    if (w.transform.position.x < 0) left  = w; else right = w;

            if (left && right && !rb.GetComponent<LimitedSlipDifferential>())
            {
                var lsd = rb.gameObject.AddComponent<LimitedSlipDifferential>();
                lsd.left  = left;
                lsd.right = right;
                carsDone++;
            }
        }

        EditorUtility.DisplayDialog("Limited-Slip Differential",
            carsDone == 0
              ? "Select a car’s root Rigidbody that has at least one rear axle."
              : $"Added LSD to {carsDone} car(s).", "OK");
    }
}
#endif