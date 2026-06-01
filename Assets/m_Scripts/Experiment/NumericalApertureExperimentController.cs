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

    [Header("Experiment Entry")]
    [SerializeField] private SuperAssemblyPartSelectionController selectionController;
    [SerializeField] private ModelExploder modelExploder;
    [SerializeField] private Interactor interactor;
    [SerializeField] private Move moveController;
    [SerializeField] private XRRayInteractor rightRayInteractor;
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
    [Min(0f)]
    [SerializeField] private float sliderSpeed = 0.35f;
    [Range(0f, 0.95f)]
    [SerializeField] private float joystickDeadZone = 0.2f;

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
    [SerializeField] private float minApproxMagnification = 5f;
    [SerializeField] private float maxApproxMagnification = 100f;
    [Min(1f)]
    [SerializeField] private float magnificationRoundStep = 5f;

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
    private float currentNA;
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

    private Vector3 objectiveBaseLocalPosition;
    private bool hasObjectiveBaseLocalPosition;

    public bool IsExperimentActive => isExperimentActive;
    public bool IsTransitioning => isTransitioning;
    public float CurrentNA => currentNA;
    public float NormalizedNA => CalculateNormalizedNA(currentNA);

    private void Awake()
    {
        EnsureReferences();
        BindUi();
        CacheObjectiveBasePose();
        PrepareConeMesh();
        ApplyInitialUiState();
    }

    private void OnEnable()
    {
        EnsureReferences();
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
        if (isExperimentActive || isTransitioning)
        {
            return;
        }

        EnsureReferences();
        if (!CanStartExperiment())
        {
            DebugLogWarning("Numerical aperture experiment can only start from a stable selected super-assembly part.");
            return;
        }

        transitionRoutine = StartCoroutine(StartExperimentRoutine());
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
        SetFadeCanvas(0f, true);

        yield return FadeTo(1f);

        selectionController?.SetSelectionUiTemporarilyHidden(true);
        CacheViewState();
        SwitchToExperimentView();
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

        yield return FadeTo(0f);

        CleanupExperimentState(immediate: false);
        onExperimentEnded?.Invoke();
        DebugLog("Numerical aperture experiment ended.");
    }

    private bool CanStartExperiment()
    {
        if (selectionController != null && !selectionController.IsSelectionActive)
        {
            return false;
        }

        if (modelExploder != null && (!modelExploder.IsExploded || modelExploder.IsAnimating))
        {
            return false;
        }

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
        float safeMin = Mathf.Min(minNA, maxNA);
        float safeMax = Mathf.Max(minNA, maxNA);
        minNA = safeMin;
        maxNA = safeMax;

        if (naSlider != null)
        {
            ignoreSliderCallback = true;
            naSlider.wholeNumbers = false;
            naSlider.minValue = minNA;
            naSlider.maxValue = maxNA;
            naSlider.SetValueWithoutNotify(Mathf.Clamp(initialNA, minNA, maxNA));
            ignoreSliderCallback = false;
            SetNA(naSlider.value);
            return;
        }

        SetNA(initialNA);
    }

    private void HandleSliderValueChanged(float value)
    {
        if (ignoreSliderCallback)
        {
            return;
        }

        SetNA(value);
    }

    private void HandleJoystickSliderInput()
    {
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

        float nextValue = naSlider.value + input.x * sliderSpeed * Time.unscaledDeltaTime;
        naSlider.value = Mathf.Clamp(nextValue, minNA, maxNA);
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
        currentNA = Mathf.Clamp(value, minNA, maxNA);

        if (naSlider != null && !Mathf.Approximately(naSlider.value, currentNA))
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
        float approximateMagnification = CalculateApproximateMagnification(normalized);
        float depthTolerance = 1f - normalized;

        UpdateTexts(thetaDegrees, fullApertureDegrees, approximateMagnification, normalized, depthTolerance);
        UpdateCone(radius, normalized);
        UpdateVisualResponse(normalized, depthTolerance);
        onNormalizedNAChanged?.Invoke(normalized);
    }

    private float CalculateNormalizedNA(float na)
    {
        float range = Mathf.Max(0.0001f, maxNA - minNA);
        return Mathf.Clamp01((na - minNA) / range);
    }

    private float CalculateApproximateMagnification(float normalized)
    {
        float rawMagnification = Mathf.Lerp(minApproxMagnification, maxApproxMagnification, normalized);
        float safeStep = Mathf.Max(1f, magnificationRoundStep);
        return Mathf.Round(rawMagnification / safeStep) * safeStep;
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
            currentNaText.text = currentNA.ToString("0.00");
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
            magnificationText.text = approximateMagnification.ToString("0") + "x";
        }

        if (normalizedNaText != null)
        {
            normalizedNaText.text = normalized.ToString("0.00");
        }

        if (formulaText != null)
        {
            formulaText.text = $"NA = n * sin(theta) = {refractiveIndex:0.00} * sin({thetaDegrees:0.0} deg)";
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
        if (objectiveLensVisual != null)
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
            experimentRoot.SetActive(visible);
        }

        if (experimentCanvasGroup == null)
        {
            return;
        }

        experimentCanvasGroup.alpha = visible ? 1f : 0f;
        experimentCanvasGroup.blocksRaycasts = visible;
        experimentCanvasGroup.interactable = visible;
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
            return;
        }

        Debug.LogWarning($"[NumericalApertureExperiment] {message}", this);
    }
}
