using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class SpatialFrequencyLineGratingGraphic : MaskableGraphic
{
    [SerializeField, Min(4)] private int lowFrequencyLineCount = 14;
    [SerializeField, Min(4)] private int highFrequencyLineCount = 72;
    [SerializeField, Range(0.05f, 0.9f)] private float lineWidthRatio = 0.45f;

    private float normalizedFrequency = 1f;

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

    public void SetFrequency(float normalized)
    {
        normalizedFrequency = Mathf.Clamp01(normalized);
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();
        Rect rect = rectTransform.rect;
        int lineCount = Mathf.RoundToInt(Mathf.Lerp(lowFrequencyLineCount, highFrequencyLineCount,
            normalizedFrequency));
        float spacing = rect.width / Mathf.Max(1, lineCount);
        float width = Mathf.Max(1f, spacing * lineWidthRatio);

        for (int i = 0; i < lineCount; i++)
        {
            float centerX = rect.xMin + spacing * (i + 0.5f);
            AddQuad(vertexHelper,
                new Rect(centerX - width * 0.5f, rect.yMin, width, rect.height),
                color);
        }
    }

    private static void AddQuad(VertexHelper vertexHelper, Rect rect, Color vertexColor)
    {
        int index = vertexHelper.currentVertCount;
        vertexHelper.AddVert(new Vector2(rect.xMin, rect.yMin), vertexColor, Vector2.zero);
        vertexHelper.AddVert(new Vector2(rect.xMin, rect.yMax), vertexColor, Vector2.zero);
        vertexHelper.AddVert(new Vector2(rect.xMax, rect.yMax), vertexColor, Vector2.zero);
        vertexHelper.AddVert(new Vector2(rect.xMax, rect.yMin), vertexColor, Vector2.zero);
        vertexHelper.AddTriangle(index, index + 1, index + 2);
        vertexHelper.AddTriangle(index, index + 2, index + 3);
    }
}
