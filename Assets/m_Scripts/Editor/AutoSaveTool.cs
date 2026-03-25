using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_2021_2_OR_NEWER
using PrefabStageUtility = UnityEditor.SceneManagement.PrefabStageUtility;
#else
using PrefabStageUtility = UnityEditor.Experimental.SceneManagement.PrefabStageUtility;
#endif

public class AutoSaveToolWindow : EditorWindow
{
    [MenuItem("Tools/Auto Save/Settings")]
    private static void OpenWindow()
    {
        AutoSaveToolWindow window = GetWindow<AutoSaveToolWindow>("Auto Save");
        window.minSize = new Vector2(360f, 220f);
    }

    [MenuItem("Tools/Auto Save/Save Now")]
    private static void SaveNow()
    {
        AutoSaveTool.SaveNow("手动立即保存");
    }

    [MenuItem("Tools/Auto Save/Toggle")]
    private static void ToggleAutoSave()
    {
        AutoSaveTool.Enabled = !AutoSaveTool.Enabled;
    }

    private void OnGUI()
    {
        GUILayout.Label("自动保存", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Unity 没有一个稳定通用的项目级自动保存。这个工具会在编辑器里定时保存打开的场景、Prefab Mode 和资源。", MessageType.Info);

        EditorGUI.BeginChangeCheck();

        bool enabled = EditorGUILayout.Toggle("启用自动保存", AutoSaveTool.Enabled);
        float intervalMinutes = EditorGUILayout.FloatField("保存间隔(分钟)", AutoSaveTool.IntervalMinutes);
        bool saveScenes = EditorGUILayout.Toggle("保存打开场景", AutoSaveTool.SaveOpenScenes);
        bool savePrefabStage = EditorGUILayout.Toggle("保存 Prefab Mode", AutoSaveTool.SavePrefabStage);
        bool saveAssets = EditorGUILayout.Toggle("保存资源文件", AutoSaveTool.SaveAssets);
        bool logSaves = EditorGUILayout.Toggle("输出保存日志", AutoSaveTool.LogSaves);

        if (EditorGUI.EndChangeCheck())
        {
            AutoSaveTool.Enabled = enabled;
            AutoSaveTool.IntervalMinutes = Mathf.Max(0.1f, intervalMinutes);
            AutoSaveTool.SaveOpenScenes = saveScenes;
            AutoSaveTool.SavePrefabStage = savePrefabStage;
            AutoSaveTool.SaveAssets = saveAssets;
            AutoSaveTool.LogSaves = logSaves;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("当前状态", AutoSaveTool.Enabled ? "运行中" : "已关闭");
        EditorGUILayout.LabelField("下次保存", AutoSaveTool.GetNextSaveDescription());

        EditorGUILayout.Space();
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("立即保存"))
            {
                AutoSaveTool.SaveNow("窗口手动保存");
            }

            if (GUILayout.Button(AutoSaveTool.Enabled ? "关闭自动保存" : "开启自动保存"))
            {
                AutoSaveTool.Enabled = !AutoSaveTool.Enabled;
            }
        }
    }
}

public class AutoSaveStatusWindow : EditorWindow
{
    [MenuItem("Tools/Auto Save/Monitor")]
    private static void OpenWindow()
    {
        AutoSaveStatusWindow window = GetWindow<AutoSaveStatusWindow>("Auto Save");
        window.minSize = new Vector2(220f, 60f);
    }

    private void OnEnable()
    {
        EditorApplication.update -= Repaint;
        EditorApplication.update += Repaint;
        UpdateTitle();
    }

    private void OnDisable()
    {
        EditorApplication.update -= Repaint;
    }

    private void OnGUI()
    {
        UpdateTitle();

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("自动保存状态", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("状态", AutoSaveTool.Enabled ? "运行中" : "已关闭");
            EditorGUILayout.LabelField("距离下次保存", AutoSaveTool.GetNextSaveDescription());

            EditorGUILayout.Space(2f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("立即保存", GUILayout.Height(22f)))
                {
                    AutoSaveTool.SaveNow("监视窗口手动保存");
                }

                if (GUILayout.Button(AutoSaveTool.Enabled ? "关闭" : "开启", GUILayout.Height(22f)))
                {
                    AutoSaveTool.Enabled = !AutoSaveTool.Enabled;
                    UpdateTitle();
                }
            }
        }
    }

    private void UpdateTitle()
    {
        titleContent = new GUIContent("AutoSave " + AutoSaveTool.GetShortStatusDescription());
    }
}

[InitializeOnLoad]
public static class AutoSaveTool
{
    private const string EnabledKey = "VRMicroscope.AutoSave.Enabled";
    private const string IntervalMinutesKey = "VRMicroscope.AutoSave.IntervalMinutes";
    private const string SaveScenesKey = "VRMicroscope.AutoSave.SaveScenes";
    private const string SavePrefabStageKey = "VRMicroscope.AutoSave.SavePrefabStage";
    private const string SaveAssetsKey = "VRMicroscope.AutoSave.SaveAssets";
    private const string LogSavesKey = "VRMicroscope.AutoSave.LogSaves";

    private static double nextSaveTime;

    public static bool Enabled
    {
        get => EditorPrefs.GetBool(EnabledKey, true);
        set
        {
            EditorPrefs.SetBool(EnabledKey, value);
            ScheduleNextSave();
        }
    }

    public static float IntervalMinutes
    {
        get => EditorPrefs.GetFloat(IntervalMinutesKey, 3f);
        set
        {
            EditorPrefs.SetFloat(IntervalMinutesKey, Mathf.Max(0.1f, value));
            ScheduleNextSave();
        }
    }

    public static bool SaveOpenScenes
    {
        get => EditorPrefs.GetBool(SaveScenesKey, true);
        set => EditorPrefs.SetBool(SaveScenesKey, value);
    }

    public static bool SavePrefabStage
    {
        get => EditorPrefs.GetBool(SavePrefabStageKey, true);
        set => EditorPrefs.SetBool(SavePrefabStageKey, value);
    }

    public static bool SaveAssets
    {
        get => EditorPrefs.GetBool(SaveAssetsKey, true);
        set => EditorPrefs.SetBool(SaveAssetsKey, value);
    }

    public static bool LogSaves
    {
        get => EditorPrefs.GetBool(LogSavesKey, false);
        set => EditorPrefs.SetBool(LogSavesKey, value);
    }

    static AutoSaveTool()
    {
        ScheduleNextSave();
        EditorApplication.update -= Update;
        EditorApplication.update += Update;
    }

    public static void SaveNow(string reason)
    {
        if (!CanAutoSave())
        {
            ScheduleNextSave();
            return;
        }

        bool savedAnything = false;

        if (SavePrefabStage && TrySaveCurrentPrefabStage())
        {
            savedAnything = true;
        }

        if (SaveOpenScenes && HasDirtyOpenScenes())
        {
            EditorSceneManager.SaveOpenScenes();
            savedAnything = true;
        }

        if (SaveAssets)
        {
            AssetDatabase.SaveAssets();
            savedAnything = true;
        }

        if (savedAnything && LogSaves)
        {
            Debug.Log("[AutoSave] 已自动保存场景/资源。原因: " + reason);
        }

        ScheduleNextSave();
    }

    public static string GetNextSaveDescription()
    {
        if (!Enabled)
        {
            return "未启用";
        }

        double remainingSeconds = nextSaveTime - EditorApplication.timeSinceStartup;
        if (remainingSeconds <= 0d)
        {
            return "即将保存";
        }

        int totalSeconds = Mathf.CeilToInt((float)remainingSeconds);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public static string GetShortStatusDescription()
    {
        return Enabled ? GetNextSaveDescription() : "Off";
    }

    private static void Update()
    {
        if (!Enabled)
        {
            return;
        }

        if (EditorApplication.timeSinceStartup >= nextSaveTime)
        {
            SaveNow("定时自动保存");
        }
    }

    private static void ScheduleNextSave()
    {
        nextSaveTime = EditorApplication.timeSinceStartup + IntervalMinutes * 60f;
    }

    private static bool CanAutoSave()
    {
        return Enabled &&
               !EditorApplication.isCompiling &&
               !EditorApplication.isUpdating &&
               !EditorApplication.isPlayingOrWillChangePlaymode &&
               !BuildPipeline.isBuildingPlayer;
    }

    private static bool HasDirtyOpenScenes()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.IsValid() && scene.isDirty)
            {
                return true;
            }
        }

        return false;
    }

    private static bool TrySaveCurrentPrefabStage()
    {
        var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        if (prefabStage == null || !prefabStage.scene.IsValid() || !prefabStage.scene.isDirty)
        {
            return false;
        }

        MethodInfo saveMethod = typeof(PrefabStageUtility).GetMethod(
            "SaveCurrentPrefabStage",
            BindingFlags.Public | BindingFlags.Static,
            null,
            Type.EmptyTypes,
            null);

        if (saveMethod != null)
        {
            object result = saveMethod.Invoke(null, null);
            return result is bool saved ? saved : true;
        }

        if (prefabStage.prefabContentsRoot != null)
        {
            PrefabUtility.SavePrefabAsset(prefabStage.prefabContentsRoot);
            return true;
        }

        return false;
    }
}
