using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 静态状态统一管理器
/// 用于在存读档时统一清理所有模块的静态变量
/// 解决多次存读档后静态变量指向已销毁对象的问题
/// </summary>
public static class StaticStateResetManager
{
    /// <summary>
    /// 重置所有静态状态（在读档前调用）
    /// </summary>
    public static void ResetAllStaticState()
    {
        Debug.Log("[StaticStateResetManager] 开始重置所有静态状态...");

        // UI 模块
        DialogTrigger.ResetStaticState();
        TutorialGuideManager.ResetStaticState();
        TutorialStepRevealer.ResetStaticState();
        TutorialManager.ResetStaticState();
        
        // 打工模块
        JobManager.ResetStaticState();
        CharacterJobListItem.ResetStaticState();
        JobLogItem.ResetStaticState();
        
        // 地图模块
        HoverZoom.ResetStaticState();
        MapNameplate.ResetStaticState();
        ShowMapButton.ResetStaticState();
        
        // 对话模块
        DialogList.ResetStaticState();
        DialogVisual.ResetStaticState();
        DialogKeywordDetector.ResetStaticState();
        
        // 好感度模块
        AffectionManager.ResetStaticState();

        // DontDestroyOnLoad 单例清空 Instance（确保场景切换后重新初始化）
        StoryDotManager.ResetStaticState();
        UIFadeManager.ResetStaticState();
        LocalizationManager.ResetStaticState();

        Debug.Log("[StaticStateResetManager] 所有静态状态已重置");
    }

    /// <summary>
    /// 注册新的静态状态清理方法
    /// 扩展点：新增模块时可以在这里添加清理调用
    /// </summary>
    /// <param name="resetAction">清理方法</param>
    public static void RegisterResetAction(System.Action resetAction)
    {
        // 可以扩展为动态注册机制
        resetAction?.Invoke();
    }
}