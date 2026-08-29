using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

public static class SpatialFrequencyExperimentSceneSetup
{
    private const string MainScenePath = "Assets/Scene/Scene_Laboratory/MainScene_Labortory.unity";
    private static readonly Color PageColor = new Color(0.965f, 0.972f, 0.982f, 1f);
    private static readonly Color InkColor = new Color(0.055f, 0.09f, 0.14f, 1f);
    private static readonly Color MutedColor = new Color(0.24f, 0.3f, 0.38f, 1f);
    private static readonly Color AccentColor = new Color(0.04f, 0.43f, 0.95f, 1f);

    [MenuItem("Tools/Experiments/Setup Spatial Frequency Experiment")]
    public static void SetupMainSceneFromMenu()
    {
        SetupMainScene();
    }

    public static void SetupMainSceneBatch()
    {
        try
        {
            SetupMainScene();
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    private static void SetupMainScene()
    {
        Scene scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        BackupSceneFile();

        GameObject experimentManager = FindSceneObject(scene, "Experiment Manager");
        if (experimentManager == null)
        {
            throw new InvalidOperationException("Cannot find 'Experiment Manager' in the main scene.");
        }

        Transform hostTransform = GetOrCreateChild(experimentManager.transform, "Spatial Frequency ExperimentController");
        SpatialFrequencyExperimentController controller =
            GetOrAddComponent<SpatialFrequencyExperimentController>(hostTransform.gameObject);

        RectTransform root = GetOrCreateRect(hostTransform, "SpatialFrequencyExperimentRoot");
        root.gameObject.SetActive(true);
        Canvas canvas = GetOrAddComponent<Canvas>(root.gameObject);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;
        CanvasScaler scaler = GetOrAddComponent<CanvasScaler>(root.gameObject);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        GetOrAddComponent<GraphicRaycaster>(root.gameObject);
        GetOrAddComponent<TrackedDeviceGraphicRaycaster>(root.gameObject);
        CanvasGroup rootGroup = GetOrAddComponent<CanvasGroup>(root.gameObject);
        Stretch(root);

        Image background = EnsureImage(root, "Background", PageColor);
        Stretch(background.rectTransform);
        background.raycastTarget = true;
        background.transform.SetAsFirstSibling();

        EnsureText(root, "Title", "Spatial Frequency and Image Resolution",
            new Vector2(0.035f, 0.91f), new Vector2(0.78f, 0.985f), 36f, FontStyles.Bold,
            TextAlignmentOptions.Left, InkColor);

        Button exitButton = EnsureButton(root, "Exit Button", "Exit Experiment",
            new Vector2(0.84f, 0.915f), new Vector2(0.965f, 0.972f));

        RectTransform opticalPanel = GetOrCreateRect(root, "Optical Path Panel");
        SetAnchors(opticalPanel, new Vector2(0.025f, 0.11f), new Vector2(0.42f, 0.89f),
            new Vector2(12f, 8f), new Vector2(-12f, -8f));
        Image opticalPanelImage = GetOrAddComponent<Image>(opticalPanel.gameObject);
        opticalPanelImage.color = new Color(1f, 1f, 1f, 0.72f);
        opticalPanelImage.raycastTarget = false;

        SpatialFrequencyOpticalPathGraphic opticalGraphic =
            GetOrAddComponent<SpatialFrequencyOpticalPathGraphic>(GetOrCreateRect(opticalPanel, "Optical Path Graphic").gameObject);
        SetAnchors(opticalGraphic.rectTransform, new Vector2(0.36f, 0f), new Vector2(0.98f, 0.96f),
            Vector2.zero, Vector2.zero);
        opticalGraphic.raycastTarget = false;

        EnsureText(opticalPanel, "Back Focal Plane Label", "Objective Back\nFocal Plane",
            new Vector2(0.02f, 0.82f), new Vector2(0.28f, 0.95f), 22f, FontStyles.Normal,
            TextAlignmentOptions.Left, InkColor);
        EnsureText(opticalPanel, "Objective Label", "Objective",
            new Vector2(0.02f, 0.61f), new Vector2(0.28f, 0.69f), 22f, FontStyles.Normal,
            TextAlignmentOptions.Left, InkColor);
        EnsureText(opticalPanel, "Diffraction Angle Label", "Diffraction Angle (psi)",
            new Vector2(0.02f, 0.52f), new Vector2(0.32f, 0.60f), 20f, FontStyles.Normal,
            TextAlignmentOptions.Left, InkColor);
        EnsureText(opticalPanel, "Specimen Label", "Specimen",
            new Vector2(0.02f, 0.38f), new Vector2(0.28f, 0.46f), 22f, FontStyles.Normal,
            TextAlignmentOptions.Left, InkColor);
        EnsureText(opticalPanel, "Condenser Lens Label", "Condenser Lens",
            new Vector2(0.02f, 0.19f), new Vector2(0.28f, 0.27f), 22f, FontStyles.Normal,
            TextAlignmentOptions.Left, InkColor);
        EnsureText(opticalPanel, "Aperture Stop Label", "Condenser Aperture Stop",
            new Vector2(0.02f, 0.015f), new Vector2(0.34f, 0.10f), 20f, FontStyles.Normal,
            TextAlignmentOptions.Left, InkColor);

        RectTransform rightPanel = GetOrCreateRect(root, "Result Panel");
        SetAnchors(rightPanel, new Vector2(0.43f, 0.08f), new Vector2(0.975f, 0.89f),
            Vector2.zero, Vector2.zero);

        RectTransform backFocalRoot = GetOrCreateRect(rightPanel, "Objective Back Focal Plane");
        SetAnchors(backFocalRoot, new Vector2(0.16f, 0.62f), new Vector2(0.84f, 0.91f),
            Vector2.zero, Vector2.zero);
        AspectRatioFitter aspect = backFocalRoot.GetComponent<AspectRatioFitter>();
        if (aspect != null)
        {
            UnityEngine.Object.DestroyImmediate(aspect);
        }
        SpatialFrequencyCircleGraphic circle = GetOrAddComponent<SpatialFrequencyCircleGraphic>(backFocalRoot.gameObject);
        circle.color = Color.black;
        circle.raycastTarget = false;
        Mask mask = GetOrAddComponent<Mask>(backFocalRoot.gameObject);
        mask.showMaskGraphic = true;

        RawImage sampleOverlay = GetOrAddComponent<RawImage>(GetOrCreateRect(backFocalRoot, "Selected Sample Overlay").gameObject);
        Stretch(sampleOverlay.rectTransform);
        sampleOverlay.raycastTarget = false;
        sampleOverlay.color = Color.white;

        RectTransform diffractionRoot;
        Transform legacyDiffraction = backFocalRoot.Find("Diffraction Pattern");
        if (legacyDiffraction != null)
        {
            diffractionRoot = (RectTransform)legacyDiffraction;
            diffractionRoot.name = "Reference Diffraction Pattern";
            diffractionRoot.SetParent(rightPanel, false);
        }
        else
        {
            diffractionRoot = GetOrCreateRect(rightPanel, "Reference Diffraction Pattern");
        }

        SetAnchors(diffractionRoot, new Vector2(0.12f, 0.47f), new Vector2(0.88f, 0.55f),
            Vector2.zero, Vector2.zero);
        SpatialFrequencyDiffractionPatternGraphic diffractionPattern =
            GetOrAddComponent<SpatialFrequencyDiffractionPatternGraphic>(diffractionRoot.gameObject);
        diffractionPattern.raycastTarget = false;

        TextMeshProUGUI backFocalLabel = EnsureText(rightPanel, "Back Focal Plane Caption",
            "Objective Back Focal Plane", new Vector2(0.12f, 0.555f), new Vector2(0.88f, 0.61f),
            25f, FontStyles.Normal, TextAlignmentOptions.Center, InkColor);

        RectTransform gratingRoot = GetOrCreateRect(rightPanel, "Line Grating");
        SetAnchors(gratingRoot, new Vector2(0.12f, 0.34f), new Vector2(0.88f, 0.46f),
            Vector2.zero, Vector2.zero);
        Image gratingBackground = GetOrAddComponent<Image>(gratingRoot.gameObject);
        gratingBackground.color = Color.white;
        gratingBackground.raycastTarget = false;
        SpatialFrequencyLineGratingGraphic grating =
            GetOrAddComponent<SpatialFrequencyLineGratingGraphic>(GetOrCreateRect(gratingRoot, "Grating Lines").gameObject);
        Stretch(grating.rectTransform);
        grating.color = Color.black;
        grating.raycastTarget = false;

        TextMeshProUGUI gratingLabel = EnsureText(rightPanel, "Grating Caption",
            "Line grating with 250 lines/mm", new Vector2(0.08f, 0.275f), new Vector2(0.92f, 0.34f),
            22f, FontStyles.Normal, TextAlignmentOptions.Center, InkColor);
        TextMeshProUGUI sampleSourceLabel = EnsureText(rightPanel, "Sample Source",
            "Reference diffraction orders for the selected spatial frequency",
            new Vector2(0.06f, 0.225f), new Vector2(0.94f, 0.275f), 16f, FontStyles.Italic,
            TextAlignmentOptions.Center, MutedColor);

        // Keep the previous schematic widgets serialized for comparison, but the
        // three CPU-generated planes are now the authoritative experiment output.
        backFocalRoot.gameObject.SetActive(false);
        diffractionRoot.gameObject.SetActive(false);
        gratingRoot.gameObject.SetActive(false);
        backFocalLabel.gameObject.SetActive(false);
        gratingLabel.gameObject.SetActive(false);
        sampleSourceLabel.gameObject.SetActive(false);

        FourierOpticsCpuSimulator fourierSimulator =
            GetOrAddComponent<FourierOpticsCpuSimulator>(rightPanel.gameObject);
        RawImage objectPlaneView = EnsurePlaneView(
            rightPanel,
            "Object Plane View",
            new Vector2(0.01f, 0.58f),
            new Vector2(0.32f, 0.93f));
        RawImage fourierPlaneView = EnsurePlaneView(
            rightPanel,
            "Fourier Plane View",
            new Vector2(0.345f, 0.58f),
            new Vector2(0.655f, 0.93f));
        RawImage reconstructionView = EnsurePlaneView(
            rightPanel,
            "Reconstruction View",
            new Vector2(0.68f, 0.58f),
            new Vector2(0.99f, 0.93f));
        TextMeshProUGUI objectPlaneLabel = EnsureText(
            rightPanel, "Object Plane Label", "Object Plane",
            new Vector2(0.01f, 0.51f), new Vector2(0.32f, 0.575f), 18f,
            FontStyles.Bold, TextAlignmentOptions.Center, InkColor);
        TextMeshProUGUI fourierPlaneLabel = EnsureText(
            rightPanel, "Fourier Plane Label", "Objective Back Focal Plane | Fourier Spectrum",
            new Vector2(0.335f, 0.51f), new Vector2(0.665f, 0.575f), 16f,
            FontStyles.Bold, TextAlignmentOptions.Center, InkColor);
        TextMeshProUGUI reconstructionLabel = EnsureText(
            rightPanel, "Reconstruction Label", "Pupil-Filtered Reconstruction",
            new Vector2(0.68f, 0.51f), new Vector2(0.99f, 0.575f), 17f,
            FontStyles.Bold, TextAlignmentOptions.Center, InkColor);

        RemoveChildIfPresent(rightPanel, "Input Pattern Button");
        RemoveChildIfPresent(rightPanel, "Numerical Aperture Value");
        RemoveChildIfPresent(rightPanel, "Numerical Aperture Slider");
        RemoveChildIfPresent(rightPanel, "DMD Tilt Value");
        RemoveChildIfPresent(rightPanel, "DMD Tilt Slider");

        Button whiteLightButton = EnsureChoiceButton(
            rightPanel,
            "White Light Button",
            "White Light",
            new Vector2(0.18f, 0.39f),
            new Vector2(0.48f, 0.46f));
        Button laserExcitationButton = EnsureChoiceButton(
            rightPanel,
            "Laser Excitation Button",
            "Laser Excitation",
            new Vector2(0.52f, 0.39f),
            new Vector2(0.82f, 0.46f));

        EnsureText(rightPanel, "Spatial Frequency Heading", "Spatial Frequency",
            new Vector2(0.05f, 0.28f), new Vector2(0.4f, 0.35f), 22f, FontStyles.Bold,
            TextAlignmentOptions.Left, InkColor);
        ToggleGroup toggleGroup = GetOrAddComponent<ToggleGroup>(rightPanel.gameObject);
        Toggle highToggle = EnsureToggle(rightPanel, "High Frequency Toggle", "High   250 lines/mm",
            new Vector2(0.35f, 0.25f), new Vector2(0.95f, 0.32f), toggleGroup);
        Toggle middleToggle = EnsureToggle(rightPanel, "Middle Frequency Toggle", "Middle   125 lines/mm",
            new Vector2(0.35f, 0.17f), new Vector2(0.95f, 0.24f), toggleGroup);
        Toggle lowToggle = EnsureToggle(rightPanel, "Low Frequency Toggle", "Low   62.5 lines/mm",
            new Vector2(0.35f, 0.09f), new Vector2(0.95f, 0.16f), toggleGroup);
        highToggle.SetIsOnWithoutNotify(true);
        middleToggle.SetIsOnWithoutNotify(false);
        lowToggle.SetIsOnWithoutNotify(false);

        RectTransform valuesPanel = GetOrCreateRect(root, "Values Panel");
        SetAnchors(valuesPanel, new Vector2(0.035f, 0.015f), new Vector2(0.42f, 0.115f),
            Vector2.zero, Vector2.zero);
        TextMeshProUGUI frequencyText = EnsureText(valuesPanel, "Frequency Text", string.Empty,
            new Vector2(0f, 0.52f), new Vector2(0.34f, 1f), 17f, FontStyles.Bold,
            TextAlignmentOptions.Left, InkColor);
        TextMeshProUGUI lineSpacingText = EnsureText(valuesPanel, "Line Spacing Text", string.Empty,
            new Vector2(0.34f, 0.52f), new Vector2(0.67f, 1f), 17f, FontStyles.Normal,
            TextAlignmentOptions.Left, InkColor);
        TextMeshProUGUI angleText = EnsureText(valuesPanel, "Diffraction Angle Text", string.Empty,
            new Vector2(0.67f, 0.52f), new Vector2(1f, 1f), 17f, FontStyles.Normal,
            TextAlignmentOptions.Left, InkColor);
        TextMeshProUGUI orderSpacingText = EnsureText(valuesPanel, "Order Spacing Text", string.Empty,
            new Vector2(0f, 0f), new Vector2(0.34f, 0.48f), 16f, FontStyles.Normal,
            TextAlignmentOptions.Left, MutedColor);
        TextMeshProUGUI formulaText = EnsureText(valuesPanel, "Formula Text", string.Empty,
            new Vector2(0.34f, 0f), new Vector2(0.72f, 0.48f), 15f, FontStyles.Normal,
            TextAlignmentOptions.Left, MutedColor);
        TextMeshProUGUI resolutionText = EnsureText(valuesPanel, "Resolution Text", string.Empty,
            new Vector2(0.72f, 0f), new Vector2(1f, 0.48f), 15f, FontStyles.Normal,
            TextAlignmentOptions.Left, MutedColor);
        TextMeshProUGUI sampleNameText = EnsureText(root, "Selected Sample Text", string.Empty,
            new Vector2(0.58f, 0.015f), new Vector2(0.95f, 0.07f), 16f, FontStyles.Normal,
            TextAlignmentOptions.Center, MutedColor);

        SpatialFrequencyExperimentDiagramView diagramView =
            GetOrAddComponent<SpatialFrequencyExperimentDiagramView>(rightPanel.gameObject);
        SetReference(diagramView, "opticalPathGraphic", opticalGraphic);
        SetReference(diagramView, "diffractionPatternGraphic", diffractionPattern);
        SetReference(diagramView, "lineGratingGraphic", grating);
        SetReference(diagramView, "backFocalSampleOverlay", sampleOverlay);
        Shader sampleFilterShader = AssetDatabase.LoadAssetAtPath<Shader>(
            "Assets/Shader/SpatialFrequencySampleFilter.shader");
        SetReference(diagramView, "sampleFilterShader", sampleFilterShader);
        SetReference(diagramView, "backFocalPlaneLabel", backFocalLabel);
        SetReference(diagramView, "gratingLabel", gratingLabel);
        SetReference(diagramView, "sampleSourceLabel", sampleSourceLabel);
        SetReference(diagramView, "fourierSimulator", fourierSimulator);
        SetReference(diagramView, "objectPlaneView", objectPlaneView);
        SetReference(diagramView, "fourierPlaneView", fourierPlaneView);
        SetReference(diagramView, "reconstructedPlaneView", reconstructionView);
        SetReference(diagramView, "objectPlaneLabel", objectPlaneLabel);
        SetReference(diagramView, "fourierPlaneLabel", fourierPlaneLabel);
        SetReference(diagramView, "reconstructedPlaneLabel", reconstructionLabel);

        NumericalApertureExperimentController numericalController =
            FindSceneComponent<NumericalApertureExperimentController>(scene);
        CopyReference(numericalController, controller, "selectionController");
        CopyReference(numericalController, controller, "modelExploder");
        CopyReference(numericalController, controller, "interactor");
        CopyReference(numericalController, controller, "moveController");
        CopyReference(numericalController, controller, "rightRayInteractor");
        CopyReference(numericalController, controller, "sampleInteraction");
        CopyReference(numericalController, controller, "fadeCanvasGroup");
        CopyReference(numericalController, controller, "progressControl");
        CopyReference(numericalController, controller, "experimentVirtualCamera");
        CopyReference(numericalController, controller, "targetCamera");
        CopyReference(numericalController, controller, "xrOrigin");
        CopyReference(numericalController, controller, "experimentCameraPose");
        CopyReference(numericalController, controller, "manualFixedCameraPose");

        SetReference(controller, "experimentCanvasGroup", rootGroup);
        SetReference(controller, "experimentRoot", root.gameObject);
        SetReference(controller, "exitButton", exitButton);
        SetReference(controller, "highFrequencyToggle", highToggle);
        SetReference(controller, "middleFrequencyToggle", middleToggle);
        SetReference(controller, "lowFrequencyToggle", lowToggle);
        SetReference(controller, "whiteLightButton", whiteLightButton);
        SetReference(controller, "laserExcitationButton", laserExcitationButton);
        SetReference(controller, "diagramView", diagramView);
        SetReference(controller, "frequencyText", frequencyText);
        SetReference(controller, "lineSpacingText", lineSpacingText);
        SetReference(controller, "diffractionAngleText", angleText);
        SetReference(controller, "orderSpacingText", orderSpacingText);
        SetReference(controller, "formulaText", formulaText);
        SetReference(controller, "resolutionText", resolutionText);
        SetReference(controller, "sampleNameText", sampleNameText);

        SuperAssemblyPartSelectionController selectionController =
            FindSceneComponent<SuperAssemblyPartSelectionController>(scene);
        ConfigureObjectiveExperimentBinding(selectionController, controller);

        diagramView.ApplyProfile(1f, 250f, 7.9f, 1, null, false);
        root.gameObject.SetActive(false);
        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(selectionController);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[SpatialFrequencyExperimentSceneSetup] Spatial frequency experiment created and bound to Objective.");
    }

    private static void ConfigureObjectiveExperimentBinding(
        SuperAssemblyPartSelectionController selectionController,
        SpatialFrequencyExperimentController experimentController)
    {
        if (selectionController == null)
        {
            throw new InvalidOperationException("Cannot find SuperAssemblyPartSelectionController.");
        }

        SerializedObject serialized = new SerializedObject(selectionController);
        SerializedProperty partInfos = serialized.FindProperty("partInfos");
        UnityEngine.Object objectiveTransform = null;
        for (int i = 0; i < partInfos.arraySize; i++)
        {
            SerializedProperty item = partInfos.GetArrayElementAtIndex(i);
            string displayName = item.FindPropertyRelative("displayName").stringValue;
            if (displayName.Contains("物镜") || displayName.IndexOf("Objective", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                objectiveTransform = item.FindPropertyRelative("partTransform").objectReferenceValue;
                break;
            }
        }

        if (objectiveTransform == null)
        {
            throw new InvalidOperationException("Cannot resolve the Objective transform from partInfos.");
        }

        SerializedProperty bindings = serialized.FindProperty("partExperimentBindings");
        SerializedProperty targetBinding = null;
        for (int i = 0; i < bindings.arraySize; i++)
        {
            SerializedProperty item = bindings.GetArrayElementAtIndex(i);
            if (item.FindPropertyRelative("partTransform").objectReferenceValue == objectiveTransform)
            {
                targetBinding = item;
                break;
            }
        }

        if (targetBinding == null)
        {
            int index = bindings.arraySize;
            bindings.InsertArrayElementAtIndex(index);
            targetBinding = bindings.GetArrayElementAtIndex(index);
        }

        targetBinding.FindPropertyRelative("partTransform").objectReferenceValue = objectiveTransform;
        targetBinding.FindPropertyRelative("numericalApertureExperiment").objectReferenceValue = null;
        targetBinding.FindPropertyRelative("spatialFrequencyExperiment").objectReferenceValue = experimentController;
        targetBinding.FindPropertyRelative("buttonText").stringValue = "Start Spatial Frequency Experiment";
        serialized.FindProperty("autoBindObjectiveSpatialFrequency").boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Toggle EnsureToggle(
        Transform parent,
        string objectName,
        string label,
        Vector2 anchorMin,
        Vector2 anchorMax,
        ToggleGroup group)
    {
        RectTransform rect = GetOrCreateRect(parent, objectName);
        SetAnchors(rect, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
        Image background = GetOrAddComponent<Image>(rect.gameObject);
        background.color = new Color(1f, 1f, 1f, 0.01f);
        Toggle toggle = GetOrAddComponent<Toggle>(rect.gameObject);
        toggle.group = group;
        toggle.targetGraphic = background;

        Image checkmark = EnsureImage(rect, "Checkmark", AccentColor);
        SetAnchors(checkmark.rectTransform, new Vector2(0.01f, 0.22f), new Vector2(0.055f, 0.78f),
            Vector2.zero, Vector2.zero);
        toggle.graphic = checkmark;
        EnsureText(rect, "Label", label, new Vector2(0.075f, 0f), new Vector2(1f, 1f), 20f,
            FontStyles.Normal, TextAlignmentOptions.Left, InkColor);
        return toggle;
    }

    private static Button EnsureButton(
        Transform parent,
        string objectName,
        string label,
        Vector2 anchorMin,
        Vector2 anchorMax)
    {
        RectTransform rect = GetOrCreateRect(parent, objectName);
        SetAnchors(rect, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
        Image image = GetOrAddComponent<Image>(rect.gameObject);
        image.color = new Color(0.86f, 0.19f, 0.14f, 1f);
        Button button = GetOrAddComponent<Button>(rect.gameObject);
        button.targetGraphic = image;
        EnsureText(rect, "Label", label, Vector2.zero, Vector2.one, 19f, FontStyles.Bold,
            TextAlignmentOptions.Center, Color.white);
        return button;
    }

    private static Button EnsureChoiceButton(
        Transform parent,
        string objectName,
        string label,
        Vector2 anchorMin,
        Vector2 anchorMax)
    {
        RectTransform rect = GetOrCreateRect(parent, objectName);
        SetAnchors(rect, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
        Image image = GetOrAddComponent<Image>(rect.gameObject);
        image.color = new Color(0.27f, 0.33f, 0.43f, 1f);
        Button button = GetOrAddComponent<Button>(rect.gameObject);
        button.targetGraphic = image;
        EnsureText(rect, "Label", label, Vector2.zero, Vector2.one, 17f, FontStyles.Bold,
            TextAlignmentOptions.Center, Color.white);
        return button;
    }

    private static RawImage EnsurePlaneView(
        Transform parent,
        string objectName,
        Vector2 anchorMin,
        Vector2 anchorMax)
    {
        RectTransform rect = GetOrCreateRect(parent, objectName);
        SetAnchors(rect, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
        Image background = GetOrAddComponent<Image>(rect.gameObject);
        background.color = Color.black;
        background.raycastTarget = false;

        RectTransform imageRect = GetOrCreateRect(rect, "Texture");
        SetAnchors(imageRect, Vector2.zero, Vector2.one, new Vector2(3f, 3f), new Vector2(-3f, -3f));
        RawImage image = GetOrAddComponent<RawImage>(imageRect.gameObject);
        image.color = Color.white;
        image.raycastTarget = false;
        image.uvRect = new Rect(0f, 0f, 1f, 1f);
        return image;
    }

    private static TextMeshProUGUI EnsureText(
        Transform parent,
        string objectName,
        string value,
        Vector2 anchorMin,
        Vector2 anchorMax,
        float fontSize,
        FontStyles style,
        TextAlignmentOptions alignment,
        Color color)
    {
        RectTransform rect = GetOrCreateRect(parent, objectName);
        SetAnchors(rect, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
        TextMeshProUGUI text = GetOrAddComponent<TextMeshProUGUI>(rect.gameObject);
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = color;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        return text;
    }

    private static Image EnsureImage(Transform parent, string objectName, Color color)
    {
        RectTransform rect = GetOrCreateRect(parent, objectName);
        Image image = GetOrAddComponent<Image>(rect.gameObject);
        image.color = color;
        return image;
    }

    private static RectTransform GetOrCreateRect(Transform parent, string objectName)
    {
        Transform existing = parent.Find(objectName);
        GameObject target = existing != null
            ? existing.gameObject
            : new GameObject(objectName, typeof(RectTransform));
        target.transform.SetParent(parent, false);
        return GetOrAddComponent<RectTransform>(target);
    }

    private static Transform GetOrCreateChild(Transform parent, string objectName)
    {
        Transform existing = parent.Find(objectName);
        if (existing != null)
        {
            return existing;
        }

        GameObject target = new GameObject(objectName);
        target.transform.SetParent(parent, false);
        return target.transform;
    }

    private static void RemoveChildIfPresent(Transform parent, string objectName)
    {
        Transform existing = parent.Find(objectName);
        if (existing != null)
        {
            UnityEngine.Object.DestroyImmediate(existing.gameObject);
        }
    }

    private static void Stretch(RectTransform rect)
    {
        SetAnchors(rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
    }

    private static void SetAnchors(
        RectTransform rect,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private static T GetOrAddComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    private static void SetReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
        {
            throw new MissingFieldException(target.GetType().Name, propertyName);
        }

        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CopyReference(UnityEngine.Object source, UnityEngine.Object target, string propertyName)
    {
        if (source == null || target == null)
        {
            return;
        }

        SerializedProperty sourceProperty = new SerializedObject(source).FindProperty(propertyName);
        if (sourceProperty != null)
        {
            SetReference(target, propertyName, sourceProperty.objectReferenceValue);
        }
    }

    private static GameObject FindSceneObject(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform item in transforms)
            {
                if (item.name == objectName)
                {
                    return item.gameObject;
                }
            }
        }

        return null;
    }

    private static T FindSceneComponent<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>(true);
            if (component != null)
            {
                return component;
            }
        }

        return null;
    }

    private static void BackupSceneFile()
    {
        string sourcePath = Path.GetFullPath(MainScenePath);
        string backupDirectory = Path.GetFullPath("Temp/SceneBackups");
        Directory.CreateDirectory(backupDirectory);
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string backupPath = Path.Combine(backupDirectory,
            $"MainScene_Labortory_before_spatial_frequency_{timestamp}.unity");
        File.Copy(sourcePath, backupPath, false);
        Debug.Log($"[SpatialFrequencyExperimentSceneSetup] Scene backup: {backupPath}");
    }
}
