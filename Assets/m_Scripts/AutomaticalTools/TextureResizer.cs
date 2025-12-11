using UnityEngine;
using UnityEditor;

public class TextureResizer : EditorWindow
{
    [MenuItem("Tools/Resize All Textures to 512")]
    public static void ResizeTextures()
    {
        // 1. Find all files that are Textures
        string[] guids = AssetDatabase.FindAssets("t:Texture");
        int count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            
            // 2. Get the settings for this texture
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer != null)
            {
                // 3. Check if we need to change it
                if (importer.maxTextureSize != 512)
                {
                    importer.maxTextureSize = 512;
                    
                    // 4. Apply the settings (This triggers a re-import)
                    AssetDatabase.ImportAsset(path);
                    count++;
                }
            }
        }
        Debug.Log($"Finished! Resized {count} textures to 512.");
    }
}