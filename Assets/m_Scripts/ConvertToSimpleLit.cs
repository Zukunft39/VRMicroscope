using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ConvertToSimpleLit : MonoBehaviour
{
    [MenuItem("Tools/将所有 Lit 转换为 Simple Lit")]
    public static void ConvertLitToSimpleLit()
    {
        // 1. 获取项目中所有材质的 GUID
        string[] guids = AssetDatabase.FindAssets("t:Material");
        
        int count = 0;

        foreach (string guid in guids)
        {
            // 2. 加载材质
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            // 3. 检查当前 Shader 是否为 URP Lit
            if (mat != null && mat.shader.name == "Universal Render Pipeline/Lit")
            {
                // 4. 切换为 Simple Lit
                mat.shader = Shader.Find("Universal Render Pipeline/Simple Lit");
                count++;
            }
        }

        // 5. 保存更改
        if (count > 0)
        {
            AssetDatabase.SaveAssets();
            Debug.Log($"成功将 {count} 个材质从 Lit 转换为 Simple Lit！");
        }
        else
        {
            Debug.Log("未找到使用 Lit Shader 的材质。");
        }
    }
}
