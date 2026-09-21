using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 教学引导与打工系统的桥接。
/// 挂载在计划表 UI 上，监听打工确认事件。
/// </summary>
public class TutorialGuideJobBridge : MonoBehaviour
{
    public static TutorialGuideJobBridge Instance;

    /// <summary>
    /// 玩家确认打工计划时触发
    /// </summary>
    public static event System.Action OnJobConfirmed;

    [Tooltip("计划表的确认按钮")]
    public Button confirmJobButton;

    [Tooltip("打开计划表的按钮")]
    public Button openJobPanelButton;

    [Tooltip("计划表面板根节点")]
    public GameObject jobPanel;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        // 监听打工确认按钮
        if (confirmJobButton != null)
        {
            confirmJobButton.onClick.AddListener(NotifyJobConfirmed);
        }
    }

    /// <summary>
    /// 通知打工已确认
    /// </summary>
    private void NotifyJobConfirmed()
    {
        Debug.Log("[教学引导桥接] 打工确认事件");
        OnJobConfirmed?.Invoke();
    }

    /// <summary>
    /// 打开计划表面板
    /// </summary>
    public void OpenJobPanel()
    {
        if (openJobPanelButton != null)
        {
            openJobPanelButton.onClick.Invoke();
        }
        else if (jobPanel != null)
        {
            jobPanel.SetActive(true);
        }

        Debug.Log("[教学引导桥接] 打开计划表");
    }

    /// <summary>
    /// 关闭计划表面板
    /// </summary>
    public void CloseJobPanel()
    {
        if (jobPanel != null)
        {
            jobPanel.SetActive(false);
        }

        Debug.Log("[教学引导桥接] 关闭计划表");
    }
}
