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

    [Header("零件来源")]
    [Tooltip("若列表有内容，则优先按该列表作为拆解零件；为空时自动收集。")]
    public List<Transform> manualParts = new List<Transform>();

    [Tooltip("自动收集模式下，是否递归收集全部子节点。")]
    public bool includeAllDescendants = false;

    [Tooltip("自动收集模式下，是否包含非激活节点。")]
    public bool includeInactiveParts = false;

    [Header("拆解参数")]
    [Tooltip("每个零件沿质心方向外移的基础距离（米）。")]
    [Min(0f)]
    public float explodeDistance = 0.18f;

    [Tooltip("按零件半径增加的额外位移。开启归一化后，该值表示“最大额外位移（米）”。")]
    [Min(0f)]
    public float radialDistanceFactor = 0.06f;

    [Tooltip("是否使用归一化半径计算额外位移。开启后，不会因模型尺寸变大而导致位移过大。")]
    public bool normalizeRadialDistance = true;

    [Tooltip("拆解位移最大值（米）。0 表示不限制。")]
    [Min(0f)]
    public float maxExplodeDistance = 0.3f;

    [Tooltip("每次切换前是否重新计算目标位姿。若模型结构会动态变化，建议开启。")]
    public bool rebuildCacheBeforeEachPlay = false;

    [Header("定向拆解覆盖")]
    [Tooltip("在此列表指定某些零件的拆解方向与距离规则。列表中的零件会优先使用这里配置的方向。")]
    public List<DirectionalExplodeOverride> directionalOverrides = new List<DirectionalExplodeOverride>();

    [Header("动画参数")]
    [Min(0.01f)]
    public float animationDuration = 0.8f;

    [Tooltip("为每个零件增加启动延迟，形成分批拆解效果。0 表示同时运动。")]
    [Min(0f)]
    public float staggerPerPart = 0.0f;

    [Tooltip("强制所有零件同步开始/结束。开启后会忽略 staggerPerPart。")]
    public bool forceSynchronizedAnimation = true;

    [Tooltip("移动缓动曲线。")]
    public AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Tooltip("组装时是否恢复到初始局部旋转。")]
    public bool restoreOriginalRotationOnAssemble = true;

    [Tooltip("启用后以世界坐标进行位移（推荐）。可避免父级缩放导致位移过小。")]
    public bool moveInWorldSpace = true;

    [Tooltip("是否使用不受 Time.timeScale 影响的更新。")]
    public bool useUnscaledTime = false;

    [Header("调试")]
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
            DebugLogWarning("InitializeParts: 未收集到任何零件。请检查 manualParts / includeAllDescendants / 挂载物体层级。");
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

        DebugLog($"InitializeParts: 收集零件={partsList.Count}, 静态零件={staticPartsCount}, 计划最大位移={maxPlannedDistance:F3}m, moveInWorldSpace={moveInWorldSpace}");

        if (staticPartsCount > 0)
        {
            DebugLogWarning("检测到零件 isStatic=true。若开启了静态批处理，运行时位移可能看起来无效。建议在拆装对象上取消 Static。");
        }

        if (maxPlannedDistance <= 0.0001f)
        {
            DebugLogWarning("所有零件计划位移接近 0。请检查 explodeDistance / radialDistanceFactor。");
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
            DebugLogWarning("PlayExplodeAnimation: partsList 为空，动画未执行。");
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
            DebugLogWarning("PlayExplodeAnimation: 有效零件为 0（可能 manualParts 全为空引用）。");
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
            DebugLogWarning("ApplyStateImmediate: partsList 为空，状态未更新。");
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
