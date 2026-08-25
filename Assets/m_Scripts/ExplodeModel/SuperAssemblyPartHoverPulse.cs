using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class SuperAssemblyPartHoverPulse : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MicroscopeExploderModeController modeController;
    [SerializeField] private XRRayInteractor rightRayInteractor;
    [SerializeField] private ModelExploder modelExploder;
    [SerializeField] private Transform partRoot;

    [Tooltip("Optional override list. If empty, the script follows ModelExploder's part source.")]
    [SerializeField] private List<Transform> selectableParts = new List<Transform>();

    [Header("Selection")]
    [SerializeField] private bool useRendererBoundsFallback = true;
    [SerializeField] private bool rebuildPartCacheOnEnable = true;

    [Tooltip("Extra tolerance for renderer bounds ray checks, in meters.")]
    [Min(0f)]
    [SerializeField] private float boundsPadding = 0.005f;

    [Header("Pulse Animation")]
    [Min(1f)]
    [SerializeField] private float pulseScaleMultiplier = 1.08f;

    [Min(0.01f)]
    [SerializeField] private float pulseHalfDuration = 0.35f;

    [Min(0.01f)]
    [SerializeField] private float restoreDuration = 0.18f;

    [SerializeField] private Ease pulseEase = Ease.InOutSine;
    [SerializeField] private Ease restoreEase = Ease.OutSine;
    [SerializeField] private bool useUnscaledTime = false;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    private readonly List<Transform> cachedParts = new List<Transform>();
    private readonly Dictionary<Transform, Vector3> originalLocalScales = new Dictionary<Transform, Vector3>();
    private readonly Dictionary<Transform, Tween> activeScaleTweens = new Dictionary<Transform, Tween>();

    private Transform currentPart;
    private bool wasInSuperAssembly;

    private void OnEnable()
    {
        EnsureReferences();

        if (rebuildPartCacheOnEnable)
        {
            RebuildPartCache();
        }
    }

    private void OnDisable()
    {
        ClearCurrentSelection(restoreImmediately: true);
        RestoreAllCachedPartsImmediately();
    }

    private void Update()
    {
        EnsureReferences();

        bool isInSuperAssembly = modeController != null &&
                                 modeController.CurrentMode == MicroscopeExploderModeController.AssemblyMode.SuperAssembly;

        if (!isInSuperAssembly)
        {
            if (wasInSuperAssembly || currentPart != null)
            {
                ClearCurrentSelection(restoreImmediately: true);
            }

            wasInSuperAssembly = false;
            return;
        }

        wasInSuperAssembly = true;

        if (!CanRunHoverPulse())
        {
            ClearCurrentSelection(restoreImmediately: true);
            return;
        }

        if (cachedParts.Count == 0)
        {
            RebuildPartCache();
        }

        Transform hoveredPart = ResolveHoveredPart();
        if (hoveredPart != currentPart)
        {
            SetCurrentPart(hoveredPart);
        }
    }

    private bool CanRunHoverPulse()
    {
        if (SuperAssemblyPartSelectionController.HasActiveSelection)
        {
            return false;
        }

        return modelExploder == null || (modelExploder.IsExploded && !modelExploder.IsAnimating);
    }

    [ContextMenu("Rebuild Part Cache")]
    public void RebuildPartCache()
    {
        cachedParts.Clear();

        if (selectableParts != null && selectableParts.Count > 0)
        {
            for (int i = 0; i < selectableParts.Count; i++)
            {
                AddPartIfValid(selectableParts[i]);
            }
        }
        else if (modelExploder != null && modelExploder.manualParts != null && modelExploder.manualParts.Count > 0)
        {
            for (int i = 0; i < modelExploder.manualParts.Count; i++)
            {
                AddPartIfValid(modelExploder.manualParts[i]);
            }
        }
        else
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

        for (int i = 0; i < cachedParts.Count; i++)
        {
            EnsureOriginalScale(cachedParts[i]);
        }

        DebugLog($"Rebuilt part cache. parts={cachedParts.Count}");
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

    private Transform ResolveHoveredPart()
    {
        if (CameraTryMove.TryGetDesktopPointerRay(out Ray desktopRay, out float desktopMaxDistance))
        {
            if (Physics.Raycast(desktopRay, out RaycastHit desktopHitInfo, desktopMaxDistance, ~0, QueryTriggerInteraction.Collide))
            {
                Transform desktopHitPart = ResolvePartFromTransform(desktopHitInfo.transform);
                if (desktopHitPart != null)
                {
                    return desktopHitPart;
                }

            }

            return useRendererBoundsFallback ? ResolvePartByRendererBounds(desktopRay, desktopMaxDistance) : null;
        }

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

    private void SetCurrentPart(Transform newPart)
    {
        if (currentPart != null)
        {
            RestorePart(currentPart, restoreImmediately: false);
        }

        currentPart = newPart;

        if (currentPart != null)
        {
            StartPulse(currentPart);
            DebugLog($"Hover part='{currentPart.name}'");
        }
    }

    private void StartPulse(Transform part)
    {
        if (part == null)
        {
            return;
        }

        EnsureOriginalScale(part);
        KillOwnedScaleTween(part);

        Vector3 originalScale = originalLocalScales[part];
        Vector3 pulseScale = originalScale * pulseScaleMultiplier;

        Sequence pulseSequence = DOTween.Sequence()
            .SetUpdate(useUnscaledTime)
            .SetLoops(-1, LoopType.Restart);

        pulseSequence.Append(part.DOScale(pulseScale, pulseHalfDuration).SetEase(pulseEase));
        pulseSequence.Append(part.DOScale(originalScale, pulseHalfDuration).SetEase(pulseEase));
        pulseSequence.OnKill(() => RemoveOwnedTweenIfMatched(part, pulseSequence));

        activeScaleTweens[part] = pulseSequence;
        pulseSequence.Play();
    }

    private void ClearCurrentSelection(bool restoreImmediately)
    {
        if (currentPart != null)
        {
            RestorePart(currentPart, restoreImmediately);
            currentPart = null;
        }
    }

    private void RestorePart(Transform part, bool restoreImmediately)
    {
        if (part == null)
        {
            return;
        }

        EnsureOriginalScale(part);
        KillOwnedScaleTween(part);

        Vector3 originalScale = originalLocalScales[part];
        if (restoreImmediately || !part.gameObject.activeInHierarchy)
        {
            part.localScale = originalScale;
            return;
        }

        Tween restoreTween = part
            .DOScale(originalScale, restoreDuration)
            .SetEase(restoreEase)
            .SetUpdate(useUnscaledTime);

        restoreTween.OnKill(() => RemoveOwnedTweenIfMatched(part, restoreTween));
        activeScaleTweens[part] = restoreTween;
    }

    private void RestoreAllCachedPartsImmediately()
    {
        for (int i = 0; i < cachedParts.Count; i++)
        {
            Transform part = cachedParts[i];
            if (part == null)
            {
                continue;
            }

            KillOwnedScaleTween(part);
            if (originalLocalScales.TryGetValue(part, out Vector3 originalScale))
            {
                part.localScale = originalScale;
            }
        }
    }

    private void EnsureOriginalScale(Transform part)
    {
        if (part != null && !originalLocalScales.ContainsKey(part))
        {
            originalLocalScales.Add(part, part.localScale);
        }
    }

    private void KillOwnedScaleTween(Transform part)
    {
        if (part == null || !activeScaleTweens.TryGetValue(part, out Tween tween))
        {
            return;
        }

        if (tween != null && tween.IsActive())
        {
            tween.Kill(false);
        }

        activeScaleTweens.Remove(part);
    }

    private void RemoveOwnedTweenIfMatched(Transform part, Tween tween)
    {
        if (part == null)
        {
            return;
        }

        if (activeScaleTweens.TryGetValue(part, out Tween currentTween) && currentTween == tween)
        {
            activeScaleTweens.Remove(part);
        }
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

        Debug.Log($"[SuperAssemblyPartHoverPulse] {message}", this);
    }
}
