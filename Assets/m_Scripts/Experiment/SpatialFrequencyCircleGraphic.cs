using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class SpatialFrequencyCircleGraphic : MaskableGraphic
{
    [SerializeField, Min(12)] private int segments = 64;

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

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();
        int safeSegments = Mathf.Max(12, segments);
        float radius = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * 0.5f;
        vertexHelper.AddVert(Vector3.zero, color, new Vector2(0.5f, 0.5f));

        for (int i = 0; i <= safeSegments; i++)
        {
            float angle = Mathf.PI * 2f * i / safeSegments;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            vertexHelper.AddVert(direction * radius, color, direction * 0.5f + Vector2.one * 0.5f);
        }

        for (int i = 0; i < safeSegments; i++)
        {
            vertexHelper.AddTriangle(0, i + 1, i + 2);
        }
    }
}
