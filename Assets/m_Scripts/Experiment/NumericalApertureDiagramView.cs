using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class NumericalApertureDiagramView : MonoBehaviour
{
    [SerializeField] private RectTransform objectiveGroup;
    [SerializeField] private RectTransform opticalColumn;
    [SerializeField] private RectTransform lightCone;
    [SerializeField] private RectTransform specimen;
    [SerializeField] private RectTransform opticalAxis;
    [SerializeField] private NumericalApertureAngleGraphic angleGraphic;
    [SerializeField] private TextMeshProUGUI angleText;

    [Header("Diagram Geometry")]
    [SerializeField] private float specimenY = -245f;
    [SerializeField] private float coneBaseWidth = 150f;
    [SerializeField] private float lowNaConeHeight = 300f;
    [SerializeField] private float highNaConeHeight = 52f;
    [SerializeField] private float objectiveBodyHeight = 245f;

    public void ApplyContinuous(float normalizedNA, float halfAngleDegrees)
    {
        float step = Mathf.Clamp01(normalizedNA);
        float coneHeight = Mathf.Lerp(lowNaConeHeight, highNaConeHeight, step);

        if (lightCone != null)
        {
            lightCone.pivot = new Vector2(0.5f, 0f);
            lightCone.anchoredPosition = new Vector2(0f, specimenY);
            lightCone.sizeDelta = new Vector2(coneBaseWidth, coneHeight);
        }

        if (objectiveGroup != null)
        {
            float objectiveCenterY = specimenY + coneHeight + objectiveBodyHeight * 0.5f;
            objectiveGroup.anchoredPosition = new Vector2(0f, objectiveCenterY);
        }

        if (opticalColumn != null)
        {
            opticalColumn.sizeDelta = new Vector2(coneBaseWidth, objectiveBodyHeight + 48f);
        }

        if (specimen != null)
        {
            specimen.anchoredPosition = new Vector2(0f, specimenY - 10f);
        }

        if (opticalAxis != null)
        {
            float axisTop = specimenY + coneHeight + objectiveBodyHeight;
            float axisHeight = axisTop - specimenY + 60f;
            opticalAxis.pivot = new Vector2(0.5f, 0f);
            opticalAxis.anchoredPosition = new Vector2(0f, specimenY - 8f);
            opticalAxis.sizeDelta = new Vector2(10f, axisHeight);
        }

        if (angleGraphic != null)
        {
            angleGraphic.rectTransform.anchoredPosition = new Vector2(0f, specimenY + 8f);
            angleGraphic.SetAngle(halfAngleDegrees);
        }

        if (angleText != null)
        {
            angleText.text = $"theta ({halfAngleDegrees:0.0} deg)";
            angleText.rectTransform.anchoredPosition = new Vector2(150f, specimenY + 65f);
        }
    }
}

[RequireComponent(typeof(CanvasRenderer))]
public sealed class NumericalApertureAngleGraphic : MaskableGraphic
{
    [SerializeField, Range(0f, 89f)] private float angleDegrees = 14.5f;
    [SerializeField, Min(8)] private int segments = 24;
    [SerializeField, Min(1f)] private float thickness = 3f;
    [SerializeField, Min(4f)] private float radius = 52f;

    public void SetAngle(float value)
    {
        angleDegrees = Mathf.Clamp(value, 0f, 89f);
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();

        int safeSegments = Mathf.Max(8, segments);
        float startRadians = (90f - angleDegrees) * Mathf.Deg2Rad;
        float endRadians = 90f * Mathf.Deg2Rad;

        for (int i = 0; i < safeSegments; i++)
        {
            float t0 = i / (float)safeSegments;
            float t1 = (i + 1) / (float)safeSegments;
            float angle0 = Mathf.Lerp(startRadians, endRadians, t0);
            float angle1 = Mathf.Lerp(startRadians, endRadians, t1);

            Vector2 direction0 = new Vector2(Mathf.Cos(angle0), Mathf.Sin(angle0));
            Vector2 direction1 = new Vector2(Mathf.Cos(angle1), Mathf.Sin(angle1));
            Vector2 inner0 = direction0 * (radius - thickness * 0.5f);
            Vector2 outer0 = direction0 * (radius + thickness * 0.5f);
            Vector2 inner1 = direction1 * (radius - thickness * 0.5f);
            Vector2 outer1 = direction1 * (radius + thickness * 0.5f);

            int baseIndex = vertexHelper.currentVertCount;
            vertexHelper.AddVert(inner0, color, Vector2.zero);
            vertexHelper.AddVert(outer0, color, Vector2.zero);
            vertexHelper.AddVert(outer1, color, Vector2.zero);
            vertexHelper.AddVert(inner1, color, Vector2.zero);
            vertexHelper.AddTriangle(baseIndex, baseIndex + 1, baseIndex + 2);
            vertexHelper.AddTriangle(baseIndex, baseIndex + 2, baseIndex + 3);
        }
    }
}
