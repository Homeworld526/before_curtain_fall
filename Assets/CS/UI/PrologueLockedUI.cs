using UnityEngine;

public class PrologueLockedUI : MonoBehaviour
{
    [Header("解锁条件配置")]
    [Tooltip("是否在序章结束后自动解锁")]
    public bool unlockOnPrologueEnd = true;

    [Tooltip("是否需要检查特定对话索引（0=序章，1=第一章）")]
    public bool checkDialogIndex = false;
    public int requiredDialogIndex = 1;

    [Tooltip("存档Key：用于持久化解锁状态")]
    public string unlockSaveKey = "PrologueCompleted";

    [Header("联动控制")]
    [Tooltip("同时控制的其他UI对象")]
    public GameObject[] linkedObjects;

    private NormalEndEvent normalEndEvent;
    private bool isUnlocked = false;

    private void Awake()
    {
        CheckInitialState();
    }

    private void OnEnable()
    {
        SubscribeToEvents();
    }

    private void OnDisable()
    {
        UnsubscribeFromEvents();
    }

    private void CheckInitialState()
    {
        isUnlocked = PlayerPrefs.GetInt(unlockSaveKey, 0) == 1;

        if (!isUnlocked)
        {
            HideUI();
        }
        else
        {
            ShowUI();
        }
    }

    private void SubscribeToEvents()
    {
        if (!unlockOnPrologueEnd) return;

        normalEndEvent = FindObjectOfType<NormalEndEvent>();
        if (normalEndEvent != null)
        {
            normalEndEvent.DialogEnded += OnPrologueEnded;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (normalEndEvent != null)
        {
            normalEndEvent.DialogEnded -= OnPrologueEnded;
        }
    }

    private void OnPrologueEnded()
    {
        if (checkDialogIndex)
        {
            int currentIndex = PlayerPrefs.GetInt("dialogIndex", 0);
            if (currentIndex >= requiredDialogIndex)
            {
                UnlockUI();
            }
        }
        else
        {
            UnlockUI();
        }
    }

    private void UnlockUI()
    {
        if (isUnlocked) return;

        isUnlocked = true;
        PlayerPrefs.SetInt(unlockSaveKey, 1);
        PlayerPrefs.Save();

        ShowUI();

        UnsubscribeFromEvents();
    }

    private void ShowUI()
    {
        gameObject.SetActive(true);

        foreach (var obj in linkedObjects)
        {
            if (obj != null)
            {
                obj.SetActive(true);
            }
        }
    }

    private void HideUI()
    {
        gameObject.SetActive(false);

        foreach (var obj in linkedObjects)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }
    }

    [ContextMenu("强制解锁（测试用）")]
    private void ForceUnlock()
    {
        UnlockUI();
        Debug.Log($"[PrologueLockedUI] {gameObject.name} 已强制解锁");
    }

    [ContextMenu("重置锁状态（测试用）")]
    private void ResetLock()
    {
        isUnlocked = false;
        PlayerPrefs.SetInt(unlockSaveKey, 0);
        PlayerPrefs.Save();
        HideUI();
        SubscribeToEvents();
        Debug.Log($"[PrologueLockedUI] {gameObject.name} 已重置锁状态");
    }
}