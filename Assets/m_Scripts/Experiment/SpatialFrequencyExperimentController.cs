using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using DG.Tweening;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

public sealed class SpatialFrequencyExperimentController : MonoBehaviour
{
    private enum IlluminationMode
    {
        WhiteLight,
        LaserExcitation
    }

    private readonly struct FrequencyProfile
    {
        public FrequencyProfile(string label, float linesPerMillimeter, int visibleOrderCount)
        {
            Label = label;
            LinesPerMillimeter = linesPerMillimeter;
            VisibleOrderCount = visibleOrderCount;
        }

        public string Label { get; }
        public float LinesPerMillimeter { get; }
        public int VisibleOrderCount { get; }
    }

    private static readonly FrequencyProfile[] Profiles =
    {
        new FrequencyProfile("High", 250f, 1),
        new FrequencyProfile("Middle", 125f, 2),
        new FrequencyProfile("Low", 62.5f, 4)
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
    [SerializeField] private Transform experimentCameraPose;
    [SerializeField] private Transform manualFixedCameraPose;
    [SerializeField] private bool restorePlayerPoseOnExit = true;

    [Header("Spatial Frequency Controls")]
    [SerializeField] private Toggle highFrequencyToggle;
    [SerializeField] private Toggle middleFrequencyToggle;
    [SerializeField] private Toggle lowFrequencyToggle;
    [SerializeField] private Button whiteLightButton;
    [SerializeField] private Button laserExcitationButton;
    [SerializeField] private SpatialFrequencyExperimentDiagramView diagramView;
    [Min(380f)]
    [SerializeField] private float illuminationWavelengthNm = 550f;
    [Min(0.1f)]
    [SerializeField] private float objectiveFocalLengthMm = 18f;

    [Header("Text Output")]
    [SerializeField] private TextMeshProUGUI frequencyText;
    [SerializeField] private TextMeshProUGUI lineSpacingText;
    [SerializeField] private TextMeshProUGUI diffractionAngleText;
    [SerializeField] private TextMeshProUGUI orderSpacingText;
    [SerializeField] private TextMeshProUGUI formulaText;
    [SerializeField] private TextMeshProUGUI resolutionText;
    [SerializeField] private TextMeshProUGUI sampleNameText;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs;

    private readonly List<InputActionMap> blockedInputMaps = new List<InputActionMap>();
    private readonly List<bool> blockedInputMapWasEnabled = new List<bool>();
    private Coroutine transitionRoutine;
    private InputActionAsset fallbackInputActions;
    private bool isExperimentActive;
    private bool isTransitioning;
    private bool controlsBound;
    private bool gameplayInputBlocked;
    private bool rightRayUiInteractionCached;
    private bool originalRightRayUiInteraction;
    private GameObject samplePreviewRoot;
    private bool samplePreviewWasActive;
    private bool samplePreviewStateCached;
    private int currentProfileIndex;
    private IlluminationMode illuminationMode = IlluminationMode.WhiteLight;

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
    private Transform cachedOriginTransform;
    private Vector3 cachedOriginPosition;
    private Quaternion cachedOriginRotation;
    private bool hasCachedOriginPose;

    public bool IsExperimentActive => isExperimentActive;
    public bool IsTransitioning => isTransitioning;
    public Selectable[] AssistantControls => new Selectable[] {highFrequencyToggle, middleFrequencyToggle, lowFrequencyToggle, whiteLightButton, laserExcitationButton, exitButton};
    public int AssistantProfile => currentProfileIndex;
    public string AssistantIllumination => illuminationMode.ToString();
    private void Awake()
    {
        EnsureReferences();
        PrepareHybridControls();
        BindControls();
        SetExperimentRootVisible(false);
        SetFadeCanvas(0f, false);
        ApplyProfile(0, synchronizeToggles: true);
    }

    private void OnEnable()
    {
        BindControls();
    }

    private void OnDisable()
    {
        StopActiveTransition();
        CleanupExperimentState(immediate: true);
        UnbindControls();
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

    public void StartExperiment()
    {
        TryStartExperiment();
    }

    public bool TryStartExperiment()
    {
        if (isExperimentActive || isTransitioning)
        {
            return false;
        }

        EnsureReferences();
        BindControls();
        if (!CanStartExperiment())
        {
            DebugLogWarning("Experiment requires an active part selection and a completed explode animation.");
            return false;
        }

        transitionRoutine = StartCoroutine(StartExperimentRoutine());
        return true;
    }

    public void ExitExperiment()
    {
        if ((!isExperimentActive && !isTransitioning) || transitionRoutine != null)
        {
            return;
        }

        transitionRoutine = StartCoroutine(ExitExperimentRoutine());
    }

    public void SelectHighFrequency()
    {
        ApplyProfile(0, synchronizeToggles: true);
    }

    public void SelectMiddleFrequency()
    {
        ApplyProfile(1, synchronizeToggles: true);
    }

    public void SelectLowFrequency()
    {
        ApplyProfile(2, synchronizeToggles: true);
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
        SetExperimentRootVisible(true);
        ApplyProfile(0, synchronizeToggles: true);

        yield return FadeTo(0f);

        isExperimentActive = true;
        isTransitioning = false;
        transitionRoutine = null;
        DebugLog("Spatial frequency experiment started.");
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
        DebugLog("Spatial frequency experiment ended.");
    }

    private bool CanStartExperiment()
    {
        if (selectionController != null && !selectionController.IsSelectionActive)
        {
            return false;
        }

        return modelExploder == null || modelExploder.IsExploded && !modelExploder.IsAnimating;
    }

    private void ApplyProfile(int profileIndex, bool synchronizeToggles)
    {
        int safeIndex = Mathf.Clamp(profileIndex, 0, Profiles.Length - 1);
        currentProfileIndex = safeIndex;
        FrequencyProfile profile = Profiles[safeIndex];
        float normalizedFrequency = Mathf.InverseLerp(
            Profiles[Profiles.Length - 1].LinesPerMillimeter,
            Profiles[0].LinesPerMillimeter,
            profile.LinesPerMillimeter);

        float wavelengthMm = illuminationWavelengthNm * 0.000001f;
        float lineSpacingMm = 1f / profile.LinesPerMillimeter;
        float sinPsi = Mathf.Clamp(wavelengthMm / lineSpacingMm, -1f, 1f);
        float diffractionAngleRadians = Mathf.Asin(sinPsi);
        float diffractionAngleDegrees = diffractionAngleRadians * Mathf.Rad2Deg;
        float orderSpacingMm = objectiveFocalLengthMm * Mathf.Tan(diffractionAngleRadians);
        bool hasSelectedSample = sampleInteraction != null &&
            sampleInteraction.HasSampleOnHand() &&
            sampleInteraction.CurrentSampleTexture != null;
        Texture sampleTexture = hasSelectedSample
            ? sampleInteraction.CurrentSampleTexture
            : null;

        if (synchronizeToggles)
        {
            highFrequencyToggle?.SetIsOnWithoutNotify(safeIndex == 0);
            middleFrequencyToggle?.SetIsOnWithoutNotify(safeIndex == 1);
            lowFrequencyToggle?.SetIsOnWithoutNotify(safeIndex == 2);
        }
        UpdateFrequencyToggleVisuals(safeIndex);

        if (frequencyText != null)
        {
            frequencyText.text = $"{profile.Label}: {profile.LinesPerMillimeter:0} lines/mm";
        }

        if (lineSpacingText != null)
        {
            lineSpacingText.text = $"Line spacing D: {lineSpacingMm * 1000f:0.00} um";
        }

        if (diffractionAngleText != null)
        {
            diffractionAngleText.text = $"Diffraction angle psi: {diffractionAngleDegrees:0.00} deg";
        }

        if (orderSpacingText != null)
        {
            orderSpacingText.text = $"Back focal spacing S: {orderSpacingMm:0.00} mm";
        }

        if (formulaText != null)
        {
            formulaText.text =
                "S / f approximately equals lambda / D = sin(psi)\n" +
                $"lambda = {illuminationWavelengthNm:0} nm, f = {objectiveFocalLengthMm:0.0} mm, " +
                $"source = {(illuminationMode == IlluminationMode.WhiteLight ? "white light" : "laser excitation")}";
        }

        if (resolutionText != null)
        {
            resolutionText.text = profile.VisibleOrderCount <= 1
                ? "Resolution limit: zero and first orders are required to resolve the grating."
                : $"{profile.VisibleOrderCount} diffraction orders are represented; lower frequencies place orders closer together.";
        }

        if (sampleNameText != null)
        {
            sampleNameText.text = sampleTexture != null
                ? $"Selected specimen: {sampleInteraction.CurrentSampleName}"
                : "Selected specimen: none (teaching grating used)";
        }

        UpdateIlluminationButtonVisuals();
        diagramView?.ApplyProfile(
            normalizedFrequency,
            profile.LinesPerMillimeter,
            diffractionAngleDegrees,
            profile.VisibleOrderCount,
            sampleTexture,
            illuminationMode == IlluminationMode.LaserExcitation);
    }

    private void BindControls()
    {
        if (controlsBound)
        {
            return;
        }

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(ExitExperiment);
        }

        if (highFrequencyToggle != null)
        {
            highFrequencyToggle.onValueChanged.AddListener(HandleHighFrequencyChanged);
        }

        if (middleFrequencyToggle != null)
        {
            middleFrequencyToggle.onValueChanged.AddListener(HandleMiddleFrequencyChanged);
        }

        if (lowFrequencyToggle != null)
        {
            lowFrequencyToggle.onValueChanged.AddListener(HandleLowFrequencyChanged);
        }

        whiteLightButton?.onClick.AddListener(SelectWhiteLight);
        laserExcitationButton?.onClick.AddListener(SelectLaserExcitation);
        if (sampleInteraction != null)
        {
            sampleInteraction.SampleChanged += HandleSampleChanged;
        }

        controlsBound = true;
    }

    private void UnbindControls()
    {
        if (!controlsBound)
        {
            return;
        }

        exitButton?.onClick.RemoveListener(ExitExperiment);
        highFrequencyToggle?.onValueChanged.RemoveListener(HandleHighFrequencyChanged);
        middleFrequencyToggle?.onValueChanged.RemoveListener(HandleMiddleFrequencyChanged);
        lowFrequencyToggle?.onValueChanged.RemoveListener(HandleLowFrequencyChanged);
        whiteLightButton?.onClick.RemoveListener(SelectWhiteLight);
        laserExcitationButton?.onClick.RemoveListener(SelectLaserExcitation);
        if (sampleInteraction != null)
        {
            sampleInteraction.SampleChanged -= HandleSampleChanged;
        }
        controlsBound = false;
    }

    private void HandleHighFrequencyChanged(bool isOn)
    {
        if (isOn)
        {
            ApplyProfile(0, synchronizeToggles: false);
        }
    }

    private void HandleMiddleFrequencyChanged(bool isOn)
    {
        if (isOn)
        {
            ApplyProfile(1, synchronizeToggles: false);
        }
    }

    private void HandleLowFrequencyChanged(bool isOn)
    {
        if (isOn)
        {
            ApplyProfile(2, synchronizeToggles: false);
        }
    }

    public void SelectWhiteLight()
    {
        SetIlluminationMode(IlluminationMode.WhiteLight);
    }

    public void SelectLaserExcitation()
    {
        SetIlluminationMode(IlluminationMode.LaserExcitation);
    }

    private void SetIlluminationMode(IlluminationMode mode)
    {
        illuminationMode = mode;
        UpdateIlluminationButtonVisuals();
        ApplyProfile(currentProfileIndex, synchronizeToggles: false);
    }

    private void UpdateIlluminationButtonVisuals()
    {
        SetIlluminationButtonSelected(
            whiteLightButton,
            illuminationMode == IlluminationMode.WhiteLight);
        SetIlluminationButtonSelected(
            laserExcitationButton,
            illuminationMode == IlluminationMode.LaserExcitation);
    }

    private void HandleSampleChanged()
    {
        ApplyProfile(currentProfileIndex, synchronizeToggles: false);
    }

    private void PrepareHybridControls()
    {
        SetControlRect(whiteLightButton, new Vector2(0.05f, 0.12f), new Vector2(0.46f, 0.19f));
        SetControlRect(laserExcitationButton, new Vector2(0.54f, 0.12f), new Vector2(0.95f, 0.19f));
        SetControlRect(highFrequencyToggle, new Vector2(0.31f, 0.02f), new Vector2(0.52f, 0.10f));
        SetControlRect(middleFrequencyToggle, new Vector2(0.54f, 0.02f), new Vector2(0.75f, 0.10f));
        SetControlRect(lowFrequencyToggle, new Vector2(0.77f, 0.02f), new Vector2(0.98f, 0.10f));
        SetToggleLabel(highFrequencyToggle, "High  250");
        SetToggleLabel(middleFrequencyToggle, "Middle  125");
        SetToggleLabel(lowFrequencyToggle, "Low  62.5");

        Transform rootTransform = experimentRoot != null
            ? experimentRoot.transform
            : transform.Find("SpatialFrequencyExperimentRoot");
        TextMeshProUGUI heading = rootTransform != null
            ? rootTransform.Find("Result Panel/Spatial Frequency Heading")?.GetComponent<TextMeshProUGUI>()
            : null;
        if (heading != null)
        {
            heading.text = "Spatial Frequency / Fourier Carrier";
            SetRect(heading.rectTransform, new Vector2(0.01f, 0.02f), new Vector2(0.30f, 0.10f));
        }
    }

    private static void SetControlRect(Component control, Vector2 anchorMin, Vector2 anchorMax)
    {
        if (control == null)
        {
            return;
        }

        control.gameObject.SetActive(true);
        SetRect((RectTransform)control.transform, anchorMin, anchorMax);
    }

    private static void SetToggleLabel(Toggle toggle, string text)
    {
        TextMeshProUGUI label = toggle != null
            ? toggle.GetComponentInChildren<TextMeshProUGUI>(true)
            : null;
        if (label != null)
        {
            label.text = text;
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 17f;
        }
    }

    private void UpdateFrequencyToggleVisuals(int selectedIndex)
    {
        SetFrequencyToggleVisual(highFrequencyToggle, selectedIndex == 0);
        SetFrequencyToggleVisual(middleFrequencyToggle, selectedIndex == 1);
        SetFrequencyToggleVisual(lowFrequencyToggle, selectedIndex == 2);
    }

    private static void SetFrequencyToggleVisual(Toggle toggle, bool selected)
    {
        if (toggle == null)
        {
            return;
        }

        toggle.transition = Selectable.Transition.None;
        Image background = toggle.GetComponent<Image>();
        if (background != null)
        {
            background.color = selected
                ? new Color(0.035f, 0.27f, 0.62f, 0.98f)
                : new Color(0.88f, 0.91f, 0.95f, 0.96f);
        }

        Outline frame = toggle.GetComponent<Outline>();
        if (frame == null)
        {
            frame = toggle.gameObject.AddComponent<Outline>();
        }

        frame.effectColor = selected
            ? new Color(0.12f, 0.78f, 1f, 1f)
            : new Color(0.36f, 0.43f, 0.52f, 0.75f);
        frame.effectDistance = selected ? new Vector2(3f, -3f) : new Vector2(1f, -1f);
        frame.useGraphicAlpha = false;

        TextMeshProUGUI label = toggle.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
        {
            label.color = selected ? Color.white : new Color(0.12f, 0.16f, 0.22f, 1f);
            label.fontStyle = selected ? FontStyles.Bold : FontStyles.Normal;
        }

        Image checkmark = toggle.graphic as Image;
        if (checkmark != null)
        {
            checkmark.color = selected
                ? new Color(0.18f, 0.86f, 1f, 1f)
                : Color.clear;
        }

        toggle.transform.localScale = selected ? Vector3.one * 1.035f : Vector3.one;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetIlluminationButtonSelected(Button button, bool selected)
    {
        if (button == null)
        {
            return;
        }

        Color background = selected
            ? new Color(0.02f, 0.30f, 0.78f, 1f)
            : new Color(0.27f, 0.33f, 0.43f, 1f);

        if (button.targetGraphic is Image image)
        {
            image.color = Color.white;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = background;
        colors.highlightedColor = Color.Lerp(background, Color.white, 0.16f);
        colors.pressedColor = Color.Lerp(background, Color.black, 0.22f);
        colors.selectedColor = background;
        colors.disabledColor = new Color(background.r, background.g, background.b, 0.45f);
        colors.colorMultiplier = 1f;
        button.colors = colors;
        button.transform.localScale = selected ? Vector3.one * 1.06f : Vector3.one;

        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
        {
            label.color = Color.white;
            label.fontStyle = selected ? FontStyles.Bold : FontStyles.Normal;
        }
    }

    private void EnsureReferences()
    {
        selectionController = selectionController != null
            ? selectionController
            : SuperAssemblyPartSelectionController.Instance;
        modelExploder = modelExploder != null ? modelExploder : FindObjectOfType<ModelExploder>();
        interactor = interactor != null ? interactor : Interactor.Instance;
        moveController = moveController != null ? moveController : Move.Instance;
        progressControl = progressControl != null ? progressControl : ProgressControl.Instance;
        rightRayInteractor = rightRayInteractor != null
            ? rightRayInteractor
            : FindObjectOfType<XRRayInteractor>();
        sampleInteraction = sampleInteraction != null
            ? sampleInteraction
            : FindObjectOfType<InteractWithSamples>();
        Transform rootTransform = experimentRoot != null
            ? experimentRoot.transform
            : transform.Find("SpatialFrequencyExperimentRoot");
        if (rootTransform != null)
        {
            whiteLightButton = whiteLightButton != null
                ? whiteLightButton
                : rootTransform.Find("Result Panel/White Light Button")?.GetComponent<Button>();
            laserExcitationButton = laserExcitationButton != null
                ? laserExcitationButton
                : rootTransform.Find("Result Panel/Laser Excitation Button")?.GetComponent<Button>();
        }
        targetCamera = targetCamera != null ? targetCamera : ResolveTargetCamera();
        xrOrigin = xrOrigin != null ? xrOrigin : FindObjectOfType<XROrigin>();
        if (fallbackInputActions == null && interactor != null)
        {
            fallbackInputActions = interactor.inputActionAsset;
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

        if (experimentCanvasGroup != null)
        {
            experimentCanvasGroup.alpha = visible ? 1f : 0f;
            experimentCanvasGroup.blocksRaycasts = visible;
            experimentCanvasGroup.interactable = visible;
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
            interactor = Interactor.Instance != null
                ? Interactor.Instance
                : FindObjectOfType<Interactor>();
        }

        if (moveController == null)
        {
            moveController = Move.Instance != null
                ? Move.Instance
                : FindObjectOfType<Move>();
        }

        gameplayInputBlocked = blocked;
        if (blocked)
        {
            if (interactor != null)
            {
                interactor.SetForceTutorialGameplayInputBlocked(true);
            }

            if (moveController != null)
            {
                moveController.SetGripMovementBlocked(true);
            }

            SetGlobalInputMapsBlocked(true);
            return;
        }

        SetGlobalInputMapsBlocked(false);
        if (interactor != null)
        {
            interactor.SetForceTutorialGameplayInputBlocked(false);
        }

        if (moveController != null)
        {
            moveController.SetGripMovementBlocked(false);
        }
    }

    private void SetGlobalInputMapsBlocked(bool blocked)
    {
        if (!blockGlobalInputMaps || fallbackInputActions == null || globalInputMapsToBlock == null)
        {
            return;
        }

        if (blocked)
        {
            blockedInputMaps.Clear();
            blockedInputMapWasEnabled.Clear();
            for (int i = 0; i < globalInputMapsToBlock.Length; i++)
            {
                InputActionMap map = fallbackInputActions.FindActionMap(globalInputMapsToBlock[i], false);
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
        samplePreviewRoot = previewRoot;
        samplePreviewWasActive = previewRoot.activeSelf;
        samplePreviewStateCached = true;
        previewRoot.SetActive(false);
    }

    private void RestoreSamplePreview()
    {
        if (!samplePreviewStateCached || samplePreviewRoot == null)
        {
            return;
        }

        samplePreviewRoot.SetActive(samplePreviewWasActive);
        samplePreviewRoot = null;
        samplePreviewStateCached = false;
    }

    private void CacheViewState()
    {
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

        cachedOriginTransform = GetXrOriginTransform();
        if (cachedOriginTransform != null)
        {
            cachedOriginPosition = cachedOriginTransform.position;
            cachedOriginRotation = cachedOriginTransform.rotation;
            hasCachedOriginPose = true;
        }
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
    }

    private void RestoreXrOriginPose()
    {
        if (hasCachedOriginPose && cachedOriginTransform != null)
        {
            cachedOriginTransform.SetPositionAndRotation(cachedOriginPosition, cachedOriginRotation);
        }
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

        targetCamera.transform.SetPositionAndRotation(manualFixedCameraPose.position, manualFixedCameraPose.rotation);
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

    private void CleanupExperimentState(bool immediate)
    {
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

    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[SpatialFrequencyExperiment] {message}", this);
        }
    }

    private void DebugLogWarning(string message)
    {
        Debug.LogWarning($"[SpatialFrequencyExperiment] {message}", this);
    }
}
