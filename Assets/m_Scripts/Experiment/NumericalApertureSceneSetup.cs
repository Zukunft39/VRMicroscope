using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public sealed class NumericalApertureSceneSetup : MonoBehaviour
{
#if UNITY_EDITOR
    private const string DiagramAssetRoot = "Assets/m_Images/Experiment/NumericalAperture/";
    private static readonly Color PageColor = new Color(0.975f, 0.972f, 0.955f, 1f);
    private static readonly Color TextColor = new Color(0.075f, 0.12f, 0.18f, 1f);
    private static readonly Color SecondaryTextColor = new Color(0.25f, 0.31f, 0.38f, 1f);
    private static readonly Color AccentColor = new Color(0.035f, 0.43f, 0.92f, 1f);

    [ContextMenu("Setup Numerical Aperture Experiment")]
    public void SetupSceneObjects()
    {
        Transform root = FindChild("ExperimentRoot");
        Transform experimentCanvasAnchor = FindChild("ExperimentRoot/Experiment Canvas");
        Transform fade = FindChild("Fade Canvas");
        Transform slider = FindChild("ExperimentRoot/NA Slider");
        Transform exit = FindChild("ExperimentRoot/Exit Button");
        Transform naText = FindChild("ExperimentRoot/NA Text");
        Transform thetaText = FindChild("ExperimentRoot/Theta Text");
        Transform fullApertureText = FindChild("ExperimentRoot/Full Aperture Text");
        Transform magnificationText = FindChild("ExperimentRoot/Magnification Text");
        Transform lightCone = FindChild("LightCone");

        if (root == null)
        {
            return;
        }

        Transform normalizedNaText = EnsureTextObject(root, "Normalized NA Text");
        Transform formulaText = EnsureTextObject(root, "Formula Text");
        Transform depthToleranceText = EnsureTextObject(root, "Depth Tolerance Text");

        Canvas experimentCanvas = EnsureComponent<Canvas>(root.gameObject);
        root.localScale = Vector3.one;
        experimentCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        experimentCanvas.sortingOrder = 900;
        CanvasScaler rootScaler = EnsureComponent<CanvasScaler>(root.gameObject);
        rootScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        rootScaler.referenceResolution = new Vector2(1920f, 1080f);
        rootScaler.matchWidthOrHeight = 0.5f;
        EnsureComponent<GraphicRaycaster>(root.gameObject);
        EnsureComponent<TrackedDeviceGraphicRaycaster>(root.gameObject);
        CanvasGroup experimentGroup = EnsureComponent<CanvasGroup>(root.gameObject);

        XRUIInputModule uiInputModule = FindObjectOfType<XRUIInputModule>();
        if (uiInputModule != null)
        {
            // Keep mouse dragging available for desktop testing while retaining XR ray input.
            uiInputModule.enableMouseInput = true;
        }

        ConfigureRect(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(1920f, 1080f));
        EnsureBackground(root, "Experiment Background", PageColor);
        EnsureLabel(root, "Experiment Title", "Numerical Aperture Light Cones",
            new Vector2(0.04f, 0.91f), new Vector2(0.55f, 0.97f), 38f);
        EnsureLabel(root, "Formula Title", "Formula",
            new Vector2(0.59f, 0.84f), new Vector2(0.93f, 0.90f), 30f);
        EnsureLabel(root, "Current Values Title", "Current Values",
            new Vector2(0.59f, 0.48f), new Vector2(0.93f, 0.54f), 30f);
        EnsureLabel(root, "NA Label", "NA",
            new Vector2(0.59f, 0.40f), new Vector2(0.75f, 0.45f), 22f);
        EnsureLabel(root, "Theta Label", "Half Angle",
            new Vector2(0.59f, 0.33f), new Vector2(0.75f, 0.38f), 22f);
        EnsureLabel(root, "Full Aperture Label", "Full Aperture",
            new Vector2(0.59f, 0.26f), new Vector2(0.75f, 0.31f), 22f);
        EnsureLabel(root, "Magnification Label", "Approx. Magnification",
            new Vector2(0.59f, 0.19f), new Vector2(0.79f, 0.24f), 22f);
        EnsureLabel(root, "Normalized NA Label", "normalizedNA",
            new Vector2(0.59f, 0.12f), new Vector2(0.75f, 0.17f), 20f);
        EnsureLabel(root, "Depth Tolerance Label", "Depth Tolerance",
            new Vector2(0.59f, 0.06f), new Vector2(0.76f, 0.11f), 20f);
        EnsureLabel(root, "NA Slider Title", "Numerical Aperture",
            new Vector2(0.04f, 0.12f), new Vector2(0.50f, 0.18f), 27f);
        EnsureLabel(root, "NA Slider Low Label", "Low",
            new Vector2(0.04f, 0.035f), new Vector2(0.09f, 0.085f), 20f);
        EnsureLabel(root, "NA Slider High Label", "High",
            new Vector2(0.50f, 0.035f), new Vector2(0.55f, 0.085f), 20f);

        ConfigureRect(slider, new Vector2(0.09f, 0.04f), new Vector2(0.49f, 0.085f), Vector2.zero,
            Vector2.zero);
        ConfigureRect(exit, new Vector2(0.86f, 0.91f), new Vector2(0.95f, 0.965f), Vector2.zero,
            Vector2.zero);
        ConfigureRect(naText, new Vector2(0.80f, 0.40f), new Vector2(0.93f, 0.45f), Vector2.zero,
            Vector2.zero);
        ConfigureRect(thetaText, new Vector2(0.80f, 0.33f), new Vector2(0.93f, 0.38f), Vector2.zero,
            Vector2.zero);
        ConfigureRect(fullApertureText, new Vector2(0.80f, 0.26f), new Vector2(0.93f, 0.31f), Vector2.zero,
            Vector2.zero);
        ConfigureRect(magnificationText, new Vector2(0.80f, 0.19f), new Vector2(0.93f, 0.24f), Vector2.zero,
            Vector2.zero);
        ConfigureRect(normalizedNaText, new Vector2(0.80f, 0.12f), new Vector2(0.93f, 0.17f), Vector2.zero,
            Vector2.zero);
        ConfigureRect(formulaText, new Vector2(0.59f, 0.65f), new Vector2(0.93f, 0.81f), Vector2.zero,
            Vector2.zero);
        ConfigureRect(depthToleranceText, new Vector2(0.80f, 0.06f), new Vector2(0.93f, 0.11f), Vector2.zero,
            Vector2.zero);

        NumericalApertureDiagramView diagramView = SetupDiagram(root);

        if (experimentCanvasAnchor != null && experimentCanvasAnchor.gameObject != null)
        {
            Canvas anchorCanvas = EnsureComponent<Canvas>(experimentCanvasAnchor.gameObject);
            anchorCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            anchorCanvas.sortingOrder = 900;
            CanvasScaler anchorScaler = EnsureComponent<CanvasScaler>(experimentCanvasAnchor.gameObject);
            anchorScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            anchorScaler.referenceResolution = new Vector2(1920f, 1080f);
            anchorScaler.matchWidthOrHeight = 0.5f;
            EnsureComponent<GraphicRaycaster>(experimentCanvasAnchor.gameObject);
        }

        CanvasGroup fadeGroup = null;
        if (fade != null)
        {
            fade.localScale = Vector3.one;
            Canvas fadeCanvas = EnsureComponent<Canvas>(fade.gameObject);
            fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            fadeCanvas.sortingOrder = 901;
            EnsureComponent<GraphicRaycaster>(fade.gameObject);
            fadeGroup = EnsureComponent<CanvasGroup>(fade.gameObject);
            Image fadeImage = EnsureComponent<Image>(fade.gameObject);
            fadeImage.color = Color.black;
        }

        Slider naSlider = slider == null ? null : EnsureComponent<Slider>(slider.gameObject);
        if (naSlider != null)
        {
            naSlider.wholeNumbers = false;
            naSlider.minValue = 0.03f;
            naSlider.maxValue = 0.95f;
            naSlider.value = 0.16f;
            EnsureSliderVisuals(slider, naSlider);
        }

        Button exitButton = exit == null ? null : EnsureComponent<Button>(exit.gameObject);
        if (exitButton != null)
        {
            Image exitImage = EnsureComponent<Image>(exit.gameObject);
            exitImage.color = new Color(0.08f, 0.18f, 0.32f, 1f);
            exitButton.targetGraphic = exitImage;
            EnsureButtonLabel(exit, "Exit");
        }

        EnsureText(naText, "0.16");
        EnsureText(thetaText, "9.2 deg");
        EnsureText(fullApertureText, "18.4 deg");
        EnsureText(magnificationText, "5x");
        EnsureText(normalizedNaText, "0.14");
        EnsureText(formulaText,
            "NA = n * sin(theta)\n\nn = 1.00 (air)\ntheta = half angular aperture");
        EnsureText(depthToleranceText, "0.86");

        if (lightCone != null)
        {
            lightCone.gameObject.SetActive(false);
        }

        NumericalApertureExperimentController controller = GetComponent<NumericalApertureExperimentController>();
        if (controller != null)
        {
            SerializedObject serialized = new SerializedObject(controller);
            SetReference(serialized, "sampleInteraction", FindObjectOfType<InteractWithSamples>());
            SetReference(serialized, "fadeCanvasGroup", fadeGroup);
            SetReference(serialized, "experimentCanvasGroup", experimentGroup);
            SetReference(serialized, "experimentRoot", root.gameObject);
            SetReference(serialized, "exitButton", exitButton);
            SetReference(serialized, "naSlider", naSlider);
            SetReference(serialized, "currentNaText", GetText(naText));
            SetReference(serialized, "thetaText", GetText(thetaText));
            SetReference(serialized, "fullApertureText", GetText(fullApertureText));
            SetReference(serialized, "magnificationText", GetText(magnificationText));
            SetReference(serialized, "normalizedNaText", GetText(normalizedNaText));
            SetReference(serialized, "formulaText", GetText(formulaText));
            SetReference(serialized, "depthToleranceText", GetText(depthToleranceText));
            SetReference(serialized, "diagramView", diagramView);
            SetReference(serialized, "lightConeMeshFilter", null);
            SetReference(serialized, "lightConeRenderer", null);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }

    private static NumericalApertureDiagramView SetupDiagram(Transform root)
    {
        Transform diagramRoot = EnsureRectObject(root, "NA Diagram");
        ConfigureRect(diagramRoot, new Vector2(0.035f, 0.19f), new Vector2(0.555f, 0.88f),
            Vector2.zero, Vector2.zero);

        Transform cone = EnsureDiagramImage(diagramRoot, "Light Cone",
            LoadDiagramSprite("na_light_cone.png"));
        ConfigureFixedRect(cone, Vector2.zero, new Vector2(150f, 220f), new Vector2(0.5f, 0f));

        Transform objectiveGroup = EnsureRectObject(diagramRoot, "Objective Group");
        ConfigureFixedRect(objectiveGroup, new Vector2(0f, 100f), new Vector2(540f, 245f),
            new Vector2(0.5f, 0.5f));

        Transform objectiveBody = EnsureDiagramImage(objectiveGroup, "Objective Body",
            LoadDiagramSprite("na_objective_body.png"));
        ConfigureRect(objectiveBody, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        Transform opticalColumn = EnsureDiagramImage(objectiveGroup, "Optical Column",
            LoadDiagramSprite("na_optical_column.png"));
        ConfigureFixedRect(opticalColumn, Vector2.zero, new Vector2(150f, 293f),
            new Vector2(0.5f, 0.5f));
        Transform specimen = EnsureDiagramImage(diagramRoot, "Specimen",
            LoadDiagramSprite("na_specimen_slide.png"));
        ConfigureFixedRect(specimen, new Vector2(0f, -255f), new Vector2(340f, 28f),
            new Vector2(0.5f, 0.5f));

        Transform opticalAxis = EnsureDiagramImage(diagramRoot, "Optical Axis",
            LoadDiagramSprite("na_optical_axis.png"));
        ConfigureFixedRect(opticalAxis, new Vector2(0f, -253f), new Vector2(10f, 560f),
            new Vector2(0.5f, 0f));

        Transform angleObject = EnsureRectObject(diagramRoot, "Theta Arc");
        ConfigureFixedRect(angleObject, new Vector2(0f, -237f), new Vector2(160f, 160f),
            new Vector2(0.5f, 0.5f));
        NumericalApertureAngleGraphic angleGraphic =
            EnsureComponent<NumericalApertureAngleGraphic>(angleObject.gameObject);
        angleGraphic.color = SecondaryTextColor;
        angleGraphic.raycastTarget = false;

        TextMeshProUGUI angleText = EnsureDiagramText(diagramRoot, "Theta Diagram Text",
            "theta (9.2 deg)", new Vector2(150f, -180f), new Vector2(230f, 50f), 24f);
        EnsureDiagramText(diagramRoot, "Objective Diagram Label", "Objective",
            new Vector2(-355f, 80f), new Vector2(190f, 48f), 25f);
        EnsureDiagramText(diagramRoot, "Specimen Diagram Label", "Specimen",
            new Vector2(-355f, -255f), new Vector2(190f, 48f), 25f);

        cone.SetSiblingIndex(0);
        objectiveGroup.SetSiblingIndex(1);
        specimen.SetSiblingIndex(2);
        opticalAxis.SetAsLastSibling();
        angleObject.SetAsLastSibling();
        angleText.transform.SetAsLastSibling();

        NumericalApertureDiagramView view = EnsureComponent<NumericalApertureDiagramView>(diagramRoot.gameObject);
        SerializedObject serializedView = new SerializedObject(view);
        SetReference(serializedView, "objectiveGroup", objectiveGroup as RectTransform);
        SetReference(serializedView, "opticalColumn", opticalColumn as RectTransform);
        SetReference(serializedView, "lightCone", cone as RectTransform);
        SetReference(serializedView, "specimen", specimen as RectTransform);
        SetReference(serializedView, "opticalAxis", opticalAxis as RectTransform);
        SetReference(serializedView, "angleGraphic", angleGraphic);
        SetReference(serializedView, "angleText", angleText);
        serializedView.ApplyModifiedPropertiesWithoutUndo();
        view.ApplyContinuous(0.14f, 9.2f);
        return view;
    }

    private static Transform EnsureRectObject(Transform parent, string objectName)
    {
        Transform existing = parent.Find(objectName);
        if (existing != null)
        {
            return existing;
        }

        GameObject created = new GameObject(objectName, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(created, "Create numerical aperture diagram object");
        created.transform.SetParent(parent, false);
        return created.transform;
    }

    private static Transform EnsureDiagramImage(Transform parent, string objectName, Sprite sprite)
    {
        Transform child = EnsureRectObject(parent, objectName);
        Image image = EnsureComponent<Image>(child.gameObject);
        image.sprite = sprite;
        image.color = Color.white;
        image.preserveAspect = false;
        image.raycastTarget = false;
        return child;
    }

    private static TextMeshProUGUI EnsureDiagramText(Transform parent, string objectName, string value,
        Vector2 anchoredPosition, Vector2 size, float fontSize)
    {
        Transform child = EnsureTextObjectStatic(parent, objectName);
        ConfigureFixedRect(child, anchoredPosition, size, new Vector2(0.5f, 0.5f));
        TextMeshProUGUI text = EnsureComponent<TextMeshProUGUI>(child.gameObject);
        text.text = value;
        text.fontSize = fontSize;
        text.color = TextColor;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
        return text;
    }

    private static Transform EnsureTextObjectStatic(Transform parent, string objectName)
    {
        Transform existing = parent.Find(objectName);
        if (existing != null)
        {
            return existing;
        }

        GameObject created = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        Undo.RegisterCreatedObjectUndo(created, "Create numerical aperture diagram text");
        created.transform.SetParent(parent, false);
        return created.transform;
    }

    private static void ConfigureFixedRect(Transform target, Vector2 anchoredPosition, Vector2 size,
        Vector2 pivot)
    {
        RectTransform rect = target as RectTransform;
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private static Sprite LoadDiagramSprite(string fileName)
    {
        string assetPath = DiagramAssetRoot + fileName;
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            bool needsImport = importer.textureType != TextureImporterType.Sprite ||
                               importer.spriteImportMode != SpriteImportMode.Single ||
                               !importer.alphaIsTransparency || importer.mipmapEnabled;
            if (needsImport)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    private Transform FindChild(string path)
    {
        return transform.Find(path);
    }

    private Transform EnsureTextObject(Transform parent, string objectName)
    {
        Transform existing = parent.Find(objectName);
        if (existing != null)
        {
            return existing;
        }

        GameObject created = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        Undo.RegisterCreatedObjectUndo(created, "Create numerical aperture UI text");
        created.transform.SetParent(parent, false);
        return created.transform;
    }

    private static void ConfigureRect(Transform target, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        if (target == null)
        {
            return;
        }

        RectTransform rect = target as RectTransform;
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        rect.localScale = Vector3.one;
    }

    private static void EnsureBackground(Transform parent, string objectName, Color color)
    {
        Transform existing = parent.Find(objectName);
        GameObject background = existing != null ? existing.gameObject :
            new GameObject(objectName, typeof(RectTransform), typeof(Image));
        if (existing == null)
        {
            Undo.RegisterCreatedObjectUndo(background, "Create numerical aperture background");
            background.transform.SetParent(parent, false);
        }

        ConfigureRect(background.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image image = background.GetComponent<Image>();
        image.color = color;
        background.transform.SetSiblingIndex(0);
    }

    private static void EnsureLabel(Transform parent, string objectName, string value,
        Vector2 anchorMin, Vector2 anchorMax, float fontSize)
    {
        Transform existing = parent.Find(objectName);
        GameObject labelObject = existing != null ? existing.gameObject :
            new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        if (existing == null)
        {
            Undo.RegisterCreatedObjectUndo(labelObject, "Create numerical aperture UI label");
            labelObject.transform.SetParent(parent, false);
        }

        ConfigureRect(labelObject.transform, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = value;
        label.fontSize = fontSize;
        label.color = TextColor;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Left;
        label.enableWordWrapping = false;
    }

    private static void EnsureSliderVisuals(Transform sliderTransform, Slider slider)
    {
        if (sliderTransform == null || slider == null)
        {
            return;
        }

        Transform background = EnsureImageChild(sliderTransform, "Background",
            new Color(0.78f, 0.82f, 0.86f, 1f));
        ConfigureRect(background, new Vector2(0f, 0.43f), new Vector2(1f, 0.57f),
            Vector2.zero, Vector2.zero);

        Transform fill = EnsureImageChild(sliderTransform, "Fill",
            AccentColor);
        ConfigureRect(fill, new Vector2(0f, 0.43f), new Vector2(0.5f, 0.57f),
            Vector2.zero, Vector2.zero);

        Transform handle = EnsureImageChild(sliderTransform, "Handle",
            new Color(0.98f, 0.99f, 1f, 1f));
        ConfigureRect(handle, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(42f, 42f));

        for (int i = 0; i < 7; i++)
        {
            float x = i / 6f;
            Transform tick = EnsureImageChild(sliderTransform, $"Tick {i + 1}",
                new Color(0.20f, 0.27f, 0.34f, 0.55f));
            ConfigureRect(tick, new Vector2(x, 0.34f), new Vector2(x, 0.66f),
                Vector2.zero, new Vector2(2f, 0f));
            tick.GetComponent<Image>().raycastTarget = false;
        }

        slider.fillRect = fill as RectTransform;
        slider.handleRect = handle as RectTransform;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.direction = Slider.Direction.LeftToRight;
        background.GetComponent<Image>().raycastTarget = true;
        fill.GetComponent<Image>().raycastTarget = false;
        handle.GetComponent<Image>().raycastTarget = true;
        background.SetSiblingIndex(0);
        fill.SetSiblingIndex(1);
        handle.SetAsLastSibling();
    }

    private static Transform EnsureImageChild(Transform parent, string objectName, Color color)
    {
        Transform existing = parent.Find(objectName);
        GameObject child = existing != null ? existing.gameObject :
            new GameObject(objectName, typeof(RectTransform), typeof(Image));
        if (existing == null)
        {
            Undo.RegisterCreatedObjectUndo(child, "Create numerical aperture slider visual");
            child.transform.SetParent(parent, false);
        }

        Image image = child.GetComponent<Image>();
        image.color = color;
        return child.transform;
    }

    private static T EnsureComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(target);
    }

    private static TextMeshProUGUI EnsureText(Transform target, string value)
    {
        if (target == null)
        {
            return null;
        }

        TextMeshProUGUI text = EnsureComponent<TextMeshProUGUI>(target.gameObject);
        text.text = value;

        text.fontSize = 28f;
        text.color = SecondaryTextColor;
        text.alignment = TextAlignmentOptions.Left;
        text.enableWordWrapping = true;
        return text;
    }

    private static void EnsureButtonLabel(Transform button, string value)
    {
        Transform labelTransform = EnsureTextObjectStatic(button, "Label");
        ConfigureRect(labelTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        TextMeshProUGUI label = EnsureComponent<TextMeshProUGUI>(labelTransform.gameObject);
        label.text = value;
        label.fontSize = 23f;
        label.color = Color.white;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
    }

    private static TextMeshProUGUI GetText(Transform target)
    {
        return target == null ? null : target.GetComponent<TextMeshProUGUI>();
    }

    private static void SetReference(SerializedObject serialized, string propertyName, Object value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }
#endif
}
