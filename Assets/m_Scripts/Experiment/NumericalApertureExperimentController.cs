using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using DG.Tweening;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

public class NumericalApertureExperimentController : MonoBehaviour
{
    [Serializable]
    public class FloatEvent : UnityEvent<float>
    {
    }

    private readonly struct ApertureProfile
    {
        public ApertureProfile(float na, float halfAngleDegrees, float magnification)
        {
            NA = na;
            HalfAngleDegrees = halfAngleDegrees;
            Magnification = magnification;
        }

        public float NA { get; }
        public float HalfAngleDegrees { get; }
        public float Magnification { get; }
    }

    private static readonly ApertureProfile[] ApertureProfiles =
    {
        new ApertureProfile(0.03f, 1.7f, 1.25f),
        new ApertureProfile(0.085f, 4.9f, 2.5f),
        new ApertureProfile(0.16f, 9.2f, 5f),
        new ApertureProfile(0.25f, 14.5f, 10f),
        new ApertureProfile(0.5f, 30f, 20f),
        new ApertureProfile(0.75f, 48.6f, 40f),
        new ApertureProfile(0.95f, 71.8f, 63f)
    };

    [Header("Experiment Entry")]
    [SerializeField] private SuperAssemblyPartSelectionController selectionController;
    [SerializeField] private ModelExploder modelExploder;
    [SerializeField] private Interactor interactor;
    [SerializeField] private Move moveController;
    [SerializeField] private XRRayInteractor rightRayInteractor;
    [SerializeField] private InteractWithSamples sampleInteraction;
    [SerializeField] private bool blockGlobalInputMaps = true;
    [SerializeField] private string[] globalInputMapsToBlock = { "Global" };

    [Header("Transition")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private CanvasGroup experimentCanvasGroup;
    [SerializeField] private GameObject experimentRoot;
    [SerializeField] private Button exitButton;
    [Min(0f)]
    [SerializeField] private float fadeDuration = 0.65f;
    [SerializeField] private Ease fadeEase = Ease.InOutCubic;

    [Header("Fixed Experiment View")]
    [SerializeField] private ProgressControl progressControl;
    [SerializeField] private CinemachineVirtualCamera experimentVirtualCamera;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private XROrigin xrOrigin;
    [Tooltip("Camera/world pose used after the screen is fully black. The XR origin is moved so the player camera reaches this pose.")]
    [SerializeField] private Transform experimentCameraPose;
    [Tooltip("Fallback used when no Cinemachine virtual camera is assigned.")]
    [SerializeField] private Transform manualFixedCameraPose;
    [SerializeField] private bool restorePlayerPoseOnExit = true;

    [Header("NA Input")]
    [SerializeField] private Slider naSlider;
    [SerializeField] private InputActionReference sliderAxisAction;
    [SerializeField] private InputActionAsset fallbackInputActions;
    [SerializeField] private string fallbackActionMapName = "Roaming";
    [SerializeField] private string fallbackActionName = "ChangeFocusOrChangeLIght";
    [Range(0f, 0.95f)]
    [SerializeField] private float joystickDeadZone = 0.2f;
    [Min(0f)]
    [SerializeField] private float sliderSpeed = 0.45f;

    [Header("NA Formula")]
    [SerializeField] private float minNA = 0.03f;
    [SerializeField] private float maxNA = 0.95f;
    [SerializeField] private float initialNA = 0.16f;
    [Min(0.0001f)]
    [SerializeField] private float refractiveIndex = 1.0f;
    [Min(0.01f)]
    [SerializeField] private float coneHeight = 1.2f;
    [Min(8)]
    [SerializeField] private int coneSegments = 64;
    [Header("Text Output")]
    [SerializeField] private TextMeshProUGUI currentNaText;
    [SerializeField] private TextMeshProUGUI thetaText;
    [SerializeField] private TextMeshProUGUI fullApertureText;
    [SerializeField] private TextMeshProUGUI magnificationText;
    [SerializeField] private TextMeshProUGUI normalizedNaText;
    [SerializeField] private TextMeshProUGUI formulaText;
    [SerializeField] private TextMeshProUGUI depthToleranceText;

    [Header("Light Cone Visual")]
    [SerializeField] private MeshFilter lightConeMeshFilter;
    [SerializeField] private MeshRenderer lightConeRenderer;
    [SerializeField] private Color lowNaConeColor = new Color(1f, 0.78f, 0f, 0.38f);
    [SerializeField] private Color highNaConeColor = new Color(1f, 0.92f, 0.12f, 0.72f);

    [Header("Visual Response")]
    [SerializeField] private NumericalApertureDiagramView diagramView;
    [SerializeField] private Transform objectiveLensVisual;
    [SerializeField] private Vector3 objectiveMovementAxis = Vector3.up;
    [Min(0f)]
    [SerializeField] private float objectiveMovementDistance = 0.18f;
    [SerializeField] private Light brightnessLight;
    [Min(0f)]
    [SerializeField] private float minLightIntensity = 0.4f;
    [Min(0f)]
    [SerializeField] private float maxLightIntensity = 2.0f;
    [SerializeField] private CanvasGroup sharpImageLayer;
    [SerializeField] private CanvasGroup blurredImageLayer;

    [Header("Events")]
    [SerializeField] private UnityEvent onExperimentStarted = new UnityEvent();
    [SerializeField] private UnityEvent onExperimentEnded = new UnityEvent();
    [SerializeField] private FloatEvent onNormalizedNAChanged = new FloatEvent();

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    [Header("Runtime Fallback UI")]
    [SerializeField] private bool createRuntimeFallbackUi = true;
    [SerializeField] private int runtimeCanvasSortingOrder = 900;
    [SerializeField] private Color runtimePanelColor = new Color(0.04f, 0.05f, 0.06f, 0.92f);
    [SerializeField] private Color runtimeAccentColor = new Color(1f, 0.78f, 0.12f, 0.95f);

    private Coroutine transitionRoutine;
    private InputAction resolvedSliderAction;
    private Mesh coneMesh;
    private MaterialPropertyBlock conePropertyBlock;
    private bool isExperimentActive;
    private bool isTransitioning;
    private bool exitButtonBound;
    private bool sliderBound;
    private bool ignoreSliderCallback;
    private bool gameplayInputBlocked;
    private bool rightRayUiInteractionCached;
    private bool originalRightRayUiInteraction;
    private bool sliderReadWarningShown;
    private bool runtimeFallbackUiCreated;
    private float currentNA;
    private int currentProfileIndex;
    private Canvas runtimeFadeCanvas;
    private Canvas runtimeExperimentCanvas;
    private readonly List<InputActionMap> blockedInputMaps = new List<InputActionMap>();
    private readonly List<bool> blockedInputMapWasEnabled = new List<bool>();

    private Transform cachedOriginTransform;
    private Vector3 cachedOriginPosition;
    private Quaternion cachedOriginRotation;
    private bool hasCachedOriginPose;

    private CinemachineVirtualCamera cachedPreviousVirtualCamera;
    private bool cachedWasFreeView;
    private bool hasCachedProgressView;

    private CinemachineBrain cachedBrain;
    private TrackedPoseDriver cachedTrackedPoseDriver;
    private bool cachedBrainEnabled;
    private bool cachedTrackedPoseDriverEnabled;
    private Vector3 cachedCameraPosition;
    private Quaternion cachedCameraRotation;
    private bool hasCachedManualCameraPose;
    private bool samplePreviewStateCached;
    private bool samplePreviewWasActive;
    private GameObject samplePreviewRoot;

    private Vector3 objectiveBaseLocalPosition;
    private bool hasObjectiveBaseLocalPosition;

    public bool IsExperimentActive => isExperimentActive;
    public bool IsTransitioning => isTransitioning;
    public float CurrentNA => currentNA;
    public Slider AssistantSlider => naSlider;
    public Button AssistantExit => exitButton;
    public float NormalizedNA => CalculateNormalizedNA(currentNA);
    private void Awake()
    {
        EnsureReferences();
        EnsureRuntimeFallbackUi();
        BindUi();
        CacheObjectiveBasePose();
        if (diagramView == null)
        {
            PrepareConeMesh();
        }
        ApplyInitialUiState();
    }

    private void OnEnable()
    {
        EnsureReferences();
        EnsureRuntimeFallbackUi();
        BindUi();
    }

    private void OnDisable()
    {
        StopActiveTransition();
        CleanupExperimentState(immediate: true);
        UnbindUi();
    }

    private void OnDestroy()
    {
        if (coneMesh != null)
        {
            Destroy(coneMesh);
        }
    }

    private void Update()
    {
        if (!isExperimentActive || isTransitioning)
        {
            return;
        }

        HandleJoystickSliderInput();
    }

    public void StartExperiment()
    {
        TryStartExperiment();
    }

    public bool TryStartExperiment()
    {
        if (isExperimentActive || isTransitioning)
        {
            DebugLogWarning("Numerical aperture experiment is already active or transitioning.");
            return false;
        }

        EnsureReferences();
        EnsureRuntimeFallbackUi();
        BindUi();
        if (!CanStartExperiment(out string blockedReason))
        {
            DebugLogWarning($"Numerical aperture experiment cannot start: {blockedReason}");
            return false;
        }

        transitionRoutine = StartCoroutine(StartExperimentRoutine());
        return true;
    }

    public void ConfigureRuntimeContext(
        SuperAssemblyPartSelectionController selection,
        ModelExploder exploder,
        XRRayInteractor rayInteractor)
    {
        if (selection != null)
        {
            selectionController = selection;
        }

        if (exploder != null)
        {
            modelExploder = exploder;
        }

        if (rayInteractor != null)
        {
            rightRayInteractor = rayInteractor;
        }
    }

    public void ExitExperiment()
    {
        if ((!isExperimentActive && !isTransitioning) || transitionRoutine != null)
        {
            return;
        }

        transitionRoutine = StartCoroutine(ExitExperimentRoutine());
    }

    public void SetNAFromExternal(float value)
    {
        SetNA(value);
    }

    private IEnumerator StartExperimentRoutine()
    {
        isTransitioning = true;
        selectionController?.SetExternalInteractionLocked(true);
        SetGameplayInputBlocked(true);
        CacheAndEnableRightRayUiInteraction();
        CacheAndHideSamplePreview();
        SetFadeCanvas(0f, true);

        yield return FadeTo(1f);

        selectionController?.SetSelectionUiTemporarilyHidden(true);
        CacheViewState();
        SwitchToExperimentView();
        RefreshRuntimeCanvasCameras();
        SetExperimentRootVisible(true);
        ApplySliderSettings();

        yield return FadeTo(0f);

        isExperimentActive = true;
        isTransitioning = false;
        transitionRoutine = null;
        onExperimentStarted?.Invoke();
        DebugLog("Numerical aperture experiment started.");
    }

    private IEnumerator ExitExperimentRoutine()
    {
        isTransitioning = true;
        SetFadeCanvas(0f, true);

        yield return FadeTo(1f);

        SetExperimentRootVisible(false);
        RestoreViewState();
        selectionController?.SetSelectionUiTemporarilyHidden(false);
        RestoreSamplePreview();

        yield return FadeTo(0f);

        CleanupExperimentState(immediate: false);
        onExperimentEnded?.Invoke();
        DebugLog("Numerical aperture experiment ended.");
    }

    private bool CanStartExperiment(out string blockedReason)
    {
        if (selectionController != null && !selectionController.IsSelectionActive)
        {
            blockedReason = "no super-assembly part is currently selected.";
            return false;
        }

        if (modelExploder != null && (!modelExploder.IsExploded || modelExploder.IsAnimating))
        {
            blockedReason =
                $"model exploder is not ready. IsExploded={modelExploder.IsExploded}, IsAnimating={modelExploder.IsAnimating}.";
            return false;
        }

        blockedReason = string.Empty;
        return true;
    }

    private void CleanupExperimentState(bool immediate)
    {
        if (!isExperimentActive && !isTransitioning && !gameplayInputBlocked)
        {
            return;
        }

        if (immediate)
        {
            SetExperimentRootVisible(false);
            RestoreViewState();
            selectionController?.SetSelectionUiTemporarilyHidden(false);
            RestoreSamplePreview();
            SetFadeCanvas(0f, false);
        }

        selectionController?.SetExternalInteractionLocked(false);
        RestoreRightRayUiInteraction();
        SetGameplayInputBlocked(false);
        isExperimentActive = false;
        isTransitioning = false;
        transitionRoutine = null;
    }

    private void StopActiveTransition()
    {
        if (transitionRoutine == null)
        {
            return;
        }

        StopCoroutine(transitionRoutine);
        transitionRoutine = null;
    }

    private void EnsureReferences()
    {
        if (selectionController == null)
        {
            selectionController = SuperAssemblyPartSelectionController.Instance;
        }

        if (modelExploder == null)
        {
            modelExploder = FindObjectOfType<ModelExploder>();
        }

        if (interactor == null)
        {
            interactor = Interactor.Instance;
        }

        if (moveController == null)
        {
            moveController = Move.Instance;
        }

        if (progressControl == null)
        {
            progressControl = ProgressControl.Instance;
        }

        if (rightRayInteractor == null)
        {
            rightRayInteractor = FindObjectOfType<XRRayInteractor>();
        }

        if (sampleInteraction == null)
        {
            sampleInteraction = FindObjectOfType<InteractWithSamples>();
        }

        if (targetCamera == null)
        {
            targetCamera = ResolveTargetCamera();
        }

        if (xrOrigin == null)
        {
            xrOrigin = FindObjectOfType<XROrigin>();
        }

        if (lightConeRenderer == null && lightConeMeshFilter != null)
        {
            lightConeRenderer = lightConeMeshFilter.GetComponent<MeshRenderer>();
        }

        if (fallbackInputActions == null && interactor != null)
        {
            fallbackInputActions = interactor.inputActionAsset;
        }

        RefreshRuntimeCanvasCameras();
    }

    private void EnsureRuntimeFallbackUi()
    {
        if (!createRuntimeFallbackUi || runtimeFallbackUiCreated)
        {
            return;
        }

        if (fadeCanvasGroup != null &&
            experimentRoot != null &&
            experimentCanvasGroup != null &&
            exitButton != null &&
            naSlider != null &&
            currentNaText != null)
        {
            return;
        }

        CreateRuntimeFadeCanvasIfNeeded();
        CreateRuntimeExperimentCanvasIfNeeded();
        runtimeFallbackUiCreated = true;
    }

    private void CreateRuntimeFadeCanvasIfNeeded()
    {
        if (fadeCanvasGroup != null)
        {
            return;
        }

        GameObject fadeRoot = new GameObject("NumericalApertureExperiment_FadeCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(CanvasGroup),
            typeof(Image));
        fadeRoot.transform.SetParent(transform, false);

        Canvas canvas = fadeRoot.GetComponent<Canvas>();
        canvas.sortingOrder = runtimeCanvasSortingOrder + 1;
        runtimeFadeCanvas = canvas;
        ConfigureRuntimeCanvas(canvas);

        RectTransform rectTransform = fadeRoot.GetComponent<RectTransform>();
        StretchToParent(rectTransform);

        Image image = fadeRoot.GetComponent<Image>();
        image.color = Color.black;

        fadeCanvasGroup = fadeRoot.GetComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;
        fadeCanvasGroup.interactable = false;
        fadeRoot.SetActive(false);
    }

    private void CreateRuntimeExperimentCanvasIfNeeded()
    {
        Transform rootTransform;
        if (experimentRoot == null)
        {
            GameObject root = new GameObject("NumericalApertureExperiment_RuntimeCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(CanvasGroup));
            root.transform.SetParent(transform, false);

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.sortingOrder = runtimeCanvasSortingOrder;
            runtimeExperimentCanvas = canvas;
            ConfigureRuntimeCanvas(canvas);

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform rootRect = root.GetComponent<RectTransform>();
            StretchToParent(rootRect);

            experimentRoot = root;
            experimentCanvasGroup = root.GetComponent<CanvasGroup>();
            rootTransform = root.transform;
        }
        else
        {
            rootTransform = experimentRoot.transform;
            runtimeExperimentCanvas = experimentRoot.GetComponent<Canvas>();
            if (runtimeExperimentCanvas == null)
            {
                runtimeExperimentCanvas = experimentRoot.AddComponent<Canvas>();
                runtimeExperimentCanvas.sortingOrder = runtimeCanvasSortingOrder;
                ConfigureRuntimeCanvas(runtimeExperimentCanvas);

                if (experimentRoot.GetComponent<CanvasScaler>() == null)
                {
                    CanvasScaler scaler = experimentRoot.AddComponent<CanvasScaler>();
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1920f, 1080f);
                    scaler.matchWidthOrHeight = 0.5f;
                }

                if (experimentRoot.GetComponent<GraphicRaycaster>() == null)
                {
                    experimentRoot.AddComponent<GraphicRaycaster>();
                }
            }

            if (experimentCanvasGroup == null)
            {
                experimentCanvasGroup = experimentRoot.GetComponent<CanvasGroup>();
                if (experimentCanvasGroup == null)
                {
                    experimentCanvasGroup = experimentRoot.AddComponent<CanvasGroup>();
                }
            }
        }

        Image panel = CreateImage(rootTransform, "RuntimePanel",
            new Vector2(0.04f, 0.06f),
            new Vector2(0.96f, 0.94f),
            runtimePanelColor);

        TextMeshProUGUI title = CreateText(panel.transform, "Title",
            "Interactive Numerical Aperture Simulation",
            new Vector2(0.06f, 0.84f),
            new Vector2(0.62f, 0.93f),
            44f,
            TextAlignmentOptions.Left,
            FontStyles.Bold);
        title.color = Color.white;

        TextMeshProUGUI explanation = CreateText(panel.transform, "Explanation",
            "Adjust NA to observe how the collected light cone, angular aperture, brightness, sharpness, and depth tolerance change.",
            new Vector2(0.06f, 0.73f),
            new Vector2(0.58f, 0.82f),
            24f,
            TextAlignmentOptions.Left,
            FontStyles.Normal);
        explanation.color = new Color(0.86f, 0.9f, 0.94f, 1f);

        formulaText = CreateText(panel.transform, "FormulaText",
            "NA = n * sin(theta)",
            new Vector2(0.62f, 0.76f),
            new Vector2(0.92f, 0.86f),
            28f,
            TextAlignmentOptions.Left,
            FontStyles.Bold);
        formulaText.color = Color.white;

        currentNaText = CreateValueRow(panel.transform, "NA", "0.16", 0.65f);
        thetaText = CreateValueRow(panel.transform, "Half Angle", "9.2 deg", 0.56f);
        fullApertureText = CreateValueRow(panel.transform, "Full Aperture", "18.4 deg", 0.47f);
        magnificationText = CreateValueRow(panel.transform, "Approx. Magnification", "5x", 0.38f);
        normalizedNaText = CreateValueRow(panel.transform, "normalizedNA", "0.00", 0.29f);
        depthToleranceText = CreateValueRow(panel.transform, "Depth Tolerance", "0.00", 0.20f);

        naSlider = naSlider == null ? CreateRuntimeSlider(panel.transform) : naSlider;
        exitButton = exitButton == null ? CreateRuntimeExitButton(panel.transform) : exitButton;
    }

    private void RefreshRuntimeCanvasCameras()
    {
        ConfigureRuntimeCanvas(runtimeFadeCanvas);
        ConfigureRuntimeCanvas(runtimeExperimentCanvas);
    }

    private void ConfigureRuntimeCanvas(Canvas canvas)
    {
        if (canvas == null)
        {
            return;
        }

        Camera camera = targetCamera != null ? targetCamera : ResolveTargetCamera();
        if (camera == null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            return;
        }

        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 1.15f;
    }

    private TextMeshProUGUI CreateValueRow(Transform parent, string label, string initialValue, float centerY)
    {
        TextMeshProUGUI labelText = CreateText(parent, label + "Label",
            label,
            new Vector2(0.62f, centerY - 0.03f),
            new Vector2(0.78f, centerY + 0.03f),
            23f,
            TextAlignmentOptions.Left,
            FontStyles.Normal);
        labelText.color = new Color(0.78f, 0.82f, 0.87f, 1f);

        TextMeshProUGUI valueText = CreateText(parent, label + "Value",
            initialValue,
            new Vector2(0.80f, centerY - 0.03f),
            new Vector2(0.92f, centerY + 0.03f),
            24f,
            TextAlignmentOptions.Right,
            FontStyles.Bold);
        valueText.color = Color.white;
        return valueText;
    }

    private Slider CreateRuntimeSlider(Transform parent)
    {
        TextMeshProUGUI sliderLabel = CreateText(parent, "SliderLabel",
            "Numerical Aperture",
            new Vector2(0.06f, 0.15f),
            new Vector2(0.42f, 0.22f),
            28f,
            TextAlignmentOptions.Left,
            FontStyles.Bold);
        sliderLabel.color = Color.white;

        GameObject sliderObject = new GameObject("NASlider", typeof(RectTransform), typeof(Slider));
        sliderObject.transform.SetParent(parent, false);
        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        SetAnchors(sliderRect, new Vector2(0.06f, 0.08f), new Vector2(0.56f, 0.13f));

        Image background = CreateImage(sliderObject.transform, "Background", Vector2.zero, Vector2.one,
            new Color(1f, 1f, 1f, 0.18f));

        Image fill = CreateImage(sliderObject.transform, "Fill", new Vector2(0f, 0.35f), new Vector2(1f, 0.65f),
            runtimeAccentColor);
        RectTransform fillRect = fill.GetComponent<RectTransform>();

        Image handle = CreateImage(sliderObject.transform, "Handle", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            Color.white);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(34f, 34f);

        Slider slider = sliderObject.GetComponent<Slider>();
        slider.targetGraphic = handle;
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.direction = Slider.Direction.LeftToRight;
        slider.transition = Selectable.Transition.ColorTint;
        background.raycastTarget = true;
        return slider;
    }

    private Button CreateRuntimeExitButton(Transform parent)
    {
        Image image = CreateImage(parent, "ExitButton", new Vector2(0.86f, 0.86f), new Vector2(0.94f, 0.93f),
            new Color(0.9f, 0.24f, 0.18f, 0.92f));
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        TextMeshProUGUI text = CreateText(image.transform, "Text", "Exit",
            Vector2.zero,
            Vector2.one,
            25f,
            TextAlignmentOptions.Center,
            FontStyles.Bold);
        text.color = Color.white;
        return button;
    }

    private Image CreateImage(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        gameObject.transform.SetParent(parent, false);
        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        SetAnchors(rectTransform, anchorMin, anchorMax);

        Image image = gameObject.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private TextMeshProUGUI CreateText(
        Transform parent,
        string name,
        string text,
        Vector2 anchorMin,
        Vector2 anchorMax,
        float fontSize,
        TextAlignmentOptions alignment,
        FontStyles fontStyle)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        gameObject.transform.SetParent(parent, false);
        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        SetAnchors(rectTransform, anchorMin, anchorMax);

        TextMeshProUGUI textComponent = gameObject.GetComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.alignment = alignment;
        textComponent.fontStyle = fontStyle;
        textComponent.enableWordWrapping = true;
        return textComponent;
    }

    private void SetAnchors(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax)
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;
    }

    private void StretchToParent(RectTransform rectTransform)
    {
        SetAnchors(rectTransform, Vector2.zero, Vector2.one);
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
        }
    }

    private Camera ResolveTargetCamera()
    {
        if (progressControl != null &&
            progressControl.cinemachineBrain != null &&
            progressControl.cinemachineBrain.OutputCamera != null)
        {
            return progressControl.cinemachineBrain.OutputCamera;
        }

        return Camera.main;
    }

    private void BindUi()
    {
        if (exitButton != null && !exitButtonBound)
        {
            exitButton.onClick.AddListener(ExitExperiment);
            exitButtonBound = true;
        }

        if (naSlider != null && !sliderBound)
        {
            naSlider.onValueChanged.AddListener(HandleSliderValueChanged);
            sliderBound = true;
        }
    }

    private void UnbindUi()
    {
        if (exitButton != null && exitButtonBound)
        {
            exitButton.onClick.RemoveListener(ExitExperiment);
            exitButtonBound = false;
        }

        if (naSlider != null && sliderBound)
        {
            naSlider.onValueChanged.RemoveListener(HandleSliderValueChanged);
            sliderBound = false;
        }
    }

    private void ApplyInitialUiState()
    {
        SetExperimentRootVisible(false);
        SetFadeCanvas(0f, false);
        ApplySliderSettings();
    }

    private void ApplySliderSettings()
    {
        minNA = ApertureProfiles[0].NA;
        maxNA = ApertureProfiles[ApertureProfiles.Length - 1].NA;
        if (naSlider != null)
        {
            ignoreSliderCallback = true;
            naSlider.wholeNumbers = false;
            naSlider.minValue = minNA;
            naSlider.maxValue = maxNA;
            naSlider.SetValueWithoutNotify(Mathf.Clamp(initialNA, minNA, maxNA));
            ignoreSliderCallback = false;
            ApplyNA(initialNA, false);
            return;
        }

        ApplyNA(initialNA, false);
    }

    private void HandleSliderValueChanged(float value)
    {
        if (ignoreSliderCallback)
        {
            return;
        }

        ApplyNA(value, false);
    }

    private void HandleJoystickSliderInput()
    {
        if (VRMicroscope.Assistant.AssistantChatPanel.BlocksGameplay) return;
        InputAction action = ResolveSliderAction();
        if (action == null || naSlider == null)
        {
            return;
        }

        if (!action.enabled)
        {
            action.Enable();
        }

        Vector2 input;
        try
        {
            input = action.ReadValue<Vector2>();
        }
        catch (Exception exception)
        {
            if (!sliderReadWarningShown)
            {
                sliderReadWarningShown = true;
                DebugLogWarning($"Failed to read slider axis input: {exception.Message}");
            }

            return;
        }

        if (Mathf.Abs(input.x) < joystickDeadZone)
        {
            return;
        }

        naSlider.value = Mathf.Clamp(
            naSlider.value + input.x * sliderSpeed * Time.deltaTime,
            minNA,
            maxNA);
    }

    private InputAction ResolveSliderAction()
    {
        if (resolvedSliderAction != null)
        {
            return resolvedSliderAction;
        }

        if (sliderAxisAction != null)
        {
            resolvedSliderAction = sliderAxisAction.action;
        }

        if (resolvedSliderAction != null)
        {
            return resolvedSliderAction;
        }

        if (fallbackInputActions == null)
        {
            return null;
        }

        InputActionMap map = fallbackInputActions.FindActionMap(fallbackActionMapName, false);
        resolvedSliderAction = map != null ? map.FindAction(fallbackActionName, false) : null;
        return resolvedSliderAction;
    }

    private void SetNA(float value)
    {
        ApplyNA(value, true);
    }

    private void ApplyNA(float value, bool synchronizeSlider)
    {
        currentNA = Mathf.Clamp(value, minNA, maxNA);
        currentProfileIndex = FindNearestProfileIndex(currentNA);

        if (synchronizeSlider && naSlider != null && !Mathf.Approximately(naSlider.value, currentNA))
        {
            ignoreSliderCallback = true;
            naSlider.SetValueWithoutNotify(currentNA);
            ignoreSliderCallback = false;
        }

        float safeN = Mathf.Max(0.0001f, refractiveIndex);
        float thetaRadians = Mathf.Asin(Mathf.Clamp(currentNA / safeN, -1f, 1f));
        float thetaDegrees = thetaRadians * Mathf.Rad2Deg;
        float fullApertureDegrees = thetaDegrees * 2f;
        float radius = coneHeight * Mathf.Tan(thetaRadians);
        float normalized = CalculateNormalizedNA(currentNA);
        float depthTolerance = 1f - normalized;
        float approximateMagnification = CalculateApproximateMagnification(currentNA);

        UpdateTexts(thetaDegrees, fullApertureDegrees, approximateMagnification, normalized, depthTolerance);
        if (diagramView != null)
        {
            diagramView.ApplyContinuous(normalized, thetaDegrees);
        }
        else
        {
            UpdateCone(radius, normalized);
        }
        UpdateVisualResponse(normalized, depthTolerance);
        onNormalizedNAChanged?.Invoke(normalized);
    }

    private float CalculateApproximateMagnification(float na)
    {
        if (na <= ApertureProfiles[0].NA)
        {
            return ApertureProfiles[0].Magnification;
        }

        for (int i = 0; i < ApertureProfiles.Length - 1; i++)
        {
            ApertureProfile lower = ApertureProfiles[i];
            ApertureProfile upper = ApertureProfiles[i + 1];
            if (na > upper.NA)
            {
                continue;
            }

            float t = Mathf.InverseLerp(lower.NA, upper.NA, na);
            return Mathf.Lerp(lower.Magnification, upper.Magnification, t);
        }

        return ApertureProfiles[ApertureProfiles.Length - 1].Magnification;
    }

    private int FindNearestProfileIndex(float na)
    {
        int nearestIndex = 0;
        float nearestDistance = Mathf.Abs(na - ApertureProfiles[0].NA);
        for (int i = 1; i < ApertureProfiles.Length; i++)
        {
            float distance = Mathf.Abs(na - ApertureProfiles[i].NA);
            if (distance >= nearestDistance)
            {
                continue;
            }

            nearestDistance = distance;
            nearestIndex = i;
        }

        return nearestIndex;
    }

    private float CalculateNormalizedNA(float na)
    {
        float range = Mathf.Max(0.0001f, maxNA - minNA);
        return Mathf.Clamp01((na - minNA) / range);
    }

    private void UpdateTexts(
        float thetaDegrees,
        float fullApertureDegrees,
        float approximateMagnification,
        float normalized,
        float depthTolerance)
    {
        if (currentNaText != null)
        {
            currentNaText.text = currentNA.ToString("0.###");
        }

        if (thetaText != null)
        {
            thetaText.text = thetaDegrees.ToString("0.0") + " deg";
        }

        if (fullApertureText != null)
        {
            fullApertureText.text = fullApertureDegrees.ToString("0.0") + " deg";
        }

        if (magnificationText != null)
        {
            magnificationText.text = approximateMagnification.ToString("0.##") + "x";
        }

        if (normalizedNaText != null)
        {
            normalizedNaText.text = normalized.ToString("0.00");
        }

        if (formulaText != null)
        {
            formulaText.text =
                $"NA = n * sin(theta)\n\n" +
                $"{currentNA:0.###} = {refractiveIndex:0.00} * sin({thetaDegrees:0.0} deg)\n\n" +
                "n = refractive index (1.00 for air)\n" +
                "theta = half angular aperture";
        }

        if (depthToleranceText != null)
        {
            depthToleranceText.text = depthTolerance.ToString("0.00");
        }
    }

    private void PrepareConeMesh()
    {
        if (lightConeMeshFilter == null)
        {
            return;
        }

        if (coneMesh == null)
        {
            coneMesh = new Mesh();
            coneMesh.name = "Numerical Aperture Light Cone";
        }

        lightConeMeshFilter.sharedMesh = coneMesh;

        if (lightConeRenderer != null && lightConeRenderer.sharedMaterial == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            if (shader != null)
            {
                Material material = new Material(shader)
                {
                    name = "Numerical Aperture Cone Material"
                };
                if (material.HasProperty("_Surface"))
                {
                    material.SetFloat("_Surface", 1f);
                }

                if (material.HasProperty("_Blend"))
                {
                    material.SetFloat("_Blend", 0f);
                }

                lightConeRenderer.sharedMaterial = material;
            }
        }
    }

    private void UpdateCone(float radius, float normalized)
    {
        PrepareConeMesh();
        if (coneMesh != null)
        {
            RebuildConeMesh(radius);
        }

        if (lightConeRenderer == null)
        {
            return;
        }

        if (conePropertyBlock == null)
        {
            conePropertyBlock = new MaterialPropertyBlock();
        }

        Color color = Color.Lerp(lowNaConeColor, highNaConeColor, normalized);
        lightConeRenderer.GetPropertyBlock(conePropertyBlock);
        conePropertyBlock.SetColor("_Color", color);
        conePropertyBlock.SetColor("_BaseColor", color);
        lightConeRenderer.SetPropertyBlock(conePropertyBlock);
    }

    private void RebuildConeMesh(float radius)
    {
        int segments = Mathf.Max(8, coneSegments);
        Vector3[] vertices = new Vector3[segments + 2];
        int[] triangles = new int[segments * 6];

        vertices[0] = Vector3.zero;
        for (int i = 0; i < segments; i++)
        {
            float angle = Mathf.PI * 2f * i / segments;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle) * radius, coneHeight, Mathf.Sin(angle) * radius);
        }

        int centerIndex = segments + 1;
        vertices[centerIndex] = new Vector3(0f, coneHeight, 0f);

        for (int i = 0; i < segments; i++)
        {
            int current = i + 1;
            int next = i == segments - 1 ? 1 : i + 2;
            int sideIndex = i * 3;
            triangles[sideIndex] = 0;
            triangles[sideIndex + 1] = current;
            triangles[sideIndex + 2] = next;

            int baseIndex = segments * 3 + i * 3;
            triangles[baseIndex] = centerIndex;
            triangles[baseIndex + 1] = next;
            triangles[baseIndex + 2] = current;
        }

        coneMesh.Clear();
        coneMesh.vertices = vertices;
        coneMesh.triangles = triangles;
        coneMesh.RecalculateNormals();
        coneMesh.RecalculateBounds();
    }

    private void CacheObjectiveBasePose()
    {
        if (objectiveLensVisual == null || hasObjectiveBaseLocalPosition)
        {
            return;
        }

        objectiveBaseLocalPosition = objectiveLensVisual.localPosition;
        hasObjectiveBaseLocalPosition = true;
    }

    private void UpdateVisualResponse(float normalized, float depthTolerance)
    {
        CacheObjectiveBasePose();
        if (diagramView == null && objectiveLensVisual != null)
        {
            Vector3 axis = objectiveMovementAxis.sqrMagnitude > 0.000001f
                ? objectiveMovementAxis.normalized
                : Vector3.up;
            objectiveLensVisual.localPosition =
                objectiveBaseLocalPosition + axis * Mathf.Lerp(-objectiveMovementDistance * 0.5f,
                    objectiveMovementDistance * 0.5f, normalized);
        }

        if (brightnessLight != null)
        {
            brightnessLight.intensity = Mathf.Lerp(minLightIntensity, maxLightIntensity, normalized);
        }

        if (sharpImageLayer != null)
        {
            sharpImageLayer.alpha = normalized;
        }

        if (blurredImageLayer != null)
        {
            blurredImageLayer.alpha = depthTolerance;
        }
    }

    private void SetExperimentRootVisible(bool visible)
    {
        if (experimentRoot != null && experimentRoot != gameObject)
        {
            if (visible)
            {
                experimentRoot.transform.localScale = Vector3.one;
            }

            experimentRoot.SetActive(visible);
        }

        if (experimentCanvasGroup == null)
        {
            if (diagramView == null && lightConeMeshFilter != null)
            {
                lightConeMeshFilter.gameObject.SetActive(visible);
            }

            return;
        }

        experimentCanvasGroup.alpha = visible ? 1f : 0f;
        experimentCanvasGroup.blocksRaycasts = visible;
        experimentCanvasGroup.interactable = visible;

        if (diagramView == null && lightConeMeshFilter != null)
        {
            lightConeMeshFilter.gameObject.SetActive(visible);
        }
    }

    private void SetFadeCanvas(float alpha, bool blockRaycasts)
    {
        if (fadeCanvasGroup == null)
        {
            return;
        }

        fadeCanvasGroup.gameObject.SetActive(blockRaycasts || alpha > 0.001f);
        fadeCanvasGroup.alpha = alpha;
        fadeCanvasGroup.blocksRaycasts = blockRaycasts;
        fadeCanvasGroup.interactable = blockRaycasts;
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        if (fadeCanvasGroup == null)
        {
            yield break;
        }

        fadeCanvasGroup.gameObject.SetActive(true);
        fadeCanvasGroup.blocksRaycasts = true;
        fadeCanvasGroup.interactable = true;

        if (fadeDuration <= 0f)
        {
            fadeCanvasGroup.alpha = targetAlpha;
        }
        else
        {
            Tween tween = fadeCanvasGroup
                .DOFade(targetAlpha, fadeDuration)
                .SetEase(fadeEase)
                .SetUpdate(true);
            yield return tween.WaitForCompletion();
        }

        if (targetAlpha <= 0.001f)
        {
            SetFadeCanvas(0f, false);
        }
    }

    private void SetGameplayInputBlocked(bool blocked)
    {
        if (gameplayInputBlocked == blocked)
        {
            return;
        }

        if (interactor == null)
        {
            interactor = Interactor.Instance;
        }

        if (moveController == null)
        {
            moveController = Move.Instance;
        }

        gameplayInputBlocked = blocked;
        if (blocked)
        {
            interactor?.SetForceTutorialGameplayInputBlocked(true);
            moveController?.SetGripMovementBlocked(true);
            SetGlobalInputMapsBlocked(true);
            return;
        }

        SetGlobalInputMapsBlocked(false);
        interactor?.SetForceTutorialGameplayInputBlocked(false);
        moveController?.SetGripMovementBlocked(false);
    }

    private void SetGlobalInputMapsBlocked(bool blocked)
    {
        if (!blockGlobalInputMaps)
        {
            return;
        }

        InputActionAsset inputActions = fallbackInputActions;
        if (inputActions == null && interactor != null)
        {
            inputActions = interactor.inputActionAsset;
        }

        if (inputActions == null || globalInputMapsToBlock == null)
        {
            return;
        }

        if (blocked)
        {
            blockedInputMaps.Clear();
            blockedInputMapWasEnabled.Clear();

            for (int i = 0; i < globalInputMapsToBlock.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(globalInputMapsToBlock[i]))
                {
                    continue;
                }

                InputActionMap map = inputActions.FindActionMap(globalInputMapsToBlock[i], false);
                if (map == null)
                {
                    continue;
                }

                blockedInputMaps.Add(map);
                blockedInputMapWasEnabled.Add(map.enabled);
                map.Disable();
            }

            return;
        }

        for (int i = 0; i < blockedInputMaps.Count; i++)
        {
            InputActionMap map = blockedInputMaps[i];
            if (map == null)
            {
                continue;
            }

            if (i < blockedInputMapWasEnabled.Count && blockedInputMapWasEnabled[i])
            {
                map.Enable();
            }
            else
            {
                map.Disable();
            }
        }

        blockedInputMaps.Clear();
        blockedInputMapWasEnabled.Clear();
    }

    private void CacheAndEnableRightRayUiInteraction()
    {
        if (rightRayInteractor == null)
        {
            return;
        }

        if (!rightRayUiInteractionCached)
        {
            originalRightRayUiInteraction = rightRayInteractor.enableUIInteraction;
            rightRayUiInteractionCached = true;
        }

        rightRayInteractor.enableUIInteraction = true;
    }

    private void RestoreRightRayUiInteraction()
    {
        if (!rightRayUiInteractionCached || rightRayInteractor == null)
        {
            return;
        }

        rightRayInteractor.enableUIInteraction = originalRightRayUiInteraction;
        rightRayUiInteractionCached = false;
    }

    private void CacheAndHideSamplePreview()
    {
        if (sampleInteraction == null || sampleInteraction.Inventory == null)
        {
            return;
        }

        Transform inventoryTransform = sampleInteraction.Inventory.transform;
        GameObject previewRoot = inventoryTransform.parent != null
            ? inventoryTransform.parent.gameObject
            : inventoryTransform.gameObject;

        if (!samplePreviewStateCached)
        {
            samplePreviewRoot = previewRoot;
            samplePreviewWasActive = previewRoot.activeSelf;
            samplePreviewStateCached = true;
        }

        previewRoot.SetActive(false);
    }

    private void RestoreSamplePreview()
    {
        if (!samplePreviewStateCached || samplePreviewRoot == null)
        {
            return;
        }

        samplePreviewRoot.SetActive(samplePreviewWasActive);
        samplePreviewStateCached = false;
        samplePreviewRoot = null;
    }

    private void CacheViewState()
    {
        if (progressControl == null)
        {
            progressControl = ProgressControl.Instance;
        }

        if (progressControl != null)
        {
            cachedPreviousVirtualCamera = progressControl.CurrentCinema;
            cachedWasFreeView = ProgressControl.isFreeView;
            hasCachedProgressView = true;
        }

        targetCamera = targetCamera != null ? targetCamera : ResolveTargetCamera();
        if (targetCamera != null)
        {
            cachedBrain = targetCamera.GetComponent<CinemachineBrain>();
            cachedTrackedPoseDriver = targetCamera.GetComponent<TrackedPoseDriver>();
            cachedBrainEnabled = cachedBrain != null && cachedBrain.enabled;
            cachedTrackedPoseDriverEnabled = cachedTrackedPoseDriver != null && cachedTrackedPoseDriver.enabled;
            cachedCameraPosition = targetCamera.transform.position;
            cachedCameraRotation = targetCamera.transform.rotation;
            hasCachedManualCameraPose = true;
        }

        if (!restorePlayerPoseOnExit)
        {
            return;
        }

        if (xrOrigin == null)
        {
            xrOrigin = FindObjectOfType<XROrigin>();
        }

        cachedOriginTransform = GetXrOriginTransform();
        if (cachedOriginTransform == null)
        {
            return;
        }

        cachedOriginPosition = cachedOriginTransform.position;
        cachedOriginRotation = cachedOriginTransform.rotation;
        hasCachedOriginPose = true;
    }

    private void SwitchToExperimentView()
    {
        MoveXrOriginToExperimentPose();

        if (progressControl != null && experimentVirtualCamera != null)
        {
            progressControl.SwitchToPresetCamera(experimentVirtualCamera);
            return;
        }

        ApplyManualFixedCameraPose();
    }

    private void RestoreViewState()
    {
        if (hasCachedProgressView && progressControl != null)
        {
            if (cachedWasFreeView)
            {
                progressControl.SwitchToFreeView(experimentVirtualCamera);
            }
            else if (cachedPreviousVirtualCamera != null)
            {
                progressControl.SwitchToPresetCamera(cachedPreviousVirtualCamera);
            }
        }
        else
        {
            RestoreManualCameraPose();
        }

        RestoreXrOriginPose();
        hasCachedProgressView = false;
        hasCachedManualCameraPose = false;
        hasCachedOriginPose = false;
    }

    private void MoveXrOriginToExperimentPose()
    {
        if (xrOrigin == null || experimentCameraPose == null)
        {
            return;
        }

        if (xrOrigin.Camera == null)
        {
            xrOrigin.Camera = targetCamera != null ? targetCamera : ResolveTargetCamera();
        }

        Vector3 targetForward = Vector3.ProjectOnPlane(experimentCameraPose.forward, Vector3.up);
        if (targetForward.sqrMagnitude < 0.000001f)
        {
            targetForward = Vector3.forward;
        }

        xrOrigin.MatchOriginUpCameraForward(Vector3.up, targetForward.normalized);
        if (xrOrigin.Camera != null)
        {
            xrOrigin.MoveCameraToWorldLocation(experimentCameraPose.position);
        }
        else
        {
            Transform originTransform = GetXrOriginTransform();
            originTransform?.SetPositionAndRotation(experimentCameraPose.position, experimentCameraPose.rotation);
        }
    }

    private void RestoreXrOriginPose()
    {
        if (!hasCachedOriginPose || cachedOriginTransform == null)
        {
            return;
        }

        cachedOriginTransform.SetPositionAndRotation(cachedOriginPosition, cachedOriginRotation);
    }

    private void ApplyManualFixedCameraPose()
    {
        if (targetCamera == null || manualFixedCameraPose == null)
        {
            return;
        }

        if (cachedBrain != null)
        {
            cachedBrain.enabled = false;
        }

        if (cachedTrackedPoseDriver != null)
        {
            cachedTrackedPoseDriver.enabled = false;
        }

        targetCamera.transform.SetPositionAndRotation(
            manualFixedCameraPose.position,
            manualFixedCameraPose.rotation);
    }

    private void RestoreManualCameraPose()
    {
        if (!hasCachedManualCameraPose || targetCamera == null)
        {
            return;
        }

        if (cachedBrain != null)
        {
            cachedBrain.enabled = cachedBrainEnabled;
        }

        if (cachedTrackedPoseDriver != null)
        {
            cachedTrackedPoseDriver.enabled = cachedTrackedPoseDriverEnabled;
        }

        targetCamera.transform.SetPositionAndRotation(cachedCameraPosition, cachedCameraRotation);
    }

    private Transform GetXrOriginTransform()
    {
        if (xrOrigin == null)
        {
            return null;
        }

        return xrOrigin.Origin != null ? xrOrigin.Origin.transform : xrOrigin.transform;
    }

    private void DebugLog(string message)
    {
        if (!enableDebugLogs)
        {
            return;
        }

        Debug.Log($"[NumericalApertureExperiment] {message}", this);
    }

    private void DebugLogWarning(string message)
    {
        if (!enableDebugLogs)
        {
            Debug.LogWarning($"[NumericalApertureExperiment] {message}", this);
            return;
        }

        Debug.LogWarning($"[NumericalApertureExperiment] {message}", this);
    }
}
