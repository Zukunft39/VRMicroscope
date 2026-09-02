using UnityEngine;
using UnityEngine.UI;

public sealed class SNOMDemonstrationGraphic : MaskableGraphic
{
    public enum DiagramMode
    {
        Overview,
        TimeDomain,
        BeamFocus,
        Probe,
        AfmFeedback,
        NearField,
        Background,
        Harmonics,
        Scan,
        Results
    }

    [SerializeField] private DiagramMode mode;
    [Range(0f, 1f)]
    [SerializeField] private float progress;

    private static readonly Color32 Dark = new Color32(9, 21, 36, 255);
    private static readonly Color32 Grid = new Color32(42, 67, 91, 180);
    private static readonly Color32 Cyan = new Color32(32, 203, 233, 255);
    private static readonly Color32 Amber = new Color32(255, 166, 35, 255);
    private static readonly Color32 Green = new Color32(72, 214, 139, 255);
    private static readonly Color32 Grey = new Color32(132, 151, 169, 255);
    private static readonly Color32 White = new Color32(236, 245, 250, 255);

    public void SetMode(DiagramMode value)
    {
        if (mode == value)
        {
            return;
        }

        mode = value;
        SetVerticesDirty();
    }

    public void SetProgress(float value)
    {
        value = Mathf.Clamp01(value);
        if (Mathf.Abs(progress - value) < 0.002f)
        {
            return;
        }

        progress = value;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect rect = GetPixelAdjustedRect();
        AddRect(vh, rect, Dark);
        DrawGrid(vh, rect);

        switch (mode)
        {
            case DiagramMode.Overview:
                DrawOverview(vh, rect);
                break;
            case DiagramMode.TimeDomain:
                DrawTimeDomain(vh, rect);
                break;
            case DiagramMode.BeamFocus:
                DrawBeam(vh, rect);
                break;
            case DiagramMode.Probe:
                DrawProbe(vh, rect, false);
                break;
            case DiagramMode.AfmFeedback:
                DrawAfm(vh, rect);
                break;
            case DiagramMode.NearField:
                DrawProbe(vh, rect, true);
                break;
            case DiagramMode.Background:
                DrawBackground(vh, rect);
                break;
            case DiagramMode.Harmonics:
                DrawHarmonics(vh, rect);
                break;
            case DiagramMode.Scan:
                DrawScan(vh, rect, progress);
                break;
            case DiagramMode.Results:
                DrawResults(vh, rect);
                break;
        }
    }

    private static void DrawGrid(VertexHelper vh, Rect rect)
    {
        for (int i = 1; i < 6; i++)
        {
            float x = Mathf.Lerp(rect.xMin, rect.xMax, i / 6f);
            AddLine(vh, new Vector2(x, rect.yMin), new Vector2(x, rect.yMax), 1f, Grid);
        }

        for (int i = 1; i < 4; i++)
        {
            float y = Mathf.Lerp(rect.yMin, rect.yMax, i / 4f);
            AddLine(vh, new Vector2(rect.xMin, y), new Vector2(rect.xMax, y), 1f, Grid);
        }
    }

    private static void DrawOverview(VertexHelper vh, Rect r)
    {
        AddRoundedBlock(vh, N(r, 0.07f, 0.60f, 0.22f, 0.25f), Amber);
        AddRoundedBlock(vh, N(r, 0.38f, 0.60f, 0.22f, 0.25f), new Color32(62, 126, 222, 255));
        AddRoundedBlock(vh, N(r, 0.69f, 0.60f, 0.22f, 0.25f), Cyan);
        AddRoundedBlock(vh, N(r, 0.38f, 0.13f, 0.22f, 0.25f), Green);
        AddArrow(vh, P(r, 0.29f, 0.72f), P(r, 0.37f, 0.72f), White);
        AddArrow(vh, P(r, 0.60f, 0.72f), P(r, 0.68f, 0.72f), White);
        AddArrow(vh, P(r, 0.80f, 0.58f), P(r, 0.58f, 0.35f), White);
    }

    private static void DrawTimeDomain(VertexHelper vh, Rect r)
    {
        Vector2 previous = P(r, 0.03f, 0.5f);
        for (int i = 1; i <= 100; i++)
        {
            float t = i / 100f;
            float envelope = Mathf.Exp(-Mathf.Pow((t - 0.46f) * 7f, 2f));
            float y = 0.5f + Mathf.Sin((t - 0.46f) * 42f) * envelope * 0.34f;
            Vector2 current = P(r, 0.03f + t * 0.94f, y);
            AddLine(vh, previous, current, 2.5f, Amber);
            previous = current;
        }

        AddArrow(vh, P(r, 0.08f, 0.16f), P(r, 0.92f, 0.16f), Grey);
    }

    private static void DrawBeam(VertexHelper vh, Rect r)
    {
        Vector2[] path =
        {
            P(r, 0.06f, 0.76f), P(r, 0.29f, 0.76f), P(r, 0.42f, 0.46f),
            P(r, 0.66f, 0.73f), P(r, 0.86f, 0.25f)
        };

        for (int i = 0; i < path.Length - 1; i++)
        {
            AddLine(vh, path[i], path[i + 1], 3f, Amber);
            AddCircle(vh, path[i], 6f, 12, White);
        }

        AddCircle(vh, path[path.Length - 1], 10f, 18, Cyan);
        AddLine(vh, P(r, 0.78f, 0.84f), P(r, 0.86f, 0.25f), 1.5f, Grey);
        AddLine(vh, P(r, 0.94f, 0.84f), P(r, 0.86f, 0.25f), 1.5f, Grey);
    }

    private static void DrawProbe(VertexHelper vh, Rect r, bool nearField)
    {
        Vector2 tip = P(r, 0.5f, 0.36f);
        AddTriangle(vh, P(r, 0.38f, 0.90f), P(r, 0.62f, 0.90f), tip, Grey);
        AddLine(vh, P(r, 0.13f, 0.22f), P(r, 0.87f, 0.22f), 4f, White);
        AddCircle(vh, tip, nearField ? 20f : 12f, 24, nearField ? Amber : Cyan);

        if (!nearField)
        {
            AddCircleOutline(vh, tip, 39f, 28, 2.5f, Cyan);
            return;
        }

        for (int i = 0; i < 5; i++)
        {
            float offset = (i - 2) * 0.04f;
            AddLine(vh, P(r, 0.5f + offset, 0.34f), P(r, 0.5f + offset * 1.8f, 0.23f), 2f, Amber);
        }

        AddCircle(vh, P(r, 0.5f, 0.12f), 9f, 18, new Color32(255, 166, 35, 120));
    }

    private static void DrawAfm(VertexHelper vh, Rect r)
    {
        Vector2 laser = P(r, 0.09f, 0.75f);
        Vector2 cantilever = P(r, 0.51f, 0.43f);
        Vector2 detector = P(r, 0.88f, 0.75f);
        AddLine(vh, laser, cantilever, 3f, Green);
        AddLine(vh, cantilever, detector, 3f, Green);
        AddCircle(vh, laser, 10f, 18, Green);
        AddTriangle(vh, P(r, 0.43f, 0.58f), P(r, 0.61f, 0.58f), cantilever, Grey);
        Rect quadrant = N(r, 0.78f, 0.60f, 0.18f, 0.25f);
        AddRect(vh, quadrant, new Color32(30, 66, 57, 255));
        AddLine(vh, new Vector2(quadrant.center.x, quadrant.yMin), new Vector2(quadrant.center.x, quadrant.yMax), 2f, White);
        AddLine(vh, new Vector2(quadrant.xMin, quadrant.center.y), new Vector2(quadrant.xMax, quadrant.center.y), 2f, White);
        AddCircle(vh, quadrant.center + new Vector2(8f, 5f), 6f, 14, Green);
        AddArrow(vh, P(r, 0.79f, 0.42f), P(r, 0.55f, 0.20f), Cyan);
        AddArrow(vh, P(r, 0.45f, 0.20f), P(r, 0.18f, 0.42f), Cyan);
    }

    private static void DrawBackground(VertexHelper vh, Rect r)
    {
        AddRect(vh, N(r, 0.12f, 0.62f, 0.76f, 0.18f), Grey);
        AddRect(vh, N(r, 0.12f, 0.34f, 0.08f, 0.18f), Cyan);
        AddLine(vh, P(r, 0.12f, 0.24f), P(r, 0.88f, 0.24f), 2f, White);
        AddCircle(vh, P(r, 0.86f, 0.71f), 7f, 16, White);
        AddCircle(vh, P(r, 0.18f, 0.43f), 7f, 16, White);
    }

    private static void DrawHarmonics(VertexHelper vh, Rect r)
    {
        DrawWave(vh, r, 0.76f, 1f, 0.10f, Grey);
        DrawWave(vh, r, 0.50f, 2f, 0.075f, Cyan);
        DrawWave(vh, r, 0.24f, 3f, 0.052f, Green);
        AddRect(vh, N(r, 0.04f, 0.67f, 0.012f, 0.18f), Grey);
        AddRect(vh, N(r, 0.04f, 0.42f, 0.012f, 0.15f), Cyan);
        AddRect(vh, N(r, 0.04f, 0.18f, 0.012f, 0.12f), Green);
    }

    private static void DrawWave(VertexHelper vh, Rect r, float centerY, float frequency, float amplitude, Color32 color)
    {
        Vector2 previous = P(r, 0.08f, centerY);
        for (int i = 1; i <= 72; i++)
        {
            float t = i / 72f;
            Vector2 current = P(r, 0.08f + t * 0.86f,
                centerY + Mathf.Sin(t * Mathf.PI * 8f * frequency) * amplitude);
            AddLine(vh, previous, current, 2f, color);
            previous = current;
        }
    }

    private static void DrawScan(VertexHelper vh, Rect r, float reveal)
    {
        const int columns = 20;
        const int rows = 9;
        int visible = Mathf.RoundToInt(columns * rows * Mathf.Clamp01(reveal));
        Rect area = N(r, 0.06f, 0.12f, 0.88f, 0.76f);
        float cw = area.width / columns;
        float ch = area.height / rows;

        for (int index = 0; index < visible; index++)
        {
            int row = index / columns;
            int col = index % columns;
            int displayCol = row % 2 == 0 ? col : columns - 1 - col;
            float value = SampleField(displayCol / (float)(columns - 1), row / (float)(rows - 1));
            Color32 cell = Color32.Lerp(new Color32(8, 47, 71, 255), Amber, value);
            AddRect(vh, new Rect(area.xMin + displayCol * cw, area.yMin + row * ch, cw + 0.5f, ch + 0.5f), cell);
        }

        if (visible > 0 && visible < columns * rows)
        {
            int cursor = visible - 1;
            int row = cursor / columns;
            int col = cursor % columns;
            int displayCol = row % 2 == 0 ? col : columns - 1 - col;
            AddCircleOutline(vh,
                new Vector2(area.xMin + (displayCol + 0.5f) * cw, area.yMin + (row + 0.5f) * ch),
                Mathf.Max(5f, Mathf.Min(cw, ch) * 0.45f), 16, 2f, White);
        }
    }

    private static void DrawResults(VertexHelper vh, Rect r)
    {
        Rect left = N(r, 0.035f, 0.16f, 0.28f, 0.68f);
        Rect middle = N(r, 0.36f, 0.16f, 0.28f, 0.68f);
        Rect right = N(r, 0.685f, 0.16f, 0.28f, 0.68f);
        DrawResultMap(vh, left, 0);
        DrawResultMap(vh, middle, 1);
        AddRect(vh, right, new Color32(8, 29, 48, 255));

        Vector2 previous = new Vector2(right.xMin, right.center.y);
        for (int i = 1; i <= 52; i++)
        {
            float t = i / 52f;
            float peakA = Mathf.Exp(-Mathf.Pow((t - 0.34f) * 11f, 2f));
            float peakB = 0.6f * Mathf.Exp(-Mathf.Pow((t - 0.69f) * 15f, 2f));
            Vector2 current = new Vector2(Mathf.Lerp(right.xMin, right.xMax, t),
                right.yMin + right.height * (0.12f + 0.70f * (peakA + peakB)));
            AddLine(vh, previous, current, 2f, Green);
            previous = current;
        }
    }

    private static void DrawResultMap(VertexHelper vh, Rect area, int variant)
    {
        const int columns = 12;
        const int rows = 12;
        float cw = area.width / columns;
        float ch = area.height / rows;
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                float u = x / (float)(columns - 1);
                float v = y / (float)(rows - 1);
                float sample = SampleField(u, v);
                Color32 low = variant == 0 ? new Color32(18, 38, 50, 255) : new Color32(6, 40, 68, 255);
                Color32 high = variant == 0 ? White : Amber;
                AddRect(vh, new Rect(area.xMin + x * cw, area.yMin + y * ch, cw + 0.4f, ch + 0.4f),
                    Color32.Lerp(low, high, sample));
            }
        }
    }

    private static float SampleField(float u, float v)
    {
        float a = Mathf.Exp(-((u - 0.32f) * (u - 0.32f) + (v - 0.58f) * (v - 0.58f)) * 28f);
        float b = 0.8f * Mathf.Exp(-((u - 0.71f) * (u - 0.71f) + (v - 0.38f) * (v - 0.38f)) * 48f);
        float texture = 0.12f * (Mathf.Sin(u * 31f) * Mathf.Sin(v * 23f) + 1f);
        return Mathf.Clamp01(a + b + texture);
    }

    private static void AddRoundedBlock(VertexHelper vh, Rect rect, Color32 color)
    {
        AddRect(vh, rect, new Color32(color.r, color.g, color.b, 115));
        AddCircle(vh, rect.center, Mathf.Min(rect.width, rect.height) * 0.20f, 18, color);
    }

    private static void AddArrow(VertexHelper vh, Vector2 from, Vector2 to, Color32 color)
    {
        AddLine(vh, from, to, 2.5f, color);
        Vector2 direction = (to - from).normalized;
        Vector2 side = new Vector2(-direction.y, direction.x);
        AddTriangle(vh, to, to - direction * 12f + side * 6f, to - direction * 12f - side * 6f, color);
    }

    private static void AddCircleOutline(VertexHelper vh, Vector2 center, float radius, int segments, float width, Color32 color)
    {
        Vector2 previous = center + Vector2.right * radius;
        for (int i = 1; i <= segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 2f;
            Vector2 current = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            AddLine(vh, previous, current, width, color);
            previous = current;
        }
    }

    private static void AddLine(VertexHelper vh, Vector2 a, Vector2 b, float width, Color32 color)
    {
        Vector2 direction = (b - a).normalized;
        Vector2 normal = new Vector2(-direction.y, direction.x) * width * 0.5f;
        int start = vh.currentVertCount;
        vh.AddVert(a - normal, color, Vector2.zero);
        vh.AddVert(a + normal, color, Vector2.zero);
        vh.AddVert(b + normal, color, Vector2.zero);
        vh.AddVert(b - normal, color, Vector2.zero);
        vh.AddTriangle(start, start + 1, start + 2);
        vh.AddTriangle(start, start + 2, start + 3);
    }

    private static void AddRect(VertexHelper vh, Rect rect, Color32 color)
    {
        int start = vh.currentVertCount;
        vh.AddVert(new Vector2(rect.xMin, rect.yMin), color, Vector2.zero);
        vh.AddVert(new Vector2(rect.xMin, rect.yMax), color, Vector2.up);
        vh.AddVert(new Vector2(rect.xMax, rect.yMax), color, Vector2.one);
        vh.AddVert(new Vector2(rect.xMax, rect.yMin), color, Vector2.right);
        vh.AddTriangle(start, start + 1, start + 2);
        vh.AddTriangle(start, start + 2, start + 3);
    }

    private static void AddTriangle(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color32 color)
    {
        int start = vh.currentVertCount;
        vh.AddVert(a, color, Vector2.zero);
        vh.AddVert(b, color, Vector2.zero);
        vh.AddVert(c, color, Vector2.zero);
        vh.AddTriangle(start, start + 1, start + 2);
    }

    private static void AddCircle(VertexHelper vh, Vector2 center, float radius, int segments, Color32 color)
    {
        int centerIndex = vh.currentVertCount;
        vh.AddVert(center, color, new Vector2(0.5f, 0.5f));
        for (int i = 0; i <= segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 2f;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            vh.AddVert(center + direction * radius, color, (direction + Vector2.one) * 0.5f);
            if (i > 0)
            {
                vh.AddTriangle(centerIndex, centerIndex + i, centerIndex + i + 1);
            }
        }
    }

    private static Vector2 P(Rect rect, float x, float y)
    {
        return new Vector2(Mathf.Lerp(rect.xMin, rect.xMax, x), Mathf.Lerp(rect.yMin, rect.yMax, y));
    }

    private static Rect N(Rect rect, float x, float y, float width, float height)
    {
        return new Rect(Mathf.Lerp(rect.xMin, rect.xMax, x), Mathf.Lerp(rect.yMin, rect.yMax, y),
            rect.width * width, rect.height * height);
    }
}
