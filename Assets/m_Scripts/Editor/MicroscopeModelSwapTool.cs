using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class MicroscopeModelSwapTool
{
    private const string OldModelPath = "Assets/FBX/Microscope(Decimate).fbx";
    private const string NewModelPath = "Assets/FBX/KnobChanged.fbx";

    [MenuItem("Tools/Microscope/Compare Decimate And KnobChanged Parts")]
    private static void CompareParts()
    {
        GameObject oldRoot = AssetDatabase.LoadAssetAtPath<GameObject>(OldModelPath);
        GameObject newRoot = AssetDatabase.LoadAssetAtPath<GameObject>(NewModelPath);
        if (oldRoot == null || newRoot == null)
        {
            Debug.LogError($"[MicroscopeSwap] Missing model asset. old={OldModelPath}, new={NewModelPath}");
            return;
        }

        HashSet<string> oldTransformPaths = CollectTransformPaths(oldRoot.transform);
        HashSet<string> newTransformPaths = CollectTransformPaths(newRoot.transform);

        HashSet<string> oldMeshNames = CollectMeshNames(OldModelPath);
        HashSet<string> newMeshNames = CollectMeshNames(NewModelPath);

        List<string> missingTransformPaths = Subtract(oldTransformPaths, newTransformPaths);
        List<string> extraTransformPaths = Subtract(newTransformPaths, oldTransformPaths);
        List<string> missingMeshNames = Subtract(oldMeshNames, newMeshNames);
        List<string> extraMeshNames = Subtract(newMeshNames, oldMeshNames);

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("[MicroscopeSwap] Compare report");
        sb.AppendLine($"Old model: {OldModelPath}");
        sb.AppendLine($"New model: {NewModelPath}");
        sb.AppendLine($"Transform paths old/new: {oldTransformPaths.Count}/{newTransformPaths.Count}");
        sb.AppendLine($"Mesh names old/new: {oldMeshNames.Count}/{newMeshNames.Count}");
        sb.AppendLine($"Missing transform paths in new: {missingTransformPaths.Count}");
        AppendList(sb, missingTransformPaths, 40);
        sb.AppendLine($"Extra transform paths in new: {extraTransformPaths.Count}");
        AppendList(sb, extraTransformPaths, 40);
        sb.AppendLine($"Missing mesh names in new: {missingMeshNames.Count}");
        AppendList(sb, missingMeshNames, 60);
        sb.AppendLine($"Extra mesh names in new: {extraMeshNames.Count}");
        AppendList(sb, extraMeshNames, 60);

        Debug.Log(sb.ToString());
    }

    [MenuItem("Tools/Microscope/Replace Selected Microscope Meshes With KnobChanged")]
    private static void ReplaceSelectedMicroscopeMeshes()
    {
        GameObject selectedRoot = Selection.activeGameObject;
        if (selectedRoot == null)
        {
            Debug.LogError("[MicroscopeSwap] Select the scene Microscope root first.");
            return;
        }

        Dictionary<string, Mesh> newMeshMap = BuildMeshMap(NewModelPath);
        if (newMeshMap.Count == 0)
        {
            Debug.LogError($"[MicroscopeSwap] No mesh found in new model: {NewModelPath}");
            return;
        }

        int replaced = 0;
        int skippedNoMatch = 0;

        MeshFilter[] meshFilters = selectedRoot.GetComponentsInChildren<MeshFilter>(true);
        for (int i = 0; i < meshFilters.Length; i++)
        {
            MeshFilter filter = meshFilters[i];
            if (filter == null || filter.sharedMesh == null)
            {
                continue;
            }

            if (!IsMeshFromModel(filter.sharedMesh, OldModelPath))
            {
                continue;
            }

            if (TryGetReplacementMesh(newMeshMap, filter.sharedMesh.name, out Mesh replacement))
            {
                Undo.RecordObject(filter, "Replace Microscope MeshFilter Mesh");
                filter.sharedMesh = replacement;
                EditorUtility.SetDirty(filter);
                replaced++;
            }
            else
            {
                skippedNoMatch++;
            }
        }

        SkinnedMeshRenderer[] skinnedRenderers = selectedRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int i = 0; i < skinnedRenderers.Length; i++)
        {
            SkinnedMeshRenderer renderer = skinnedRenderers[i];
            if (renderer == null || renderer.sharedMesh == null)
            {
                continue;
            }

            if (!IsMeshFromModel(renderer.sharedMesh, OldModelPath))
            {
                continue;
            }

            if (TryGetReplacementMesh(newMeshMap, renderer.sharedMesh.name, out Mesh replacement))
            {
                Undo.RecordObject(renderer, "Replace Microscope SkinnedMeshRenderer Mesh");
                renderer.sharedMesh = replacement;
                EditorUtility.SetDirty(renderer);
                replaced++;
            }
            else
            {
                skippedNoMatch++;
            }
        }

        MeshCollider[] meshColliders = selectedRoot.GetComponentsInChildren<MeshCollider>(true);
        for (int i = 0; i < meshColliders.Length; i++)
        {
            MeshCollider collider = meshColliders[i];
            if (collider == null || collider.sharedMesh == null)
            {
                continue;
            }

            if (!IsMeshFromModel(collider.sharedMesh, OldModelPath))
            {
                continue;
            }

            if (TryGetReplacementMesh(newMeshMap, collider.sharedMesh.name, out Mesh replacement))
            {
                Undo.RecordObject(collider, "Replace Microscope MeshCollider Mesh");
                collider.sharedMesh = replacement;
                EditorUtility.SetDirty(collider);
                replaced++;
            }
            else
            {
                skippedNoMatch++;
            }
        }

        Debug.Log($"[MicroscopeSwap] Replace finished on '{selectedRoot.name}'. Replaced={replaced}, noMatch={skippedNoMatch}.");
    }

    [MenuItem("Tools/Microscope/Validate Selected Microscope Mesh Sources")]
    private static void ValidateSelectedMicroscopeMeshSources()
    {
        GameObject selectedRoot = Selection.activeGameObject;
        if (selectedRoot == null)
        {
            Debug.LogError("[MicroscopeSwap] Select the scene Microscope root first.");
            return;
        }

        int oldCount = 0;
        int newCount = 0;
        int otherCount = 0;
        HashSet<string> oldMeshNames = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> otherAssetPaths = new HashSet<string>(StringComparer.Ordinal);

        MeshFilter[] meshFilters = selectedRoot.GetComponentsInChildren<MeshFilter>(true);
        for (int i = 0; i < meshFilters.Length; i++)
        {
            Mesh mesh = meshFilters[i] != null ? meshFilters[i].sharedMesh : null;
            CountMeshSource(mesh, ref oldCount, ref newCount, ref otherCount, oldMeshNames, otherAssetPaths);
        }

        SkinnedMeshRenderer[] skinnedRenderers = selectedRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int i = 0; i < skinnedRenderers.Length; i++)
        {
            Mesh mesh = skinnedRenderers[i] != null ? skinnedRenderers[i].sharedMesh : null;
            CountMeshSource(mesh, ref oldCount, ref newCount, ref otherCount, oldMeshNames, otherAssetPaths);
        }

        MeshCollider[] meshColliders = selectedRoot.GetComponentsInChildren<MeshCollider>(true);
        for (int i = 0; i < meshColliders.Length; i++)
        {
            Mesh mesh = meshColliders[i] != null ? meshColliders[i].sharedMesh : null;
            CountMeshSource(mesh, ref oldCount, ref newCount, ref otherCount, oldMeshNames, otherAssetPaths);
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"[MicroscopeSwap] Validate '{selectedRoot.name}'");
        sb.AppendLine($"Old(decimate) mesh refs: {oldCount}");
        sb.AppendLine($"New(knobChanged) mesh refs: {newCount}");
        sb.AppendLine($"Other mesh refs: {otherCount}");

        if (oldMeshNames.Count > 0)
        {
            List<string> names = new List<string>(oldMeshNames);
            names.Sort(StringComparer.Ordinal);
            sb.AppendLine("Remaining old mesh names:");
            for (int i = 0; i < names.Count; i++)
            {
                sb.AppendLine($"  - {names[i]}");
            }
        }

        if (otherAssetPaths.Count > 0)
        {
            List<string> paths = new List<string>(otherAssetPaths);
            paths.Sort(StringComparer.Ordinal);
            sb.AppendLine("Other mesh asset paths:");
            for (int i = 0; i < paths.Count; i++)
            {
                sb.AppendLine($"  - {paths[i]}");
            }
        }

        Debug.Log(sb.ToString());
    }

    private static HashSet<string> CollectTransformPaths(Transform root)
    {
        HashSet<string> paths = new HashSet<string>(StringComparer.Ordinal);
        if (root == null)
        {
            return paths;
        }

        Stack<(Transform node, string path)> stack = new Stack<(Transform node, string path)>();
        stack.Push((root, root.name));

        while (stack.Count > 0)
        {
            (Transform node, string path) item = stack.Pop();
            paths.Add(item.path);

            for (int i = 0; i < item.node.childCount; i++)
            {
                Transform child = item.node.GetChild(i);
                stack.Push((child, item.path + "/" + child.name));
            }
        }

        return paths;
    }

    private static HashSet<string> CollectMeshNames(string modelPath)
    {
        HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
        UnityEngine.Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(modelPath);
        for (int i = 0; i < subAssets.Length; i++)
        {
            Mesh mesh = subAssets[i] as Mesh;
            if (mesh != null)
            {
                names.Add(mesh.name);
            }
        }

        return names;
    }

    private static Dictionary<string, Mesh> BuildMeshMap(string modelPath)
    {
        Dictionary<string, Mesh> map = new Dictionary<string, Mesh>(StringComparer.Ordinal);
        UnityEngine.Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(modelPath);
        for (int i = 0; i < subAssets.Length; i++)
        {
            Mesh mesh = subAssets[i] as Mesh;
            if (mesh == null)
            {
                continue;
            }

            if (!map.ContainsKey(mesh.name))
            {
                map.Add(mesh.name, mesh);
            }
        }

        return map;
    }

    private static bool IsMeshFromModel(Mesh mesh, string modelPath)
    {
        string assetPath = AssetDatabase.GetAssetPath(mesh);
        return string.Equals(assetPath, modelPath, StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryGetReplacementMesh(Dictionary<string, Mesh> newMeshMap, string oldMeshName, out Mesh replacement)
    {
        if (newMeshMap.TryGetValue(oldMeshName, out replacement))
        {
            return true;
        }

        string normalized = NormalizeMeshName(oldMeshName);
        if (!string.Equals(normalized, oldMeshName, StringComparison.Ordinal) &&
            newMeshMap.TryGetValue(normalized, out replacement))
        {
            return true;
        }

        replacement = null;
        return false;
    }

    private static string NormalizeMeshName(string meshName)
    {
        if (string.IsNullOrEmpty(meshName))
        {
            return string.Empty;
        }

        if (meshName.EndsWith(".001", StringComparison.OrdinalIgnoreCase))
        {
            meshName = meshName.Substring(0, meshName.Length - 4);
        }

        // Blender/Unity style duplicated suffix: "main (1)" -> "main"
        int suffixStart = meshName.LastIndexOf(" (", StringComparison.Ordinal);
        if (suffixStart > 0 && meshName.EndsWith(")", StringComparison.Ordinal))
        {
            string numberPart = meshName.Substring(suffixStart + 2, meshName.Length - suffixStart - 3);
            if (int.TryParse(numberPart, out _))
            {
                meshName = meshName.Substring(0, suffixStart);
            }
        }

        return meshName;
    }

    private static void CountMeshSource(
        Mesh mesh,
        ref int oldCount,
        ref int newCount,
        ref int otherCount,
        HashSet<string> oldMeshNames,
        HashSet<string> otherAssetPaths)
    {
        if (mesh == null)
        {
            return;
        }

        string assetPath = AssetDatabase.GetAssetPath(mesh);
        if (string.Equals(assetPath, OldModelPath, StringComparison.OrdinalIgnoreCase))
        {
            oldCount++;
            oldMeshNames.Add(mesh.name);
            return;
        }

        if (string.Equals(assetPath, NewModelPath, StringComparison.OrdinalIgnoreCase))
        {
            newCount++;
            return;
        }

        otherCount++;
        if (!string.IsNullOrEmpty(assetPath))
        {
            otherAssetPaths.Add(assetPath);
        }
    }

    private static List<string> Subtract(HashSet<string> lhs, HashSet<string> rhs)
    {
        List<string> result = new List<string>();
        foreach (string item in lhs)
        {
            if (!rhs.Contains(item))
            {
                result.Add(item);
            }
        }

        result.Sort(StringComparer.Ordinal);
        return result;
    }

    private static void AppendList(StringBuilder sb, List<string> items, int maxItems)
    {
        int count = Mathf.Min(items.Count, maxItems);
        for (int i = 0; i < count; i++)
        {
            sb.AppendLine($"  - {items[i]}");
        }

        if (items.Count > maxItems)
        {
            sb.AppendLine($"  ... ({items.Count - maxItems} more)");
        }
    }
}
