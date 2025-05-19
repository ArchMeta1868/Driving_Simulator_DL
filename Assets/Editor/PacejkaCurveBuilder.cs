/*
 *  PacejkaTyreMenu.cs
 *  -------------------------------------
 *  Quick tyre-friction presets for WheelCollider.
 *  Place in Assets/Editor/.  Works on Unity 2020-2024.
 */
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

// ---------- INTERNAL MAGIC-FORMULA HELPER ----------
static class PacejkaUtility
{
    // Evaluate the Magic-Formula curve (pure longitudinal or lateral)
    public static float MagicFormula(float B, float C, float D, float E, float slip)
    {
        return D * Mathf.Sin(C * Mathf.Atan(B * slip - E * (B * slip - Mathf.Atan(B * slip))));
    }

    // Build either a legacy WheelFrictionCurve or (when available) one with forceCurve.
    public static WheelFrictionCurve BuildCurve(
        float B, float C, float D, float E,
        float peakSlip    = 0.11f,
        float asympSlip   = 0.90f,
        float asympFactor = 0.5f)
    {
        float muPeak = Mathf.Abs(MagicFormula(B, C, D, E, peakSlip));

        WheelFrictionCurve f = new WheelFrictionCurve
        {
            extremumSlip   = peakSlip,
            extremumValue  = muPeak,
            asymptoteSlip  = asympSlip,
            asymptoteValue = muPeak * asympFactor,
            stiffness      = 1f
        };

        // On Unity 2022.2+ add the full analytical curve for higher fidelity
#if UNITY_2022_2_OR_NEWER
        var pi = typeof(WheelFrictionCurve).GetProperty("forceCurve");
        if (pi != null)                                // still guard against odd betas
        {
            const int SAMPLES = 17;
            Keyframe[] k = new Keyframe[SAMPLES];
            for (int i = 0; i < SAMPLES; i++)
            {
                float slip = i / (SAMPLES - 1f);       // 0 … 1
                float mu   = Mathf.Abs(MagicFormula(B, C, D, E, slip));
                k[i] = new Keyframe(slip, mu);
            }
            pi.SetValue(f, new AnimationCurve(k));
        }
#endif
        return f;
    }
}

// ---------- EDITOR MENU ----------
#if UNITY_EDITOR
public sealed class PacejkaTyreMenu : Editor
{
    //--------   PRESET DATABASE   --------
    struct Preset
    {
        public string name;           // menu label
        public float B, C, D, E;      // Magic-Formula coefficients
    }
    static readonly Preset[] presets = new Preset[]
    {
        new Preset{ name = "Dry Asphalt", B = 10f, C = 1.9f, D = 1.00f, E = 0.97f},
        new Preset{ name = "Wet Asphalt", B =  8f, C = 1.7f, D = 0.70f, E = 1.00f},
        new Preset{ name = "Gravel",      B =  6f, C = 1.3f, D = 0.60f, E = 1.10f},
        new Preset{ name = "Ice / Snow",  B =  4f, C = 1.2f, D = 0.20f, E = 1.60f},
    };

    // Dynamically add one menu item per preset
    [MenuItem("Tools/Pacejka/Apply/ Dry Asphalt")]
    private static void ApplyDry()  => ApplyPreset(0);

    [MenuItem("Tools/Pacejka/Apply/ Wet Asphalt")]
    private static void ApplyWet()  => ApplyPreset(1);

    [MenuItem("Tools/Pacejka/Apply/ Gravel")]
    private static void ApplyGravel() => ApplyPreset(2);

    [MenuItem("Tools/Pacejka/Apply/ Ice - Snow")]
    private static void ApplyIce()  => ApplyPreset(3);

    //--------   CORE ROUTINE   --------
    private static void ApplyPreset(int idx)
    {
        if (Selection.count == 0)
        {
            EditorUtility.DisplayDialog("Pacejka Tyre Menu",
                "Select one or more WheelCollider objects in the Hierarchy first.", "OK");
            return;
        }

        Preset p = presets[idx];
        int wheelCount = 0;

        foreach (WheelCollider wc in Selection.GetFiltered<WheelCollider>(SelectionMode.Deep))
        {
            wheelCount++;
            // Forward = longitudinal, Sideways = lateral (slightly less peak μ)
            wc.forwardFriction  = PacejkaUtility.BuildCurve(p.B, p.C, p.D, p.E);
            wc.sidewaysFriction = PacejkaUtility.BuildCurve(
                                     p.B * 0.8f, p.C, p.D * 0.9f, p.E,    // little less grip sideways
                                     peakSlip: 0.08f);                     // steeper initial build-up
            EditorUtility.SetDirty(wc);
        }

        if (wheelCount == 0)
        {
            EditorUtility.DisplayDialog("Pacejka Tyre Menu",
                "No WheelCollider found in the current selection.", "OK");
        }
        else
        {
            Debug.Log($"[Pacejka]  Applied '{p.name}' preset to {wheelCount} WheelCollider(s).");
        }
    }
}
#endif
