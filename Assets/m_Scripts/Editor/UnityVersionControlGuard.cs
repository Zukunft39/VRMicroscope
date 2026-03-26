using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class UnityVersionControlGuard
{
    private const string CollabPackageName = "com.unity.collab-proxy";
    private const string ManifestPath = "Packages/manifest.json";
    private const string VersionControlSettingsPath = "ProjectSettings/VersionControlSettings.asset";
    private const double CheckIntervalSeconds = 10d;

    private static double nextCheckTime;

    static UnityVersionControlGuard()
    {
        ScheduleNextCheck();
        EditorApplication.update -= Update;
        EditorApplication.update += Update;
        EditorApplication.delayCall += EnforceDisabled;
    }

    [MenuItem("Tools/Project/Disable Unity Version Control (Enforce)")]
    private static void EnforceDisabledMenu()
    {
        EnforceDisabled();
    }

    private static void Update()
    {
        if (EditorApplication.timeSinceStartup < nextCheckTime)
        {
            return;
        }

        EnforceDisabled();
    }

    private static void EnforceDisabled()
    {
        bool changedManifest = RemoveCollabProxyFromManifest();
        bool changedSettings = DisableVersionControlSettings();

        if (changedManifest || changedSettings)
        {
            AssetDatabase.Refresh();
            Debug.Log("[Project] Unity Version Control 已被自动关闭，并移除 collab-proxy 依赖。");
        }

        ScheduleNextCheck();
    }

    private static void ScheduleNextCheck()
    {
        nextCheckTime = EditorApplication.timeSinceStartup + CheckIntervalSeconds;
    }

    private static bool RemoveCollabProxyFromManifest()
    {
        if (!File.Exists(ManifestPath))
        {
            return false;
        }

        string original = File.ReadAllText(ManifestPath);
        string updated = original;

        updated = RemoveJsonStringEntry(updated, CollabPackageName);

        if (updated == original)
        {
            return false;
        }

        File.WriteAllText(ManifestPath, updated);
        return true;
    }

    private static bool DisableVersionControlSettings()
    {
        if (!File.Exists(VersionControlSettingsPath))
        {
            return false;
        }

        string original = File.ReadAllText(VersionControlSettingsPath);
        string updated = original.Replace("inProgressEnabled: 1", "inProgressEnabled: 0");

        if (updated == original)
        {
            return false;
        }

        File.WriteAllText(VersionControlSettingsPath, updated);
        return true;
    }

    private static string RemoveJsonStringEntry(string json, string key)
    {
        string keyToken = "\"" + key + "\"";
        int keyIndex = json.IndexOf(keyToken);
        if (keyIndex < 0)
        {
            return json;
        }

        int lineStart = json.LastIndexOf('\n', keyIndex);
        lineStart = lineStart >= 0 ? lineStart + 1 : 0;

        int colonIndex = json.IndexOf(':', keyIndex);
        if (colonIndex < 0)
        {
            return json;
        }

        int valueStart = colonIndex + 1;
        while (valueStart < json.Length && char.IsWhiteSpace(json[valueStart]))
        {
            valueStart++;
        }

        if (valueStart >= json.Length || json[valueStart] != '"')
        {
            return json;
        }

        int valueEnd = FindStringEnd(json, valueStart);
        if (valueEnd < 0)
        {
            return json;
        }

        int trailingIndex = valueEnd + 1;
        while (trailingIndex < json.Length && (json[trailingIndex] == ' ' || json[trailingIndex] == '\t'))
        {
            trailingIndex++;
        }

        if (trailingIndex < json.Length && json[trailingIndex] == ',')
        {
            int removeEnd = trailingIndex + 1;
            while (removeEnd < json.Length && (json[removeEnd] == '\r' || json[removeEnd] == '\n'))
            {
                removeEnd++;
            }

            return json.Remove(lineStart, removeEnd - lineStart);
        }

        int previousComma = json.LastIndexOf(',', lineStart);
        if (previousComma >= 0)
        {
            return json.Remove(previousComma, trailingIndex - previousComma);
        }

        return json.Remove(lineStart, trailingIndex - lineStart);
    }

    private static int FindStringEnd(string text, int startQuoteIndex)
    {
        bool escaped = false;
        for (int i = startQuoteIndex + 1; i < text.Length; i++)
        {
            char current = text[i];
            if (escaped)
            {
                escaped = false;
                continue;
            }

            if (current == '\\')
            {
                escaped = true;
                continue;
            }

            if (current == '"')
            {
                return i;
            }
        }

        return -1;
    }
}
