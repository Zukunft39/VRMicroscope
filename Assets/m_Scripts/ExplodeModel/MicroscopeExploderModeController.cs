using Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR.Interaction.Toolkit;

public class MicroscopeExploderModeController : MonoBehaviour
{
    public enum AssemblyMode
    {
        Normal,
        PreAssembly,
        SuperAssembly
    }

    public static MicroscopeExploderModeController Instance { get; private set; }

    private static int tutorialModeLockCount = 0;

    [Header("Core References")]
    [SerializeField] private XRRayInteractor rightRayInteractor;
    [SerializeField] private ProgressControl progressControl;
    [SerializeField] private CinemachineVirtualCamera preAssemblyCamera;
    [SerializeField] private GameObject microscopeRoot;
    [SerializeField] private GameObject microscopeExploderRoot;
    [SerializeField] private ModelExploder modelExploder;
    [SerializeField] private SuperAssemblyPartSelectionController partSelectionController;

    [SerializeField, HideInInspector, FormerlySerializedAs("preAssemblyCameraObject")]
    private GameObject legacyPreAssemblyCameraObject;

    [Header("Mode Options")]
    [SerializeField] private bool disableLocomotionWhileInAssemblyModes = true;
    [SerializeField] private bool animateExploderTransitions = true;
    [SerializeField] private bool copyFieldOfViewFromCurrentView = true;
    [SerializeField] private bool allowMicroscopeAsAssemblyTarget = true;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    public AssemblyMode CurrentMode => currentMode;
    public static bool IsTutorialModeLocked => tutorialModeLockCount > 0;

    private AssemblyMode currentMode = AssemblyMode.Normal;
    private Renderer[] cachedExploderRenderers;
    private Renderer[] cachedMicroscopeRenderers;
    private GameObject cachedLocomotionRoot;
    private XRInteractorLineVisual cachedRightLineVisual;
    private ModelExploder subscribedModelExploder;
    private bool pendingMicroscopeRestoreAfterAssemble;
    private bool rayPresentationCached;
    private bool originalEnableUiInteraction;
    private bool originalHitClosestOnly;
    private bool originalOverrideInteractorLineLength;
    private bool originalAutoAdjustLineLength;
    private bool originalUseDistanceToHitAsMaxLineLength;
    private float originalLineLength;
    private float originalMinLineLength;

    public static bool TryHandleRightTrigger()
    {
        if (IsTutorialModeLocked)
        {
            MicroscopeExploderModeController lockedController = ResolveAvailableInstance();
            lockedController?.ApplyTutorialModeLockState();
            return true;
        }

        MicroscopeExploderModeController controller = ResolveAvailableInstance();
        return controller != null && controller.TryHandleRightTriggerInRoaming();
    }

    public static void PushTutorialModeLock()
    {
        tutorialModeLockCount++;
        MicroscopeExploderModeController controller = ResolveAvailableInstance();
        controller?.ApplyTutorialModeLockState();
    }

    public static void PopTutorialModeLock()
    {
        tutorialModeLockCount = Mathf.Max(0, tutorialModeLockCount - 1);
        MicroscopeExploderModeController controller = ResolveAvailableInstance();
        if (controller != null && IsTutorialModeLocked)
        {
            controller.ApplyTutorialModeLockState();
        }
    }

    private void Awake()
    {
        MicroscopeExploderModeController preferredInstance = ChoosePreferredInstance(Instance, this);
        if (preferredInstance != this)
        {
            enabled = false;
            return;
        }

        Instance = this;
        RefreshModelExploderSubscription();

        if (preferredInstance != null && preferredInstance != this)
        {
            preferredInstance.enabled = false;
        }
    }

    private void OnEnable()
    {
        EnsureReferences();
        RefreshModelExploderSubscription();
        ApplyModeObjectVisibility(forceExploderReset: true);
    }

    private void OnDisable()
    {
        if (currentMode != AssemblyMode.Normal)
        {
            ReturnToNormalOperation();
        }

        RestoreDefaultRayPresentation();
        UnsubscribeFromModelExploder();
    }

    private void OnDestroy()
    {
        RestoreDefaultRayPresentation();
        UnsubscribeFromModelExploder();

        if (Instance == this)
        {
            Instance = null;
            ResolveAvailableInstance();
        }
    }

    private static MicroscopeExploderModeController ResolveAvailableInstance()
    {
        if (Instance != null && Instance.enabled)
        {
            return Instance;
        }

        MicroscopeExploderModeController[] controllers = FindObjectsOfType<MicroscopeExploderModeController>(true);
        MicroscopeExploderModeController bestController = null;
        for (int i = 0; i < controllers.Length; i++)
        {
            bestController = ChoosePreferredInstance(bestController, controllers[i]);
        }

        if (bestController != null)
        {
            Instance = bestController;
        }

        return bestController;
    }

    private static MicroscopeExploderModeController ChoosePreferredInstance(
        MicroscopeExploderModeController first,
        MicroscopeExploderModeController second)
    {
        if (first == null)
        {
            return second;
        }

        if (second == null)
        {
            return first;
        }

        int firstScore = first.GetConfigurationScore();
        int secondScore = second.GetConfigurationScore();
        if (secondScore > firstScore)
        {
            return second;
        }

        if (secondScore < firstScore)
        {
            return first;
        }

        bool firstActive = first.isActiveAndEnabled;
        bool secondActive = second.isActiveAndEnabled;
        if (secondActive && !firstActive)
        {
            return second;
        }

        return first;
    }

    private int GetConfigurationScore()
    {
        int score = 0;
        if (rightRayInteractor != null) score += 3;
        if (progressControl != null) score += 2;
        if (preAssemblyCamera != null || legacyPreAssemblyCameraObject != null) score += 3;
        if (microscopeRoot != null) score += 1;
        if (microscopeExploderRoot != null) score += 2;
        if (modelExploder != null) score += 2;
        if (partSelectionController != null) score += 1;
        return score;
    }

    public bool TryHandleRightTriggerInRoaming()
    {
        if (IsTutorialModeLocked)
        {
            ApplyTutorialModeLockState();
            return true;
        }

        if (!isActiveAndEnabled)
        {
            return false;
        }

        if (!EnsureReferences())
        {
            return false;
        }

        bool isPointingAtExploder = IsPointingAtAssemblyTarget();
        DebugLog($"TryHandleRightTriggerInRoaming mode={currentMode}, pointingAtAssemblyTarget={isPointingAtExploder}");
        switch (currentMode)
        {
            case AssemblyMode.Normal:
                if (!isPointingAtExploder)
                {
                    return false;
                }

                EnterPreAssembly();
                return true;

            case AssemblyMode.PreAssembly:
                if (isPointingAtExploder)
                {
                    EnterSuperAssembly();
                }
                else
                {
                    ReturnToNormalOperation();
                }

                return true;

            case AssemblyMode.SuperAssembly:
                if (SuperAssemblyPartSelectionController.TryHandleRightTrigger())
                {
                    return true;
                }

                if (isPointingAtExploder)
                {
                    return true;
                }

                ExitSuperAssemblyToPreAssembly();
                return true;
        }

        return false;
    }

    public void EnterPreAssembly()
    {
        if (!EnsureReferences())
        {
            return;
        }

        SyncPresetCameraFieldOfView();
        SwitchToPreAssemblyView();
        SetLocomotionEnabled(false);
        ApplyAssemblyRayPresentation();
        pendingMicroscopeRestoreAfterAssemble = false;
        currentMode = AssemblyMode.PreAssembly;
        partSelectionController?.ForceExitSelection(true);
        UpdateModelsVisibility();
        SetExploderState(false, true);
        DebugLog("Entered PreAssembly mode.");
    }

    public void EnterSuperAssembly()
    {
        if (!EnsureReferences())
        {
            return;
        }

        SyncPresetCameraFieldOfView();
        SwitchToPreAssemblyView();
        SetLocomotionEnabled(false);
        ApplyAssemblyRayPresentation();
        pendingMicroscopeRestoreAfterAssemble = false;
        currentMode = AssemblyMode.SuperAssembly;
        partSelectionController?.ForceExitSelection(true);
        UpdateModelsVisibility();
        SetExploderState(true, false);
        DebugLog("Entered SuperAssembly mode.");
    }

    public void ExitSuperAssemblyToPreAssembly()
    {
        if (!EnsureReferences())
        {
            return;
        }

        SyncPresetCameraFieldOfView();
        SwitchToPreAssemblyView();
        SetLocomotionEnabled(false);
        ApplyAssemblyRayPresentation();
        pendingMicroscopeRestoreAfterAssemble = false;
        partSelectionController?.ForceExitSelection(true);
        currentMode = AssemblyMode.PreAssembly;
        UpdateModelsVisibility();
        SetExploderState(false, false);
        DebugLog("Returned from SuperAssembly to PreAssembly mode.");
    }

    public void ReturnToNormalOperation()
    {
        pendingMicroscopeRestoreAfterAssemble = false;
        partSelectionController?.ForceExitSelection(true);
        currentMode = AssemblyMode.Normal;
        UpdateModelsVisibility();
        SetExploderState(false, true);
        RestoreDefaultRayPresentation();
        SwitchToFreeView();
        SetLocomotionEnabled(true);
        XRActionReferenceGuard.RepairAllTurnProviders();
        DebugLog("Returned to Normal mode.");
    }

    private void ApplyTutorialModeLockState()
    {
        if (!EnsureReferences())
        {
            return;
        }

        if (currentMode != AssemblyMode.Normal)
        {
            ReturnToNormalOperation();
        }
        else
        {
            pendingMicroscopeRestoreAfterAssemble = false;
            UpdateModelsVisibility();
            SetExploderState(false, true);
            RestoreDefaultRayPresentation();
        }

        DebugLog($"Tutorial mode lock active. lockCount={tutorialModeLockCount}");
    }

    private bool EnsureReferences()
    {
        if (rightRayInteractor == null)
        {
            rightRayInteractor = FindPreferredRightRayInteractor();
        }

        if (progressControl == null)
        {
            progressControl = ProgressControl.Instance;
        }

        if (progressControl == null)
        {
            progressControl = FindObjectOfType<ProgressControl>();
        }

        if (preAssemblyCamera == null && legacyPreAssemblyCameraObject != null)
        {
            preAssemblyCamera = legacyPreAssemblyCameraObject.GetComponent<CinemachineVirtualCamera>();
        }

        if (modelExploder == null && microscopeExploderRoot != null)
        {
            modelExploder = microscopeExploderRoot.GetComponent<ModelExploder>();
        }

        EnsurePartSelectionController();

        if (cachedRightLineVisual == null && rightRayInteractor != null)
        {
            cachedRightLineVisual = rightRayInteractor.GetComponent<XRInteractorLineVisual>();
        }

        RefreshModelExploderSubscription();

        if (cachedExploderRenderers == null || cachedExploderRenderers.Length == 0)
        {
            CacheExploderRenderers();
        }

        if (allowMicroscopeAsAssemblyTarget &&
            (cachedMicroscopeRenderers == null || cachedMicroscopeRenderers.Length == 0))
        {
            CacheMicroscopeRenderers();
        }

        if (cachedLocomotionRoot == null && Interactor.Instance != null && Interactor.Instance.xrLocomotionSys != null)
        {
            cachedLocomotionRoot = Interactor.Instance.xrLocomotionSys.gameObject;
        }

        bool isReady = rightRayInteractor != null &&
                       progressControl != null &&
                       progressControl.cinemachineBrain != null &&
                       preAssemblyCamera != null &&
                       microscopeExploderRoot != null &&
                       modelExploder != null &&
                       microscopeRoot != null;

        if (!isReady)
        {
            DebugLogWarning("Missing references. Please assign Right Ray Interactor, ProgressControl, PreAssembly Camera (CinemachineVirtualCamera), Microscope Exploder Root, and ModelExploder.");
        }
        else
        {
            DebugLog(
                $"EnsureReferences OK. controller='{name}', ray='{rightRayInteractor.name}', preAssemblyCamera='{preAssemblyCamera.name}', microscope='{GetSafeObjectName(microscopeRoot)}', exploder='{GetSafeObjectName(microscopeExploderRoot)}'");
        }

        return isReady;
    }

    private void EnsurePartSelectionController()
    {
        if (partSelectionController == null && microscopeExploderRoot != null)
        {
            partSelectionController = microscopeExploderRoot.GetComponentInChildren<SuperAssemblyPartSelectionController>(true);
        }

        if (partSelectionController == null)
        {
            partSelectionController = GetComponentInChildren<SuperAssemblyPartSelectionController>(true);
        }

        if (partSelectionController != null)
        {
            Transform rootTransform = microscopeExploderRoot != null ? microscopeExploderRoot.transform : null;
            partSelectionController.Configure(this, rightRayInteractor, modelExploder, rootTransform);
        }
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

    private void CacheExploderRenderers()
    {
        if (microscopeExploderRoot == null)
        {
            cachedExploderRenderers = null;
            return;
        }

        cachedExploderRenderers = microscopeExploderRoot.GetComponentsInChildren<Renderer>(true);
        DebugLog($"CacheExploderRenderers count={cachedExploderRenderers.Length}");
    }

    private void CacheMicroscopeRenderers()
    {
        if (microscopeRoot == null)
        {
            cachedMicroscopeRenderers = null;
            return;
        }

        cachedMicroscopeRenderers = microscopeRoot.GetComponentsInChildren<Renderer>(true);
        DebugLog($"CacheMicroscopeRenderers count={cachedMicroscopeRenderers.Length}");
    }

    private void RefreshModelExploderSubscription()
    {
        if (subscribedModelExploder == modelExploder)
        {
            return;
        }

        if (subscribedModelExploder != null)
        {
            subscribedModelExploder.ExplodedStateApplied -= HandleExploderStateApplied;
        }

        subscribedModelExploder = modelExploder;

        if (subscribedModelExploder != null)
        {
            subscribedModelExploder.ExplodedStateApplied += HandleExploderStateApplied;
        }
    }

    private void UnsubscribeFromModelExploder()
    {
        if (subscribedModelExploder == null)
        {
            return;
        }

        subscribedModelExploder.ExplodedStateApplied -= HandleExploderStateApplied;
        subscribedModelExploder = null;
    }

    private void HandleExploderStateApplied(bool isExploded)
    {
        DebugLog(
            $"HandleExploderStateApplied isExploded={isExploded}, pendingMicroscopeRestoreAfterAssemble={pendingMicroscopeRestoreAfterAssemble}, currentMode={currentMode}");

        if (isExploded)
        {
            return;
        }

        if (pendingMicroscopeRestoreAfterAssemble && currentMode == AssemblyMode.PreAssembly)
        {
            pendingMicroscopeRestoreAfterAssemble = false;
            UpdateModelsVisibility();
            DebugLog("Microscope restored after exploder assemble completed.");
        }
    }

    private bool IsPointingAtAssemblyTarget()
    {
        if (rightRayInteractor == null || (microscopeExploderRoot == null && microscopeRoot == null))
        {
            return false;
        }

        bool canTargetMicroscope = currentMode == AssemblyMode.Normal || allowMicroscopeAsAssemblyTarget;

        if (rightRayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hitInfo))
        {
            if (IsTransformPartOfExploder(hitInfo.transform))
            {
                DebugLog($"Raycast hit exploder directly: '{GetHierarchyPath(hitInfo.transform)}'");
                return true;
            }

            if (canTargetMicroscope && IsTransformPartOfMicroscope(hitInfo.transform))
            {
                DebugLog($"Raycast hit microscope directly: '{GetHierarchyPath(hitInfo.transform)}'");
                return true;
            }

            DebugLog($"Raycast hit non-target object first: '{GetHierarchyPath(hitInfo.transform)}'. Continue bounds test.");
        }

        bool hitExploderBounds = DoesInteractorRayHitBounds(cachedExploderRenderers, microscopeExploderRoot, "Exploder");
        if (hitExploderBounds)
        {
            return true;
        }

        if (canTargetMicroscope)
        {
            return DoesInteractorRayHitBounds(cachedMicroscopeRenderers, microscopeRoot, "Microscope");
        }

        return false;
    }

    private bool IsTransformPartOfExploder(Transform hitTransform)
    {
        return IsTransformPartOfRoot(hitTransform, microscopeExploderRoot);
    }

    private bool IsTransformPartOfMicroscope(Transform hitTransform)
    {
        return IsTransformPartOfRoot(hitTransform, microscopeRoot);
    }

    private bool IsTransformPartOfRoot(Transform hitTransform, GameObject rootObject)
    {
        return hitTransform != null &&
               rootObject != null &&
               (hitTransform == rootObject.transform || hitTransform.IsChildOf(rootObject.transform));
    }

    private bool DoesInteractorRayHitBounds(Renderer[] renderers, GameObject rootObject, string targetLabel)
    {
        if (renderers == null || renderers.Length == 0 || rootObject == null)
        {
            return false;
        }

        Vector3 origin = rightRayInteractor.transform.position;
        Vector3 endPoint = rightRayInteractor.rayEndPoint;
        Vector3 direction = endPoint != Vector3.zero ? (endPoint - origin) : rightRayInteractor.transform.forward;
        float maxDistance = rightRayInteractor.maxRaycastDistance;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = rightRayInteractor.transform.forward;
        }

        if (maxDistance <= 0.0001f)
        {
            maxDistance = 100f;
        }

        Ray ray = new Ray(origin, direction.normalized);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.gameObject.activeInHierarchy || !renderer.enabled)
            {
                continue;
            }

            if (renderer.bounds.IntersectRay(ray, out float distance) && distance <= maxDistance)
            {
                DebugLog($"Bounds hit {targetLabel}: renderer='{renderer.name}', distance={distance:F3}, maxDistance={maxDistance:F3}");
                return true;
            }
        }

        DebugLog($"Bounds miss {targetLabel}: root='{rootObject.name}', maxDistance={maxDistance:F3}");
        return false;
    }

    private void SetExploderState(bool exploded, bool instant)
    {
        if (modelExploder == null)
        {
            return;
        }

        if (instant || !animateExploderTransitions)
        {
            modelExploder.SetExplodedState(exploded, true);
            return;
        }

        modelExploder.SetExplodedState(exploded, false);
    }

    private void ApplyAssemblyRayPresentation()
    {
        if (rightRayInteractor == null)
        {
            return;
        }

        if (cachedRightLineVisual == null)
        {
            cachedRightLineVisual = rightRayInteractor.GetComponent<XRInteractorLineVisual>();
        }

        if (!rayPresentationCached)
        {
            originalEnableUiInteraction = rightRayInteractor.enableUIInteraction;
            originalHitClosestOnly = rightRayInteractor.hitClosestOnly;

            if (cachedRightLineVisual != null)
            {
                originalOverrideInteractorLineLength = cachedRightLineVisual.overrideInteractorLineLength;
                originalAutoAdjustLineLength = cachedRightLineVisual.autoAdjustLineLength;
                originalUseDistanceToHitAsMaxLineLength = cachedRightLineVisual.useDistanceToHitAsMaxLineLength;
                originalLineLength = cachedRightLineVisual.lineLength;
                originalMinLineLength = cachedRightLineVisual.minLineLength;
            }

            rayPresentationCached = true;
        }

        rightRayInteractor.enableUIInteraction = false;
        rightRayInteractor.hitClosestOnly = true;

        if (cachedRightLineVisual != null)
        {
            float stableLineLength = Mathf.Max(rightRayInteractor.maxRaycastDistance, 2f);
            stableLineLength = Mathf.Max(stableLineLength, originalLineLength);

            cachedRightLineVisual.overrideInteractorLineLength = true;
            cachedRightLineVisual.lineLength = stableLineLength;
            cachedRightLineVisual.minLineLength = stableLineLength;
            cachedRightLineVisual.autoAdjustLineLength = false;
            cachedRightLineVisual.useDistanceToHitAsMaxLineLength = false;
        }

        DebugLog(
            $"ApplyAssemblyRayPresentation ray='{rightRayInteractor.name}', lineVisualFound={cachedRightLineVisual != null}, maxDistance={rightRayInteractor.maxRaycastDistance:F2}");
    }

    private void RestoreDefaultRayPresentation()
    {
        if (!rayPresentationCached || rightRayInteractor == null)
        {
            return;
        }

        rightRayInteractor.enableUIInteraction = originalEnableUiInteraction;
        rightRayInteractor.hitClosestOnly = originalHitClosestOnly;

        if (cachedRightLineVisual != null)
        {
            cachedRightLineVisual.overrideInteractorLineLength = originalOverrideInteractorLineLength;
            cachedRightLineVisual.lineLength = originalLineLength;
            cachedRightLineVisual.minLineLength = originalMinLineLength;
            cachedRightLineVisual.autoAdjustLineLength = originalAutoAdjustLineLength;
            cachedRightLineVisual.useDistanceToHitAsMaxLineLength = originalUseDistanceToHitAsMaxLineLength;
        }

        rayPresentationCached = false;
        DebugLog("RestoreDefaultRayPresentation completed.");
    }

    private void SwitchToPreAssemblyView()
    {
        if (progressControl == null || preAssemblyCamera == null)
        {
            return;
        }

        progressControl.SwitchToPresetCamera(preAssemblyCamera);
    }

    private void SwitchToFreeView()
    {
        if (progressControl == null)
        {
            return;
        }

        progressControl.SwitchToFreeView(preAssemblyCamera);
    }

    private void SyncPresetCameraFieldOfView()
    {
        if (!copyFieldOfViewFromCurrentView || preAssemblyCamera == null)
        {
            return;
        }

        Camera sourceCamera = null;
        if (progressControl != null && progressControl.cinemachineBrain != null)
        {
            sourceCamera = progressControl.cinemachineBrain.OutputCamera;
        }

        if (sourceCamera == null)
        {
            sourceCamera = Camera.main;
        }

        if (sourceCamera == null)
        {
            return;
        }

        LensSettings lens = preAssemblyCamera.m_Lens;
        lens.FieldOfView = sourceCamera.fieldOfView;
        preAssemblyCamera.m_Lens = lens;
    }

    private void SetLocomotionEnabled(bool isEnabled)
    {
        if (!disableLocomotionWhileInAssemblyModes)
        {
            return;
        }

        if (cachedLocomotionRoot != null)
        {
            cachedLocomotionRoot.SetActive(isEnabled);
        }
    }

    private void ApplyModeObjectVisibility(bool forceExploderReset)
    {
        UpdateModelsVisibility();

        if (forceExploderReset)
        {
            SetExploderState(false, true);
        }
    }

    private void UpdateModelsVisibility()
    {
        bool useExploder = !IsTutorialModeLocked && currentMode != AssemblyMode.Normal;

        if (microscopeRoot != null && microscopeRoot.activeSelf == useExploder)
        {
            microscopeRoot.SetActive(!useExploder);
            DebugLog($"UpdateModelsVisibility: Microscope visibility set to {!useExploder}");
        }

        if (microscopeExploderRoot != null && microscopeExploderRoot.activeSelf != useExploder)
        {
            microscopeExploderRoot.SetActive(useExploder);
            DebugLog($"UpdateModelsVisibility: MicroscopeExploder visibility set to {useExploder}");
        }
    }

    [ContextMenu("Debug/Force Enter PreAssembly")]
    private void DebugForceEnterPreAssembly()
    {
        EnterPreAssembly();
    }

    [ContextMenu("Debug/Force Enter SuperAssembly")]
    private void DebugForceEnterSuperAssembly()
    {
        EnterSuperAssembly();
    }

    [ContextMenu("Debug/Force Return To Normal")]
    private void DebugForceReturnToNormal()
    {
        ReturnToNormalOperation();
    }

    private void DebugLog(string message)
    {
        if (!enableDebugLogs)
        {
            return;
        }

        Debug.Log($"[MicroscopeExploderModeController] {message}", this);
    }

    private void DebugLogWarning(string message)
    {
        if (!enableDebugLogs)
        {
            return;
        }

        Debug.LogWarning($"[MicroscopeExploderModeController] {message}", this);
    }

    private string GetSafeObjectName(GameObject target)
    {
        return target != null ? target.name : "null";
    }
}
