using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ShowObject : MonoBehaviour
{
    public MeshRenderer meshRenderer;
    public Material material;
    // 此值设置为Shader中实际控制透明度的属性名
    public string alphaPropertyName = "_Alpha";

    void OnEnable()
    {
        // 获取组件引用
        if (meshRenderer == null)
            meshRenderer = gameObject.GetComponentInChildren<MeshRenderer>();

        if (meshRenderer != null && material == null)
            material = meshRenderer.sharedMaterial;

        if (meshRenderer == null)
            Debug.LogError("找不到MeshRenderer组件", this);
        
        if (material == null)
            Debug.LogError("找不到Material", this);
    }

    // 直接设置透明度
    public void SetColor(float targetAlpha)
    {
        if (material == null) return;
        // 确保透明度值在0-1范围内
        targetAlpha = Mathf.Clamp01(targetAlpha);

        material.SetFloat(alphaPropertyName, targetAlpha);
    }
}
    