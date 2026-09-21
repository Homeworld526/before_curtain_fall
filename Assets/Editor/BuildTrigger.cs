using UnityEditor;
using UnityEngine;
using System.IO;

[InitializeOnLoad]
public static class BuildTrigger
{
    static readonly string TriggerFile = Path.Combine(
        Path.GetDirectoryName(Application.dataPath), "build", "trigger.lock");

    static double lastCheck = 0;

    static BuildTrigger()
    {
        EditorApplication.update += Poll;
    }

    static void Poll()
    {
        if (EditorApplication.timeSinceStartup - lastCheck < 2.0)
            return;
        lastCheck = EditorApplication.timeSinceStartup;

        if (!File.Exists(TriggerFile))
            return;

        string buildPath = File.ReadAllText(TriggerFile).Trim();
        File.Delete(TriggerFile);

        Debug.Log($"[BuildTrigger] Trigger detected, building to: {buildPath}");
        BuildScript.BuildToPath(buildPath);
    }
}
