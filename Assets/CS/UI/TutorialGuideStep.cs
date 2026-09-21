using UnityEngine;

/// <summary>
/// 箭头朝向
/// </summary>
public enum ArrowDirection { Up, Down, Left, Right }

/// <summary>
/// 引导步骤的触发方式
/// </summary>
public enum GuideTriggerType
{
    ClickTarget,      // 点击目标元素
    ScrollWheel,      // 滚轮操作
    AutoDelay,        // 自动延时后进入下一步
    ClickAnywhere,    // 点击任意位置
    MapOpened,        // 等待地图打开
    MapClosed,        // 等待地图关闭
    NameplateClicked, // 等待名牌被点击
    AvatarClicked,    // 等待头像被点击
    JobConfirmed,     // 等待打工确认
}

/// <summary>
/// 步骤开始/结束时执行的动作
/// </summary>
public enum GuideAction
{
    None,
    OpenMap,
    CloseMap,
    OpenJobPanel,
    CloseJobPanel,
    ZoomMapToMin,
    ZoomMapToMax,
}

/// <summary>
/// 单个引导步骤的数据定义
/// 在 Inspector 中配置每一步的内容
/// </summary>
[System.Serializable]
public class TutorialGuideStep
{
    [Tooltip("调试用名称，仅方便在 Inspector 中识别")]
    public string stepName;

    [Tooltip("显示给玩家的提示文字")]
    [TextArea(2, 4)]
    public string tipMessage;

    [Header("箭头指向")]
    [Tooltip("是否显示箭头")]
    public bool showArrow = true;

    [Tooltip("箭头指向的目标 UI 元素（RectTransform）")]
    public RectTransform target;

    [Tooltip("箭头指向的场景物体（Transform），优先级低于 target")]
    public Transform sceneTarget;

    [Tooltip("箭头相对目标中心的偏移（屏幕像素）")]
    public Vector2 arrowOffset;

    [Tooltip("箭头朝向")]
    public ArrowDirection arrowDir = ArrowDirection.Down;

    [Header("高亮遮罩")]
    [Tooltip("是否在目标上挖洞高亮")]
    public bool useHighlight = true;

    [Tooltip("高亮区域的额外边距")]
    public Vector2 highlightPadding = new Vector2(10f, 10f);

    [Header("触发方式")]
    [Tooltip("此步骤期间是否屏蔽游戏点击（头像不进剧情、确认不执行打工）")]
    public bool blockGameInput = true;

    [Tooltip("玩家需要做什么才能进入下一步")]
    public GuideTriggerType triggerType = GuideTriggerType.ClickTarget;

    [Tooltip("自动延时秒数（仅 AutoDelay 模式使用）")]
    public float autoDelay = 3f;

    [Header("执行动作")]
    [Tooltip("步骤开始时执行的动作")]
    public GuideAction actionOnStart = GuideAction.None;

    [Tooltip("步骤完成时执行的动作")]
    public GuideAction actionOnComplete = GuideAction.None;
}
