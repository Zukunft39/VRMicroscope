using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[ExecuteAlways]
public class UIFit : MonoBehaviour
{
    public Camera cam;                 // 主/头显相机
    [Range(0.2f, 5f)] public float distance = 1f;
    public Vector2Int rtSize = new Vector2Int(2048, 2048);
    public Mode mode = Mode.FitInside; // 三种适配模式

    public enum Mode { FitInside, Fill, Stretch }

    void LateUpdate()
    {
        if (!cam) return;

        // 垂直FOV -> 距离distance处的可视高
        float vFovRad = cam.fieldOfView * Mathf.Deg2Rad;
        float viewH = 2f * distance * Mathf.Tan(vFovRad * 0.5f);
        float viewW = viewH * cam.aspect;      // 可视宽

        float w = viewW, h = viewH;

        if (mode != Mode.Stretch)
        {
            float viewAspect = viewW / viewH;
            float rtAspect = (float)rtSize.x / rtSize.y;

            if (mode == Mode.FitInside)
            {
                // 保持比例完整显示（可能留边）
                w = viewW; h = w / rtAspect;
                if (h > viewH) { h = viewH; w = h * rtAspect; }
            }
            else // Fill
            {
                // 保持比例铺满（不留边，可能裁切）
                h = viewH; w = h * rtAspect;
                if (w < viewW) { w = viewW; h = w / rtAspect; }
            }
        }
        // Stretch 模式：直接覆盖整个视野，不保持比例（会拉伸）

        transform.localPosition = new Vector3(0, 0, distance);
        transform.localRotation = Quaternion.identity;
        transform.localScale    = new Vector3(w, h, 1f);
    }
}
