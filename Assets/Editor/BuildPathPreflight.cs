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
    private const string ReturnHomeStoryConfigPath = "Assets/Resources/ReturnHomeStories/ReturnHomeStoryConfig.asset";
    private const string EndingConfigPath = "Assets/Resources/Endings/EndingConfig.asset";
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
        ValidateEndingConfiguration(errors);
        ValidateReturnHomeStoryConfiguration(errors);
        ValidateChoirConfiguration(errors);
        ValidateJobChoiceConfiguration(errors);
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
        RequireComponent<EndingPresentation>(scene, scenePath, errors);
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

    private static void ValidateReturnHomeStoryConfiguration(List<string> errors)
    {
        ReturnHomeStoryConfig returnHomeStoryConfig =
            AssetDatabase.LoadAssetAtPath<ReturnHomeStoryConfig>(ReturnHomeStoryConfigPath);
        if (returnHomeStoryConfig == null)
        {
            errors.Add($"回家自动剧情配置不存在或类型错误：{ReturnHomeStoryConfigPath}");
            return;
        }

        try
        {
            returnHomeStoryConfig.ValidateEntries();
        }
        catch (InvalidOperationException exception)
        {
            errors.Add($"回家自动剧情配置无效：{exception.Message}");
        }

        foreach (string guid in AssetDatabase.FindAssets("t:StoryDotConfig"))
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            StoryDotConfig storyDotConfig = AssetDatabase.LoadAssetAtPath<StoryDotConfig>(assetPath);
            if (storyDotConfig == null || storyDotConfig.stories == null)
                continue;

            try
            {
                storyDotConfig.ValidateStoryEntries();
            }
            catch (InvalidOperationException exception)
            {
                errors.Add($"剧情点配置无效 {assetPath}：{exception.Message}");
            }
        }
    }

    private static void ValidateEndingConfiguration(List<string> errors)
    {
        EndingConfig endingConfig = AssetDatabase.LoadAssetAtPath<EndingConfig>(EndingConfigPath);
        if (endingConfig == null)
        {
            errors.Add($"结局配置不存在或类型错误：{EndingConfigPath}");
            return;
        }

        try
        {
            endingConfig.ValidateEntries();
        }
        catch (InvalidOperationException exception)
        {
            errors.Add($"结局配置无效：{exception.Message}");
            return;
        }

        SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            Scene mainScene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            DialogList dialogList = FindComponent<DialogList>(mainScene);
            if (dialogList == null)
            {
                errors.Add($"场景 {MainScenePath} 缺少 DialogList 组件，无法验证结局剧情表索引。");
                return;
            }
            if (dialogList.dialogs == null)
            {
                errors.Add($"场景 {MainScenePath} 的 DialogList 未绑定剧情表列表，无法验证结局剧情表索引。");
                return;
            }

            foreach (EndingConfig.Entry entry in endingConfig.entries)
            {
                if (entry.storyDialogIndex >= dialogList.dialogs.Count)
                {
                    errors.Add(
                        $"结局“{entry.id}”的剧情表索引 {entry.storyDialogIndex} 超出 DialogList.dialogs 范围。");
                }
            }
        }
        finally
        {
            EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
        }
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

    private static void ValidateJobChoiceConfiguration(List<string> errors)
    {
        if (!File.Exists(NormalJobsPath))
            return;

        string[] rows = File.ReadAllLines(NormalJobsPath);
        for (int rowIndex = 1; rowIndex < rows.Length; rowIndex++)
        {
            if (string.IsNullOrWhiteSpace(rows[rowIndex]))
                continue;

            string[] columns = rows[rowIndex].Split(',');
            if (columns.Length <= 16)
                continue;

            bool hasQuestion = !string.IsNullOrWhiteSpace(columns[16]);
            bool hasChoicePayload = false;
            for (int columnIndex = 17; columnIndex < columns.Length; columnIndex++)
            {
                if (!string.IsNullOrWhiteSpace(columns[columnIndex]))
                {
                    hasChoicePayload = true;
                    break;
                }
            }

            if (!hasQuestion)
            {
                if (hasChoicePayload)
                    errors.Add($"普通打工第 {rowIndex + 1} 行填写了打工选择效果，但未填写题目。");
                continue;
            }

            if (columns.Length < 23)
            {
                errors.Add($"普通打工第 {rowIndex + 1} 行配置了打工选择题，但列数不足 23 列。");
                continue;
            }

            if (string.IsNullOrWhiteSpace(columns[17]) || string.IsNullOrWhiteSpace(columns[20]))
                errors.Add($"普通打工第 {rowIndex + 1} 行的打工选择题缺少任一选项文本。");

            int[] numericColumns = { 18, 19, 21, 22 };
            foreach (int columnIndex in numericColumns)
            {
                if (!int.TryParse(columns[columnIndex], out _))
                    errors.Add($"普通打工第 {rowIndex + 1} 行的打工选择题第 {columnIndex + 1} 列必须是整数。");
            }
        }
    }

    private static string FormatErrors(IEnumerable<string> errors)
    {
        return "[BuildPathPreflight] 构建路径基础检查失败：\n- " + string.Join("\n- ", errors);
    }
}
