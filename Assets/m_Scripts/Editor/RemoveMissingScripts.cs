using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

public class RemoveMissingScripts : Editor
{
    [MenuItem("Tools/Remove Missing Scripts/In Active Scene")]
    private static void CleanActiveScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        GameObject[] rootObjects = currentScene.GetRootGameObjects();
        
        int totalRemovedCount = 0;
        int totalAffectedGameObjects = 0;

        foreach (GameObject go in rootObjects)
        {
            // 遍历所有子物体（包括隐藏的物体）
            Transform[] transforms = go.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in transforms)
            {
                // Unity 官方提供的移除 Missing 脚本的 API
                int removedCount = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                if (removedCount > 0)
                {
                    totalRemovedCount += removedCount;
                    totalAffectedGameObjects++;
                }
            }
        }

        if (totalRemovedCount > 0)
        {
            // 标记场景为已修改，以便可以按 Ctrl+S 保存
            EditorSceneManager.MarkSceneDirty(currentScene);
        }

        Debug.Log($"[Cleanup] Removed {totalRemovedCount} missing scripts from {totalAffectedGameObjects} GameObjects.");
    }

    [MenuItem("Tools/Remove Missing Scripts/In Selected GameObjects")]
    private static void CleanSelectedObjects()
    {
        GameObject[] selectedObjects = Selection.gameObjects;
        if (selectedObjects.Length == 0)
        {
            Debug.LogWarning("Select at least one GameObject in the Hierarchy first.");
            return;
        }

        int totalRemovedCount = 0;
        int totalAffectedGameObjects = 0;

        foreach (GameObject go in selectedObjects)
        {
            Transform[] transforms = go.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in transforms)
            {
                int removedCount = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                if (removedCount > 0)
                {
                    totalRemovedCount += removedCount;
                    totalAffectedGameObjects++;
                }
            }
        }

        Debug.Log($"[Cleanup] Removed {totalRemovedCount} missing scripts from {totalAffectedGameObjects} selected GameObjects.");
    }

    [MenuItem("Tools/Remove Missing Scripts/In All Prefabs")]
    private static void CleanAllPrefabs()
    {
        string[] allPrefabGuids = AssetDatabase.FindAssets("t:Prefab");
        int totalRemovedCount = 0;
        int totalAffectedPrefabs = 0;

        try
        {
            for (int i = 0; i < allPrefabGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(allPrefabGuids[i]);
                EditorUtility.DisplayProgressBar("Cleaning Prefabs", $"Checking {path}", (float)i / allPrefabGuids.Length);

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                int removedCount = 0;
                Transform[] transforms = prefab.GetComponentsInChildren<Transform>(true);
                foreach (Transform t in transforms)
                {
                    int count = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                    if (count > 0)
                    {
                        removedCount += count;
                        EditorUtility.SetDirty(prefab);
                    }
                }

                if (removedCount > 0)
                {
                    totalRemovedCount += removedCount;
                    totalAffectedPrefabs++;
                    Debug.Log($"[Prefab cleanup] Removed {removedCount} missing scripts from {path}");
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
        }

        Debug.Log($"[Cleanup] Removed {totalRemovedCount} missing scripts from {totalAffectedPrefabs} prefabs.");
    }
}
