using System;
using UnityEngine;

/// <summary>
/// Game values that can be used as story requirements or rewards.
/// </summary>
public enum GameResourceType
{
    Money,
    Backbone,
    XiaLunAffection,
    ZhangHaixinAffection,
    ChenYushuAffection,
}

public static class GameResourceTypeExtensions
{
    public static bool IsAffection(this GameResourceType resourceType)
    {
        return resourceType == GameResourceType.XiaLunAffection ||
            resourceType == GameResourceType.ZhangHaixinAffection ||
            resourceType == GameResourceType.ChenYushuAffection;
    }

    public static string GetAffectionCharacterName(this GameResourceType resourceType)
    {
        switch (resourceType)
        {
            case GameResourceType.XiaLunAffection:
                return "夏伦";
            case GameResourceType.ZhangHaixinAffection:
                return "张海心";
            case GameResourceType.ChenYushuAffection:
                return "陈玉澍";
            default:
                throw new ArgumentOutOfRangeException(nameof(resourceType), resourceType,
                    "Only affection resources have a character name.");
        }
    }

    public static long GetCurrentValue(this GameResourceType resourceType)
    {
        switch (resourceType)
        {
            case GameResourceType.Money:
                if (JobManager.Instance == null)
                    throw new InvalidOperationException("读取金钱数值需要活动的 JobManager 实例。");
                return JobManager.Instance.currentMoney;
            case GameResourceType.Backbone:
                return BackboneSystem.CurrentValue;
            default:
                if (!resourceType.IsAffection())
                    throw new ArgumentOutOfRangeException(nameof(resourceType), resourceType,
                        "Unsupported game resource type.");
                if (AffectionManager.Instance == null)
                    throw new InvalidOperationException(
                        $"读取{resourceType}需要活动的 AffectionManager 实例。");

                string characterName = resourceType.GetAffectionCharacterName();
                if (!AffectionManager.Instance.HasAffectionCharacter(characterName))
                {
                    throw new InvalidOperationException(
                        $"AffectionManager 未注册{resourceType}对应的角色“{characterName}”。");
                }

                return AffectionManager.Instance.GetAffection(characterName);
        }
    }

    /// <summary>
    /// 调整资源值，并沿用各资源系统既有的 UI 刷新、事件与存档链路。
    /// </summary>
    public static long Adjust(this GameResourceType resourceType, long delta, string source)
    {
        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("资源数值变更必须提供来源。", nameof(source));

        switch (resourceType)
        {
            case GameResourceType.Money:
                if (JobManager.Instance == null)
                    throw new InvalidOperationException("调整金钱数值需要活动的 JobManager 实例。");

                checked
                {
                    _ = JobManager.Instance.currentMoney + delta;
                }
                JobManager.Instance.AddMoney(delta);
                Debug.Log($"[GameResource] {source}: 现金调整 {delta:+#;-#;0}，当前 {JobManager.Instance.currentMoney}。");
                return JobManager.Instance.currentMoney;

            case GameResourceType.Backbone:
                return BackboneSystem.Adjust(ToIntDelta(resourceType, delta), source);

            default:
                if (!resourceType.IsAffection())
                {
                    throw new ArgumentOutOfRangeException(nameof(resourceType), resourceType,
                        "Unsupported game resource type.");
                }

                if (AffectionManager.Instance == null)
                {
                    throw new InvalidOperationException(
                        $"调整{resourceType}需要活动的 AffectionManager 实例。");
                }

                string characterName = resourceType.GetAffectionCharacterName();
                if (!AffectionManager.Instance.HasAffectionCharacter(characterName))
                {
                    throw new InvalidOperationException(
                        $"AffectionManager 未注册{resourceType}对应的角色“{characterName}”。");
                }

                int affectionDelta = ToIntDelta(resourceType, delta);
                checked
                {
                    _ = AffectionManager.Instance.GetAffection(characterName) + affectionDelta;
                }
                AffectionManager.Instance.AddAffection(characterName, affectionDelta);
                long currentAffection = AffectionManager.Instance.GetAffection(characterName);
                Debug.Log($"[GameResource] {source}: {characterName}好感度调整 {delta:+#;-#;0}，当前 {currentAffection}。");
                return currentAffection;
        }
    }

    private static int ToIntDelta(GameResourceType resourceType, long delta)
    {
        if (delta < int.MinValue || delta > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(delta), delta,
                $"{resourceType}单次调整值必须位于 {int.MinValue} 到 {int.MaxValue} 之间。");
        }

        return (int)delta;
    }
}
