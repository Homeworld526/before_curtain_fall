using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class ShowDialog : MonoBehaviour, IPointerClickHandler
{
    public static ShowDialog Instance;

    public bool isDialogPlaying => DialogList.IsInDialog;

    public event Action<int> OnDialogStarted;

    [Header("UI配置")]
    public List<GameObject> OtherUI = new List<GameObject>();
    public List<GameObject> Buttons = new List<GameObject>();
    public GameObject dialog;

    [Header("序章控制")]
    [Tooltip("是否需要等待序章结束才恢复UI")]
    public bool waitForPrologueEnd = true;

    [Tooltip("序章结束关键词")]
    public string prologueEndKeyword = "还债吧";

    private NormalEndEvent normalEndEvent;
    private bool hasReachedEndKeyword = false;

    #region 单例初始化
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion

    private void OnDisable()
    {
        // 存档加载期间不重置 PlayerPrefs，否则会干扰存档恢复
        if (!DialogList.SuppressAutoDialog)
        {
            PlayerPrefs.SetInt("dialogIndex", 0);
            PlayerPrefs.SetInt("dialogLine", 0);
        }
    }

    void Start()
    {
        normalEndEvent = FindObjectOfType<NormalEndEvent>();
        if (normalEndEvent != null)
        {
            Debug.Log("订阅 DialogEnded 事件");
            normalEndEvent.DialogEnded += Hide;
        }

        var dialogVisual = FindObjectOfType<DialogVisual>();
        if (dialogVisual != null)
        {
            dialogVisual.ContentTracker += OnDialogContent;
        }

        // 存档加载期间不重置 PlayerPrefs，由 SaveSystem.ApplyPlayerData 负责恢复
        if (!DialogList.SuppressAutoDialog)
        {
            PlayerPrefs.SetInt("dialogIndex", 0);
            PlayerPrefs.SetInt("dialogLine", 0);
        }

        // 有存档时读存档；无存档（新游戏）默认隐藏，因为新游戏必定从对话开始
        var saveData = SaveSystem.Instance?.GetCurrentSaveData();
        bool inDialog = saveData != null ? saveData.PlayerData.isInDialog : true;
        foreach (var ui in OtherUI)
            if (ui != null) ui.SetActive(!inDialog);
    }

    private void OnDestroy()
    {
        // 清空静态变量，避免指向已销毁的对象
        if (Instance == this)
        {
            Instance = null;
        }
        
        if (normalEndEvent != null)
        {
            normalEndEvent.DialogEnded -= Hide;
        }

        var dialogVisual = FindObjectOfType<DialogVisual>();
        if (dialogVisual != null)
        {
            dialogVisual.ContentTracker -= OnDialogContent;
        }
    }

    private void OnDialogContent(DialogNode node)
    {
        if (node != null && !string.IsNullOrEmpty(node.content))
        {
            if (node.content.Contains(prologueEndKeyword))
            {
                hasReachedEndKeyword = true;
                Debug.Log("[ShowDialog] 检测到序章结束关键词");
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("点击了对话框");
        BlackoutTransition.Instance.FadeInOut(Show, 0.6f, 0.8f, 0f);
    }

    public void Show()
    {
        PlayerPrefs.SetInt("dialogIndex", 0);
        PlayerPrefs.SetInt("dialogLine", 8);
        if (SmallTalkManager.Instance != null)
            SmallTalkManager.Instance.RestoreMainDialogRoot();
        DialogLocator.Instance.Dialog.SetActive(true);

        foreach (var ui in OtherUI)
        {
            ui.SetActive(false);
        }
    }

    public void Show(int index, int line)
    {
        Debug.Log("启动对话" + index);

        PlayerPrefs.SetInt("dialogIndex", index);
        PlayerPrefs.SetInt("dialogLine", line);
        if (SmallTalkManager.Instance != null)
            SmallTalkManager.Instance.RestoreMainDialogRoot();
        DialogLocator.Instance.Dialog.SetActive(true);

        foreach (var ui in OtherUI)
        {
            ui.SetActive(false);
        }

        OnDialogStarted?.Invoke(index);
    }

    public void Hide()
    {
        if (waitForPrologueEnd)
        {
            if (!hasReachedEndKeyword)
            {
                Debug.Log("[ShowDialog] 序章未结束，不恢复UI");
                return;
            }
        }

        foreach (var ui in OtherUI)
        {
            ui.SetActive(true);
        }

        if (PlayerPrefs.GetInt("dialogLine") != 0 && PlayerPrefs.GetInt("dialogIndex") != 0)
        {
            foreach (var button in Buttons)
            {
                if (!button.activeInHierarchy)
                {
                    button.SetActive(true);
                }
            }
        }
    }

    public void Hide2()
    {
        foreach (var ui in OtherUI)
        {
            ui.SetActive(true);
        }

        foreach (var button in Buttons)
        {
            if (!button.activeInHierarchy)
            {
                button.SetActive(true);
            }
        }
    }

    public void HideImmediate()
    {
        foreach (var ui in OtherUI)
            if (ui != null) ui.SetActive(false);
    }

    [ContextMenu("重置序章状态")]
    public void ResetForNewGame()
    {
        hasReachedEndKeyword = false;
        foreach (var ui in OtherUI)
            if (ui != null) ui.SetActive(false);
    }

    public void SetPrologueCompleted()
    {
        hasReachedEndKeyword = true;
        Debug.Log("[ShowDialog] 已设置序章为完成状态");
    }

    [ContextMenu("重置序章状态(旧)")]
    private void ResetPrologueState()
    {
        hasReachedEndKeyword = false;
        Debug.Log("[ShowDialog] 已重置序章状态");
    }
}