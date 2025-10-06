using UnityEngine;

[ExecuteAlways]
public class UIFit : MonoBehaviour
{
    public Camera cam;                      // 主/头显相机（Center Eye）
    [Range(0.2f, 5f)] public float distance = 1f;
    public Vector2Int rtSize = new Vector2Int(2048, 2048);
    public Mode mode = Mode.FitInside;      // 三种适配模式
    [Range(0.7f, 1f)] public float safe = 0.98f; // 安全缩放，避免边缘被裁

    public enum Mode { FitInside, Fill, Stretch }

    void LateUpdate()
    {
        if (!cam) return;

        // ① 计算在 distance 处的可视宽高（优先用 XR 的双眼投影）
        float viewW, viewH;
        if (cam.stereoEnabled)
        {
            GetXRViewSize(cam, distance, out viewW, out viewH);
            viewW *= safe;
            viewH *= safe;
        }
        else
        {
            // 非 XR 回退到传统 FOV
            float vFovRad = cam.fieldOfView * Mathf.Deg2Rad;
            viewH = 2f * distance * Mathf.Tan(vFovRad * 0.5f);
            viewW = viewH * cam.aspect;
        }

        // ② 按你的三种模式做适配
        float w = viewW, h = viewH;
        if (mode != Mode.Stretch)
        {
            float rtAspect = (float)rtSize.x / rtSize.y;

            if (mode == Mode.FitInside)
            {
                w = viewW; h = w / rtAspect;
                if (h > viewH) { h = viewH; w = h * rtAspect; }
            }
            else // Fill
            {
                h = viewH; w = h * rtAspect;
                if (w < viewW) { w = viewW; h = w / rtAspect; }
            }
        }

        // ③ 放到相机前方 distance 处并正对相机
        transform.localPosition = new Vector3(0, 0, distance);
        transform.localRotation = Quaternion.identity;
        transform.localScale    = new Vector3(w, h, 1f);
    }

    // —— 从投影矩阵提取每只眼的左右上下切线（tan 半角）——
    static void ExtractTangents(Matrix4x4 p, float near, out float l, out float r, out float b, out float t)
    {
        float m00 = p[0,0], m02 = p[0,2];
        float m11 = p[1,1], m12 = p[1,2];

        float left   = near * (m02 - 1f) / m00;
        float right  = near * (m02 + 1f) / m00;
        float bottom = near * (m12 - 1f) / m11;
        float top    = near * (m12 + 1f) / m11;

        l = left   / near;
        r = right  / near;
        b = bottom / near;
        t = top    / near;
    }

    // —— 计算“双眼交集”的视口宽高（保证两只眼都看得到边缘）——
    static void GetXRViewSize(Camera cam, float dist, out float viewW, out float viewH)
    {
        var pL = cam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left);
        var pR = cam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right);
        float n = cam.nearClipPlane;

        ExtractTangents(pL, n, out var lL, out var rL, out var bL, out var tL);
        ExtractTangents(pR, n, out var lR, out var rR, out var bR, out var tR);

        // 交集：更“内收”的那一对边界
        float l = Mathf.Max(lL, lR);
        float r = Mathf.Min(rL, rR);
        float b = Mathf.Max(bL, bR);
        float t = Mathf.Min(tL, tR);

        viewW = (r - l) * dist;
        viewH = (t - b) * dist;
    }
}
