using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

public static class SuperAssemblySelectionSceneSetup
{
    private const string MainScenePath = "Assets/Scene/Scene_Laboratory/MainScene_Labortory.unity";
    private const string ExploderObjectName = "Microscope Exploder";
    private const string SelectionUiObjectName = "SuperAssemblySelectionUI";

    [MenuItem("Tools/Super Assembly/Setup Selection UI In Main Scene")]
    public static void SetupMainSceneFromMenu()
    {
        SetupMainScene();
    }

    public static void SetupMainSceneBatch()
    {
        try
        {
            SetupMainScene();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
            return;
        }

        EditorApplication.Exit(0);
    }

    private static void SetupMainScene()
    {
        Scene scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        GameObject exploderRoot = FindSceneObject(scene, ExploderObjectName);
        if (exploderRoot == null)
        {
            throw new InvalidOperationException($"Cannot find '{ExploderObjectName}' in {MainScenePath}.");
        }

        BackupSceneFile();

        ModelExploder modelExploder = exploderRoot.GetComponent<ModelExploder>();
        SuperAssemblyPartSelectionController selectionController =
            GetOrAddComponent<SuperAssemblyPartSelectionController>(exploderRoot);
        SuperAssemblyPartHoverPulse hoverPulse = GetOrAddComponent<SuperAssemblyPartHoverPulse>(exploderRoot);
        MicroscopeExploderModeController modeController = FindSceneComponent<MicroscopeExploderModeController>(scene);
        XRRayInteractor rightRayInteractor = FindPreferredRightRayInteractor(scene);

        CanvasGroup selectionCanvasGroup = GetOrCreateSelectionUi(exploderRoot.transform, out TextMeshProUGUI nameText,
            out TextMeshProUGUI descriptionText, out Button experimentButton, out TextMeshProUGUI buttonText);

        SetObjectReference(selectionController, "modeController", modeController);
        SetObjectReference(selectionController, "rightRayInteractor", rightRayInteractor);
        SetObjectReference(selectionController, "modelExploder", modelExploder);
        SetObjectReference(selectionController, "partRoot", exploderRoot.transform);
        SetObjectReference(selectionController, "uiCanvasGroup", selectionCanvasGroup);
        SetObjectReference(selectionController, "partNameText", nameText);
        SetObjectReference(selectionController, "partDescriptionText", descriptionText);
        SetObjectReference(selectionController, "experimentButton", experimentButton);
        SetObjectReference(selectionController, "experimentButtonText", buttonText);

        SetObjectReference(hoverPulse, "modeController", modeController);
        SetObjectReference(hoverPulse, "rightRayInteractor", rightRayInteractor);
        SetObjectReference(hoverPulse, "modelExploder", modelExploder);
        SetObjectReference(hoverPulse, "partRoot", exploderRoot.transform);

        if (modeController != null)
        {
            SetObjectReference(modeController, "partSelectionController", selectionController);
        }

        EditorUtility.SetDirty(exploderRoot);
        EditorUtility.SetDirty(selectionController);
        EditorUtility.SetDirty(hoverPulse);
        if (modeController != null)
        {
            EditorUtility.SetDirty(modeController);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log("[SuperAssemblySelectionSceneSetup] Selection UI and references were set up in MainScene_Labortory.");
    }

    private static void BackupSceneFile()
    {
        string fullScenePath = Path.GetFullPath(MainScenePath);
        string directory = Path.GetDirectoryName(fullScenePath);
        string fileName = Path.GetFileNameWithoutExtension(fullScenePath);
        string extension = Path.GetExtension(fullScenePath);
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string backupPath = Path.Combine(directory ?? string.Empty, $"{fileName}_before_super_assembly_selection_{timestamp}{extension}");
        File.Copy(fullScenePath, backupPath, false);
        Debug.Log($"[SuperAssemblySelectionSceneSetup] Scene backup created: {backupPath}");
    }

    private static CanvasGroup GetOrCreateSelectionUi(
        Transform parent,
        out TextMeshProUGUI nameText,
        out TextMeshProUGUI descriptionText,
        out Button experimentButton,
        out TextMeshProUGUI buttonText)
    {
        Transform existingRoot = parent.Find(SelectionUiObjectName);
        GameObject rootObject = existingRoot != null ? existingRoot.gameObject : CreateWorldSpaceCanvas(parent);
        rootObject.name = SelectionUiObjectName;

        CanvasGroup canvasGroup = GetOrAddComponent<CanvasGroup>(rootObject);
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        RectTransform rootRect = rootObject.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(520f, 680f);
        rootRect.localScale = Vector3.one * 0.00135f;

        nameText = GetOrCreatePanelText(rootRect, "NamePanel", new Vector2(0f, 0.72f), new Vector2(1f, 1f), 34f,
            FontStyles.Bold);
        descriptionText = GetOrCreatePanelText(rootRect, "DescriptionPanel", new Vector2(0f, 0.24f),
            new Vector2(1f, 0.69f), 21f, FontStyles.Normal);
        experimentButton = GetOrCreateExperimentButton(rootRect, out buttonText);

        rootObject.SetActive(false);
        return canvasGroup;
    }

    private static GameObject CreateWorldSpaceCanvas(Transform parent)
    {
        GameObject canvasObject = new GameObject(SelectionUiObjectName, typeof(RectTransform), typeof(Canvas),
            typeof(GraphicRaycaster), typeof(CanvasGroup));
        canvasObject.transform.SetParent(parent, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 60;

        return canvasObject;
    }

    private static TextMeshProUGUI GetOrCreatePanelText(
        RectTransform parent,
        string panelName,
        Vector2 anchorMin,
        Vector2 anchorMax,
        float fontSize,
        FontStyles fontStyle)
    {
        Transform existingPanel = parent.Find(panelName);
        GameObject panelObject = existingPanel != null
            ? existingPanel.gameObject
            : new GameObject(panelName, typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(parent, false);

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = anchorMin;
        panelRect.anchorMax = anchorMax;
        panelRect.offsetMin = new Vector2(12f, 10f);
        panelRect.offsetMax = new Vector2(-12f, -10f);

        Image image = GetOrAddComponent<Image>(panelObject);
        image.color = new Color(0.04f, 0.07f, 0.08f, 0.78f);

        Transform existingText = panelRect.Find("Text");
        GameObject textObject = existingText != null
            ? existingText.gameObject
            : new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(panelRect, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(24f, 18f);
        textRect.offsetMax = new Vector2(-24f, -18f);

        TextMeshProUGUI text = GetOrAddComponent<TextMeshProUGUI>(textObject);
        text.text = string.Empty;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = Color.white;
        text.enableWordWrapping = true;
        text.alignment = TextAlignmentOptions.Left;

        return text;
    }

    private static Button GetOrCreateExperimentButton(RectTransform parent, out TextMeshProUGUI buttonText)
    {
        Transform existingPanel = parent.Find("ExperimentButtonPanel");
        GameObject buttonObject = existingPanel != null
            ? existingPanel.gameObject
            : new GameObject("ExperimentButtonPanel", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0f, 0f);
        buttonRect.anchorMax = new Vector2(1f, 0.21f);
        buttonRect.offsetMin = new Vector2(12f, 10f);
        buttonRect.offsetMax = new Vector2(-12f, -10f);

        Image image = GetOrAddComponent<Image>(buttonObject);
        image.color = new Color(0.12f, 0.27f, 0.24f, 0.9f);

        Button button = GetOrAddComponent<Button>(buttonObject);
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.12f, 0.27f, 0.24f, 0.9f);
        colors.highlightedColor = new Color(0.18f, 0.42f, 0.37f, 1f);
        colors.pressedColor = new Color(0.08f, 0.2f, 0.18f, 1f);
        button.colors = colors;

        Transform existingText = buttonRect.Find("Text");
        GameObject textObject = existingText != null
            ? existingText.gameObject
            : new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(buttonRect, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(18f, 10f);
        textRect.offsetMax = new Vector2(-18f, -10f);

        buttonText = GetOrAddComponent<TextMeshProUGUI>(textObject);
        buttonText.text = "Related Experiment";
        buttonText.fontSize = 24f;
        buttonText.fontStyle = FontStyles.Bold;
        buttonText.color = Color.white;
        buttonText.alignment = TextAlignmentOptions.Center;

        return button;
    }

    private static T GetOrAddComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    private static void SetObjectReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
    {
        if (target == null)
        {
            return;
        }

        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogWarning($"[SuperAssemblySelectionSceneSetup] Missing serialized property '{propertyName}' on {target.name}.");
            return;
        }

        property.objectReferenceValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static XRRayInteractor FindPreferredRightRayInteractor(Scene scene)
    {
        XRRayInteractor[] rayInteractors = FindSceneComponents<XRRayInteractor>(scene);
        XRRayInteractor fallback = null;

        for (int i = 0; i < rayInteractors.Length; i++)
        {
            XRRayInteractor candidate = rayInteractors[i];
            if (candidate == null)
            {
                continue;
            }

            if (fallback == null)
            {
                fallback = candidate;
            }

            string path = GetHierarchyPath(candidate.transform).ToLowerInvariant();
            if (path.Contains("right controller") || path.Contains("right hand") || path.Contains("righthand"))
            {
                return candidate;
            }
        }

        return fallback;
    }

    private static GameObject FindSceneObject(Scene scene, string objectName)
    {
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform target = transforms[i];
            if (target == null || target.gameObject.scene != scene || target.name != objectName)
            {
                continue;
            }

            return target.gameObject;
        }

        return null;
    }

    private static T FindSceneComponent<T>(Scene scene) where T : Component
    {
        T[] components = FindSceneComponents<T>(scene);
        return components.Length > 0 ? components[0] : null;
    }

    private static T[] FindSceneComponents<T>(Scene scene) where T : Component
    {
        T[] allComponents = Resources.FindObjectsOfTypeAll<T>();
        var results = new System.Collections.Generic.List<T>();
        for (int i = 0; i < allComponents.Length; i++)
        {
            T component = allComponents[i];
            if (component != null && component.gameObject.scene == scene)
            {
                results.Add(component);
            }
        }

        return results.ToArray();
    }

    private static string GetHierarchyPath(Transform target)
    {
        string path = target.name;
        Transform current = target.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }
}
