using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 验证当前构建路径依赖的场景、脚本和骨气系统的基础配置。
/// 资源迁移后若出现失效脚本或关键配置被删除，应在构建前明确报错。
/// </summary>
public sealed class BuildPathPreflight : IPreprocessBuildWithReport
{
    private const string StartScenePath = "Assets/Scenes/Start.unity";
    private const string MainScenePath = "Assets/Scenes/SampleScene.unity";
    private const string DialogStatDataPath = "Assets/Systems/Dialog/DialogStatData.asset";
    private const string NormalJobsPath = "Assets/Resources/SchemaTable/Configs/NormalJobs.csv";
    private static readonly Regex GuidReference = new Regex(
        @"guid: ([0-9a-f]{32})",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public int callbackOrder => -100;

    public void OnPreprocessBuild(BuildReport report)
    {
        ValidateOrThrow();
    }

    [MenuItem("Tools/Validation/Validate Build Path")]
    public static void ValidateFromMenu()
    {
        List<string> errors = CollectErrors();
        if (errors.Count == 0)
        {
            Debug.Log("[BuildPathPreflight] 构建路径基础检查通过。");
            return;
        }

        Debug.LogError(FormatErrors(errors));
    }

    public static void ValidateOrThrow()
    {
        List<string> errors = CollectErrors();
        if (errors.Count > 0)
            throw new BuildFailedException(FormatErrors(errors));
    }

    private static List<string> CollectErrors()
    {
        var errors = new List<string>();
        string[] buildScenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        RequireBuildScene(buildScenes, StartScenePath, errors);
        RequireBuildScene(buildScenes, MainScenePath, errors);
        ValidateSceneScripts(buildScenes, errors);
        ValidateSerializedReferences(buildScenes, errors);
        ValidateDialogStatData(errors);
        ValidateChoirConfiguration(errors);
        return errors;
    }

    private static void RequireBuildScene(IEnumerable<string> buildScenes, string requiredPath, List<string> errors)
    {
        if (!buildScenes.Contains(requiredPath))
            errors.Add($"构建设置缺少必需场景：{requiredPath}");
    }

    private static void ValidateSceneScripts(IEnumerable<string> buildScenes, List<string> errors)
    {
        SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();

        try
        {
            foreach (string scenePath in buildScenes)
            {
                if (!File.Exists(scenePath))
                {
                    errors.Add($"构建场景文件不存在：{scenePath}");
                    continue;
                }

                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                    {
                        int missingScriptCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject);
                        if (missingScriptCount > 0)
                        {
                            errors.Add(
                                $"场景 {scenePath} 的对象 {transform.name} 含 {missingScriptCount} 个 Missing Script 组件。");
                        }
                    }
                }

                ValidateRequiredComponents(scene, scenePath, errors);
            }
        }
        finally
        {
            EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
        }
    }

    private static void ValidateRequiredComponents(Scene scene, string scenePath, List<string> errors)
    {
        if (scenePath == StartScenePath)
        {
            RequireComponent<SceneCenter>(scene, scenePath, errors);
            RequireComponent<StatEventCenter>(scene, scenePath, errors);
            return;
        }

        if (scenePath != MainScenePath)
            return;

        RequireComponent<JobManager>(scene, scenePath, errors);
        RequireComponent<ConfigManager>(scene, scenePath, errors);
        RequireComponent<PrayLuck>(scene, scenePath, errors);
        RequireComponent<SaveSystem>(scene, scenePath, errors);
        DialogList dialogList = FindComponent<DialogList>(scene);
        if (dialogList == null)
        {
            errors.Add($"场景 {scenePath} 缺少 DialogList 组件。");
        }
        else if (dialogList.statData == null)
        {
            errors.Add($"场景 {scenePath} 的 DialogList 未绑定 DialogStatDataSO。");
        }
    }

    private static void RequireComponent<T>(Scene scene, string scenePath, List<string> errors)
        where T : Component
    {
        if (FindComponent<T>(scene) == null)
            errors.Add($"场景 {scenePath} 缺少 {typeof(T).Name} 组件。");
    }

    private static T FindComponent<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>(true);
            if (component != null)
                return component;
        }

        return null;
    }

    private static void ValidateSerializedReferences(IEnumerable<string> buildScenes, List<string> errors)
    {
        var checkedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var missingReferences = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string scenePath in buildScenes)
        {
            foreach (string assetPath in AssetDatabase.GetDependencies(scenePath, true))
            {
                if (!checkedPaths.Add(assetPath) ||
                    !assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) ||
                    !IsTextSerializedAsset(assetPath) ||
                    !File.Exists(assetPath))
                    continue;

                foreach (Match match in GuidReference.Matches(File.ReadAllText(assetPath)))
                {
                    string guid = match.Groups[1].Value;
                    if (guid == "00000000000000000000000000000000")
                        continue;

                    if (string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(guid)))
                        missingReferences.Add($"{assetPath} -> {guid}");
                }
            }
        }

        foreach (string missingReference in missingReferences.OrderBy(reference => reference, StringComparer.Ordinal))
            errors.Add($"构建路径存在无法解析的资源引用：{missingReference}");
    }

    private static bool IsTextSerializedAsset(string assetPath)
    {
        switch (Path.GetExtension(assetPath).ToLowerInvariant())
        {
            case ".asset":
            case ".controller":
            case ".mat":
            case ".playable":
            case ".prefab":
            case ".unity":
                return true;
            default:
                return false;
        }
    }

    private static void ValidateDialogStatData(List<string> errors)
    {
        DialogStatDataSO statData = AssetDatabase.LoadAssetAtPath<DialogStatDataSO>(DialogStatDataPath);
        if (statData == null)
            errors.Add($"骨气值存储资产不存在或类型错误：{DialogStatDataPath}");
    }

    private static void ValidateChoirConfiguration(List<string> errors)
    {
        if (!File.Exists(NormalJobsPath))
        {
            errors.Add($"普通打工配置不存在：{NormalJobsPath}");
            return;
        }

        string[] rows = File.ReadAllLines(NormalJobsPath);
        string choirRow = rows.FirstOrDefault(row => row.StartsWith("唱诗班,", StringComparison.Ordinal));
        if (choirRow == null)
        {
            errors.Add("普通打工配置缺少“唱诗班”条目。");
            return;
        }

        string[] columns = choirRow.Split(',');
        if (columns.Length < 16)
        {
            errors.Add($"唱诗班配置列数不足：需要至少 16 列，实际为 {columns.Length} 列。");
            return;
        }

        if (columns[2] != "SundayOnly")
            errors.Add($"唱诗班必须为 SundayOnly，当前值为“{columns[2]}”。");

        if (!int.TryParse(columns[7], out int backboneValue) || backboneValue != 2)
            errors.Add($"唱诗班的 backbone 必须为 2，当前值为“{columns[7]}”。");
    }

    private static string FormatErrors(IEnumerable<string> errors)
    {
        return "[BuildPathPreflight] 构建路径基础检查失败：\n- " + string.Join("\n- ", errors);
    }
}
