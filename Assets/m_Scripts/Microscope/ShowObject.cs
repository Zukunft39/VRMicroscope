using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowObject : MonoBehaviour
{
    public MeshRenderer meshRenderer;
    public Material material;
    // 请将此值设置为你Shader中实际控制透明度的属性名
    public string alphaPropertyName = "_Alpha";

    void OnEnable()
    {
        // 获取组件引用
        if (meshRenderer == null)
            meshRenderer = gameObject.GetComponentInChildren<MeshRenderer>();
        
        if (meshRenderer != null && material == null)
            material = meshRenderer.material; // 使用material属性而非materials数组
        
        // 验证组件是否获取成功
        if (meshRenderer == null)
            Debug.LogError("找不到MeshRenderer组件", this);
        
        if (material == null)
            Debug.LogError("找不到Material", this);
    }

    // 直接设置透明度
    public void SetColor(float targetAlpha)
    {
        // 安全检查
        if (material == null) return;
        // 确保透明度值在0-1范围内
        targetAlpha = Mathf.Clamp01(targetAlpha);

        // 直接设置Shader中的透明度属性
        material.SetFloat(alphaPropertyName, targetAlpha);
    }

    // 快捷方法：显示物体（完全不透明）
    public void Show()
    {
        SetColor(1f);
    }

    // 快捷方法：隐藏物体（完全透明）
    public void Hide()
    {
        SetColor(0f);
    }
}
    