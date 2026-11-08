using System;
using UnityEngine;

[CreateAssetMenu(fileName = "BackboneEndingConfig", menuName = "Backbone/Ending Config")]
public sealed class BackboneEndingConfig : ScriptableObject
{
    [Tooltip("关闭时不接管正常剧情结束流程。正式 BE 文本、CG 与对话索引配置完成后再开启。")]
    public bool enableZeroBackboneEnding;

    [Tooltip("归零 BE 最早可在此周的剧情结束并返回房间时触发。正式版应填写最终周。")]
    [Min(1)] public int zeroBackboneAvailableAtWeek = 13;

    [Tooltip("归零 BE 在 DialogList.dialogs 中的从 0 开始索引。-1 表示待配置。")]
    public int zeroBackboneEndingDialogIndex = -1;

    public void ValidateZeroBackboneEnding()
    {
        if (zeroBackboneAvailableAtWeek < 1)
            throw new InvalidOperationException("归零 BE 的最早触发周必须大于等于 1。");
        if (zeroBackboneEndingDialogIndex < 0)
            throw new InvalidOperationException("归零 BE 已启用，但未配置 DialogList 中的对话索引。");
    }
}

/// <summary>
/// 归零 BE 在最终剧情返回房间时接管对话。触发标记随 DialogStatDataSO 存档，避免重复进入。
/// </summary>
public static class BackboneEndingService
{
    private const string ResourcePath = "Backbone/BackboneEndingConfig";
    private const string ZeroBackboneEndingTriggeredKey = "骨气归零BE已触发";
    private static BackboneEndingConfig _config;

    public static void ResetForNewGame(DialogStatDataSO statData)
    {
        if (statData == null)
            throw new ArgumentNullException(nameof(statData));

        statData.SetInt(ZeroBackboneEndingTriggeredKey, 0);
    }

    public static bool TryStartZeroBackboneEnding()
    {
        BackboneEndingConfig config = GetConfig();
        if (!config.enableZeroBackboneEnding)
            return false;

        config.ValidateZeroBackboneEnding();
        if (BackboneSystem.CurrentValue != 0)
            return false;

        if (JobManager.Instance == null)
            throw new InvalidOperationException("归零 BE 判定需要活动的 JobManager 实例。");
        if (JobManager.Instance.currentWeek < config.zeroBackboneAvailableAtWeek)
            return false;

        if (DialogList.Instance == null || DialogList.Instance.statData == null)
            throw new InvalidOperationException("归零 BE 判定需要带 DialogStatDataSO 的 DialogList 实例。");
        if (DialogList.Instance.dialogs == null)
            throw new InvalidOperationException("归零 BE 判定需要 DialogList.dialogs 配置。");
        if (DialogList.Instance.statData.GetInt(ZeroBackboneEndingTriggeredKey) != 0)
            return false;
        if (config.zeroBackboneEndingDialogIndex >= DialogList.Instance.dialogs.Count)
        {
            throw new InvalidOperationException(
                $"归零 BE 对话索引 {config.zeroBackboneEndingDialogIndex} 超出 DialogList.dialogs 范围。");
        }

        DialogList.Instance.statData.SetInt(ZeroBackboneEndingTriggeredKey, 1);
        DialogList.Instance.InvokeDialog(config.zeroBackboneEndingDialogIndex);
        Debug.Log($"[BackboneEndingService] 骨气归零，启动 BE 对话索引 {config.zeroBackboneEndingDialogIndex}。");
        return true;
    }

    private static BackboneEndingConfig GetConfig()
    {
        if (_config != null)
            return _config;

        _config = Resources.Load<BackboneEndingConfig>(ResourcePath);
        if (_config == null)
        {
            throw new InvalidOperationException(
                $"缺少骨气结局配置 Resources/{ResourcePath}.asset。请恢复或创建该配置资产。");
        }

        return _config;
    }
}
