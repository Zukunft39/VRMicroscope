using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ShowObject : MonoBehaviour
{
    public Microscope microscope;
    public MeshRenderer meshRenderer;
    public Material material;
    // 此值设置为Shader中实际控制透明度的属性名
    public string alphaPropertyName = "_Alpha";

    public string lightName = "LightnessGate";

    public List<Material> textures;

    public int choice;

    void OnEnable()
    {
        
    }

    // 直接设置透明度
    public void SetColor(float targetAlpha)
    {
        if (material == null) return;
        // 确保透明度值在0-1范围内
        targetAlpha = Mathf.Clamp01(targetAlpha);

        material.SetFloat(alphaPropertyName, targetAlpha);

        if(microscope!=null)
        {
            SetLight(microscope.GetLight());
        }
    }

    public void SetLight(float targetLight)
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

        material.SetFloat(lightName, targetLight);
        Debug.Log(targetLight);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            choice += 1;
            choice %= textures.Count;
            meshRenderer.sharedMaterial = textures[choice];
            material = meshRenderer.sharedMaterial;
        }

        if(microscope!=null)
        {
            SetLight(microscope.GetLight());
        }
    }
}
    