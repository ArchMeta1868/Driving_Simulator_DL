// UIIntegrityDebugger.cs
// Thorough sanity-check for common “button won’t click” / “No camera rendering” issues.
// ©2025 YourName – MIT licence.  Drop in Assets/Editor to enable the menu item.

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

[DefaultExecutionOrder(-9999)] // run as early as possible
public class UIIntegrityDebugger : MonoBehaviour
{
    // ----------  RUNTIME CHECKS (runs in Play mode) ----------
    void Awake()
    {
#if UNITY_EDITOR
        // never instantiate twice if you reload domain
        if (Application.isPlaying && FindObjectsOfType<UIIntegrityDebugger>().Length > 1)
        {
            Destroy(gameObject);
            return;
        }
#endif
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += (scene, mode) => ValidateScene(scene);
    }

    // Called automatically in Play mode when any scene loads
    private void ValidateScene(Scene scene)
    {
        if (!Application.isPlaying) return;
        Debug.Log($"<b><color=#00ffff>▶ UIIntegrityDebugger validating scene “{scene.name}” (runtime)</color></b>");
        RunValidations(scene, logPrefix: "[Runtime] ");
    }

    // ----------  EDITOR MENU COMMAND ----------
#if UNITY_EDITOR
    [MenuItem("Tools/UI/Validate Current Scene UI %#u")]   // Ctrl/Cmd+Shift+U
    private static void ValidateCurrentSceneFromMenu()
    {
        Scene active = SceneManager.GetActiveScene();
        Debug.Log($"<b><color=#00ffff>▶ UIIntegrityDebugger validating scene “{active.name}” (editor)</color></b>");
        RunValidations(active, logPrefix: "[Editor] ");
    }
#endif

    // ----------  CORE VALIDATION ----------
    private static void RunValidations(Scene scene, string logPrefix = "")
    {
        if (!scene.isLoaded)
        {
            Debug.LogWarning($"{logPrefix}Scene {scene.name} is not loaded.");
            return;
        }

        bool ok = true;

        // 1. Camera check -----------------------------------------------------
        Camera[] cams = GameObject.FindObjectsOfType<Camera>(true);
        bool hasActiveCam = false;
        foreach (Camera c in cams) if (c.enabled && c.gameObject.activeInHierarchy) { hasActiveCam = true; break; }
        if (!hasActiveCam)
        {
            Debug.LogError($"{logPrefix}<color=red>No active Camera found — scene will show “No camera rendering”.</color>");
            ok = false;
        }
        else
        {
            Debug.Log($"{logPrefix}<color=green>✓ At least one enabled Camera present.</color>");
        }

        // 2. EventSystem check -----------------------------------------------
        EventSystem es = GameObject.FindObjectOfType<EventSystem>();
        if (es == null)
        {
            Debug.LogError($"{logPrefix}<color=red>No EventSystem in scene ► UI will never receive clicks.</color>");
            ok = false;
        }
        else if (!es.enabled || !es.gameObject.activeInHierarchy)
        {
            Debug.LogError($"{logPrefix}<color=red>EventSystem exists but is disabled.</color>");
            ok = false;
        }
        else
        {
            Debug.Log($"{logPrefix}<color=green>✓ EventSystem found ({es.name}).</color>");
#if ENABLE_INPUT_SYSTEM
            if (es.GetComponent("InputSystemUIInputModule") == null)
                Debug.LogWarning($"{logPrefix}Project uses the new Input System yet EventSystem lacks <i>InputSystemUIInputModule</i>.");
#else
            if (es.GetComponent<StandaloneInputModule>() == null)
                Debug.LogWarning($"{logPrefix}Project uses the old Input Manager yet EventSystem lacks <i>StandaloneInputModule</i>.");
#endif
        }

        // 3. Canvas + GraphicRaycaster check ----------------------------------
        Canvas[] canvases = GameObject.FindObjectsOfType<Canvas>(true);
        foreach (Canvas cv in canvases)
        {
            if (cv.GetComponent<GraphicRaycaster>() == null)
            {
                Debug.LogError($"{logPrefix}<color=red>Canvas “{cv.name}” has no GraphicRaycaster ► buttons on it cannot be clicked.</color>");
                ok = false;
            }
        }
        if (canvases.Length > 0)
            Debug.Log($"{logPrefix}<color=green>✓ Found {canvases.Length} Canvas object(s).</color>");
        else
            Debug.LogWarning($"{logPrefix}No Canvas found in scene.");

        // 4. Per-button deep inspection ---------------------------------------
        Button[] buttons = GameObject.FindObjectsOfType<Button>(true);
        foreach (Button bt in buttons)
        {
            CheckButton(bt, logPrefix, ref ok);
        }
        if (buttons.Length == 0)
            Debug.LogWarning($"{logPrefix}No UnityEngine.UI.Button found in scene.");

        // 5. Summary ----------------------------------------------------------
        if (ok)
            Debug.Log($"{logPrefix}<b><color=lime>Scene “{scene.name}” passed all UI integrity checks.</color></b>");
        else
            Debug.Log($"{logPrefix}<b><color=yellow>Scene “{scene.name}” finished with warnings/errors (see above).</color></b>");
    }

    private static void CheckButton(Button bt, string logPrefix, ref bool okFlag)
    {
        string path = bt.transform.GetHierarchyPath();
        RectTransform rt = bt.GetComponent<RectTransform>();

        // 4.a size
        if (rt == null || rt.rect.width < 5f || rt.rect.height < 5f)
        {
            Debug.LogError($"{logPrefix}<color=red>Button “{path}” has a click area ≤ 5 px in at least one dimension.</color>");
            okFlag = false;
        }

        // 4.b interactable
        if (!bt.IsInteractable())
            Debug.LogWarning($"{logPrefix}Button “{path}” is not <b>Interactable</b>; clicks will be ignored.");

        // 4.c onClick listeners
        if (bt.onClick == null || bt.onClick.GetPersistentEventCount() == 0)
            Debug.LogWarning($"{logPrefix}Button “{path}” has no persistent OnClick listeners; nothing will happen when pressed.");
        else
        {
            for (int i = 0; i < bt.onClick.GetPersistentEventCount(); i++)
            {
                Object target = bt.onClick.GetPersistentTarget(i);
                if (target == null)
                {
                    Debug.LogWarning($"{logPrefix}Button “{path}” has a missing (null) OnClick target at index {i}.");
                    okFlag = false;
                }
            }
        }

        // 4.d raycast target
        bool hasRaycastGraphic = false;
        foreach (Graphic g in bt.GetComponentsInChildren<Graphic>())
        {
            if (g.raycastTarget) { hasRaycastGraphic = true; break; }
        }
        if (!hasRaycastGraphic)
        {
            Debug.LogError($"{logPrefix}<color=red>Button “{path}” has no Graphic component with Raycast Target=true ► invisible to pointer.</color>");
            okFlag = false;
        }
    }
}

// ----------  HELPER EXTENSION ----------
internal static class TransformPathExt
{
    // Returns "Parent/Child/SubChild"
    public static string GetHierarchyPath(this Transform t)
    {
        var stack = new Stack<string>();
        while (t != null)
        {
            stack.Push(t.name);
            t = t.parent;
        }
        return string.Join("/", stack.ToArray());
    }
}
