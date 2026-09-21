using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;
using System.Linq;

public class BuildScript : IPreprocessBuildWithReport
{
    private static string GetDefaultBuildPath()
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        return Path.Combine(projectRoot, "Builds", "BeforeTheFall.exe");
    }

    private static void EnsureBuildDirectory(string buildPath)
    {
        string directory = Path.GetDirectoryName(buildPath);
        if (string.IsNullOrWhiteSpace(directory))
            throw new System.ArgumentException("Build path must include an output directory.", nameof(buildPath));

        Directory.CreateDirectory(directory);
    }

    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        string savePath = Path.Combine(Application.persistentDataPath, "CGUnlockData.json");
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
            Debug.Log("[Build] 已清空 CGUnlockData 解锁存档");
        }
    }

    [MenuItem("Build/Build Windows")]
    public static void BuildWindows()
    {
        // Keep the menu command portable: write under the current Unity project,
        // not to a developer-specific desktop path.
        string buildPath = GetDefaultBuildPath();
        EnsureBuildDirectory(buildPath);

        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        Debug.Log($"[Build] 打包 {scenes.Length} 个场景到: {buildPath}");
        foreach (var s in scenes)
            Debug.Log($"[Build]   - {s}");

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = buildPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);

        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            Debug.Log($"[Build] 打包成功! 大小: {report.summary.totalSize / 1024 / 1024}MB");
        else
            Debug.LogError($"[Build] 打包失败: {report.summary.result}");
    }

    public static void BuildWindowsCLI()
    {
        string buildPath = GetDefaultBuildPath();

        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "-buildPath")
            {
                buildPath = args[i + 1];
                break;
            }
        }

        EnsureBuildDirectory(buildPath);

        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        Debug.Log($"[Build] CLI 打包 {scenes.Length} 个场景到: {buildPath}");
        foreach (var s in scenes)
            Debug.Log($"[Build]   - {s}");

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = buildPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);

        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"[Build] 打包成功! 大小: {report.summary.totalSize / 1024 / 1024}MB");
            EditorApplication.Exit(0);
        }
        else
        {
            Debug.LogError($"[Build] 打包失败: {report.summary.result}");
            EditorApplication.Exit(1);
        }
    }

    public static void BuildToPath(string buildDir)
    {
        string resultDirectory = Path.Combine(
            Path.GetDirectoryName(Application.dataPath), "build");
        Directory.CreateDirectory(resultDirectory);
        string resultFile = Path.Combine(resultDirectory, "result.lock");

        string exeName = PlayerSettings.productName + ".exe";
        string buildPath = Path.Combine(buildDir, exeName);

        Directory.CreateDirectory(buildDir);

        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        Debug.Log($"[Build] Building {scenes.Length} scenes to: {buildPath}");

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = buildPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);

        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"[Build] Success! Size: {report.summary.totalSize / 1024 / 1024}MB");
            File.WriteAllText(resultFile, $"SUCCESS:{buildPath}");
        }
        else
        {
            Debug.LogError($"[Build] Failed: {report.summary.result}");
            File.WriteAllText(resultFile, $"FAILED:{report.summary.result}");
        }
    }
}
