using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class SpatialFrequencyOpticalPathGraphic : MaskableGraphic
{
    [SerializeField, Min(1f)] private float lineThickness = 2.2f;
    [SerializeField] private Color rayColor = new Color(1f, 0.68f, 0f, 1f);
    [SerializeField] private Color lensColor = new Color(0.28f, 0.62f, 0.93f, 0.34f);
    [SerializeField] private Color planeColor = new Color(0.18f, 0.22f, 0.26f, 0.72f);

    private float normalizedFrequency = 1f;
    private float diffractionAngleDegrees = 7.9f;
    private int visibleOrderCount = 1;

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

    public void SetFrequency(float normalized, float angleDegrees, int orderCount)
    {
        normalizedFrequency = Mathf.Clamp01(normalized);
        diffractionAngleDegrees = Mathf.Max(0f, angleDegrees);
        visibleOrderCount = Mathf.Max(1, orderCount);
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();
        Rect rect = rectTransform.rect;
        float halfWidth = rect.width * 0.34f;
        float centerX = rect.center.x + rect.width * 0.08f;
        // These normalized heights map to the centers of the labels in the parent panel.
        float apertureY = rect.yMin + rect.height * 0.06f;
        float condenserY = rect.yMin + rect.height * 0.24f;
        float specimenY = rect.yMin + rect.height * 0.4375f;
        float objectiveY = rect.yMin + rect.height * 0.677f;
        float backFocalY = rect.yMin + rect.height * 0.922f;

        AddDashedLine(vertexHelper, new Vector2(centerX - halfWidth, specimenY),
            new Vector2(centerX + halfWidth, specimenY), planeColor);
        AddDashedLine(vertexHelper, new Vector2(centerX - halfWidth, backFocalY),
            new Vector2(centerX + halfWidth, backFocalY), planeColor);
        AddLens(vertexHelper, new Vector2(centerX, condenserY), halfWidth * 0.9f, rect.height * 0.04f);
        AddLens(vertexHelper, new Vector2(centerX, objectiveY), halfWidth * 0.9f, rect.height * 0.04f);

        float firstOrderSpacing = halfWidth * Mathf.Lerp(0.2f, 0.78f, normalizedFrequency);
        float apertureOffset = halfWidth * 0.55f;
        Vector2 apertureCenter = new Vector2(centerX, apertureY);
        Vector2 specimenLeft = new Vector2(centerX - halfWidth * 0.28f, specimenY);
        Vector2 specimenRight = new Vector2(centerX + halfWidth * 0.28f, specimenY);

        AddLine(vertexHelper, apertureCenter + Vector2.left * apertureOffset, specimenLeft, rayColor);
        AddLine(vertexHelper, apertureCenter + Vector2.right * apertureOffset, specimenRight, rayColor);
        for (int order = -visibleOrderCount; order <= visibleOrderCount; order++)
        {
            Vector2 backFocalPoint = new Vector2(centerX + firstOrderSpacing * order, backFocalY);
            AddLine(vertexHelper, specimenLeft, backFocalPoint, rayColor);
            AddLine(vertexHelper, specimenRight, backFocalPoint, rayColor);
        }

        AddOpticalAxis(vertexHelper, new Vector2(centerX, apertureY), new Vector2(centerX, backFocalY));
        AddSpecimen(vertexHelper, new Vector2(centerX - halfWidth * 0.34f, specimenY),
            new Vector2(centerX + halfWidth * 0.34f, specimenY));
        AddApertureStop(vertexHelper, centerX, apertureY, halfWidth * 0.48f);

        Vector2 firstOrderPoint = new Vector2(centerX + firstOrderSpacing, backFocalY);
        AddDiffractionAngleIndicator(vertexHelper, rect, specimenLeft, firstOrderPoint);
    }

    private void AddLens(VertexHelper vertexHelper, Vector2 center, float halfWidth, float halfHeight)
    {
        Color rimColor = new Color(0.16f, 0.42f, 0.68f, 0.82f);
        Color innerColor = new Color(lensColor.r, lensColor.g, lensColor.b, 0.46f);
        Color highlightColor = new Color(0.82f, 0.94f, 1f, 0.28f);

        AddFilledEllipse(vertexHelper, center, halfWidth, halfHeight, rimColor);
        AddFilledEllipse(vertexHelper, center, halfWidth * 0.97f, halfHeight * 0.78f, innerColor);
        AddFilledEllipse(vertexHelper, center + Vector2.up * halfHeight * 0.18f,
            halfWidth * 0.78f, halfHeight * 0.3f, highlightColor);
        AddEllipseOutline(vertexHelper, center, halfWidth, halfHeight, rimColor);
    }

    private static void AddFilledEllipse(VertexHelper vertexHelper, Vector2 center, float halfWidth,
        float halfHeight, Color fillColor)
    {
        const int segments = 32;
        int baseIndex = vertexHelper.currentVertCount;
        vertexHelper.AddVert(center, fillColor, Vector2.zero);
        for (int i = 0; i <= segments; i++)
        {
            float angle = Mathf.PI * 2f * i / segments;
            Vector2 point = center + new Vector2(Mathf.Cos(angle) * halfWidth, Mathf.Sin(angle) * halfHeight);
            vertexHelper.AddVert(point, fillColor, Vector2.zero);
        }

        for (int i = 0; i < segments; i++)
        {
            vertexHelper.AddTriangle(baseIndex, baseIndex + i + 1, baseIndex + i + 2);
        }
    }

    private void AddEllipseOutline(VertexHelper vertexHelper, Vector2 center, float halfWidth,
        float halfHeight, Color outlineColor)
    {
        const int segments = 32;
        Vector2 previous = center + new Vector2(halfWidth, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float angle = Mathf.PI * 2f * i / segments;
            Vector2 current = center + new Vector2(Mathf.Cos(angle) * halfWidth, Mathf.Sin(angle) * halfHeight);
            AddLine(vertexHelper, previous, current, outlineColor);
            previous = current;
        }
    }

    private void AddDiffractionAngleIndicator(VertexHelper vertexHelper, Rect rect, Vector2 origin,
        Vector2 firstOrderPoint)
    {
        Vector2 rayDirection = (firstOrderPoint - origin).normalized;
        float signedAngle = Vector2.SignedAngle(Vector2.up, rayDirection);
        if (Mathf.Abs(signedAngle) < 0.5f)
        {
            return;
        }

        const int arcSegments = 12;
        float radius = Mathf.Min(rect.width, rect.height) * 0.055f;
        Vector2 previous = origin + Vector2.up * radius;
        Vector2 arcMiddle = previous;
        for (int i = 1; i <= arcSegments; i++)
        {
            float t = i / (float)arcSegments;
            float angle = signedAngle * t;
            Vector2 direction = Quaternion.Euler(0f, 0f, angle) * Vector2.up;
            Vector2 current = origin + direction * radius;
            AddLine(vertexHelper, previous, current, planeColor);
            previous = current;
            if (i == arcSegments / 2)
            {
                arcMiddle = current;
            }
        }

        Vector2 arcTangent = new Vector2(-rayDirection.y, rayDirection.x) * Mathf.Sign(signedAngle);
        AddArrowHead(vertexHelper, previous, arcTangent, radius * 0.28f, planeColor);

        // The label is centered at parent Y 0.56 while this graphic occupies parent Y 0-0.96.
        float diffractionLabelY = rect.yMin + rect.height * (0.56f / 0.96f);
        Vector2 leaderStart = new Vector2(rect.xMin + rect.width * 0.01f, diffractionLabelY);
        Vector2 leaderEnd = arcMiddle - Vector2.right * radius * 0.16f;
        AddArrow(vertexHelper, leaderStart, leaderEnd, planeColor, radius * 0.32f);
    }

    private void AddArrow(VertexHelper vertexHelper, Vector2 start, Vector2 end, Color arrowColor,
        float headSize)
    {
        AddLine(vertexHelper, start, end, arrowColor);
        AddArrowHead(vertexHelper, end, (end - start).normalized, headSize, arrowColor);
    }

    private void AddArrowHead(VertexHelper vertexHelper, Vector2 tip, Vector2 direction, float size,
        Color arrowColor)
    {
        Vector2 safeDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        Vector2 normal = new Vector2(-safeDirection.y, safeDirection.x);
        Vector2 baseCenter = tip - safeDirection * size;
        AddTriangle(vertexHelper, tip, baseCenter + normal * size * 0.48f,
            baseCenter - normal * size * 0.48f, arrowColor);
    }

    private void AddSpecimen(VertexHelper vertexHelper, Vector2 start, Vector2 end)
    {
        AddLine(vertexHelper, start, end, new Color(0.2f, 0.55f, 0.95f, 0.95f));
    }

    private void AddApertureStop(VertexHelper vertexHelper, float centerX, float y, float halfWidth)
    {
        Color stopColor = new Color(0.15f, 0.17f, 0.2f, 0.9f);
        AddLine(vertexHelper, new Vector2(centerX - halfWidth, y),
            new Vector2(centerX - halfWidth * 0.12f, y), stopColor);
        AddLine(vertexHelper, new Vector2(centerX + halfWidth * 0.12f, y),
            new Vector2(centerX + halfWidth, y), stopColor);
    }

    private void AddOpticalAxis(VertexHelper vertexHelper, Vector2 start, Vector2 end)
    {
        AddDashedLine(vertexHelper, start, end, new Color(0.75f, 0.1f, 0.35f, 0.7f));
    }

    private static void AddTriangle(VertexHelper vertexHelper, Vector2 first, Vector2 second, Vector2 third,
        Color vertexColor)
    {
        int index = vertexHelper.currentVertCount;
        vertexHelper.AddVert(first, vertexColor, Vector2.zero);
        vertexHelper.AddVert(second, vertexColor, Vector2.zero);
        vertexHelper.AddVert(third, vertexColor, Vector2.zero);
        vertexHelper.AddTriangle(index, index + 1, index + 2);
    }

    private void AddDashedLine(VertexHelper vertexHelper, Vector2 start, Vector2 end, Color vertexColor)
    {
        const int dashCount = 11;
        for (int i = 0; i < dashCount; i += 2)
        {
            float startT = i / (float)dashCount;
            float endT = Mathf.Min(1f, (i + 1) / (float)dashCount);
            AddLine(vertexHelper, Vector2.Lerp(start, end, startT), Vector2.Lerp(start, end, endT), vertexColor);
        }
    }

    private void AddLine(VertexHelper vertexHelper, Vector2 start, Vector2 end, Color vertexColor)
    {
        Vector2 direction = end - start;
        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Vector2 normal = new Vector2(-direction.y, direction.x).normalized * lineThickness * 0.5f;
        int index = vertexHelper.currentVertCount;
        vertexHelper.AddVert(start - normal, vertexColor, Vector2.zero);
        vertexHelper.AddVert(start + normal, vertexColor, Vector2.zero);
        vertexHelper.AddVert(end + normal, vertexColor, Vector2.zero);
        vertexHelper.AddVert(end - normal, vertexColor, Vector2.zero);
        vertexHelper.AddTriangle(index, index + 1, index + 2);
        vertexHelper.AddTriangle(index, index + 2, index + 3);
    }
}
