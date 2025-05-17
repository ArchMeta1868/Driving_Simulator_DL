// DebugMenuButtonProbe.cs   — v2  (compile-safe)
// MIT licence – remove when you’re done debugging.
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using System.Reflection;   // <- NEW

[DisallowMultipleComponent]
public class DebugMenuButtonProbe : MonoBehaviour,
                                     IPointerEnterHandler,
                                     IPointerExitHandler,
                                     IPointerClickHandler
{
    void Awake()
    {
        // Attach to one Button (self) or all descendant Buttons
        if (TryGetComponent<Button>(out var selfButton))
            InstrumentButton(selfButton);
        else
            foreach (var b in GetComponentsInChildren<Button>(true))
                InstrumentButton(b);
    }

    // ------------------------------------------------------------------ pointer echo
    public void OnPointerEnter(PointerEventData _) =>
        Debug.Log($"<color=#8888ff>[UIProbe]</color>  Pointer ENTER  → {name}", this);

    public void OnPointerExit(PointerEventData _) =>
        Debug.Log($"<color=#8888ff>[UIProbe]</color>  Pointer EXIT   ← {name}", this);

    public void OnPointerClick(PointerEventData e) =>
        Debug.Log($"<color=#00ffff>[UIProbe]</color>  *** PHYSICAL CLICK on {name} (button={e.button}) ***", this);

    // ------------------------------------------------------------------ instrumentation
    private void InstrumentButton(Button bt)
    {
        bt.onClick.AddListener(() =>
            Debug.Log($"<color=#00ff00>[UIProbe]</color>  → onClick BEGIN for {bt.name}", bt));

        int persistent = bt.onClick.GetPersistentEventCount();

        for (int i = 0; i < persistent; i++)
        {
            var target = bt.onClick.GetPersistentTarget(i);
            var method = bt.onClick.GetPersistentMethodName(i);
            int idx = i;  // local copy for closure

            if (target == null)
            {
                Debug.LogError($"[UIProbe]  🔴  Missing onClick target at index {idx} on {bt.name}", bt);
                continue;
            }

            // Disable Unity’s default call so we can wrap it ourselves
            bt.onClick.SetPersistentListenerState(idx,
                UnityEngine.Events.UnityEventCallState.Off);

            bt.onClick.AddListener(() =>
            {
                try
                {
                    var mi = target.GetType()
                                   .GetMethod(method,
                                              BindingFlags.Instance |
                                              BindingFlags.Public |
                                              BindingFlags.NonPublic);

                    if (mi == null)
                    {
                        Debug.LogError($"[UIProbe]  🔴  Method {method} not found on {target.name}", target);
                        return;
                    }

                    if (mi.GetParameters().Length > 0)
                    {
                        Debug.LogWarning($"[UIProbe]  ⚠  Method {method} requires parameters—"
                                       + "probe skips auto-invoke.");
                        return;
                    }

                    mi.Invoke(target, null);
                    Debug.Log($"<color=#00ff00>[UIProbe]</color>     ✔ Listener {method} on {target.name} completed OK");

                    // Extra scene-existence hint for common LoadX() pattern
                    if (method.StartsWith("Load"))
                    {
                        string scene = method.Replace("Load", string.Empty);
                        if (!Application.CanStreamedLevelBeLoaded(scene))
                            Debug.LogError($"[UIProbe]  🔶  Scene “{scene}” is NOT in Build Settings.");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            });
        }

        bt.onClick.AddListener(() =>
            Debug.Log($"<color=#00ff00>[UIProbe]</color>  ← onClick END   for {bt.name}", bt));
    }
}