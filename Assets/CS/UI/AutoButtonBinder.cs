using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class AutoButtonBinder : MonoBehaviour
{
    private Dictionary<string, UnityAction> _eventMap;
    public string startGameScene = "SampleScene";

    [Header("音效设置")]
    public bool enableClickSound = true;

    [Header("悬浮效果设置")]
    public float hoverScale = 1.2f;
    public float normalScale = 1f;
    public float smoothSpeed = 12f;

    [Header("淡入设置")]
    public float fadeInDuration = 1.5f;
    public float delayBetweenButtons = 0.2f;
    public bool hideButtonsAtStart = true;
    public bool sequentialFadeIn = true;

    private Transform _currentButim;
    private Transform _currentEff;
    private bool _isHovering = false;
    private List<CanvasGroup> _buttonCanvasGroups = new List<CanvasGroup>();

    void Awake()
    {
        _eventMap = new Dictionary<string, UnityAction>
        {
            { "Btn_Start", OnStartGame },
            { "Btn_Setting", OnSetting },
            { "Btn_Gallery", OnGallery },
            { "Btn_Exit", OnExitGame },
            { "Btn_Load", OnLoad }
        };
        
        _buttonCanvasGroups.Clear();
    }
    
    void Start()
    {
        AutoAddButtonComponents();
        BindButtons();
        BindButtonHoverEvents();
        AddCanvasGroupsToButtons();
        
        if (hideButtonsAtStart)
        {
            StartCoroutine(FadeInButtonsCoroutine());
        }
    }

    void Update()
    {
        if (_currentButim != null)
        {
            float targetScale = _isHovering ? hoverScale : normalScale;
            _currentButim.localScale = Vector3.Lerp(_currentButim.localScale, Vector3.one * targetScale, smoothSpeed * Time.deltaTime);
        }

        if (_currentEff != null)
        {
            float targetScale = _isHovering ? hoverScale : normalScale;
            _currentEff.localScale = Vector3.Lerp(_currentEff.localScale, Vector3.one * targetScale, smoothSpeed * Time.deltaTime);
        }
    }

    void AutoAddButtonComponents()
    {
        foreach (string btnName in _eventMap.Keys)
        {
            Transform targetBtn = transform.Find(btnName);
            if (targetBtn != null)
            {
                if (targetBtn.GetComponent<Button>() == null)
                {
                    targetBtn.gameObject.AddComponent<Button>();
                    Debug.Log($"✅ 自动添加 Button 组件: [{btnName}]");
                }
                if (enableClickSound && targetBtn.GetComponent<UISoundPlayer>() == null)
                {
                    targetBtn.gameObject.AddComponent<UISoundPlayer>();
                }
            }
        }
    }

    void AddCanvasGroupsToButtons()
    {
        _buttonCanvasGroups.Clear();
        List<Transform> buttonTransforms = new List<Transform>();
        
        foreach (string btnName in _eventMap.Keys)
        {
            Transform targetBtn = transform.Find(btnName);
            if (targetBtn != null)
            {
                buttonTransforms.Add(targetBtn);
            }
        }
        
        buttonTransforms.Sort((a, b) => a.localPosition.x.CompareTo(b.localPosition.x));
        
        foreach (var btnTransform in buttonTransforms)
        {
            CanvasGroup cg = btnTransform.GetComponent<CanvasGroup>();
            if (cg == null)
            {
                cg = btnTransform.gameObject.AddComponent<CanvasGroup>();
            }
            _buttonCanvasGroups.Add(cg);
            
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
            
            Debug.Log($"✅ 为按钮 [{btnTransform.name}] 添加了 CanvasGroup");
        }
    }

    void BindButtons()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        int bindCount = 0;
        foreach (Button btn in buttons)
        {
            string btnName = btn.gameObject.name;
            if (_eventMap.ContainsKey(btnName))
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(_eventMap[btnName]);
                Debug.Log($"✅ 绑定成功: [{btnName}]");
                bindCount++;
            }
        }
        Debug.Log($"--- 自动绑定完成，共处理 {bindCount} 个按钮 ---");
    }

    void BindButtonHoverEvents()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button btn in buttons)
        {
            EventTrigger trigger = btn.GetComponent<EventTrigger>();
            if (trigger == null) trigger = btn.gameObject.AddComponent<EventTrigger>();

            EventTrigger.Entry enter = new EventTrigger.Entry();
            enter.eventID = EventTriggerType.PointerEnter;
            enter.callback.AddListener((eventData) =>
            {
                _currentButim = btn.transform.Find("butim");
                _currentEff = btn.transform.Find("Eff");
                _isHovering = true;

                if (_currentEff != null) _currentEff.gameObject.SetActive(true);
                if (enableClickSound && SoundsManager.Instance != null)
                    SoundsManager.Instance.PlayHoverSfx();
            });
            trigger.triggers.Add(enter);

            EventTrigger.Entry exit = new EventTrigger.Entry();
            exit.eventID = EventTriggerType.PointerExit;
            exit.callback.AddListener((eventData) =>
            {
                _isHovering = false;

                if (_currentEff != null) _currentEff.gameObject.SetActive(false);
            });
            trigger.triggers.Add(exit);
        }
        Debug.Log("✅ 所有按钮鼠标悬浮事件绑定完成");
    }

    IEnumerator FadeInButtonsCoroutine()
    {
        if (sequentialFadeIn)
        {
            for (int i = 0; i < _buttonCanvasGroups.Count; i++)
            {
                if (i > 0)
                {
                    yield return new WaitForSeconds(delayBetweenButtons);
                }
                
                CanvasGroup cg = _buttonCanvasGroups[i];
                yield return StartCoroutine(FadeInSingleButton(cg));
            }
        }
        else
        {
            foreach (var cg in _buttonCanvasGroups)
            {
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }
            
            float elapsedTime = 0f;
            while (elapsedTime < fadeInDuration)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Clamp01(elapsedTime / fadeInDuration);
                foreach (var cg in _buttonCanvasGroups)
                {
                    cg.alpha = alpha;
                }
                yield return null;
            }
            
            foreach (var cg in _buttonCanvasGroups)
            {
                cg.alpha = 1f;
            }
        }
        
        Debug.Log("按钮淡入完成！");
    }

    IEnumerator FadeInSingleButton(CanvasGroup cg)
    {
        cg.interactable = true;
        cg.blocksRaycasts = true;
        
        float elapsedTime = 0f;
        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            cg.alpha = Mathf.Clamp01(elapsedTime / fadeInDuration);
            yield return null;
        }
        
        cg.alpha = 1f;
    }

    public GameObject gallery;

    [SerializeField] private ReadDialogHistorySO history;
    
    void OnStartGame()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.SetInt("DialogIndex", 0);
        PlayerPrefs.SetInt("DialogLine", 0);
        
        // 统一清理所有静态状态
        StaticStateResetManager.ResetAllStaticState();
        
        DialogKeywordDetector.ClearAllUnlockedKeywords();
        TutorialGuideManager.ResetForNewGame();
        // 重置 DemoProgressTracker 的剧情完成进度（使用 PlayerPrefs 存储，必须显式清除）
        DemoProgressTracker.Instance?.ResetAllProgress();
        PlayerPrefs.Save();
        SaveSystem.Instance?.ClearCurrentSaveData();
        // 使用静态方法清空，确保即使 Instance 为 null 也能清除静态 _completedStories
        StoryDotManager.LoadCompletedStoryIdsStatic(null);
        // 重置序章静态标志，避免新游戏因上局 isPrologueCompleted=true 跳过 CompletePrologue
        PrologueUIManager.ResetPrologueCompleted();
        ShowDialog.Instance?.ResetForNewGame();
        // 确保新游戏不会残留旧的对话加载抑制状态
        DialogList.SuppressAutoDialog = false;
        SceneManager.sceneLoaded += SceneCenter.Instance.ClearAfterLoad;
        // 使用 SceneCenter 切换场景，确保静态状态被清理
        SceneCenter.Instance.ChangeScene(startGameScene);
        history?.ClearHistory();
    }

    void OnSetting() => Debug.Log("⚙️ 打开设置");
    void OnGallery()
    {
        Debug.Log("🎬 制作人员");
        gallery.SetActive(true);
    }

    void OnExitGame()
    {
        Debug.Log("👋 退出游戏");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    void OnLoad() => Debug.Log("🔙 返回");
}