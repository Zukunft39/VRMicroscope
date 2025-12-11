using UnityEngine;
using UnityEditor;

public class ShaderSwapper : EditorWindow
{
    [MenuItem("Tools/Convert Lit to Simple Lit")]
    public static void SwapShaders()
    {
        // 1. Find the target shader (Simple Lit)
        Shader simpleLit = Shader.Find("Universal Render Pipeline/Simple Lit");
        
        if (simpleLit == null)
        {
            Debug.LogError("Error: Could not find 'Simple Lit' shader. Is URP installed?");
            return;
        }

        // 2. Find all materials in the entire project
        string[] guids = AssetDatabase.FindAssets("t:Material");
        int count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            // 3. Check if the material is currently using "Lit"
            if (mat != null && mat.shader.name == "Universal Render Pipeline/Lit")
            {
                // 4. Change it to Simple Lit
                mat.shader = simpleLit;
                EditorUtility.SetDirty(mat); // Tell Unity to save this change
                count++;
            }
        }

        // 5. Save all changes
        AssetDatabase.SaveAssets();
        Debug.Log($"Success! Converted {count} materials to Simple Lit.");
    }
}