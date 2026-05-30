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

    [SerializeField] private bool useRendererBoundsFallback = true;

    [Min(0f)]
    [SerializeField] private float boundsPadding = 0.005f;

    [Header("Selection Pose")]
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

    [Header("UI")]
    [SerializeField] private CanvasGroup uiCanvasGroup;
    [SerializeField] private TextMeshProUGUI partNameText;
    [SerializeField] private TextMeshProUGUI partDescriptionText;
    [SerializeField] private Button experimentButton;
    [SerializeField] private TextMeshProUGUI experimentButtonText;
    [SerializeField] private Vector2 uiViewportPosition = new Vector2(0.73f, 0.5f);

    [Min(0.1f)]
    [SerializeField] private float uiDistanceFromCamera = 1.15f;

    [Min(0.0001f)]
    [SerializeField] private float uiWorldScale = 0.00135f;

    [Min(0.01f)]
    [SerializeField] private float uiFadeDuration = 0.28f;

    [SerializeField] private Ease uiFadeEase = Ease.OutCubic;

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
    private Vector3 selectedPartOriginalWorldPosition;
    private Vector3 selectedPartOriginalLocalScale;
    private Sequence selectedPartTween;
    private Tween uiFadeTween;
    private bool isSelectionActive;
    private bool experimentButtonBound;
    private bool rightRayUiInteractionCached;
    private bool originalRightRayUiInteraction;

    public static bool HasActiveSelection => Instance != null && Instance.isSelectionActive;
    public bool IsSelectionActive => isSelectionActive;
    public bool CanSelectParts => IsInReadySuperAssemblyState();
    public Transform CurrentSelectedPart => selectedPart;

    public static bool TryHandleRightTrigger()
    {
        SuperAssemblyPartSelectionController controller = ResolveAvailableInstance();
        return controller != null && controller.TryHandleRightTriggerInternal();
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

    public void InvokeCurrentExperiment()
    {
        if (!isSelectionActive || selectedPart == null)
        {
            return;
        }

        selectedPartInfo?.onExperimentButtonClicked?.Invoke();
        onExperimentRequested?.Invoke();
        DebugLog($"Experiment requested for part='{selectedPart.name}'");
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

        UpdateSelectedPartTargetPose(false);
        UpdateUiPose();
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

        if (isSelectionActive)
        {
            if (IsPointingAtSelectionUi())
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

    private void EnterSelection(Transform part)
    {
        if (part == null)
        {
            return;
        }

        if (!HasRequiredUiReferences())
        {
            DebugLogWarning("Selection UI references are missing. Please assign CanvasGroup, Name Text, Description Text, and Experiment Button in the scene.");
            return;
        }

        if (isSelectionActive)
        {
            ExitSelection(true);
        }

        selectedPart = part;
        selectedPartInfo = FindPartInfo(part);
        selectedPartOriginalWorldPosition = part.position;
        selectedPartOriginalLocalScale = part.localScale;
        isSelectionActive = true;

        SetHoverPulseComponentsEnabled(false);
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
            return;
        }

        Transform partToRestore = selectedPart;
        KillSelectedPartTween();
        RestoreOtherPartsVisibility();
        HideUi(immediate);
        RestoreUiInteractionAfterSelection();

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

    private void ApplyOtherPartsVisibility(Transform partToKeep, bool visible)
    {
        if (!hideOtherParts)
        {
            return;
        }

        hiddenRendererStates.Clear();
        hiddenColliderStates.Clear();

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

            Renderer[] renderers = part.GetComponentsInChildren<Renderer>(true);
            for (int j = 0; j < renderers.Length; j++)
            {
                Renderer targetRenderer = renderers[j];
                if (targetRenderer == null || hiddenRendererStates.ContainsKey(targetRenderer))
                {
                    continue;
                }

                hiddenRendererStates.Add(targetRenderer, targetRenderer.enabled);
                targetRenderer.enabled = visible;
            }

            if (!disableCollidersForHiddenParts)
            {
                continue;
            }

            Collider[] colliders = part.GetComponentsInChildren<Collider>(true);
            for (int j = 0; j < colliders.Length; j++)
            {
                Collider targetCollider = colliders[j];
                if (targetCollider == null || hiddenColliderStates.ContainsKey(targetCollider))
                {
                    continue;
                }

                hiddenColliderStates.Add(targetCollider, targetCollider.enabled);
                targetCollider.enabled = visible;
            }
        }
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
        Vector3 targetPosition = GetViewportWorldPosition(selectedPartViewportPosition, selectedPartDistanceFromCamera);
        Vector3 targetScale = selectedPartOriginalLocalScale * selectedPartScaleMultiplier;

        selectedPartTween = DOTween.Sequence();
        selectedPartTween.Join(part.DOMove(targetPosition, selectedPartMoveDuration).SetEase(selectedPartEase));
        selectedPartTween.Join(part.DOScale(targetScale, selectedPartMoveDuration).SetEase(selectedPartEase));
        selectedPartTween.OnKill(() => selectedPartTween = null);
        selectedPartTween.OnComplete(() => selectedPartTween = null);
    }

    private void UpdateSelectedPartTargetPose(bool animate)
    {
        if (selectedPart == null)
        {
            return;
        }

        if (animate || selectedPartTween != null && selectedPartTween.IsActive())
        {
            return;
        }

        selectedPart.position = GetViewportWorldPosition(selectedPartViewportPosition, selectedPartDistanceFromCamera);
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
            experimentButtonText.text = "Related Experiment";
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
        uiTransform.position = camera.ViewportToWorldPoint(new Vector3(
            uiViewportPosition.x,
            uiViewportPosition.y,
            uiDistanceFromCamera));
        uiTransform.rotation = camera.transform.rotation;
        uiTransform.localScale = Vector3.one * uiWorldScale;

        Canvas canvas = uiCanvasGroup.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.worldCamera = camera;
        }
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

        if (!rightRayInteractor.TryGetCurrentUIRaycastResult(out RaycastResult result))
        {
            return false;
        }

        return result.gameObject != null &&
               result.gameObject.transform != null &&
               result.gameObject.transform.IsChildOf(uiCanvasGroup.transform);
    }

    private void BindExperimentButton()
    {
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
