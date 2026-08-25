using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

public class SuperAssemblyPartSelectionController : MonoBehaviour
{
    [Serializable]
    public class PartInfo
    {
        public Transform partTransform;
        public string displayName;

        [TextArea(3, 8)]
        public string description;

        public UnityEvent onExperimentButtonClicked = new UnityEvent();
    }

    [Serializable]
    public class PartExperimentBinding
    {
        public Transform partTransform;
        public NumericalApertureExperimentController numericalApertureExperiment;
        public string buttonText = "Start Numerical Aperture Experiment";
    }

    public static SuperAssemblyPartSelectionController Instance { get; private set; }

    [Header("References")]
    [SerializeField] private MicroscopeExploderModeController modeController;
    [SerializeField] private XRRayInteractor rightRayInteractor;
    [SerializeField] private ModelExploder modelExploder;
    [SerializeField] private Transform partRoot;
    [SerializeField] private Camera targetCamera;

    [Header("Parts")]
    [Tooltip("Optional part metadata. Leave empty to use ModelExploder's part list with default names.")]
    [SerializeField] private List<PartInfo> partInfos = new List<PartInfo>();

    [Header("Part Experiments")]
    [Tooltip("Parts listed here will enable the third UI panel button while selected.")]
    [SerializeField] private List<PartExperimentBinding> partExperimentBindings = new List<PartExperimentBinding>();
    [SerializeField] private bool autoBindAboveMirrorNumericalAperture = true;
    [SerializeField] private bool hideExperimentButtonWhenUnavailable = true;
    [SerializeField] private string unavailableExperimentText = "No Experiment Available";

    [SerializeField] private bool useRendererBoundsFallback = true;

    [Min(0f)]
    [SerializeField] private float boundsPadding = 0.005f;

    [Header("Selection Pose")]
    [SerializeField] private bool alignSelectedPartVisualCenter = true;

    [SerializeField] private Vector2 selectedPartViewportPosition = new Vector2(0.33f, 0.5f);

    [Min(0.1f)]
    [SerializeField] private float selectedPartDistanceFromCamera = 1.2f;

    [Min(1f)]
    [SerializeField] private float selectedPartScaleMultiplier = 1.8f;

    [Min(0.01f)]
    [SerializeField] private float selectedPartMoveDuration = 0.45f;

    [SerializeField] private Ease selectedPartEase = Ease.OutCubic;

    [Header("Selection Visibility")]
    [SerializeField] private bool hideOtherParts = true;
    [SerializeField] private bool disableCollidersForHiddenParts = true;

    [Tooltip("Extra scene objects to hide while a part is selected. This supplements the normal part cache.")]
    [SerializeField] private List<Transform> additionalHideTargets = new List<Transform>();

    [Header("UI")]
    [SerializeField] private CanvasGroup uiCanvasGroup;
    [SerializeField] private TextMeshProUGUI partNameText;
    [SerializeField] private TextMeshProUGUI partDescriptionText;
    [SerializeField] private Button experimentButton;
    [SerializeField] private TextMeshProUGUI experimentButtonText;
    [SerializeField] private Vector2 uiViewportPosition = new Vector2(0.73f, 0.5f);

    [SerializeField] private bool lockUiPoseOnSelectionEnter = true;
    [SerializeField] private bool lockUiToFixedReferencePose = true;
    [SerializeField] private bool keepUiWorldUpright = true;
    [SerializeField] private bool fitUiInsideCameraView = true;
    [SerializeField] private Vector2 uiViewportPadding = new Vector2(0.04f, 0.08f);
    [Range(0.1f, 1f)]
    [SerializeField] private float minUiFitScaleMultiplier = 0.65f;

    [Min(0.1f)]
    [SerializeField] private float uiDistanceFromCamera = 1.15f;

    [Min(0.0001f)]
    [SerializeField] private float uiWorldScale = 0.00135f;

    [Min(0.01f)]
    [SerializeField] private float uiFadeDuration = 0.28f;

    [SerializeField] private Ease uiFadeEase = Ease.OutCubic;

    [Header("Experiment Button Hover")]
    [SerializeField] private bool animateExperimentButtonOnHover = true;
    [Min(1f)]
    [SerializeField] private float experimentButtonHoverScaleMultiplier = 1.08f;
    [Min(0.01f)]
    [SerializeField] private float experimentButtonHoverPulseDuration = 0.45f;
    [SerializeField] private Ease experimentButtonHoverEase = Ease.InOutSine;

    [Header("Events")]
    [SerializeField] private UnityEvent onSelectionEntered = new UnityEvent();
    [SerializeField] private UnityEvent onSelectionExited = new UnityEvent();
    [SerializeField] private UnityEvent onExperimentRequested = new UnityEvent();

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    private readonly List<Transform> cachedParts = new List<Transform>();
    private readonly Dictionary<Renderer, bool> hiddenRendererStates = new Dictionary<Renderer, bool>();
    private readonly Dictionary<Collider, bool> hiddenColliderStates = new Dictionary<Collider, bool>();
    private readonly List<SuperAssemblyPartHoverPulse> disabledHoverPulseComponents = new List<SuperAssemblyPartHoverPulse>();

    private Transform selectedPart;
    private PartInfo selectedPartInfo;
    private PartExperimentBinding selectedExperimentBinding;
    private Vector3 selectedPartOriginalWorldPosition;
    private Vector3 selectedPartOriginalLocalScale;
    private Sequence selectedPartTween;
    private Tween uiFadeTween;
    private bool isSelectionActive;
    private bool experimentButtonBound;
    private bool rightRayUiInteractionCached;
    private bool originalRightRayUiInteraction;
    private bool hasFixedUiReferencePose;
    private bool externalInteractionLocked;
    private bool selectionUiTemporarilyHidden;
    private Vector3 fixedUiReferenceWorldPosition;
    private Quaternion fixedUiReferenceWorldRotation;
    private Vector3 fixedUiReferenceWorldScale;
    private PartExperimentBinding runtimeAboveMirrorExperimentBinding;
    private Transform cachedExperimentButtonTransform;
    private Vector3 experimentButtonOriginalLocalScale;
    private Tween experimentButtonHoverTween;
    private bool hasExperimentButtonOriginalScale;
    private int lastExperimentRequestFrame = -1;

    public static bool HasActiveSelection => Instance != null && Instance.isSelectionActive;
    public bool IsSelectionActive => isSelectionActive;
    public bool CanSelectParts => IsInReadySuperAssemblyState();
    public Transform CurrentSelectedPart => selectedPart;

    public static bool TryHandleRightTrigger()
    {
        SuperAssemblyPartSelectionController controller = ResolveAvailableInstance();
        return controller != null && controller.TryHandleRightTriggerInternal();
    }

    public static bool TryHandleDesktopPrimaryClick(Ray ray, float maxDistance, bool isPointerOverUi)
    {
        SuperAssemblyPartSelectionController controller = ResolveAvailableInstance();
        return controller != null && controller.TryHandleDesktopPrimaryClickInternal(ray, maxDistance, isPointerOverUi);
    }

    private static SuperAssemblyPartSelectionController ResolveAvailableInstance()
    {
        if (Instance != null && Instance.isActiveAndEnabled)
        {
            return Instance;
        }

        SuperAssemblyPartSelectionController[] controllers =
            FindObjectsOfType<SuperAssemblyPartSelectionController>(true);

        for (int i = 0; i < controllers.Length; i++)
        {
            if (controllers[i] == null || !controllers[i].isActiveAndEnabled)
            {
                continue;
            }

            Instance = controllers[i];
            return Instance;
        }

        return null;
    }

    public void Configure(
        MicroscopeExploderModeController controller,
        XRRayInteractor rayInteractor,
        ModelExploder exploder,
        Transform root)
    {
        if (modeController == null)
        {
            modeController = controller;
        }

        if (rightRayInteractor == null)
        {
            rightRayInteractor = rayInteractor;
        }

        if (modelExploder == null)
        {
            modelExploder = exploder;
        }

        if (partRoot == null)
        {
            partRoot = root;
        }

        if (cachedParts.Count == 0)
        {
            RebuildPartCache();
        }
    }

    public void ForceExitSelection(bool immediate)
    {
        ExitSelection(immediate);
    }

    public void SetExternalInteractionLocked(bool isLocked)
    {
        externalInteractionLocked = isLocked;
    }

    public void SetSelectionUiTemporarilyHidden(bool isHidden)
    {
        selectionUiTemporarilyHidden = isHidden;

        if (isHidden)
        {
            StopExperimentButtonHoverPulse();
            HideUi(true);
            return;
        }

        if (!isSelectionActive || selectedPart == null || !HasRequiredUiReferences())
        {
            return;
        }

        CacheAndEnableUiInteractionForSelection();
        UpdateUiContent(selectedPart, selectedPartInfo);
        EnsureUi();
        UpdateUiPose();
        ShowUi();
    }

    public void InvokeCurrentExperiment()
    {
        if (!isSelectionActive || selectedPart == null)
        {
            return;
        }

        if (lastExperimentRequestFrame == Time.frameCount)
        {
            return;
        }

        lastExperimentRequestFrame = Time.frameCount;

        bool startedExperiment = false;
        NumericalApertureExperimentController experimentController =
            selectedExperimentBinding != null
                ? selectedExperimentBinding.numericalApertureExperiment
                : null;

        if (experimentController == null && selectedExperimentBinding != null)
        {
            experimentController = ResolveNumericalApertureExperiment();
            selectedExperimentBinding.numericalApertureExperiment = experimentController;
        }

        if (experimentController != null)
        {
            experimentController.ConfigureRuntimeContext(this, modelExploder, rightRayInteractor);
            startedExperiment = experimentController.TryStartExperiment();
        }

        selectedPartInfo?.onExperimentButtonClicked?.Invoke();
        onExperimentRequested?.Invoke();
        DebugLog($"Experiment requested for part='{selectedPart.name}', started={startedExperiment}");
    }

    private void Awake()
    {
        if (Instance == null || Instance == this)
        {
            Instance = this;
        }
    }

    private void OnEnable()
    {
        EnsureReferences();
        BindExperimentButton();
        RebuildPartCache();
        HideUiImmediate();
    }

    private void OnDisable()
    {
        ExitSelection(true);
        externalInteractionLocked = false;
        selectionUiTemporarilyHidden = false;
        UnbindExperimentButton();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void LateUpdate()
    {
        if (!isSelectionActive)
        {
            return;
        }

        if (!IsInReadySuperAssemblyState())
        {
            ExitSelection(true);
            return;
        }

        if (!lockUiPoseOnSelectionEnter)
        {
            if (!selectionUiTemporarilyHidden)
            {
                UpdateUiPose();
            }
        }

        if (selectionUiTemporarilyHidden)
        {
            StopExperimentButtonHoverPulse();
            return;
        }

        UpdateExperimentButtonHoverPulse();
    }

    [ContextMenu("Rebuild Part Cache")]
    public void RebuildPartCache()
    {
        cachedParts.Clear();

        if (partInfos != null && partInfos.Count > 0)
        {
            for (int i = 0; i < partInfos.Count; i++)
            {
                if (partInfos[i] != null)
                {
                    AddPartIfValid(partInfos[i].partTransform);
                }
            }
        }

        if (cachedParts.Count == 0 && modelExploder != null && modelExploder.manualParts != null &&
            modelExploder.manualParts.Count > 0)
        {
            for (int i = 0; i < modelExploder.manualParts.Count; i++)
            {
                AddPartIfValid(modelExploder.manualParts[i]);
            }
        }

        if (cachedParts.Count == 0)
        {
            Transform sourceRoot = partRoot != null ? partRoot : transform;
            if (modelExploder != null)
            {
                sourceRoot = modelExploder.transform;
            }

            if (modelExploder != null && modelExploder.includeAllDescendants)
            {
                Transform[] descendants = sourceRoot.GetComponentsInChildren<Transform>(modelExploder.includeInactiveParts);
                for (int i = 0; i < descendants.Length; i++)
                {
                    AddPartIfValid(descendants[i]);
                }
            }
            else
            {
                foreach (Transform child in sourceRoot)
                {
                    AddPartIfValid(child);
                }
            }
        }

        DebugLog($"Rebuilt part cache. parts={cachedParts.Count}");
    }

    private bool TryHandleRightTriggerInternal()
    {
        EnsureReferences();

        if (externalInteractionLocked)
        {
            return true;
        }

        if (isSelectionActive)
        {
            if (TryInvokeExperimentButtonFromPointer(allowMousePointer: false) || IsPointingAtSelectionUi())
            {
                return true;
            }

            ExitSelection(false);
            return true;
        }

        if (!IsInReadySuperAssemblyState())
        {
            return false;
        }

        Transform hoveredPart = ResolveHoveredPart();
        if (hoveredPart == null)
        {
            return false;
        }

        EnterSelection(hoveredPart);
        return true;
    }

    private bool TryHandleDesktopPrimaryClickInternal(Ray ray, float maxDistance, bool isPointerOverUi)
    {
        EnsureReferences();

        if (externalInteractionLocked)
        {
            return true;
        }

        if (isSelectionActive)
        {
            if (TryInvokeExperimentButtonFromPointer(allowMousePointer: true) ||
                IsDesktopRayPointingAtExperimentButton(ray, maxDistance) ||
                isPointerOverUi ||
                IsDesktopRayPointingAtSelectionUi(ray, maxDistance))
            {
                return true;
            }

            ExitSelection(false);
            return true;
        }

        if (!IsInReadySuperAssemblyState())
        {
            return false;
        }

        Transform hoveredPart = ResolvePartFromRay(ray, maxDistance);
        if (hoveredPart == null)
        {
            return false;
        }

        EnterSelection(hoveredPart);
        return true;
    }

    private void EnterSelection(Transform part)
    {
        if (part == null)
        {
            return;
        }

        EnsureUi();
        if (!HasRequiredUiReferences())
        {
            DebugLogWarning("Selection UI references are missing. Please assign CanvasGroup, Name Text, Description Text, and Experiment Button in the scene.");
            return;
        }

        if (isSelectionActive)
        {
            ExitSelection(true);
        }

        SetHoverPulseComponentsEnabled(false);
        selectedPart = part;
        selectedPartInfo = FindPartInfo(part);
        selectedExperimentBinding = FindPartExperimentBinding(part, selectedPartInfo);
        selectedPartOriginalWorldPosition = part.position;
        selectedPartOriginalLocalScale = part.localScale;
        isSelectionActive = true;

        CacheAndEnableUiInteractionForSelection();
        ApplyOtherPartsVisibility(part, false);
        UpdateUiContent(part, selectedPartInfo);
        EnsureUi();
        UpdateUiPose();
        ShowUi();
        AnimateSelectedPartToFocus(part);

        onSelectionEntered?.Invoke();
        DebugLog($"Selection entered. part='{part.name}'");
    }

    private void ExitSelection(bool immediate)
    {
        if (!isSelectionActive && selectedPart == null)
        {
            HideUiImmediate();
            RestoreUiInteractionAfterSelection();
            externalInteractionLocked = false;
            selectionUiTemporarilyHidden = false;
            return;
        }

        Transform partToRestore = selectedPart;
        KillSelectedPartTween();
        StopExperimentButtonHoverPulse();
        RestoreOtherPartsVisibility();
        HideUi(immediate);
        RestoreUiInteractionAfterSelection();
        externalInteractionLocked = false;
        selectionUiTemporarilyHidden = false;

        if (partToRestore != null)
        {
            if (immediate || !partToRestore.gameObject.activeInHierarchy)
            {
                partToRestore.position = selectedPartOriginalWorldPosition;
                partToRestore.localScale = selectedPartOriginalLocalScale;
            }
            else
            {
                selectedPartTween = DOTween.Sequence();
                selectedPartTween.Join(partToRestore
                    .DOMove(selectedPartOriginalWorldPosition, selectedPartMoveDuration)
                    .SetEase(selectedPartEase));
                selectedPartTween.Join(partToRestore
                    .DOScale(selectedPartOriginalLocalScale, selectedPartMoveDuration)
                    .SetEase(selectedPartEase));
                selectedPartTween.OnKill(() => selectedPartTween = null);
                selectedPartTween.OnComplete(() => selectedPartTween = null);
            }
        }

        selectedPart = null;
        selectedPartInfo = null;
        selectedExperimentBinding = null;
        isSelectionActive = false;
        SetHoverPulseComponentsEnabled(true);
        onSelectionExited?.Invoke();
        DebugLog("Selection exited.");
    }

    private bool IsInReadySuperAssemblyState()
    {
        if (modeController == null ||
            modeController.CurrentMode != MicroscopeExploderModeController.AssemblyMode.SuperAssembly)
        {
            return false;
        }

        return modelExploder == null || (modelExploder.IsExploded && !modelExploder.IsAnimating);
    }

    private void EnsureReferences()
    {
        if (modeController == null)
        {
            modeController = MicroscopeExploderModeController.Instance;
        }

        if (modeController == null)
        {
            modeController = FindObjectOfType<MicroscopeExploderModeController>();
        }

        if (modelExploder == null)
        {
            modelExploder = GetComponent<ModelExploder>();
        }

        if (modelExploder == null)
        {
            modelExploder = GetComponentInChildren<ModelExploder>(true);
        }

        if (partRoot == null && modelExploder != null)
        {
            partRoot = modelExploder.transform;
        }

        if (rightRayInteractor == null)
        {
            rightRayInteractor = FindPreferredRightRayInteractor();
        }

        if (targetCamera == null)
        {
            targetCamera = ResolveTargetCamera();
        }

        BindExperimentButton();
    }

    private XRRayInteractor FindPreferredRightRayInteractor()
    {
        XRRayInteractor[] rayInteractors = FindObjectsOfType<XRRayInteractor>(true);
        XRRayInteractor fallbackInteractor = null;

        for (int i = 0; i < rayInteractors.Length; i++)
        {
            XRRayInteractor candidate = rayInteractors[i];
            if (candidate == null)
            {
                continue;
            }

            if (fallbackInteractor == null)
            {
                fallbackInteractor = candidate;
            }

            string hierarchyPath = GetHierarchyPath(candidate.transform).ToLowerInvariant();
            if (hierarchyPath.Contains("right controller") ||
                hierarchyPath.Contains("righthand") ||
                hierarchyPath.Contains("right hand"))
            {
                return candidate;
            }
        }

        return fallbackInteractor;
    }

    private Camera ResolveTargetCamera()
    {
        if (ProgressControl.Instance != null &&
            ProgressControl.Instance.cinemachineBrain != null &&
            ProgressControl.Instance.cinemachineBrain.OutputCamera != null)
        {
            return ProgressControl.Instance.cinemachineBrain.OutputCamera;
        }

        return Camera.main;
    }

    private Transform ResolveHoveredPart()
    {
        if (rightRayInteractor == null)
        {
            return null;
        }

        if (rightRayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hitInfo))
        {
            Transform hitPart = ResolvePartFromTransform(hitInfo.transform);
            if (hitPart != null)
            {
                return hitPart;
            }
        }

        return useRendererBoundsFallback ? ResolvePartByRendererBounds() : null;
    }

    private Transform ResolvePartFromRay(Ray ray, float maxDistance)
    {
        float safeMaxDistance = maxDistance > 0.0001f ? maxDistance : 100f;

        if (Physics.Raycast(ray, out RaycastHit hitInfo, safeMaxDistance, ~0, QueryTriggerInteraction.Collide))
        {
            Transform hitPart = ResolvePartFromTransform(hitInfo.transform);
            if (hitPart != null)
            {
                return hitPart;
            }
        }

        return useRendererBoundsFallback ? ResolvePartByRendererBounds(ray, safeMaxDistance) : null;
    }

    private Transform ResolvePartFromTransform(Transform hitTransform)
    {
        if (hitTransform == null)
        {
            return null;
        }

        for (int i = 0; i < cachedParts.Count; i++)
        {
            Transform part = cachedParts[i];
            if (part == null)
            {
                continue;
            }

            if (hitTransform == part || hitTransform.IsChildOf(part))
            {
                return part;
            }
        }

        return null;
    }

    private Transform ResolvePartByRendererBounds()
    {
        Ray ray = BuildInteractorRay(out float maxDistance);
        return ResolvePartByRendererBounds(ray, maxDistance);
    }

    private Transform ResolvePartByRendererBounds(Ray ray, float maxDistance)
    {
        Transform bestPart = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < cachedParts.Count; i++)
        {
            Transform part = cachedParts[i];
            if (part == null || !part.gameObject.activeInHierarchy)
            {
                continue;
            }

            Renderer[] renderers = part.GetComponentsInChildren<Renderer>(true);
            for (int j = 0; j < renderers.Length; j++)
            {
                Renderer targetRenderer = renderers[j];
                if (targetRenderer == null || !targetRenderer.enabled || !targetRenderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                Bounds bounds = targetRenderer.bounds;
                if (boundsPadding > 0f)
                {
                    bounds.Expand(boundsPadding * 2f);
                }

                if (bounds.IntersectRay(ray, out float distance) &&
                    distance <= maxDistance &&
                    distance < bestDistance)
                {
                    bestDistance = distance;
                    bestPart = part;
                }
            }
        }

        return bestPart;
    }

    private Ray BuildInteractorRay(out float maxDistance)
    {
        Vector3 origin = rightRayInteractor.transform.position;
        Vector3 endPoint = rightRayInteractor.rayEndPoint;
        Vector3 direction = endPoint != Vector3.zero ? endPoint - origin : rightRayInteractor.transform.forward;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = rightRayInteractor.transform.forward;
        }

        maxDistance = rightRayInteractor.maxRaycastDistance;
        if (maxDistance <= 0.0001f)
        {
            maxDistance = 100f;
        }

        return new Ray(origin, direction.normalized);
    }

    private void AddPartIfValid(Transform part)
    {
        if (part == null ||
            part == transform ||
            part == partRoot ||
            (modelExploder != null && part == modelExploder.transform) ||
            cachedParts.Contains(part))
        {
            return;
        }

        cachedParts.Add(part);
    }

    private PartInfo FindPartInfo(Transform part)
    {
        if (part == null || partInfos == null)
        {
            return null;
        }

        for (int i = 0; i < partInfos.Count; i++)
        {
            PartInfo info = partInfos[i];
            if (info == null || info.partTransform == null)
            {
                continue;
            }

            if (part == info.partTransform || part.IsChildOf(info.partTransform) || info.partTransform.IsChildOf(part))
            {
                return info;
            }
        }

        return null;
    }

    private PartExperimentBinding FindPartExperimentBinding(Transform part, PartInfo info)
    {
        if (part == null)
        {
            return null;
        }

        if (partExperimentBindings != null)
        {
            for (int i = 0; i < partExperimentBindings.Count; i++)
            {
                PartExperimentBinding binding = partExperimentBindings[i];
                if (binding == null || binding.partTransform == null)
                {
                    continue;
                }

                if (part == binding.partTransform ||
                    part.IsChildOf(binding.partTransform) ||
                    binding.partTransform.IsChildOf(part))
                {
                    if (binding.numericalApertureExperiment == null)
                    {
                        binding.numericalApertureExperiment = ResolveNumericalApertureExperiment();
                    }

                    return binding;
                }
            }
        }

        if (!autoBindAboveMirrorNumericalAperture || !IsAboveMirrorPart(part, info))
        {
            return null;
        }

        if (runtimeAboveMirrorExperimentBinding == null)
        {
            runtimeAboveMirrorExperimentBinding = new PartExperimentBinding();
        }

        runtimeAboveMirrorExperimentBinding.partTransform = part;
        runtimeAboveMirrorExperimentBinding.buttonText = "Start Numerical Aperture Experiment";
        runtimeAboveMirrorExperimentBinding.numericalApertureExperiment = ResolveNumericalApertureExperiment();
        return runtimeAboveMirrorExperimentBinding;
    }

    private bool IsAboveMirrorPart(Transform part, PartInfo info)
    {
        if (part != null && part.name.IndexOf("AboveMirror", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        if (info == null)
        {
            return false;
        }

        string displayName = info.displayName ?? string.Empty;
        return displayName.Contains("上半") && displayName.Contains("镜");
    }

    private NumericalApertureExperimentController ResolveNumericalApertureExperiment()
    {
        NumericalApertureExperimentController controller =
            FindObjectOfType<NumericalApertureExperimentController>(true);

        if (controller != null)
        {
            controller.ConfigureRuntimeContext(this, modelExploder, rightRayInteractor);
            return controller;
        }

        GameObject runtimeHost = new GameObject("NumericalApertureExperiment_RuntimeHost");
        controller = runtimeHost.AddComponent<NumericalApertureExperimentController>();
        controller.ConfigureRuntimeContext(this, modelExploder, rightRayInteractor);
        DebugLog("Created NumericalApertureExperimentController runtime host for AboveMirror.");
        return controller;
    }

    private void ApplyOtherPartsVisibility(Transform partToKeep, bool visible)
    {
        hiddenRendererStates.Clear();
        hiddenColliderStates.Clear();

        if (hideOtherParts)
        {
            for (int i = 0; i < cachedParts.Count; i++)
            {
                Transform part = cachedParts[i];
                if (part == null || part == partToKeep)
                {
                    continue;
                }

                if (partToKeep != null && (partToKeep.IsChildOf(part) || part.IsChildOf(partToKeep)))
                {
                    continue;
                }

                ApplyTransformVisibility(part, visible, null);
            }
        }

        ApplyAdditionalHideTargetsVisibility(partToKeep, visible);
    }

    private void ApplyAdditionalHideTargetsVisibility(Transform partToKeep, bool visible)
    {
        if (additionalHideTargets == null || additionalHideTargets.Count == 0)
        {
            return;
        }

        for (int i = 0; i < additionalHideTargets.Count; i++)
        {
            Transform target = additionalHideTargets[i];
            if (target == null)
            {
                continue;
            }

            ApplyTransformVisibility(target, visible, partToKeep);
        }
    }

    private void ApplyTransformVisibility(Transform target, bool visible, Transform partToKeep)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer targetRenderer = renderers[i];
            if (targetRenderer == null ||
                hiddenRendererStates.ContainsKey(targetRenderer) ||
                ShouldKeepTransformVisible(targetRenderer.transform, partToKeep))
            {
                continue;
            }

            hiddenRendererStates.Add(targetRenderer, targetRenderer.enabled);
            targetRenderer.enabled = visible;
        }

        if (!disableCollidersForHiddenParts)
        {
            return;
        }

        Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider targetCollider = colliders[i];
            if (targetCollider == null ||
                hiddenColliderStates.ContainsKey(targetCollider) ||
                ShouldKeepTransformVisible(targetCollider.transform, partToKeep))
            {
                continue;
            }

            hiddenColliderStates.Add(targetCollider, targetCollider.enabled);
            targetCollider.enabled = visible;
        }
    }

    private bool ShouldKeepTransformVisible(Transform candidate, Transform partToKeep)
    {
        return candidate != null &&
               partToKeep != null &&
               (candidate == partToKeep ||
                candidate.IsChildOf(partToKeep) ||
                partToKeep.IsChildOf(candidate));
    }

    private void RestoreOtherPartsVisibility()
    {
        foreach (KeyValuePair<Renderer, bool> item in hiddenRendererStates)
        {
            if (item.Key != null)
            {
                item.Key.enabled = item.Value;
            }
        }

        foreach (KeyValuePair<Collider, bool> item in hiddenColliderStates)
        {
            if (item.Key != null)
            {
                item.Key.enabled = item.Value;
            }
        }

        hiddenRendererStates.Clear();
        hiddenColliderStates.Clear();
    }

    private void AnimateSelectedPartToFocus(Transform part)
    {
        if (part == null)
        {
            return;
        }

        KillSelectedPartTween();
        Vector3 targetVisualCenter = GetViewportWorldPosition(selectedPartViewportPosition, selectedPartDistanceFromCamera);
        Vector3 targetPosition = GetSelectedPartTargetPosition(part, targetVisualCenter);
        Vector3 targetScale = selectedPartOriginalLocalScale * selectedPartScaleMultiplier;

        selectedPartTween = DOTween.Sequence();
        selectedPartTween.Join(part.DOMove(targetPosition, selectedPartMoveDuration).SetEase(selectedPartEase));
        selectedPartTween.Join(part.DOScale(targetScale, selectedPartMoveDuration).SetEase(selectedPartEase));
        selectedPartTween.OnKill(() => selectedPartTween = null);
        selectedPartTween.OnComplete(() => selectedPartTween = null);
    }

    private Vector3 GetSelectedPartTargetPosition(Transform part, Vector3 targetVisualCenter)
    {
        if (!alignSelectedPartVisualCenter || part == null)
        {
            return targetVisualCenter;
        }

        Vector3 visualCenterOffset = GetPartVisualCenterWorld(part) - part.position;
        return targetVisualCenter - visualCenterOffset * selectedPartScaleMultiplier;
    }

    private Vector3 GetPartVisualCenterWorld(Transform part)
    {
        if (part == null)
        {
            return selectedPart != null ? selectedPart.position : transform.position;
        }

        Renderer[] renderers = part.GetComponentsInChildren<Renderer>(true);
        if (renderers != null && renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds.center;
        }

        Collider[] colliders = part.GetComponentsInChildren<Collider>(true);
        if (colliders != null && colliders.Length > 0)
        {
            Bounds bounds = colliders[0].bounds;
            for (int i = 1; i < colliders.Length; i++)
            {
                bounds.Encapsulate(colliders[i].bounds);
            }

            return bounds.center;
        }

        return part.position;
    }

    private Vector3 GetViewportWorldPosition(Vector2 viewportPosition, float distance)
    {
        Camera camera = GetActiveCamera();
        if (camera == null)
        {
            return selectedPart != null ? selectedPart.position : transform.position;
        }

        return camera.ViewportToWorldPoint(new Vector3(viewportPosition.x, viewportPosition.y, distance));
    }

    private Camera GetActiveCamera()
    {
        if (targetCamera == null || !targetCamera.isActiveAndEnabled)
        {
            targetCamera = ResolveTargetCamera();
        }

        return targetCamera;
    }

    private void EnsureUi()
    {
        ResolveOrCreateExperimentButton();
        BindExperimentButton();
    }

    private bool HasRequiredUiReferences()
    {
        return uiCanvasGroup != null &&
               partNameText != null &&
               partDescriptionText != null &&
               experimentButton != null;
    }

    private void UpdateUiContent(Transform part, PartInfo info)
    {
        EnsureUi();

        string displayName = info != null && !string.IsNullOrWhiteSpace(info.displayName)
            ? info.displayName
            : part.name;

        string description = info != null && !string.IsNullOrWhiteSpace(info.description)
            ? info.description
            : "No description has been configured for this component yet.";

        if (partNameText != null)
        {
            partNameText.text = displayName;
        }

        if (partDescriptionText != null)
        {
            partDescriptionText.text = description;
        }

        if (experimentButtonText != null)
        {
            experimentButtonText.text = selectedExperimentBinding != null &&
                                        !string.IsNullOrWhiteSpace(selectedExperimentBinding.buttonText)
                ? selectedExperimentBinding.buttonText
                : unavailableExperimentText;
        }

        UpdateExperimentButtonState(selectedExperimentBinding != null);
    }

    private void UpdateExperimentButtonState(bool hasExperiment)
    {
        if (experimentButton == null)
        {
            return;
        }

        experimentButton.interactable = hasExperiment;
        if (hideExperimentButtonWhenUnavailable)
        {
            experimentButton.gameObject.SetActive(hasExperiment);
        }

        if (!hasExperiment)
        {
            StopExperimentButtonHoverPulse();
        }
    }

    private void UpdateUiPose()
    {
        if (uiCanvasGroup == null)
        {
            return;
        }

        Camera camera = GetActiveCamera();
        if (camera == null)
        {
            return;
        }

        Transform uiTransform = uiCanvasGroup.transform;
        Vector3 targetPosition = camera.ViewportToWorldPoint(new Vector3(
            uiViewportPosition.x,
            uiViewportPosition.y,
            uiDistanceFromCamera));
        Quaternion targetRotation = GetUiRotation(camera, targetPosition);
        Vector3 targetWorldScale = Vector3.one * uiWorldScale;

        FitUiPoseInsideCameraView(camera, uiTransform, targetRotation, ref targetPosition, ref targetWorldScale);

        if (lockUiToFixedReferencePose)
        {
            CacheFixedUiReferencePoseIfNeeded(targetPosition, targetRotation, targetWorldScale);
            ApplyUiWorldPose(
                uiTransform,
                fixedUiReferenceWorldPosition,
                fixedUiReferenceWorldRotation,
                fixedUiReferenceWorldScale);
        }
        else
        {
            ApplyUiWorldPose(uiTransform, targetPosition, targetRotation, targetWorldScale);
        }

        Canvas canvas = uiCanvasGroup.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.worldCamera = camera;
        }
    }

    private void FitUiPoseInsideCameraView(
        Camera camera,
        Transform uiTransform,
        Quaternion rotation,
        ref Vector3 position,
        ref Vector3 worldScale)
    {
        if (!fitUiInsideCameraView || camera == null || uiTransform == null)
        {
            return;
        }

        RectTransform rectTransform = uiTransform as RectTransform;
        if (rectTransform == null)
        {
            rectTransform = uiTransform.GetComponent<RectTransform>();
        }

        if (rectTransform == null)
        {
            return;
        }

        ApplyUiWorldPose(uiTransform, position, rotation, worldScale);
        Canvas.ForceUpdateCanvases();

        Vector2 viewportMin;
        Vector2 viewportMax;
        if (!TryGetUiViewportBounds(camera, rectTransform, out viewportMin, out viewportMax))
        {
            return;
        }

        Vector2 padding = new Vector2(
            Mathf.Clamp(uiViewportPadding.x, 0f, 0.45f),
            Mathf.Clamp(uiViewportPadding.y, 0f, 0.45f));

        float availableWidth = Mathf.Max(0.05f, 1f - padding.x * 2f);
        float availableHeight = Mathf.Max(0.05f, 1f - padding.y * 2f);
        float currentWidth = Mathf.Max(0.0001f, viewportMax.x - viewportMin.x);
        float currentHeight = Mathf.Max(0.0001f, viewportMax.y - viewportMin.y);

        float fitScale = Mathf.Min(1f, availableWidth / currentWidth, availableHeight / currentHeight);
        fitScale = Mathf.Clamp(fitScale, Mathf.Clamp(minUiFitScaleMultiplier, 0.1f, 1f), 1f);

        if (fitScale < 0.999f)
        {
            worldScale *= fitScale;
            ApplyUiWorldPose(uiTransform, position, rotation, worldScale);
            Canvas.ForceUpdateCanvases();
            TryGetUiViewportBounds(camera, rectTransform, out viewportMin, out viewportMax);
        }

        Vector2 viewportOffset = Vector2.zero;
        if (viewportMin.x < padding.x)
        {
            viewportOffset.x += padding.x - viewportMin.x;
        }
        else if (viewportMax.x > 1f - padding.x)
        {
            viewportOffset.x -= viewportMax.x - (1f - padding.x);
        }

        if (viewportMin.y < padding.y)
        {
            viewportOffset.y += padding.y - viewportMin.y;
        }
        else if (viewportMax.y > 1f - padding.y)
        {
            viewportOffset.y -= viewportMax.y - (1f - padding.y);
        }

        if (viewportOffset.sqrMagnitude <= 0.0000001f)
        {
            return;
        }

        Vector3 viewportPosition = camera.WorldToViewportPoint(position);
        viewportPosition.x += viewportOffset.x;
        viewportPosition.y += viewportOffset.y;
        position = camera.ViewportToWorldPoint(viewportPosition);
    }

    private bool TryGetUiViewportBounds(
        Camera camera,
        RectTransform rectTransform,
        out Vector2 viewportMin,
        out Vector2 viewportMax)
    {
        viewportMin = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        viewportMax = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

        if (camera == null || rectTransform == null)
        {
            return false;
        }

        Vector3[] worldCorners = new Vector3[4];
        rectTransform.GetWorldCorners(worldCorners);

        bool hasValidCorner = false;
        for (int i = 0; i < worldCorners.Length; i++)
        {
            Vector3 viewportPoint = camera.WorldToViewportPoint(worldCorners[i]);
            if (viewportPoint.z <= 0f)
            {
                continue;
            }

            hasValidCorner = true;
            viewportMin.x = Mathf.Min(viewportMin.x, viewportPoint.x);
            viewportMin.y = Mathf.Min(viewportMin.y, viewportPoint.y);
            viewportMax.x = Mathf.Max(viewportMax.x, viewportPoint.x);
            viewportMax.y = Mathf.Max(viewportMax.y, viewportPoint.y);
        }

        return hasValidCorner;
    }

    private void CacheFixedUiReferencePoseIfNeeded(Vector3 position, Quaternion rotation, Vector3 worldScale)
    {
        if (hasFixedUiReferencePose)
        {
            return;
        }

        fixedUiReferenceWorldPosition = position;
        fixedUiReferenceWorldRotation = rotation;
        fixedUiReferenceWorldScale = worldScale;
        hasFixedUiReferencePose = true;
    }

    private void ApplyUiWorldPose(Transform uiTransform, Vector3 position, Quaternion rotation, Vector3 worldScale)
    {
        if (uiTransform == null)
        {
            return;
        }

        uiTransform.position = position;
        uiTransform.rotation = rotation;
        SetWorldScale(uiTransform, worldScale);
    }

    private Quaternion GetUiRotation(Camera camera, Vector3 uiPosition)
    {
        if (camera == null)
        {
            return Quaternion.identity;
        }

        if (!keepUiWorldUpright)
        {
            return camera.transform.rotation;
        }

        Vector3 flatForward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up);
        if (flatForward.sqrMagnitude < 0.000001f)
        {
            flatForward = Vector3.ProjectOnPlane(uiPosition - camera.transform.position, Vector3.up);
        }

        if (flatForward.sqrMagnitude < 0.000001f)
        {
            flatForward = Vector3.forward;
        }

        return Quaternion.LookRotation(flatForward.normalized, Vector3.up);
    }

    private void SetWorldScale(Transform target, Vector3 worldScale)
    {
        if (target == null)
        {
            return;
        }

        Transform parent = target.parent;
        if (parent == null)
        {
            target.localScale = worldScale;
            return;
        }

        Vector3 parentScale = parent.lossyScale;
        target.localScale = new Vector3(
            SafeDivideScale(worldScale.x, parentScale.x),
            SafeDivideScale(worldScale.y, parentScale.y),
            SafeDivideScale(worldScale.z, parentScale.z));
    }

    private float SafeDivideScale(float targetScale, float parentScale)
    {
        return Mathf.Abs(parentScale) > 0.000001f ? targetScale / parentScale : targetScale;
    }

    private void ShowUi()
    {
        if (uiCanvasGroup == null)
        {
            return;
        }

        KillUiFadeTween();
        uiCanvasGroup.gameObject.SetActive(true);
        uiCanvasGroup.blocksRaycasts = true;
        uiCanvasGroup.interactable = true;
        uiCanvasGroup.alpha = 0f;
        uiFadeTween = uiCanvasGroup
            .DOFade(1f, uiFadeDuration)
            .SetEase(uiFadeEase)
            .OnKill(() => uiFadeTween = null)
            .OnComplete(() => uiFadeTween = null);
    }

    private void HideUi(bool immediate)
    {
        if (uiCanvasGroup == null)
        {
            return;
        }

        KillUiFadeTween();
        uiCanvasGroup.blocksRaycasts = false;
        uiCanvasGroup.interactable = false;

        if (immediate)
        {
            HideUiImmediate();
            return;
        }

        uiFadeTween = uiCanvasGroup
            .DOFade(0f, uiFadeDuration * 0.65f)
            .SetEase(uiFadeEase)
            .OnKill(() => uiFadeTween = null)
            .OnComplete(() =>
            {
                if (uiCanvasGroup != null)
                {
                    uiCanvasGroup.gameObject.SetActive(false);
                }

                uiFadeTween = null;
            });
    }

    private void HideUiImmediate()
    {
        KillUiFadeTween();

        if (uiCanvasGroup == null)
        {
            return;
        }

        uiCanvasGroup.alpha = 0f;
        uiCanvasGroup.blocksRaycasts = false;
        uiCanvasGroup.interactable = false;
        uiCanvasGroup.gameObject.SetActive(false);
    }

    private void CacheAndEnableUiInteractionForSelection()
    {
        if (rightRayInteractor == null || rightRayUiInteractionCached)
        {
            return;
        }

        originalRightRayUiInteraction = rightRayInteractor.enableUIInteraction;
        rightRayUiInteractionCached = true;
        rightRayInteractor.enableUIInteraction = true;
    }

    private void RestoreUiInteractionAfterSelection()
    {
        if (rightRayInteractor == null || !rightRayUiInteractionCached)
        {
            return;
        }

        rightRayInteractor.enableUIInteraction = originalRightRayUiInteraction;
        rightRayUiInteractionCached = false;
    }

    private bool IsPointingAtSelectionUi()
    {
        if (uiCanvasGroup == null || rightRayInteractor == null)
        {
            return false;
        }

        if (rightRayInteractor.TryGetCurrentUIRaycastResult(out RaycastResult result) &&
            result.gameObject != null &&
            result.gameObject.transform != null &&
            result.gameObject.transform.IsChildOf(uiCanvasGroup.transform))
        {
            return true;
        }

        RectTransform uiRect = uiCanvasGroup.transform as RectTransform;
        if (uiRect == null)
        {
            uiRect = uiCanvasGroup.GetComponent<RectTransform>();
        }

        if (uiRect == null)
        {
            return false;
        }

        Ray ray = BuildInteractorRay(out float maxDistance);
        return IsRayPointingAtRectTransform(ray, maxDistance, uiRect);
    }

    private bool TryInvokeExperimentButtonFromPointer(bool allowMousePointer)
    {
        if (experimentButton == null || !experimentButton.interactable || !experimentButton.gameObject.activeInHierarchy)
        {
            return false;
        }

        if (!allowMousePointer)
        {
            if (IsExperimentButtonHoveredByRightRay())
            {
                InvokeCurrentExperiment();
                return true;
            }

            return false;
        }

        if (EventSystem.current == null)
        {
            return false;
        }

        PointerEventData pointerEventData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerEventData, results);

        for (int i = 0; i < results.Count; i++)
        {
            if (IsTransformChildOf(results[i].gameObject != null ? results[i].gameObject.transform : null,
                    experimentButton.transform))
            {
                InvokeCurrentExperiment();
                return true;
            }
        }

        return false;
    }

    private void UpdateExperimentButtonHoverPulse()
    {
        bool shouldPulse = animateExperimentButtonOnHover && IsExperimentButtonPointerHovered();
        SetExperimentButtonHoverPulse(shouldPulse);
    }

    private bool IsExperimentButtonPointerHovered()
    {
        if (experimentButton == null || !experimentButton.interactable || !experimentButton.gameObject.activeInHierarchy)
        {
            return false;
        }

        return IsExperimentButtonHoveredByRightRay() || IsExperimentButtonHoveredByMouse();
    }

    private bool IsExperimentButtonHoveredByRightRay()
    {
        if (rightRayInteractor == null || experimentButton == null)
        {
            return false;
        }

        if (rightRayInteractor.TryGetCurrentUIRaycastResult(out RaycastResult xrResult) &&
            IsTransformChildOf(xrResult.gameObject != null ? xrResult.gameObject.transform : null,
                experimentButton.transform))
        {
            return true;
        }

        RectTransform buttonRect = experimentButton.transform as RectTransform;
        if (buttonRect == null)
        {
            buttonRect = experimentButton.GetComponent<RectTransform>();
        }

        if (buttonRect == null)
        {
            return false;
        }

        Ray ray = BuildInteractorRay(out float maxDistance);
        return IsRayPointingAtRectTransform(ray, maxDistance, buttonRect);
    }

    private bool IsExperimentButtonHoveredByMouse()
    {
        if (EventSystem.current == null || experimentButton == null)
        {
            return false;
        }

        PointerEventData pointerEventData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerEventData, results);

        for (int i = 0; i < results.Count; i++)
        {
            if (IsTransformChildOf(results[i].gameObject != null ? results[i].gameObject.transform : null,
                    experimentButton.transform))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsDesktopRayPointingAtExperimentButton(Ray ray, float maxDistance)
    {
        if (experimentButton == null || !experimentButton.interactable || !experimentButton.gameObject.activeInHierarchy)
        {
            return false;
        }

        RectTransform buttonRect = experimentButton.transform as RectTransform;
        if (buttonRect == null)
        {
            buttonRect = experimentButton.GetComponent<RectTransform>();
        }

        if (!IsRayPointingAtRectTransform(ray, maxDistance, buttonRect))
        {
            return false;
        }

        InvokeCurrentExperiment();
        return true;
    }

    private bool IsDesktopRayPointingAtSelectionUi(Ray ray, float maxDistance)
    {
        if (uiCanvasGroup == null)
        {
            return false;
        }

        RectTransform uiRect = uiCanvasGroup.transform as RectTransform;
        if (uiRect == null)
        {
            uiRect = uiCanvasGroup.GetComponent<RectTransform>();
        }

        return IsRayPointingAtRectTransform(ray, maxDistance, uiRect);
    }

    private bool IsRayPointingAtRectTransform(Ray ray, float maxDistance, RectTransform rectTransform)
    {
        if (rectTransform == null)
        {
            return false;
        }

        Plane plane = new Plane(rectTransform.forward, rectTransform.position);
        if (!plane.Raycast(ray, out float enter) || enter < 0f || enter > maxDistance)
        {
            return false;
        }

        Vector3 localPoint = rectTransform.InverseTransformPoint(ray.GetPoint(enter));
        return rectTransform.rect.Contains(new Vector2(localPoint.x, localPoint.y));
    }

    private void SetExperimentButtonHoverPulse(bool shouldPulse)
    {
        if (!shouldPulse)
        {
            StopExperimentButtonHoverPulse();
            return;
        }

        CacheExperimentButtonOriginalScale();
        if (!hasExperimentButtonOriginalScale || experimentButtonHoverTween != null && experimentButtonHoverTween.IsActive())
        {
            return;
        }

        experimentButtonHoverTween = cachedExperimentButtonTransform
            .DOScale(experimentButtonOriginalLocalScale * experimentButtonHoverScaleMultiplier,
                experimentButtonHoverPulseDuration)
            .SetEase(experimentButtonHoverEase)
            .SetLoops(-1, LoopType.Yoyo)
            .OnKill(() => experimentButtonHoverTween = null);
    }

    private void StopExperimentButtonHoverPulse()
    {
        if (experimentButtonHoverTween != null && experimentButtonHoverTween.IsActive())
        {
            experimentButtonHoverTween.Kill(false);
        }

        experimentButtonHoverTween = null;
        if (hasExperimentButtonOriginalScale && cachedExperimentButtonTransform != null)
        {
            cachedExperimentButtonTransform.localScale = experimentButtonOriginalLocalScale;
        }
    }

    private void CacheExperimentButtonOriginalScale()
    {
        if (experimentButton == null)
        {
            return;
        }

        Transform buttonTransform = experimentButton.transform;
        if (buttonTransform == cachedExperimentButtonTransform && hasExperimentButtonOriginalScale)
        {
            return;
        }

        cachedExperimentButtonTransform = buttonTransform;
        experimentButtonOriginalLocalScale = buttonTransform.localScale;
        hasExperimentButtonOriginalScale = true;
    }

    private bool IsTransformChildOf(Transform candidate, Transform root)
    {
        return candidate != null && root != null && (candidate == root || candidate.IsChildOf(root));
    }

    private void BindExperimentButton()
    {
        ResolveOrCreateExperimentButton();

        if (experimentButton == null || experimentButtonBound)
        {
            return;
        }

        experimentButton.onClick.AddListener(InvokeCurrentExperiment);
        experimentButtonBound = true;
    }

    private void UnbindExperimentButton()
    {
        if (experimentButton == null || !experimentButtonBound)
        {
            return;
        }

        experimentButton.onClick.RemoveListener(InvokeCurrentExperiment);
        experimentButtonBound = false;
    }

    private void ResolveOrCreateExperimentButton()
    {
        if (experimentButton == null && uiCanvasGroup != null)
        {
            experimentButton = uiCanvasGroup.GetComponentInChildren<Button>(true);
        }

        if (experimentButton == null || uiCanvasGroup == null)
        {
            return;
        }

        Graphic targetGraphic = experimentButton.targetGraphic;
        if (targetGraphic == null)
        {
            Image image = experimentButton.GetComponent<Image>();
            if (image == null)
            {
                image = experimentButton.gameObject.AddComponent<Image>();
            }

            experimentButton.targetGraphic = image;
        }

        if (experimentButtonText == null)
        {
            experimentButtonText = experimentButton.GetComponentInChildren<TextMeshProUGUI>(true);
        }
    }

    private void SetHoverPulseComponentsEnabled(bool enabled)
    {
        if (!enabled)
        {
            disabledHoverPulseComponents.Clear();
            SuperAssemblyPartHoverPulse[] components = GetComponentsInChildren<SuperAssemblyPartHoverPulse>(true);
            for (int i = 0; i < components.Length; i++)
            {
                SuperAssemblyPartHoverPulse component = components[i];
                if (component == null || !component.enabled)
                {
                    continue;
                }

                component.enabled = false;
                disabledHoverPulseComponents.Add(component);
            }

            return;
        }

        for (int i = 0; i < disabledHoverPulseComponents.Count; i++)
        {
            if (disabledHoverPulseComponents[i] != null)
            {
                disabledHoverPulseComponents[i].enabled = true;
            }
        }

        disabledHoverPulseComponents.Clear();
    }

    private void KillSelectedPartTween()
    {
        if (selectedPartTween != null && selectedPartTween.IsActive())
        {
            selectedPartTween.Kill(false);
        }

        selectedPartTween = null;
    }

    private void KillUiFadeTween()
    {
        if (uiFadeTween != null && uiFadeTween.IsActive())
        {
            uiFadeTween.Kill(false);
        }

        uiFadeTween = null;
    }

    private static string GetHierarchyPath(Transform target)
    {
        if (target == null)
        {
            return string.Empty;
        }

        string path = target.name;
        Transform current = target.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }

    private void DebugLog(string message)
    {
        if (!enableDebugLogs)
        {
            return;
        }

        Debug.Log($"[SuperAssemblyPartSelectionController] {message}", this);
    }

    private void DebugLogWarning(string message)
    {
        if (!enableDebugLogs)
        {
            return;
        }

        Debug.LogWarning($"[SuperAssemblyPartSelectionController] {message}", this);
    }
}
