using System;
using UnityEngine;

/// <summary>
/// 骨气值的唯一运行时入口。实际数据保存在 DialogStatDataSO，
/// 因而自动进入既有 DialogStats 存档流程。
/// </summary>
public static class BackboneSystem
{
    public const string StatKey = "骨气";
    public const int InitialValue = 80;
    public const int MinimumValue = 0;
    public const int MaximumValue = 100;

    private static bool _newGameInitializationRequested;

    public static event Action<int, int> ValueChanged;

    public static int CurrentValue
    {
        get
        {
            DialogStatDataSO statData = RequireStatData();
            if (!statData.TryGetInt(StatKey, out int value))
            {
                throw new InvalidOperationException(
                    "骨气值尚未初始化。请在读取骨气值前完成新游戏或读档初始化。");
            }

            return value;
        }
    }

    public static bool IsAtLeast(int value)
    {
        return CurrentValue >= value;
    }

    public static bool IsBelow(int value)
    {
        return CurrentValue < value;
    }

    /// <summary>
    /// 标记下一次场景初始化为新游戏，避免 ScriptableObject 在场景切换后保留上局数值。
    /// </summary>
    public static void BeginNewGame()
    {
        _newGameInitializationRequested = true;
    }

    /// <summary>
    /// 在 DialogList 完成初始化时调用。新游戏固定重置为 80；
    /// 没有骨气条目的旧数据按迁移规则补为 80。
    /// </summary>
    public static void EnsureInitializedForActiveScene()
    {
        DialogStatDataSO statData = RequireStatData();

        if (_newGameInitializationRequested)
        {
            _newGameInitializationRequested = false;
            SetValue(statData, InitialValue, "新游戏初始化");
            return;
        }

        EnsureStoredValue(statData, "场景初始化");
    }

    /// <summary>
    /// 在 SaveSystem 将 DialogStats 还原后调用。旧存档没有骨气条目时，
    /// 按版本迁移规则补为初始值，而非沿用场景中的临时数值。
    /// </summary>
    public static void EnsureInitializedAfterLoad()
    {
        _newGameInitializationRequested = false;
        EnsureStoredValue(RequireStatData(), "读档迁移");
    }

    public static int Adjust(int delta, string source)
    {
        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("骨气值变更必须提供来源。", nameof(source));

        DialogStatDataSO statData = RequireStatData();
        EnsureStoredValue(statData, "变更前初始化");

        int previousValue = statData.GetInt(StatKey);
        int nextValue = Clamp(previousValue + delta);
        if (nextValue == previousValue)
            return nextValue;

        statData.SetInt(StatKey, nextValue);
        Debug.Log($"[BackboneSystem] {source}: {previousValue} -> {nextValue} (delta={delta})");
        ValueChanged?.Invoke(previousValue, nextValue);
        return nextValue;
    }

    /// <summary>
    /// 场景切换时只释放订阅者，不重置数值或新游戏请求。
    /// 数值生命周期由新游戏、读档和 DialogStatDataSO 管理。
    /// </summary>
    public static void ResetRuntimeState()
    {
        ValueChanged = null;
    }

    private static void EnsureStoredValue(DialogStatDataSO statData, string reason)
    {
        if (!statData.TryGetInt(StatKey, out int storedValue))
        {
            SetValue(statData, InitialValue, reason + "（缺少条目）");
            return;
        }

        int clampedValue = Clamp(storedValue);
        if (clampedValue != storedValue)
            SetValue(statData, clampedValue, reason + "（范围修正）");
    }

    private static void SetValue(DialogStatDataSO statData, int value, string reason)
    {
        int nextValue = Clamp(value);
        int previousValue = statData.TryGetInt(StatKey, out int existingValue)
            ? existingValue
            : InitialValue;

        statData.SetInt(StatKey, nextValue);
        Debug.Log($"[BackboneSystem] {reason}: {previousValue} -> {nextValue}");
        ValueChanged?.Invoke(previousValue, nextValue);
    }

    private static int Clamp(int value)
    {
        return Mathf.Clamp(value, MinimumValue, MaximumValue);
    }

    private static DialogStatDataSO RequireStatData()
    {
        if (DialogList.Instance == null)
            throw new InvalidOperationException("骨气值系统需要活动的 DialogList 实例。");

        if (DialogList.Instance.statData == null)
            throw new InvalidOperationException("DialogList 缺少 DialogStatDataSO 引用，无法使用骨气值系统。");

        return DialogList.Instance.statData;
    }
}
