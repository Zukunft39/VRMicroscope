using UnityEngine;
using System;
using System.Collections.Generic;
using DG.Tweening;

public class ModelExploder : MonoBehaviour
{
    public enum DirectionSpace
    {
        World,
        RootLocal,
        PartLocal
    }

    [System.Serializable]
    public class PartData
    {
        public Transform partTransform;
        public Vector3 originalLocalPosition;
        public Vector3 originalWorldPosition;
        public Quaternion originalLocalRotation;
        public Vector3 partCentroidWorld;
        public Vector3 explodedLocalPosition;
        public Vector3 explodedWorldPosition;
    }

    [System.Serializable]
    public class DirectionalExplodeOverride
    {
        public bool enabled = true;
        public Transform partTransform;
        public DirectionSpace directionSpace = DirectionSpace.RootLocal;
        public Vector3 direction = Vector3.right;

        [Min(0f)]
        public float distanceMultiplier = 1f;

        public bool useAbsoluteDistance = false;

        [Min(0f)]
        public float absoluteDistance = 0.2f;
    }

    [Header("Part Sources")]
    [Tooltip("Use this list as the exploded parts when populated; otherwise collect automatically.")]
    public List<Transform> manualParts = new List<Transform>();

    [Tooltip("Recursively collect all descendants in automatic mode.")]
    public bool includeAllDescendants = false;

    [Tooltip("Include inactive nodes in automatic collection.")]
    public bool includeInactiveParts = false;

    [Header("Expansion Settings")]
    [Tooltip("Base outward displacement of each part along its centroid direction (metres).")]
    [Min(0f)]
    public float explodeDistance = 0.18f;

    [Tooltip("Additional displacement based on part radius. With normalization, this is the maximum extra displacement in metres.")]
    [Min(0f)]
    public float radialDistanceFactor = 0.06f;

    [Tooltip("Normalize radii when calculating extra displacement to avoid excessive movement for larger models.")]
    public bool normalizeRadialDistance = true;

    [Tooltip("Maximum expansion displacement in metres; 0 means unlimited.")]
    [Min(0f)]
    public float maxExplodeDistance = 0.3f;

    [Tooltip("Recalculate target poses before every switch. Recommended for dynamically changing models.")]
    public bool rebuildCacheBeforeEachPlay = false;

    [Header("Expansion Direction Overrides")]
    [Tooltip("Override expansion direction and distance rules for listed parts.")]
    public List<DirectionalExplodeOverride> directionalOverrides = new List<DirectionalExplodeOverride>();

    [Header("Animation Settings")]
    [Min(0.01f)]
    public float animationDuration = 0.8f;

    [Tooltip("Delay each part for staggered expansion; 0 starts all parts together.")]
    [Min(0f)]
    public float staggerPerPart = 0.0f;

    [Tooltip("Force all parts to start and finish together, ignoring staggerPerPart.")]
    public bool forceSynchronizedAnimation = true;

    [Tooltip("Movement easing curve.")]
    public AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Tooltip("Restore original local rotation when assembling.")]
    public bool restoreOriginalRotationOnAssemble = true;

    [Tooltip("Move in world coordinates (recommended) to avoid reduced displacement from parent scaling.")]
    public bool moveInWorldSpace = true;

    [Tooltip("Use updates independent of Time.timeScale.")]
    public bool useUnscaledTime = false;

    [Header("Debug")]
    public bool enableDebugLogs = true;

    private List<PartData> partsList = new List<PartData>();
    private bool isExploded = false;
    private Tween activeTween;

    public bool IsExploded => isExploded;
    public bool IsAnimating => activeTween != null && activeTween.IsActive();
    public event Action<bool> ExplodedStateApplied;

    private void Start()
    {
        InitializeParts();
    }

    /// <summary>
    /// 初始化并计算所有零件的原始位置与拆解目标位置
    /// </summary>
    public void InitializeParts()
    {
        KillAllPartTweens();
        partsList.Clear();

        List<Transform> targetParts = CollectTargetParts();
        if (targetParts.Count == 0)
        {
            DebugLogWarning("InitializeParts: No parts found. Check manualParts, includeAllDescendants, and the object hierarchy.");
            return;
        }

        List<Vector3> centroids = new List<Vector3>(targetParts.Count);
        int staticPartsCount = 0;
        for (int i = 0; i < targetParts.Count; i++)
        {
            if (targetParts[i] != null && targetParts[i].gameObject.isStatic)
            {
                staticPartsCount++;
            }

            centroids.Add(GetPartCentroidWorld(targetParts[i]));
        }

        Vector3 modelCentroidWorld = Vector3.zero;
        for (int i = 0; i < centroids.Count; i++)
        {
            modelCentroidWorld += centroids[i];
        }

        modelCentroidWorld /= centroids.Count;
        float maxPlannedDistance = 0f;
        float maxRadius = 0f;
        for (int i = 0; i < centroids.Count; i++)
        {
            float radius = Vector3.Distance(centroids[i], modelCentroidWorld);
            if (radius > maxRadius)
            {
                maxRadius = radius;
            }
        }

        for (int i = 0; i < targetParts.Count; i++)
        {
            Transform part = targetParts[i];
            Vector3 partCentroid = centroids[i];
            Vector3 directionWorld = partCentroid - modelCentroidWorld;
            bool hasDirectionalOverride = TryGetDirectionalOverride(
                part,
                out Vector3 overrideDirectionWorld,
                out float distanceMultiplier,
                out bool useAbsoluteDistance,
                out float absoluteDistance);

            if (hasDirectionalOverride)
            {
                directionWorld = overrideDirectionWorld;
            }

            if (directionWorld.sqrMagnitude < 0.000001f)
            {
                directionWorld = part.position - transform.position;
                if (directionWorld.sqrMagnitude < 0.000001f)
                {
                    directionWorld = transform.up;
                }
            }

            float radiusFromCenter = directionWorld.magnitude;
            float additionalDistance;
            if (normalizeRadialDistance)
            {
                float normalizedRadius = maxRadius > 0.000001f ? radiusFromCenter / maxRadius : 0f;
                additionalDistance = normalizedRadius * radialDistanceFactor;
            }
            else
            {
                additionalDistance = radiusFromCenter * radialDistanceFactor;
            }

            float moveDistance = explodeDistance + additionalDistance;
            if (hasDirectionalOverride)
            {
                moveDistance = useAbsoluteDistance
                    ? absoluteDistance
                    : moveDistance * distanceMultiplier;
            }

            if (maxExplodeDistance > 0f)
            {
                moveDistance = Mathf.Min(moveDistance, maxExplodeDistance);
            }

            Vector3 directionLocal = transform.InverseTransformDirection(directionWorld.normalized);
            Vector3 explodedWorldPosition = part.position + directionWorld.normalized * moveDistance;
            maxPlannedDistance = Mathf.Max(maxPlannedDistance, moveDistance);

            PartData data = new PartData
            {
                partTransform = part,
                originalLocalPosition = part.localPosition,
                originalWorldPosition = part.position,
                originalLocalRotation = part.localRotation,
                partCentroidWorld = partCentroid,
                explodedLocalPosition = part.localPosition + directionLocal * moveDistance,
                explodedWorldPosition = explodedWorldPosition
            };

            partsList.Add(data);

            if (enableDebugLogs && i < 5)
            {
                DebugLog($"Part[{i}] '{part.name}' moveDistance={moveDistance:F3}, override={hasDirectionalOverride}, isStatic={part.gameObject.isStatic}");
            }
        }

        DebugLog($"InitializeParts: parts={partsList.Count}, staticParts={staticPartsCount}, maxDisplacement={maxPlannedDistance:F3}m, moveInWorldSpace={moveInWorldSpace}");

        if (staticPartsCount > 0)
        {
            DebugLogWarning("Some parts have isStatic=true. Static batching may hide runtime displacement; disable Static for movable assembly parts.");
        }

        if (maxPlannedDistance <= 0.0001f)
        {
            DebugLogWarning("All planned displacements are near zero. Check explodeDistance / radialDistanceFactor.");
        }
    }

    /// <summary>
    /// 供外部 UI 按钮或输入事件调用：切换拆解/组装
    /// </summary>
    public void ToggleExplode()
    {
        DebugLog($"ToggleExplode called. currentIsExploded={isExploded}");
        PlayExplodeAnimation(!isExploded);
    }

    public void Explode()
    {
        PlayExplodeAnimation(true);
    }

    public void Assemble()
    {
        PlayExplodeAnimation(false);
    }

    public void RebuildCache()
    {
        InitializeParts();
    }

    public void SetExplodedState(bool exploded, bool instant = false)
    {
        if (instant)
        {
            ApplyStateImmediate(exploded);
            return;
        }

        PlayExplodeAnimation(exploded);
    }

    public void ExplodeImmediate()
    {
        ApplyStateImmediate(true);
    }

    public void AssembleImmediate()
    {
        ApplyStateImmediate(false);
    }

    private void PlayExplodeAnimation(bool exploding)
    {
        if (rebuildCacheBeforeEachPlay || partsList.Count == 0)
        {
            InitializeParts();
        }

        if (partsList.Count == 0)
        {
            DebugLogWarning("PlayExplodeAnimation: partsList is empty; animation skipped.");
            return;
        }

        KillAllPartTweens();

        Sequence sequence = DOTween.Sequence().SetUpdate(useUnscaledTime);
        float safeDuration = Mathf.Max(0.01f, animationDuration);
        float safeStagger = forceSynchronizedAnimation ? 0f : Mathf.Max(0f, staggerPerPart);
        int validPartCount = 0;

        for (int i = 0; i < partsList.Count; i++)
        {
            PartData part = partsList[i];
            if (part.partTransform == null)
            {
                continue;
            }

            part.partTransform.DOKill(false);

            float delay = safeStagger * i;
            Tween moveTween;
            if (moveInWorldSpace)
            {
                Vector3 targetWorldPosition = exploding ? part.explodedWorldPosition : part.originalWorldPosition;
                moveTween = part.partTransform
                    .DOMove(targetWorldPosition, safeDuration)
                    .SetEase(animationCurve)
                    .SetDelay(delay);
            }
            else
            {
                Vector3 targetLocalPosition = exploding ? part.explodedLocalPosition : part.originalLocalPosition;
                moveTween = part.partTransform
                    .DOLocalMove(targetLocalPosition, safeDuration)
                    .SetEase(animationCurve)
                    .SetDelay(delay);
            }

            sequence.Join(moveTween);
            validPartCount++;

            if (!exploding && restoreOriginalRotationOnAssemble)
            {
                Tween rotateTween = part.partTransform
                    .DOLocalRotateQuaternion(part.originalLocalRotation, safeDuration)
                    .SetEase(animationCurve)
                    .SetDelay(delay);

                sequence.Join(rotateTween);
            }
        }

        if (validPartCount == 0)
        {
            DebugLogWarning("PlayExplodeAnimation: No valid parts; manualParts may contain only null references.");
            return;
        }

        sequence.OnComplete(() =>
        {
            isExploded = exploding;
            activeTween = null;
            DebugLog($"Animation completed. isExploded={isExploded}");
            NotifyExplodedStateApplied(isExploded);
        });

        activeTween = sequence;
        DebugLog($"Animation started. exploding={exploding}, parts={validPartCount}, duration={safeDuration:F2}, stagger={safeStagger:F3}, sync={forceSynchronizedAnimation}");
        sequence.Play();
    }

    private void ApplyStateImmediate(bool exploded)
    {
        if (rebuildCacheBeforeEachPlay || partsList.Count == 0)
        {
            InitializeParts();
        }

        if (partsList.Count == 0)
        {
            DebugLogWarning("ApplyStateImmediate: partsList is empty; state unchanged.");
            return;
        }

        KillAllPartTweens();

        for (int i = 0; i < partsList.Count; i++)
        {
            PartData part = partsList[i];
            if (part == null || part.partTransform == null)
            {
                continue;
            }

            if (moveInWorldSpace)
            {
                part.partTransform.position = exploded ? part.explodedWorldPosition : part.originalWorldPosition;
            }
            else
            {
                part.partTransform.localPosition = exploded ? part.explodedLocalPosition : part.originalLocalPosition;
            }

            if (!exploded && restoreOriginalRotationOnAssemble)
            {
                part.partTransform.localRotation = part.originalLocalRotation;
            }
        }

        isExploded = exploded;
        DebugLog($"ApplyStateImmediate completed. isExploded={isExploded}");
        NotifyExplodedStateApplied(isExploded);
    }

    private void NotifyExplodedStateApplied(bool exploded)
    {
        ExplodedStateApplied?.Invoke(exploded);
    }

    private List<Transform> CollectTargetParts()
    {
        List<Transform> targets = new List<Transform>();

        if (manualParts != null && manualParts.Count > 0)
        {
            for (int i = 0; i < manualParts.Count; i++)
            {
                Transform part = manualParts[i];
                if (part == null || part == transform)
                {
                    continue;
                }

                if (!targets.Contains(part))
                {
                    targets.Add(part);
                }
            }

            return targets;
        }

        if (includeAllDescendants)
        {
            Transform[] descendants = GetComponentsInChildren<Transform>(includeInactiveParts);
            for (int i = 0; i < descendants.Length; i++)
            {
                Transform item = descendants[i];
                if (item == null || item == transform)
                {
                    continue;
                }

                targets.Add(item);
            }

            return targets;
        }

        foreach (Transform child in transform)
        {
            if (includeInactiveParts || child.gameObject.activeInHierarchy)
            {
                targets.Add(child);
            }
        }

        return targets;
    }

    private Vector3 GetPartCentroidWorld(Transform part)
    {
        Renderer[] renderers = part.GetComponentsInChildren<Renderer>(includeInactiveParts);
        if (renderers != null && renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds.center;
        }

        Collider[] colliders = part.GetComponentsInChildren<Collider>(includeInactiveParts);
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

    private bool TryGetDirectionalOverride(
        Transform part,
        out Vector3 directionWorld,
        out float distanceMultiplier,
        out bool useAbsoluteDistance,
        out float absoluteDistance)
    {
        directionWorld = Vector3.zero;
        distanceMultiplier = 1f;
        useAbsoluteDistance = false;
        absoluteDistance = 0f;

        if (part == null || directionalOverrides == null || directionalOverrides.Count == 0)
        {
            return false;
        }

        for (int i = 0; i < directionalOverrides.Count; i++)
        {
            DirectionalExplodeOverride item = directionalOverrides[i];
            if (item == null || !item.enabled || item.partTransform != part)
            {
                continue;
            }

            Vector3 configuredDirection = item.direction;
            if (configuredDirection.sqrMagnitude < 0.000001f)
            {
                DebugLogWarning($"Directional override direction is zero, part='{part.name}', index={i}");
                return false;
            }

            switch (item.directionSpace)
            {
                case DirectionSpace.World:
                    directionWorld = configuredDirection;
                    break;
                case DirectionSpace.RootLocal:
                    directionWorld = transform.TransformDirection(configuredDirection);
                    break;
                case DirectionSpace.PartLocal:
                    directionWorld = part.TransformDirection(configuredDirection);
                    break;
            }

            if (directionWorld.sqrMagnitude < 0.000001f)
            {
                DebugLogWarning($"Directional override converted to zero, part='{part.name}', index={i}");
                return false;
            }

            directionWorld.Normalize();
            distanceMultiplier = Mathf.Max(0f, item.distanceMultiplier);
            useAbsoluteDistance = item.useAbsoluteDistance;
            absoluteDistance = Mathf.Max(0f, item.absoluteDistance);
            return true;
        }

        return false;
    }

    private void KillAllPartTweens()
    {
        if (activeTween != null && activeTween.IsActive())
        {
            activeTween.Kill(false);
            activeTween = null;
        }

        for (int i = 0; i < partsList.Count; i++)
        {
            if (partsList[i] != null && partsList[i].partTransform != null)
            {
                partsList[i].partTransform.DOKill(false);
            }
        }
    }

    private void OnDestroy()
    {
        KillAllPartTweens();
    }

    private void DebugLog(string message)
    {
        if (!enableDebugLogs)
        {
            return;
        }

        Debug.Log($"[ModelExploder] {message}", this);
    }

    private void DebugLogWarning(string message)
    {
        if (!enableDebugLogs)
        {
            return;
        }

        Debug.LogWarning($"[ModelExploder] {message}", this);
    }
}
