/*
 *  TorqueCurveMenu.cs
 *  ----------------------------------------------------
 *  One-click engine-torque presets for the CarControl script.
 *  Works with any project that exposes:
 *      public AnimationCurve torqueCurve;
 *      public float idleRPM;
 *      public float maxRPM;
 *
 *  Menu path:
 *      Tools → Vehicle Upgrades → Apply Torque Curve → <preset>
 */

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

//------------------------------------------------------------------
//  INTERNAL ENGINE-CURVE UTILITY
//------------------------------------------------------------------
static class TorqueCurveUtility
{
    /// Create a smooth AnimationCurve from keyframes expressed as
    ///   (rpmFraction 0‥1 , torqueFraction 0‥1)
    public static AnimationCurve Build(params (float x, float y)[] pts)
    {
        Keyframe[] k = new Keyframe[pts.Length];
        for (int i = 0; i < pts.Length; i++)
        {
            k[i] = new Keyframe(pts[i].x, pts[i].y)
            { weightedMode = WeightedMode.Both };
        }
        return new AnimationCurve(k);
    }
}

//------------------------------------------------------------------
//  PRESET DATABASE
//------------------------------------------------------------------
#if UNITY_EDITOR
public sealed class TorqueCurveMenu : Editor
{
    private struct Preset
    {
        public string  name;
        public float   idleRPM;
        public float   maxRPM;
        public AnimationCurve curve;
    }

    private static readonly Preset[] presets = new Preset[]
    {
        new Preset
        {
            name    = "I4 Economy",
            idleRPM =  900f,
            maxRPM  = 6500f,
            curve   = TorqueCurveUtility.Build(
                        (0.00f, 0.20f), (0.20f, 0.60f), (0.40f, 1.00f),
                        (0.80f, 0.80f), (1.00f, 0.20f))
        },
        new Preset
        {
            name    = "V6 Sport",
            idleRPM = 1000f,
            maxRPM  = 7500f,
            curve   = TorqueCurveUtility.Build(
                        (0.00f, 0.30f), (0.25f, 0.70f), (0.50f, 1.00f),
                        (0.85f, 0.95f), (1.00f, 0.40f))
        },
        new Preset
        {
            name    = "V8 Muscle",
            idleRPM =  800f,
            maxRPM  = 6200f,
            curve   = TorqueCurveUtility.Build(
                        (0.00f, 0.35f), (0.25f, 0.90f), (0.45f, 1.00f),
                        (0.70f, 1.00f), (0.95f, 0.45f), (1.00f, 0.30f))
        },
        new Preset
        {
            name    = "I4 Turbo",
            idleRPM =  900f,
            maxRPM  = 7000f,
            curve   = TorqueCurveUtility.Build(
                        (0.00f, 0.20f), (0.20f, 0.30f), (0.35f, 0.70f),
                        (0.50f, 1.00f), (0.80f, 1.00f), (0.92f, 0.50f),
                        (1.00f, 0.20f))
        },
        new Preset
        {
            name    = "Electric Motor",
            idleRPM =    0f,
            maxRPM  =16000f,
            curve   = TorqueCurveUtility.Build(
                        (0.00f, 1.00f), (0.50f, 1.00f),
                        (0.80f, 0.80f), (1.00f, 0.20f))
        },
        new Preset
        {
            name    = "Diesel Truck",
            idleRPM =  600f,
            maxRPM  = 4000f,
            curve   = TorqueCurveUtility.Build(
                        (0.00f, 0.30f), (0.20f, 0.90f), (0.45f, 1.00f),
                        (0.70f, 0.80f), (1.00f, 0.30f))
        },
    };

    //------------------------------------------------------------------
    //  DYNAMIC UNITY MENU ENTRIES
    //------------------------------------------------------------------
    [MenuItem("Tools/Vehicle Upgrades/Apply Torque Curve/I4 Economy")]
    private static void ApplyI4() => ApplyPreset(0);

    [MenuItem("Tools/Vehicle Upgrades/Apply Torque Curve/V6 Sport")]
    private static void ApplyV6() => ApplyPreset(1);

    [MenuItem("Tools/Vehicle Upgrades/Apply Torque Curve/V8 Muscle")]
    private static void ApplyV8() => ApplyPreset(2);

    [MenuItem("Tools/Vehicle Upgrades/Apply Torque Curve/I4 Turbo")]
    private static void ApplyTurbo() => ApplyPreset(3);

    [MenuItem("Tools/Vehicle Upgrades/Apply Torque Curve/Electric Motor")]
    private static void ApplyEV() => ApplyPreset(4);

    [MenuItem("Tools/Vehicle Upgrades/Apply Torque Curve/Diesel Truck")]
    private static void ApplyDiesel() => ApplyPreset(5);

    //------------------------------------------------------------------
    //  CORE ROUTINE
    //------------------------------------------------------------------
    private static void ApplyPreset(int index)
    {
        if (Selection.count == 0)
        {
            EditorUtility.DisplayDialog("Torque Curve",
                "Select one or more objects that have a CarControl component.", "OK");
            return;
        }

        var p = presets[index];
        int carsDone = 0;

        foreach (var car in Selection.GetFiltered<CarControl>(SelectionMode.Deep))
        {
            Undo.RecordObject(car, "Apply Torque Curve");
            car.torqueCurve = p.curve;
            car.idleRPM     = p.idleRPM;
            car.maxRPM      = p.maxRPM;
            EditorUtility.SetDirty(car);
            carsDone++;
        }

        EditorUtility.DisplayDialog("Torque Curve",
            carsDone == 0
              ? "No CarControl found in the current selection."
              : $"Applied '{p.name}' curve to {carsDone} vehicle(s).", "OK");
    }
}
#endif
