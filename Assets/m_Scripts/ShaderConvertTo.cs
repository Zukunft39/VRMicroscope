using UnityEngine;
using UnityEditor;

public class ShaderConvertTo : EditorWindow
{
    [MenuItem("Tools/Convert All Lit to Unlit")]
    public static void ConvertLitToUnlit()
    {
        // 询问确认，防止误操作
        if (!EditorUtility.DisplayDialog("确认转换", 
            "这将把项目中所有 URP Lit 材质转换为 Unlit 材质。\n\n此操作不可撤销（除非使用版本控制回滚）。确认继续吗？", 
            "开始转换", "取消"))
        {
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Material");
        int count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat != null && mat.shader.name == "Universal Render Pipeline/Lit")
            {
                // 1. 记录原有贴图和颜色（防止 Shader 切换后属性丢失）
                Texture mainTexture = mat.GetTexture("_BaseMap");
                Color baseColor = mat.GetColor("_BaseColor");

                // 2. 切换 Shader
                mat.shader = Shader.Find("Universal Render Pipeline/Unlit");

                // 3. 重新赋值属性 (URP Lit 和 Unlit 属性名通常都是 _BaseMap 和 _BaseColor，但也可能需要重新指定)
                if (mainTexture != null) mat.SetTexture("_BaseMap", mainTexture);
                mat.SetColor("_BaseColor", baseColor);

                EditorUtility.SetDirty(mat); // 标记为已修改
                count++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"转换完成！共修改了 {count} 个材质。");
    }
}