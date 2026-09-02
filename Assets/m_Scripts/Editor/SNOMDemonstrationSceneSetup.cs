#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SNOMDemonstrationSceneSetup
{
    private const string SystemName = "SNOM_DemonstrationSystem";

    [MenuItem("Tools/Experiments/Setup SNOM Demonstration")]
    public static void SetupActiveScene()
    {
        Transform snomRoot = SNOMDemonstrationController.FindSnomRoot();
        if (snomRoot == null)
        {
            Debug.LogError("[SNOMDemonstrationSceneSetup] Cannot find TDs_edited_UnityVeryLowPoly in the active scene.");
            return;
        }

        Transform originalParent = snomRoot.parent;
        Vector3 originalPosition = snomRoot.localPosition;
        Quaternion originalRotation = snomRoot.localRotation;
        Vector3 originalScale = snomRoot.localScale;

        SNOMDemonstrationController controller = Object.FindObjectOfType<SNOMDemonstrationController>(true);
        if (controller == null)
        {
            GameObject system = new GameObject(SystemName);
            Undo.RegisterCreatedObjectUndo(system, "Create SNOM demonstration system");
            system.transform.SetParent(snomRoot.parent, false);
            controller = Undo.AddComponent<SNOMDemonstrationController>(system);
        }

        Undo.RecordObject(controller, "Bind fixed SNOM root");
        controller.Configure(snomRoot);
        EditorUtility.SetDirty(controller);

        bool rootUnchanged = snomRoot.parent == originalParent &&
                             snomRoot.localPosition == originalPosition &&
                             snomRoot.localRotation == originalRotation &&
                             snomRoot.localScale == originalScale;
        if (!rootUnchanged)
        {
            Debug.LogError("[SNOMDemonstrationSceneSetup] Setup was cancelled because the fixed SNOM root pose changed.");
            return;
        }

        Scene scene = snomRoot.gameObject.scene;
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = controller.gameObject;
        Debug.Log("[SNOMDemonstrationSceneSetup] Demonstration installed. The SNOM root transform was not changed.", controller);
    }

    [MenuItem("Tools/Experiments/Setup SNOM Demonstration", true)]
    private static bool ValidateSetupActiveScene()
    {
        return !Application.isPlaying;
    }
}
#endif
