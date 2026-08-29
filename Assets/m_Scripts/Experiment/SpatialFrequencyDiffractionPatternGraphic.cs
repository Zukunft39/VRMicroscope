using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class SpatialFrequencyDiffractionPatternGraphic : MaskableGraphic
{
    [SerializeField, Min(8)] private int circleSegments = 24;
    [SerializeField, Min(2f)] private float spotRadius = 14f;
    [SerializeField] private Color laserOrderColor = new Color(1f, 0.12f, 0.04f, 1f);
    private float normalizedFrequency = 1f;
    private int visibleOrderCount = 1;
    private float patternAlpha = 1f;
    private bool useLaserExcitation;

    protected override void OnEnable()
    {
        base.OnEnable();
        SetVerticesDirty();
    }

    protected override void OnRectTransformDimensionsChange()
    {
        base.OnRectTransformDimensionsChange();
        SetVerticesDirty();
    }

    public void SetPattern(float normalized, int orderCount, float alpha, bool laserExcitation)
    {
        normalizedFrequency = Mathf.Clamp01(normalized);
        visibleOrderCount = Mathf.Max(1, orderCount);
        patternAlpha = Mathf.Clamp01(alpha);
        useLaserExcitation = laserExcitation;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();
        AddBackground(vertexHelper, Color.black);
        Color zeroOrderColor = useLaserExcitation
            ? WithAlpha(laserOrderColor, patternAlpha)
            : new Color(1f, 1f, 1f, patternAlpha);
        AddCircle(vertexHelper, Vector2.zero, spotRadius * 0.82f, zeroOrderColor);

        float availableRadius = rectTransform.rect.width * 0.46f;
        float outerOrderRadius = availableRadius * 0.84f;
        float separation = outerOrderRadius / visibleOrderCount;
        for (int order = 1; order <= visibleOrderCount; order++)
        {
            float distance = separation * order;
            if (distance > availableRadius)
            {
                break;
            }

            if (useLaserExcitation)
            {
                AddLaserOrder(vertexHelper, -distance, order);
                AddLaserOrder(vertexHelper, distance, order);
            }
            else
            {
                AddSpectrumOrder(vertexHelper, -distance, -1f, order);
                AddSpectrumOrder(vertexHelper, distance, 1f, order);
            }
        }
    }

    private void AddBackground(VertexHelper vertexHelper, Color backgroundColor)
    {
        Rect rect = rectTransform.rect;
        int index = vertexHelper.currentVertCount;
        vertexHelper.AddVert(new Vector2(rect.xMin, rect.yMin), backgroundColor, Vector2.zero);
        vertexHelper.AddVert(new Vector2(rect.xMin, rect.yMax), backgroundColor, Vector2.zero);
        vertexHelper.AddVert(new Vector2(rect.xMax, rect.yMax), backgroundColor, Vector2.zero);
        vertexHelper.AddVert(new Vector2(rect.xMax, rect.yMin), backgroundColor, Vector2.zero);
        vertexHelper.AddTriangle(index, index + 1, index + 2);
        vertexHelper.AddTriangle(index, index + 2, index + 3);
    }

    private void AddSpectrumOrder(VertexHelper vertexHelper, float centerX, float direction, int order)
    {
        float alpha = Mathf.Clamp01(0.95f - (order - 1) * 0.2f) * patternAlpha;
        float chromaticOffset = spotRadius * 0.55f;
        AddCircle(vertexHelper, new Vector2(centerX + direction * chromaticOffset, 0f), spotRadius,
            new Color(1f, 0.08f, 0.02f, alpha));
        AddCircle(vertexHelper, new Vector2(centerX, 0f), spotRadius,
            new Color(1f, 0.95f, 0.12f, alpha));
        AddCircle(vertexHelper, new Vector2(centerX - direction * chromaticOffset, 0f), spotRadius,
            new Color(0.08f, 0.45f, 1f, alpha));
    }

    private void AddLaserOrder(VertexHelper vertexHelper, float centerX, int order)
    {
        float alpha = Mathf.Clamp01(1f - (order - 1) * 0.14f) * patternAlpha;
        AddCircle(
            vertexHelper,
            new Vector2(centerX, 0f),
            spotRadius * 0.72f,
            WithAlpha(laserOrderColor, alpha));
    }

    private static Color WithAlpha(Color source, float alpha)
    {
        source.a = Mathf.Clamp01(source.a * alpha);
        return source;
    }

    private void AddCircle(VertexHelper vertexHelper, Vector2 center, float radius, Color vertexColor)
    {
        int baseIndex = vertexHelper.currentVertCount;
        int safeSegments = Mathf.Max(8, circleSegments);
        vertexHelper.AddVert(center, vertexColor, Vector2.zero);
        for (int i = 0; i <= safeSegments; i++)
        {
            float angle = Mathf.PI * 2f * i / safeSegments;
            Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            vertexHelper.AddVert(point, vertexColor, Vector2.zero);
        }

        for (int i = 0; i < safeSegments; i++)
        {
            vertexHelper.AddTriangle(baseIndex, baseIndex + i + 1, baseIndex + i + 2);
        }
    }
}
