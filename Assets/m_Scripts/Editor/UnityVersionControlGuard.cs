using System.IO;
using UnityEditor;
using UnityEngine;

public static class UnityVersionControlGuard
{
    private const string CollabPackageName = "com.unity.collab-proxy";
    private const string ManifestPath = "Packages/manifest.json";
    private const string VersionControlSettingsPath = "ProjectSettings/VersionControlSettings.asset";

    [MenuItem("Tools/Project/Check Unity Version Control Status")]
    private static void CheckStatus()
    {
        bool packagePresent = File.Exists(ManifestPath) &&
                              File.ReadAllText(ManifestPath).Contains($"\"{CollabPackageName}\"");
        bool collabEnabled = File.Exists(VersionControlSettingsPath) &&
                             File.ReadAllText(VersionControlSettingsPath).Contains("inProgressEnabled: 1");

        if (!packagePresent && !collabEnabled)
        {
            Debug.Log("[Project] Unity Version Control is disabled. No startup changes are required.");
            return;
        }

        Debug.LogWarning(
            "[Project] Unity Version Control configuration is active. Edit Packages/manifest.json and " +
            "ProjectSettings/VersionControlSettings.asset while Unity is closed. " +
            "The project no longer changes package files automatically during assembly reload.");
    }
}
