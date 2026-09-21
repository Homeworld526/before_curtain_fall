using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GameSystem;
using System.Collections;
using TMPro;
using UnityEngine.EventSystems;

public class JobManager : MonoBehaviour
{
    #region 单例
    public static JobManager Instance { get; private set; }
    #endregion

    #region 工作数据结构
    public Dictionary<string, JobData> _allJobs = new Dictionary<string, JobData>();
    public Dictionary<string, JobData> _allcharJobs = new Dictionary<string, JobData>();
    private List<CGInfo> actCG = new List<CGInfo>();
    public JobListManager JobListManager;
    
    private struct CGInfo
    {
        public string imageName;
        public string npcName;
        public bool isCharJob;
        public int randomIndex; // 随机索引（1-3）
    }
    public jobLogManager jobLogManager;
    private JobData[] actjobs = new JobData[21]; // 使用固定长度数组，null表示空位
    private List<string> levelUpJobs = new List<string>();
    private List<string> performedCharJobs = new List<string>(); // 记录本周执行的角色打工
    private JobData _lyingFlatJob; // 躺平打工数据缓存
    
    // 小图片图集缓存（合并两个图集到一个字典）
    private static Dictionary<string, Sprite> _smallSpriteDict;
    
    // 当前选中的小图片名称
    private string currentLeftSpriteName = "";
    private string currentRightSpriteName = "";
    #endregion

    #region 时间系统
    public int currentWeek = 1;
    public const int MAX_WEEKS = 13;
    public DayOfWeek currentDay;
    public TimeSlot currentTimeSlot = TimeSlot.Morning;
    public int totalDays = 0;
    private int usedDaysInWeek = 0;
    private readonly System.DateTime startDate = new System.DateTime(2025, 11, 18);
    #endregion

    #region 核心数值
    public long totalDebt = 300000;
    public long currentMoney = 3000;
    public int totalApUsed = 0;
    public int totalChar;
    private int totalReward = 0;
    private LuckType settledLuck; // 结算时保存的运势（用于结算面板显示）

    /// <summary>
    /// 增加金钱
    /// </summary>
    /// <param name="amount">金额（正数增加，负数减少）</param>
    public void AddMoney(long amount)
    {
        currentMoney += amount;
        updateUI();
    }

    /// <summary>
    /// 设置金钱
    /// </summary>
    /// <param name="amount">金额</param>
    public void SetMoney(long amount)
    {
        currentMoney = amount;
        updateUI();
    }
    #endregion

    
    #region UI根节点
    [Header("UI配置")]
    public Transform uiRoot;
    
    [Header("BGM配置")]
    public string workBgmPath = "Music/打工BGM";
    
    private readonly Dictionary<string, GameObject> _uiCache = new Dictionary<string, GameObject>();
    #endregion

    #region 运势系统
    public enum LuckType { 吉, 平, 晦 }
    public LuckType currentLuck;

    private GameObject luckTipPanel;
    private TextMeshProUGUI luckText;
    private PrayLuck prayLuck;
    #endregion

    #region UI引用
    private Button workbut;
    private Image CG;
    private Image leftSmallImage;
    private Image rightSmallImage;
    private Button SwitchBut;
    private GameObject SwitchButCue;
    private Button normalBut;
    private GameObject normalButCue;
    private Button charWorkBut;
    private GameObject charWorkButCue;
    private GameObject workUI;
    private GameObject workCGUI;
    private TextMeshProUGUI moneyText;
    private TextMeshProUGUI weekText;
    private TextMeshProUGUI monthText;
    private Button clearAllJobsBtn;
    private Button testPlayAllSmallTalksBtn;
    private Button testAdvanceDayBtn;
    
    private string previousBgmPath;

    private GameObject SettlementPanel;
    private Button CloseSettlementBtn;
    private TextMeshProUGUI settlementTitleText;
    private TextMeshProUGUI rewardText;
    private TextMeshProUGUI levelUpText;
    private TextMeshProUGUI livingCostText;
    private TextMeshProUGUI remainingMoneyText;

    private GameObject jobDetailPanel;
    private Text jobDetailTitle;
    private Text jobDetailText;
    private TextMeshProUGUI jobDetailDescription;
    private TextMeshProUGUI jobDetailCondition;
    private Image jobDetailConditionBg;
    private Image jobDetailImage;
    private Image jobDetailExpressionImage;
    private Vector2 jobDetailImageOriginalSize; // 保存 jobDetailImage 的原始尺寸

    private Button closeWorkBtn;
    private Button switchToMapBtn;
    private Button skipCGBtn; // 跳过CG按钮
    
    [Header("小剧场跳过按钮")]
    public Button skipSmallTalkBtn; // 跳过小剧场按钮（手动引用）
    
    private int _originalSkipBtnSortingOrder = 0;
    private Transform _skipSmallTalkBtnOriginalParent;
    private Vector3 _skipSmallTalkBtnOriginalLocalPosition;

    private GameObject confirmDialogPanel;
    private TextMeshProUGUI confirmDialogText;
    private Button confirmYesBtn;
    private Button confirmNoBtn;

    //private GameObject workBackground;
    //public float moveRange = 30f;
    //public float moveSpeed = 0.5f;
    //private Vector3 bgOriginPos;
    private float bgMoveTimer;
    
    private TextMeshProUGUI reflectionText;
    public float typingSpeed = 0.1f;

    private GameObject progressBarParent;
    private Image playerIcon;
    public Vector2 playerOffset = new Vector2(10, -10);
    public float fadeDelay = 0.5f;
    public float playerMoveDuration = 0.3f;
    public float playerAnimInterval = 0.2f;
    public float fadedAlpha = 0.4f;

    private GameObject decorationParent;
    public Color decorationInactiveColor = new Color(0xBD / 255f, 0xA3 / 255f, 0x63 / 255f);
    public Color decorationActiveColor = Color.white;

    private int currentProgress = 0;
    private List<LittleCGItem> cgItems = new List<LittleCGItem>();
    private Sprite[] playerSprites = new Sprite[3];
    private int currentPlayerSpriteIndex = 0;
    private float playerAnimTimer = 0;
    private List<Image> decorationImages = new List<Image>();
    #endregion

    #region 内部状态
    public bool isShow = false;
    private bool isNormalHover = false;
    private bool isCharHover = false;
    private Coroutine smallImageAnimCoroutine;
    private bool isPlayingCG = false;
    private bool isShowingCharacterJobs = false; // 当前是否显示角色打工标签页
    private bool _hasSavedPreviousBgm = false; // 是否已保存之前的BGM
    private bool _isFirstSwitchButCueShown = false; // SwitchBut是否第一次显示过Cue
    private bool _hasUserSelectedJobs = false; // 是否有用户选择的打工
    private bool _hasUserSelectedNormalJobs = false; // 是否有用户选择的普通打工（不包括躺平）
    private bool _skipCGRequested = false; // 是否请求跳过CG

    private DayOfWeek _savedDay = DayOfWeek.Sun;
    private TimeSlot _savedTimeSlot = TimeSlot.Morning;
    private int _savedTotalApUsed = 0;
    #endregion

    #region 生命周期
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        AutoBindUIReferences();
    }
    
    private void OnDestroy()
    {
        // 清空静态变量，避免指向已销毁的对象
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    private void OnEnable()
    {
    }
    
    private void AutoBindUIReferences()
    {
        if (uiRoot == null) return;

        _uiCache.Clear();
        Transform[] allChildTrans = uiRoot.GetComponentsInChildren<Transform>(true);
        foreach (var t in allChildTrans)
        {
            if (!_uiCache.ContainsKey(t.name))
            {
                _uiCache.Add(t.name, t.gameObject);
            }
        }

        workbut = GetComp<Button>("workbut");
        CG = GetComp<Image>("workCG");
        leftSmallImage = GetComp<Image>("leftSmallImage");
        rightSmallImage = GetComp<Image>("rightSmallImage");
        SwitchBut = GetComp<Button>("SwitchBut");
        SwitchButCue = GetObj("SwitchButCue");
        normalBut = GetComp<Button>("normalBut");
        normalButCue = GetObj("normalButCue");
        charWorkBut = GetComp<Button>("charWorkBut");
        charWorkButCue = GetObj("charWorkButCue");

        workUI = GetObj("workUI");
        workCGUI = GetObj("workCGUI");
        moneyText = GetComp<TextMeshProUGUI>("moneyText");
        weekText = GetComp<TextMeshProUGUI>("weekText");
        monthText = GetComp<TextMeshProUGUI>("monthText");
        clearAllJobsBtn = GetComp<Button>("clearAllJobsBtn");
        testPlayAllSmallTalksBtn = GetComp<Button>("testPlayAllSmallTalksBtn");
        testAdvanceDayBtn = GetComp<Button>("testAdvanceDayBtn");

        luckTipPanel = GetObj("luckTipPanel");
        luckText = GetComp<TextMeshProUGUI>("luckText");

        SettlementPanel = GetObj("workSettlementPanel");
        CloseSettlementBtn = GetComp<Button>("workCloseSettlementBtn");
        settlementTitleText = GetComp<TextMeshProUGUI>("workSettlementTitleText");
        rewardText = GetComp<TextMeshProUGUI>("workRewardText");
        levelUpText = GetComp<TextMeshProUGUI>("workLevelUpText");
        livingCostText = GetComp<TextMeshProUGUI>("workLivingCostText");
        remainingMoneyText = GetComp<TextMeshProUGUI>("workRemainingMoneyText");

        jobDetailPanel = GetObj("jobDetailPanel");
        jobDetailTitle = GetComp<Text>("jobDetailTitle");
        jobDetailText = GetComp<Text>("jobDetailText");
        jobDetailDescription = GetComp<TextMeshProUGUI>("jobDetailDescription");
        jobDetailCondition = GetComp<TextMeshProUGUI>("jobDetailCondition");
        jobDetailConditionBg = GetComp<Image>("jobDetailConditionBg");
        jobDetailImage = GetComp<Image>("jobDetailImage");
        jobDetailExpressionImage = GetComp<Image>("jobDetailExpressionImage");
        
        if (jobDetailImage != null)
        {
            RectTransform rectTransform = jobDetailImage.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                jobDetailImageOriginalSize = rectTransform.sizeDelta;
            }
        }

        closeWorkBtn = GetComp<Button>("closeWorkBtn");
        switchToMapBtn = GetComp<Button>("switchToMapBtn");
        skipCGBtn = GetComp<Button>("skipCGBtn");
        confirmDialogPanel = GetObj("confirmDialogPanel");
        confirmDialogText = GetComp<TextMeshProUGUI>("confirmDialogText");
        confirmYesBtn = GetComp<Button>("workConfirmYesBtn");
        confirmNoBtn = GetComp<Button>("workConfirmNoBtn");
        
        reflectionText = GetComp<TextMeshProUGUI>("reflectionText");
        progressBarParent = GetObj("progressBarParent");
        playerIcon = GetComp<Image>("playerIcon");
        decorationParent = GetObj("decorationParent");
        
        JobListManager = uiRoot.GetComponentInChildren<JobListManager>(true);
        jobLogManager = uiRoot.GetComponentInChildren<jobLogManager>(true);
    }

    private GameObject GetObj(string name)
    {
        if (_uiCache.TryGetValue(name, out var go))
            return go;
        return null;
    }

    private T GetComp<T>(string name) where T : Component
    {
        GameObject go = GetObj(name);
        return go ? go.GetComponent<T>() : null;
    }

    private void AddButtonClickEvent(Button button, UnityEngine.Events.UnityAction call)
    {
        if (button == null) return;
        button.onClick.AddListener(call);
    }

    private void Start()
    {
        // 重置运行时状态
        totalApUsed = 0;
        currentDay = DayOfWeek.Sun;
        currentTimeSlot = TimeSlot.Morning;
        currentMoney = 3000;
        
        // 清空字典，避免重复添加或引用失效
        _allJobs.Clear();
        _allcharJobs.Clear();
        
        if (ConfigManager.Instance != null)
        {
            foreach (var job in ConfigManager.Instance.normalJobs)
                _allJobs.Add(job.name, job);

            foreach (var job in ConfigManager.Instance.characterJobs)
                _allcharJobs.Add(job.name, job);
        }

        InitSmallImageDicts();

        workbut?.onClick.AddListener(ShowWorkConfirmDialog);
        SwitchBut?.onClick.AddListener(show);
        CloseSettlementBtn?.onClick.AddListener(CloseSettlementPanel);
        closeWorkBtn?.onClick.AddListener(CloseWorkPanel);
        skipCGBtn?.onClick.AddListener(OnSkipCGClicked);
        
        if (skipSmallTalkBtn != null)
        {
            skipSmallTalkBtn.onClick.AddListener(OnSkipSmallTalkClicked);
            Debug.Log("[JobManager] skipSmallTalkBtn 事件已绑定");
        }
        else
        {
            Debug.LogWarning("[JobManager] skipSmallTalkBtn 未在 Inspector 中赋值！");
        }
        
        AddButtonClickEvent(confirmYesBtn, OnConfirmYes);
        AddButtonClickEvent(confirmNoBtn, OnConfirmNo);
        switchToMapBtn?.onClick.AddListener(SwitchToMap);
        testPlayAllSmallTalksBtn?.onClick.AddListener(TestPlayAllSmallTalks);
        testAdvanceDayBtn?.onClick.AddListener(AdvanceOneDayAndSetEmpty);

        if (normalBut != null)
        {
            EventTrigger normalTrigger = normalBut.gameObject.GetComponent<EventTrigger>();
            if (normalTrigger == null) normalTrigger = normalBut.gameObject.AddComponent<EventTrigger>();
            
            EventTrigger.Entry normalEnter = new EventTrigger.Entry();
            normalEnter.eventID = EventTriggerType.PointerEnter;
            normalEnter.callback.AddListener((data) => { OnNormalButHover(true); });
            normalTrigger.triggers.Add(normalEnter);

            EventTrigger.Entry normalExit = new EventTrigger.Entry();
            normalExit.eventID = EventTriggerType.PointerExit;
            normalExit.callback.AddListener((data) => { OnNormalButHover(false); });
            normalTrigger.triggers.Add(normalExit);

            EventTrigger.Entry normalClick = new EventTrigger.Entry();
            normalClick.eventID = EventTriggerType.PointerClick;
            normalClick.callback.AddListener((data) => { OnNormalButClick(); });
            normalTrigger.triggers.Add(normalClick);
        }

        if (charWorkBut != null)
        {
            EventTrigger charTrigger = charWorkBut.gameObject.GetComponent<EventTrigger>();
            if (charTrigger == null) charTrigger = charWorkBut.gameObject.AddComponent<EventTrigger>();
            
            EventTrigger.Entry charEnter = new EventTrigger.Entry();
            charEnter.eventID = EventTriggerType.PointerEnter;
            charEnter.callback.AddListener((data) => { OnCharButHover(true); });
            charTrigger.triggers.Add(charEnter);

            EventTrigger.Entry charExit = new EventTrigger.Entry();
            charExit.eventID = EventTriggerType.PointerExit;
            charExit.callback.AddListener((data) => { OnCharButHover(false); });
            charTrigger.triggers.Add(charExit);

            EventTrigger.Entry charClick = new EventTrigger.Entry();
            charClick.eventID = EventTriggerType.PointerClick;
            charClick.callback.AddListener((data) => { OnCharButClick(); });
            charTrigger.triggers.Add(charClick);
        }

        if (JobListManager != null)
        {
            // 预填充解锁状态缓存，避免后续检查时误判为"刚解锁"而重置 hasClickedCue
            // 这在新游戏时不会有问题（hasClickedCue 默认就是 false），但在读档时很重要
            JobListManager.PreFillUnlockedStateCache();
            OnNormalButClick();
        }
        RandomWeekLuck();
        updateUI();
        InitializeProgressBar();
    }
    
    private void InitializeProgressBar()
    {
        cgItems.Clear();
        decorationImages.Clear();
        
        if (progressBarParent != null)
        {
            LittleCGItem[] items = progressBarParent.GetComponentsInChildren<LittleCGItem>(true);
            foreach (var item in items)
            {
                item.Initialize();
                cgItems.Add(item);
            }
        }

        if (decorationParent != null)
        {
            Transform parentTransform = decorationParent.transform;
            for (int i = 0; i < parentTransform.childCount; i++)
            {
                Transform child = parentTransform.GetChild(i);
                Image image = child.GetComponent<Image>();
                
                if (image != null)
                {
                    Mask mask = image.GetComponent<Mask>();
                    if (mask != null) continue;
                    
                    if (child.GetComponent<LittleCGItem>() != null) continue;
                    
                    decorationImages.Add(image);
                    image.color = decorationInactiveColor;
                }
            }
        }

        LoadPlayerSprites();
        
        if (playerIcon != null)
        {
            playerIcon.enabled = false;
        }
    }

    private void LoadPlayerSprites()
    {
        for (int i = 0; i < 3; i++)
        {
            string path = $"workIcon/进度条小人{i + 1}";
            playerSprites[i] = Resources.Load<Sprite>(path);
            if (playerSprites[i] == null)
            {
                Debug.LogWarning($"[JobManager] 无法加载进度条小人图片: {path}");
            }
        }
    }

    private void UpdateDecorationColors(int activeIndex)
    {
        for (int i = 0; i < decorationImages.Count; i++)
        {
            if (decorationImages[i] != null)
            {
                decorationImages[i].color = (i == activeIndex) ? decorationActiveColor : decorationInactiveColor;
            }
        }
    }

    private void Update()
    {

        UpdatePlayerAnimation();
        UpdateButtonHoverScale();

        if (isShow && !isPlayingCG && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseWorkPanel();
        }
    }

    void UpdateButtonHoverScale()
    {
        float scaleSpeed = 8f;
        if (normalBut != null)
        {
            float target = isNormalHover ? 1.1f : 1f;
            normalBut.transform.localScale = Vector3.Lerp(normalBut.transform.localScale, Vector3.one * target, Time.deltaTime * scaleSpeed);
        }
        if (charWorkBut != null)
        {
            float target = isCharHover ? 1.1f : 1f;
            charWorkBut.transform.localScale = Vector3.Lerp(charWorkBut.transform.localScale, Vector3.one * target, Time.deltaTime * scaleSpeed);
        }
    }
    #endregion

    #region 鼠标悬停事件
    public void OnNormalButHover(bool isHover)
    {
        isNormalHover = isHover;
        
        GameObject effObj = normalBut.transform.Find("Eff")?.gameObject;
        if (effObj != null)
        {
            effObj.SetActive(isHover);
        }
    }

    public void OnCharButHover(bool isHover)
    {
        isCharHover = isHover;
        
        GameObject effObj = charWorkBut.transform.Find("Eff")?.gameObject;
        if (effObj != null)
        {
            effObj.SetActive(isHover);
        }
    }
    #endregion

    #region 按钮点击事件
    public void OnNormalButClick()
    {
        isShowingCharacterJobs = false;
        JobListManager.InitList(_allJobs, false);
        JobListManager.RefreshAll();
        UpdateSwitchButCueState();
    }

    public void OnCharButClick()
    {
        if (_allcharJobs.Count == 0)
        {
            if (ConfigManager.Instance != null)
            {
                foreach (var job in ConfigManager.Instance.characterJobs)
                {
                    if (!_allcharJobs.ContainsKey(job.name))
                        _allcharJobs.Add(job.name, job);
                }
            }
        }
        
        isShowingCharacterJobs = true;
        JobListManager.InitList(_allcharJobs, true);
        JobListManager.RefreshAll();
        UpdateSwitchButCueState();
    }
    
    public void RefreshCurrentJobList()
    {
        if (isShowingCharacterJobs)
        {
            OnCharButClick();
        }
        else
        {
            OnNormalButClick();
        }
    }

    /// <summary>
    /// 跳过CG按钮点击处理
    /// </summary>
    public void OnSkipCGClicked()
    {
        if (isPlayingCG)
        {
            _skipCGRequested = true;
        }
    }
    
    /// <summary>
    /// 跳过小剧场按钮点击处理
    /// </summary>
    public void OnSkipSmallTalkClicked()
    {
        Debug.Log("[JobManager] OnSkipSmallTalkClicked 被调用");
        
        if (SmallTalkManager.Instance == null)
        {
            Debug.LogWarning("[JobManager] SmallTalkManager.Instance 为 null");
            return;
        }
        
        if (!SmallTalkManager.Instance.IsPlayingSmallTalk())
        {
            Debug.LogWarning("[JobManager] IsPlayingSmallTalk() 返回 false");
            return;
        }
        
        Debug.Log("[JobManager] 调用 SkipCurrentSmallTalk()");
        SmallTalkManager.Instance.SkipCurrentSmallTalk();
    }
    
    /// <summary>
    /// 提高跳过按钮Canvas层级（在小剧场播放时调用）
    /// 将按钮移动到小剧场Canvas下最上层
    /// </summary>
    private void RaiseSkipButtonCanvasOrder()
    {
        if (skipSmallTalkBtn != null && SmallTalkManager.Instance != null && SmallTalkManager.Instance.dialog != null)
        {
            // 保存原始父对象和位置
            _skipSmallTalkBtnOriginalParent = skipSmallTalkBtn.transform.parent;
            _skipSmallTalkBtnOriginalLocalPosition = skipSmallTalkBtn.transform.localPosition;
            
            // 将按钮移动到小剧场Canvas下
            Canvas smallTalkCanvas = SmallTalkManager.Instance.dialog.GetComponent<Canvas>();
            if (smallTalkCanvas != null)
            {
                skipSmallTalkBtn.transform.SetParent(smallTalkCanvas.transform, true); // true 保持世界坐标
                // 设置为最后一个子对象，确保在最上层
                skipSmallTalkBtn.transform.SetAsLastSibling();
                
                Debug.Log("[JobManager] 将跳过按钮移动到小剧场Canvas最上层");
            }
        }
    }
    
    /// <summary>
    /// 恢复跳过按钮Canvas层级
    /// 将按钮移回原始父对象
    /// </summary>
    private void RestoreSkipButtonCanvasOrder()
    {
        if (skipSmallTalkBtn != null && _skipSmallTalkBtnOriginalParent != null)
        {
            skipSmallTalkBtn.transform.SetParent(_skipSmallTalkBtnOriginalParent, false);
            skipSmallTalkBtn.transform.localPosition = _skipSmallTalkBtnOriginalLocalPosition;
            Debug.Log("[JobManager] 将跳过按钮移回原始位置");
        }
    }
    #endregion

    #region 任务列表管理
    private static int _instanceIdCounter = 0;
    

    public void Addjob(JobData jobTemplate)
    {
        int requiredSlots = jobTemplate.apCost;
        int availableSlots = 0;
        
        for (int i = 0; i < actjobs.Length; i++)
        {
            if (actjobs[i] == null) availableSlots++;
        }
        
        if (availableSlots < requiredSlots) return;

        JobData job = new JobData
        {
            name = jobTemplate.name,
            ischar = jobTemplate.ischar,
            apCost = jobTemplate.apCost,
            timeType = jobTemplate.timeType,
            cdVal = jobTemplate.cdVal,
            rewards = jobTemplate.rewards,
            unlockWeek = jobTemplate.unlockWeek,
            imageName = jobTemplate.imageName,
            head = jobTemplate.head,
            BG = jobTemplate.BG,
            npcName = jobTemplate.npcName,
            Refs = jobTemplate.Refs,
            currentLevel = jobTemplate.currentLevel,
            exp = jobTemplate.exp,
            curcd = jobTemplate.curcd
        };
        
        job.instanceId = ++_instanceIdCounter;
        
        int startSlot = FindFirstValidSlot(job);
        if (startSlot < 0) return;
        
        int filledSlots = 0;
        int slotIndex = startSlot;
        while (slotIndex < actjobs.Length && filledSlots < requiredSlots)
        {
            if (actjobs[slotIndex] == null)
            {
                int dayIndex = slotIndex / 3;
                bool hasEmptyJobInDay = false;
                for (int j = dayIndex * 3; j < dayIndex * 3 + 3 && j < actjobs.Length; j++)
                {
                    if (actjobs[j] != null && actjobs[j].isEmpty)
                    {
                        hasEmptyJobInDay = true;
                        break;
                    }
                }
                
                if (!hasEmptyJobInDay)
                {
                    actjobs[slotIndex] = job;
                    filledSlots++;
                }
                else
                {
                    break;
                }
            }
            slotIndex++;
        }
        
        totalApUsed += job.apCost;
        
        if (job.cdVal > 0)
        {
            if (job.ischar)
            {
                if (_allcharJobs.TryGetValue(job.name, out JobData charJob))
                {
                    charJob.curcd++;
                }
            }
            else
            {
                if (_allJobs.TryGetValue(job.name, out JobData normalJob))
                {
                    normalJob.curcd++;
                }
            }
        }
        
        if (job.ischar) totalChar++;

        RefreshTimeFromAp();
        jobLogManager.updatelist(actjobs);
        updateUI();
    }

    public void Removejob(JobData job)
    {
        int targetInstanceId = job.instanceId;
        int jobApCost = job.apCost;
        bool isCharJob = job.ischar;
        
        if (job.cannotBeRemoved) return;
        
        for (int i = 0; i < actjobs.Length; i++)
        {
            if (actjobs[i] != null && actjobs[i].instanceId == targetInstanceId)
            {
                actjobs[i] = null;
            }
        }
        
        totalApUsed -= jobApCost;
        
        if (job.cdVal > 0)
        {
            if (isCharJob)
            {
                if (_allcharJobs.TryGetValue(job.name, out JobData charJob))
                {
                    if (charJob.curcd > 0) charJob.curcd--;
                }
            }
            else
            {
                if (_allJobs.TryGetValue(job.name, out JobData normalJob))
                {
                    if (normalJob.curcd > 0) normalJob.curcd--;
                }
            }
        }
        
        if (isCharJob) totalChar--;
        
        RefreshTimeFromAp();
        jobLogManager.updatelist(actjobs);
        updateUI();
    }

    public void ClearAllJobs()
    {
        foreach (var job in actjobs)
        {
            if (job != null && job.curcd > 0 && job.cdVal > 0)
                job.curcd = 0;
        }

        System.Array.Clear(actjobs, 0, actjobs.Length);
        totalApUsed = 0;
        totalChar = 0;

        currentDay = (DayOfWeek)6;
        currentTimeSlot = TimeSlot.Morning;

        jobLogManager.updatelist(actjobs);
        updateUI();
    }
    #endregion

    #region 时间与行动力计算
    public void RefreshTimeFromAp()
    {
        int dayProgress = totalApUsed / 3;
        int newDayInt = dayProgress % 7;
        
        int mappedDayInt = (newDayInt + 6) % 7;
        currentDay = (DayOfWeek)mappedDayInt;

        int slot = totalApUsed % 3;
        currentTimeSlot = slot switch
        {
            0 => TimeSlot.Morning,
            1 => TimeSlot.Afternoon,
            2 => TimeSlot.Night,
            _ => currentTimeSlot
        };
    }
    #endregion

    #region 打工确认对话框
    void ShowWorkConfirmDialog()
    {
        if (confirmDialogPanel == null)
        {
            gowork();
            return;
        }
        
        bool hasJobs = false;
        foreach (var job in actjobs)
        {
            if (job != null)
            {
                hasJobs = true;
                break;
            }
        }
        
        if (confirmDialogText != null)
        {
            confirmDialogText.text = hasJobs ? "是否按以上计划执行？" : "确定要摆烂吗？";
        }
        
        confirmDialogPanel.SetActive(true);
    }
    
    void OnConfirmYes()
    {
        if (confirmDialogPanel != null)
        {
            confirmDialogPanel.SetActive(false);
        }

        // 引导期间不真正执行打工
        if (TutorialGuideManager.IsGuideActive)
        {
            Debug.Log("[JobManager] 引导期间，跳过打工执行");
            return;
        }

        gowork();
    }

    void OnConfirmNo()
    {
        if (confirmDialogPanel != null)
        {
            confirmDialogPanel.SetActive(false);
        }

        OnNormalButClick();
    }
    #endregion

    #region 打工执行逻辑
    void gowork()
    {
        totalReward = 0;
        actCG.Clear();
        levelUpJobs.Clear();
        performedCharJobs.Clear();
        _hasUserSelectedJobs = false;
        _hasUserSelectedNormalJobs = false;
        foreach (var job in actjobs)
        {
            if (job != null && !job.isEmpty && job.name != "躺平")
            {
                _hasUserSelectedJobs = true;
                if (!job.ischar)
                {
                    _hasUserSelectedNormalJobs = true;
                }
            }
        }
        
        FillEmptySlotsWithLyingFlat();
        
        Dictionary<int, int> jobExecutionCounts = new Dictionary<int, int>();
        HashSet<int> countedInstances = new HashSet<int>();
        
        foreach (var job in actjobs)
        {
            if (job != null && !job.isEmpty)
            {
                if (countedInstances.Contains(job.instanceId)) continue;
                countedInstances.Add(job.instanceId);
                jobExecutionCounts[job.instanceId] = 1;
            }
        }

        Dictionary<string, int> jobNameExecutionCounts = new Dictionary<string, int>();
        HashSet<int> countedInstanceIds = new HashSet<int>();
        
        foreach (var job in actjobs)
        {
            if (job != null && !job.isEmpty)
            {
                if (countedInstanceIds.Contains(job.instanceId)) continue;
                countedInstanceIds.Add(job.instanceId);
                
                string jobKey = job.ischar ? job.npcName : job.name;
                if (!jobNameExecutionCounts.ContainsKey(jobKey))
                {
                    jobNameExecutionCounts[jobKey] = 0;
                }
                jobNameExecutionCounts[jobKey] += jobExecutionCounts[job.instanceId];
            }
        }

        SelectCGsByDayRange();

        HashSet<int> processedInstanceIds = new HashSet<int>();
        
        foreach (var job in actjobs)
        {
            if (job != null && !job.isEmpty)
            {
                if (processedInstanceIds.Contains(job.instanceId)) continue;
                processedInstanceIds.Add(job.instanceId);
                
                int executionCount = jobExecutionCounts[job.instanceId];

                int reward = 0;
                if (job.ischar)
                {
                    if (string.IsNullOrEmpty(job.npcName))
                    {
                        int safeLevel = Mathf.Clamp(0, 0, job.rewards.Length - 1);
                        reward = job.rewards[safeLevel];
                    }
                    else
                    {
                        int affectionLevel = 0;
                        if (AffectionManager.Instance != null)
                        {
                            affectionLevel = AffectionManager.Instance.GetAffectionLevel(job.npcName);
                        }
                        else
                        {
                            Debug.LogWarning($"[JobManager] AffectionManager.Instance 为 null，无法获取 {job.npcName} 的好感度等级");
                        }
                        int safeLevel = Mathf.Clamp(affectionLevel, 0, job.rewards.Length - 1);
                        reward = job.rewards[safeLevel];
                        
                        job.plannedAffectionLevel = affectionLevel;
                        
                        if (AffectionManager.Instance != null)
                        {
                            AffectionManager.Instance.AddAffection(job.npcName, 3 * executionCount);
                        }
                        
                        if (!performedCharJobs.Contains(job.npcName))
                        {
                            performedCharJobs.Add(job.npcName);
                        }
                    }
                }
                else
                {
                    string jobKey = job.name;
                    
                    if (job.name == "躺平")
                    {
                        reward = 0;
                    }
                    else
                    {
                        JobData originalJob = null;
                        if (_allJobs.TryGetValue(job.name, out originalJob))
                        {
                            int safeLevel = Mathf.Clamp(originalJob.currentLevel, 0, originalJob.rewards.Length - 1);
                            reward = originalJob.rewards[safeLevel];
                            originalJob.exp += executionCount;
                            while (originalJob.exp >= 6)
                            {
                                LevelUp(originalJob);
                                originalJob.exp -= 6;
                            }
                        }
                        else
                        {
                            int safeLevel = Mathf.Clamp(job.currentLevel, 0, job.rewards.Length - 1);
                            reward = job.rewards[safeLevel];
                        }
                    }
                }

                totalReward += GetFinalReward(reward) * executionCount;
            }
        }

        StartCoroutine(PlayCGAndPaySequence());
    }

    private void FillEmptySlotsWithLyingFlat()
    {
        for (int i = 0; i < actjobs.Length; i++)
        {
            if (actjobs[i] == null)
            {
                JobData lyingFlatJob = new JobData
                {
                    name = "躺平",
                    ischar = false,
                    apCost = 1,
                    timeType = TimeType.AllDay,
                    cdVal = 0,
                    rewards = new int[] { 0, 0, 0, 0 },
                    unlockWeek = 0,
                    imageName = "躺平",
                    head = "",
                    BG = "",
                    npcName = "",
                    Refs = new string[] { "报复性地每天睡满12个小时……", "听着喜欢的音乐剧磁带，时间过得很快。", "文学是逃避现实的最佳去处……" },
                    currentLevel = 0,
                    exp = 0,
                    curcd = 0
                };

                lyingFlatJob.instanceId = ++_instanceIdCounter;
                actjobs[i] = lyingFlatJob;
            }
        }
    }

    private void SelectCGsByDayRange()
    {
        HashSet<string> selectedJobKeys = new HashSet<string>();
        
        List<JobData> allNormalJobs = new List<JobData>();
        List<JobData> lyingFlatJobs = new List<JobData>();
        HashSet<int> addedInstanceIds = new HashSet<int>();
        
        for (int i = 0; i < actjobs.Length; i++)
        {
            var job = actjobs[i];
            if (job != null && !job.isEmpty && !job.ischar && !addedInstanceIds.Contains(job.instanceId))
            {
                if (!string.IsNullOrEmpty(job.imageName))
                {
                    if (job.name == "躺平")
                    {
                        lyingFlatJobs.Add(job);
                    }
                    else if (!selectedJobKeys.Contains(job.name))
                    {
                        allNormalJobs.Add(job);
                        selectedJobKeys.Add(job.name);
                    }
                    addedInstanceIds.Add(job.instanceId);
                }
            }
        }
        
        if (_hasUserSelectedNormalJobs)
        {
            int addedCount = 0;
            List<CGInfo> baseCGs = new List<CGInfo>();
            int luckIndex = GetLuckIndex(); // 统一获取运势索引

            foreach (var job in allNormalJobs)
            {
                if (actCG.Count >= 3) break;

                CGInfo cgInfo = new CGInfo {
                    imageName = job.imageName,
                    npcName = job.npcName,
                    isCharJob = job.ischar,
                    randomIndex = luckIndex // 使用正确的运势索引
                };

                actCG.Add(cgInfo);
                baseCGs.Add(cgInfo);
                addedCount++;
            }

            // 如果普通打工CG不满3个，用躺平补充
            if (actCG.Count < 3 && lyingFlatJobs.Count > 0)
            {
                while (actCG.Count < 3)
                {
                    int randomIndex = Random.Range(0, lyingFlatJobs.Count);
                    JobData lyingFlatJob = lyingFlatJobs[randomIndex];

                    actCG.Add(new CGInfo {
                        imageName = lyingFlatJob.imageName,
                        npcName = lyingFlatJob.npcName,
                        isCharJob = lyingFlatJob.ischar,
                        randomIndex = luckIndex
                    });
                }
            }

            // 如果躺平也没有，用已有的CG重复填充
            if (actCG.Count < 3 && baseCGs.Count > 0)
            {
                int repeatCount = 3 - actCG.Count;

                for (int i = 0; i < repeatCount; i++)
                {
                    CGInfo baseCG = baseCGs[i % baseCGs.Count];

                    actCG.Add(new CGInfo {
                        imageName = baseCG.imageName,
                        npcName = baseCG.npcName,
                        isCharJob = baseCG.isCharJob,
                        randomIndex = luckIndex
                    });
                }
            }
        }
        else if (_hasUserSelectedJobs)
        {
            int luckIndex = GetLuckIndex();
            
            HashSet<string> addedCharJobs = new HashSet<string>();
            for (int i = 0; i < actjobs.Length; i++)
            {
                var job = actjobs[i];
                if (job != null && !job.isEmpty && job.ischar && !string.IsNullOrEmpty(job.npcName) && !addedCharJobs.Contains(job.npcName))
                {
                    if (actCG.Count >= 3) break;
                    
                    actCG.Add(new CGInfo {
                        imageName = job.imageName,
                        npcName = job.npcName,
                        isCharJob = true,
                        randomIndex = luckIndex
                    });
                    addedCharJobs.Add(job.npcName);
                }
            }
            
            if (actCG.Count < 3 && lyingFlatJobs.Count > 0)
            {
                while (actCG.Count < 3)
                {
                    int randomIndex = Random.Range(0, lyingFlatJobs.Count);
                    JobData lyingFlatJob = lyingFlatJobs[randomIndex];
                    
                    actCG.Add(new CGInfo {
                        imageName = lyingFlatJob.imageName,
                        npcName = lyingFlatJob.npcName,
                        isCharJob = lyingFlatJob.ischar,
                        randomIndex = luckIndex
                    });
                }
            }
        }
        else if (lyingFlatJobs.Count > 0)
        {
            int luckIndex = GetLuckIndex();

            while (lyingFlatJobs.Count > 0 && actCG.Count < 3)
            {
                int randomJobIndex = Random.Range(0, lyingFlatJobs.Count);
                JobData lyingFlatJob = lyingFlatJobs[randomJobIndex];

                actCG.Add(new CGInfo {
                    imageName = lyingFlatJob.imageName,
                    npcName = lyingFlatJob.npcName,
                    isCharJob = lyingFlatJob.ischar,
                    randomIndex = luckIndex
                });
            }
        }
    }
    #endregion

    #region UI显示控制
    void show()
    {
        // 添加空值检查
        if (ShowDialog.Instance != null && ShowDialog.Instance.isDialogPlaying)
        {
            Debug.Log("[JobManager] 对话框正在播放，无法打开打工界面");
            return;
        }
        
        _isFirstSwitchButCueShown = true;
        isShow = true;
        workUI.SetActive(isShow);
        
        if (isShow)
        {
            if (workCGUI != null)
            {
                workCGUI.SetActive(false);
            }
            
            _savedDay = currentDay;
            _savedTimeSlot = currentTimeSlot;
            _savedTotalApUsed = totalApUsed;
            
            StartCoroutine(ShowWorkUICoroutine());
        }
        else
        {
            RemoveAllNonEmptyJobs();
            
            currentDay = _savedDay;
            currentTimeSlot = _savedTimeSlot;
            totalApUsed = _savedTotalApUsed;
            updateUI();
        }
    }
    
    private System.Collections.IEnumerator ShowWorkUICoroutine()
    {
        yield return null;
        OnNormalButClick();
        // 刷新打工槽位显示
        if (jobLogManager != null)
        {
            jobLogManager.updatelist(actjobs);
        }
        updateUI();
    }
    
    private void PlayWorkBgm()
    {
        if (SoundsManager.Instance != null && !string.IsNullOrEmpty(workBgmPath))
        {
            if (!_hasSavedPreviousBgm)
            {
                string currentBgm = SoundsManager.Instance.CurrentMusicPath;
                if (!string.IsNullOrEmpty(currentBgm))
                {
                    previousBgmPath = currentBgm;
                    _hasSavedPreviousBgm = true;
                }
            }
            
            SoundsManager.Instance.PlayMusic(workBgmPath);
        }
    }
    
    private void StopWorkBgm()
    {
        if (SoundsManager.Instance != null)
        {
            SoundsManager.Instance.FadeOutMusic();
            SoundsManager.Instance.PlayMusic("小平房间循环曲");
            _hasSavedPreviousBgm = false;
        }
    }

    void CloseSettlementPanel()
    {
        SettlementPanel.SetActive(false);
    }

    void CloseWorkPanel()
    {
        workUI.SetActive(false);
        isShow = false;
        
        RemoveAllNonEmptyJobs();
        
        currentDay = _savedDay;
        currentTimeSlot = _savedTimeSlot;
        totalApUsed = _savedTotalApUsed;
        updateUI();
    }

    void RemoveAllNonEmptyJobs()
    {
        int reservedAp = 0;
        for (int i = 0; i < actjobs.Length; i++)
        {
            if (actjobs[i] != null && actjobs[i].isEmpty)
            {
                reservedAp++;
            }
        }
        
        for (int i = 0; i < actjobs.Length; i++)
        {
            if (actjobs[i] != null && !actjobs[i].isEmpty)
            {
                actjobs[i] = null;
            }
        }
        
        foreach (var job in _allJobs.Values)
        {
            if (job != null)
                job.curcd = 0;
        }
        foreach (var job in _allcharJobs.Values)
        {
            if (job != null)
                job.curcd = 0;
        }
        
        totalChar = 0;
        totalApUsed = reservedAp;
        
        jobLogManager.updatelist(actjobs);
        updateUI();
    }

    void SwitchToMap()
    {
        if (ShowMapButton.instance != null)
        {
            workUI.SetActive(false);
            isShow = false;
            
            RemoveAllNonEmptyJobs();
            
            currentDay = _savedDay;
            currentTimeSlot = _savedTimeSlot;
            totalApUsed = _savedTotalApUsed;
            updateUI();
            
            if (ShowMapButton.instance.isShow)
            {
                ShowMapButton.instance.ShowMap();
            }
            
            ShowMapButton.instance.ShowMap();
        }
    }
    #endregion

    #region 打工动画与结算
    [Header("CG动画设置")]
    public float frameDuration = 0.3f;
    public int sequenceFrameCount = 3;
    public bool useFadeEffect = true;

    IEnumerator PlayCGAndPaySequence()
    {
        PlayWorkBgm();
        
        bool onlyHasCharacterJobs = _hasUserSelectedJobs && !_hasUserSelectedNormalJobs && performedCharJobs.Count > 0;
        
        if (onlyHasCharacterJobs)
        {
            // 检查 SmallTalkManager 和相关组件是否可用
            if (SmallTalkManager.Instance != null && 
                DialogList.Instance != null && 
                DialogList.Instance.smallTalk != null)
            {
                int randomIndex = Random.Range(0, performedCharJobs.Count);
                string selectedNpc = performedCharJobs[randomIndex];
                
                // 显示跳过小剧场按钮并提高Canvas层级
                if (skipSmallTalkBtn != null)
                {
                    skipSmallTalkBtn.gameObject.SetActive(true);
                }
                RaiseSkipButtonCanvasOrder();
                
                Coroutine bgmMonitorCoroutine = StartCoroutine(MonitorAndRestoreWorkBgmCoroutine());
                
                yield return StartCoroutine(SmallTalkManager.Instance.PlayCharacterSideStory(
                    selectedNpc + "-对话" + (Random.value < 0.5 ? "1" : "2")
                ));
                
                StopCoroutine(bgmMonitorCoroutine);
                
                // 隐藏跳过小剧场按钮并恢复Canvas层级
                RestoreSkipButtonCanvasOrder();
                if (skipSmallTalkBtn != null)
                {
                    skipSmallTalkBtn.gameObject.SetActive(false);
                }
                
                if (SoundsManager.Instance != null && !string.IsNullOrEmpty(workBgmPath))
                {
                    SoundsManager.Instance.PlayMusic(workBgmPath);
                }
            }
            else
            {
                Debug.LogWarning($"[JobManager] 无法播放角色打工小剧场: SmallTalkManager.Instance={SmallTalkManager.Instance != null}, DialogList.Instance={DialogList.Instance != null}, smallTalk={DialogList.Instance?.smallTalk != null}");
            }
            
            yield return StartCoroutine(WaitForBlackoutTransitionWithCallback(() =>
            {
                FinishWork();
            }));
            yield break;
        }
        
        if (progressBarParent != null && !progressBarParent.activeInHierarchy)
        {
            progressBarParent.SetActive(true);
            yield return null;
        }
        
        if (playerIcon != null && !playerIcon.gameObject.activeInHierarchy)
        {
            playerIcon.gameObject.SetActive(true);
            yield return null;
        }
        
        if (cgItems.Count == 0)
        {
            InitializeProgressBar();
            yield return null;
        }
        
        if (workCGUI != null)
        {
            workCGUI.SetActive(true);
        }

        CG.gameObject.SetActive(false);
        if (reflectionText != null)
        {
            reflectionText.gameObject.SetActive(false);
        }
        isPlayingCG = true;
        _skipCGRequested = false;

        // 显示跳过按钮
        if (skipCGBtn != null)
        {
            skipCGBtn.gameObject.SetActive(true);
        }

        ResetProgressBar();

        if (actCG == null || actCG.Count == 0)
        {
            if (workCGUI != null)
            {
                workCGUI.SetActive(false);
            }
            yield return new WaitForEndOfFrame();
            FinishWork();
            yield break;
        }

        yield return StartCoroutine(WaitForBlackoutTransition(false));

        int[] progressSteps = { 2, 2, 3 };
        int cgIndex = 0;

        foreach (var cgInfo in actCG)
            {
                // 检查是否请求跳过
                if (_skipCGRequested)
                {
                    break;
                }

                if (string.IsNullOrEmpty(cgInfo.imageName)) continue;

                Sprite s = null;
                int randomIndex;
                int stepCount = cgIndex < progressSteps.Length ? progressSteps[cgIndex] : 2;

                // 统一使用运势索引（CG和心得）
                randomIndex = cgInfo.randomIndex > 0 ? cgInfo.randomIndex : GetLuckIndex();
                string cgPath = $"CG/打工cg-{cgInfo.imageName}-{randomIndex}";
                s = Resources.Load<Sprite>(cgPath);

                if (s == null) continue;

                CG.sprite = s;
                CG.gameObject.SetActive(true);

                LoadSmallImagesForJob(cgInfo.imageName, randomIndex);

                if (smallImageAnimCoroutine != null)
                    StopCoroutine(smallImageAnimCoroutine);
                smallImageAnimCoroutine = StartCoroutine(AnimateSmallImages());

                if (useFadeEffect)
                    yield return FadeIn(CG.gameObject, 0.2f);

                // 使用修正后的randomIndex，确保心得显示与CG一致
                ShowReflectionText(cgInfo.imageName, randomIndex);

                yield return StartCoroutine(AdvanceProgressBarCoroutine(cgIndex, stepCount, cgInfo.imageName, randomIndex, false));

                // 可中断的等待时间
                float baseDuration = 3f - 1.5f;
                float durationMultiplier = (float)stepCount / 2f;
                float waitTime = baseDuration * durationMultiplier;
                float elapsed = 0f;
                while (elapsed < waitTime && !_skipCGRequested)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                if (_skipCGRequested)
                {
                    break;
                }

                if (useFadeEffect)
                    yield return FadeOut(CG.gameObject, 0.2f);

                if (smallImageAnimCoroutine != null)
                {
                    StopCoroutine(smallImageAnimCoroutine);
                    smallImageAnimCoroutine = null;
                }

                if (leftSmallImage != null) leftSmallImage.transform.rotation = Quaternion.identity;
                if (rightSmallImage != null) rightSmallImage.transform.rotation = Quaternion.identity;
                
                if (reflectionText != null)
                    reflectionText.gameObject.SetActive(false);

                cgIndex++;
            }
            
            if (smallImageAnimCoroutine != null)
            {
                StopCoroutine(smallImageAnimCoroutine);
                smallImageAnimCoroutine = null;
            }

            if (leftSmallImage != null) leftSmallImage.transform.rotation = Quaternion.identity;
            if (rightSmallImage != null) rightSmallImage.transform.rotation = Quaternion.identity;
            
            if (reflectionText != null)
                reflectionText.gameObject.SetActive(false);

            // 在进入小剧场前隐藏整个打工UI，避免跳过小剧场时闪现
            if (workUI != null)
            {
                workUI.SetActive(false);
            }
            isPlayingCG = false;

            // 检查 SmallTalkManager 和相关组件是否可用
            if (SmallTalkManager.Instance != null && 
                DialogList.Instance != null && 
                DialogList.Instance.smallTalk != null && 
                performedCharJobs.Count > 0)
            {
                // 显示跳过小剧场按钮并提高Canvas层级
                if (skipSmallTalkBtn != null)
                {
                    skipSmallTalkBtn.gameObject.SetActive(true);
                }
                RaiseSkipButtonCanvasOrder();
                
                int randomIndex = Random.Range(0, performedCharJobs.Count);
                string selectedNpc = performedCharJobs[randomIndex];
                
                Coroutine bgmMonitorCoroutine = StartCoroutine(MonitorAndRestoreWorkBgmCoroutine());
                
                yield return StartCoroutine(SmallTalkManager.Instance.PlayCharacterSideStory(
                    selectedNpc + "-对话" + (Random.value < 0.5 ? "1" : "2")
                ));
                
                StopCoroutine(bgmMonitorCoroutine);
                
                // 隐藏跳过小剧场按钮并恢复Canvas层级
                RestoreSkipButtonCanvasOrder();
                if (skipSmallTalkBtn != null)
                {
                    skipSmallTalkBtn.gameObject.SetActive(false);
                }
                
                if (SoundsManager.Instance != null && !string.IsNullOrEmpty(workBgmPath))
                {
                    SoundsManager.Instance.PlayMusic(workBgmPath);
                }
            }
            else if (performedCharJobs.Count > 0)
            {
                Debug.LogWarning($"[JobManager] 无法播放角色打工小剧场: SmallTalkManager.Instance={SmallTalkManager.Instance != null}, DialogList.Instance={DialogList.Instance != null}, smallTalk={DialogList.Instance?.smallTalk != null}");
            }
            else
            {
                // 没有小剧场播放时，隐藏跳过小剧场按钮
                if (skipSmallTalkBtn != null)
                {
                    skipSmallTalkBtn.gameObject.SetActive(false);
                }
            }

            // 使用黑屏过渡进入结算（UI已在小剧场播放前隐藏）
            yield return StartCoroutine(WaitForBlackoutTransitionWithCallback(() =>
            {
                FinishWork();
            }));
    }

    private IEnumerator WaitForBlackoutTransition(bool fadeInOut = true)
    {
        bool transitionComplete = false;
        
        if (BlackoutTransition.Instance != null)
        {
            if (fadeInOut)
            {
                BlackoutTransition.Instance.FadeInOut(() =>
                {
                    transitionComplete = true;
                });
            }
            else
            {
                BlackoutTransition.Instance.FadeOutOnly(() =>
                {
                    transitionComplete = true;
                });
            }
        }
        else
        {
            transitionComplete = true;
        }

        while (!transitionComplete)
        {
            yield return null;
        }
    }

    private IEnumerator WaitForBlackoutTransitionWithCallback(System.Action onBlackout)
    {
        bool transitionComplete = false;
        
        if (BlackoutTransition.Instance != null)
        {
            BlackoutTransition.Instance.FadeOutWithCallback(
                onBlackout,
                () =>
                {
                    transitionComplete = true;
                }
            );
        }
        else
        {
            onBlackout?.Invoke();
            transitionComplete = true;
        }

        while (!transitionComplete)
        {
            yield return null;
        }
    }
    
    private void ShowReflectionText(string imageName, int index)
    {
        if (reflectionText == null) return;
        
        JobData job = null;
        
        foreach (var j in actjobs)
        {
            if (j != null && j.imageName == imageName)
            {
                job = j;
                break;
            }
        }
        
        if (job == null)
        {
            foreach (var j in actjobs)
            {
                if (j != null && j.ischar && !string.IsNullOrEmpty(j.npcName) && imageName == j.npcName)
                {
                    job = j;
                    break;
                }
            }
        }
        
        if (job == null || job.Refs == null || job.Refs.Length == 0)
        {
            reflectionText.gameObject.SetActive(false);
            return;
        }
        
        int refIndex = index - 1;
        if (refIndex < 0 || refIndex >= job.Refs.Length)
        {
            reflectionText.gameObject.SetActive(false);
            return;
        }
        
        string reflectionContent = job.Refs[refIndex];
        if (string.IsNullOrEmpty(reflectionContent))
        {
            reflectionText.gameObject.SetActive(false);
            return;
        }
        
        reflectionText.gameObject.SetActive(true);
        StartCoroutine(TypeText(reflectionContent));
    }

    private IEnumerator TypeText(string text)
    {
        reflectionText.text = "";
        foreach (char c in text)
        {
            reflectionText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
    }

    private void ResetProgressBar()
    {
        currentProgress = 0;
        
        foreach (var item in cgItems)
        {
            if (item != null)
            {
                item.Initialize();
            }
        }
        
        foreach (var decoration in decorationImages)
        {
            if (decoration != null)
            {
                decoration.color = decorationInactiveColor;
            }
        }
        
        if (playerIcon != null)
        {
            playerIcon.enabled = false;
            
            if (cgItems.Count > 0 && cgItems[0] != null)
            {
                Transform targetTransform = cgItems[0].transform;
                RectTransform targetRect = cgItems[0].GetComponent<RectTransform>();
                RectTransform playerRect = playerIcon.GetComponent<RectTransform>();
                
                if (playerRect != null && targetRect != null)
                {
                    Vector2 targetAnchoredPos = targetRect.anchoredPosition;
                    Vector2 targetPivot = targetRect.pivot;
                    Vector2 targetAnchorMin = targetRect.anchorMin;
                    Vector2 targetAnchorMax = targetRect.anchorMax;
                    
                    Transform originalParent = playerRect.parent;
                    
                    playerRect.SetParent(targetRect.parent);
                    playerRect.pivot = targetPivot;
                    playerRect.anchorMin = targetAnchorMin;
                    playerRect.anchorMax = targetAnchorMax;
                    playerRect.anchoredPosition = targetAnchoredPos + playerOffset;
                    
                    playerRect.SetParent(originalParent);
                }
                else
                {
                    Vector3 targetWorldPos = targetTransform.position;
                    targetWorldPos.z = playerIcon.transform.position.z;
                    playerIcon.transform.position = targetWorldPos;
                }
            }
        }
    }

    private IEnumerator AdvanceProgressBarCoroutine(int cgIndex, int stepCount, string imageName, int randomIndex, bool isCharJob)
    {
        Sprite sprite = null;
        string cgPath;
        
        if (isCharJob)
        {
            cgPath = $"CG/小剧场-{imageName}-{randomIndex}";
            sprite = Resources.Load<Sprite>(cgPath);
        }
        else
        {
            cgPath = $"CG/打工cg-{imageName}-{randomIndex}";
            sprite = Resources.Load<Sprite>(cgPath);
        }

        float totalTime = 1.5f;
        float timePerCell = totalTime / stepCount;

        for (int i = 0; i < stepCount && currentProgress < cgItems.Count; i++)
        {
            var item = cgItems[currentProgress];
            if (item != null)
            {
                bool isEmptyDay = IsDayEmptyJob(currentProgress);

                if (isEmptyDay)
                {
                    EnableNullChildObject(item);
                }
                else
                {
                    // 检查是否为角色打工
                    string characterName = GetCharacterJobForDay(currentProgress);
                    Debug.Log($"[JobManager] 进度条格子 {currentProgress}, 角色打工检测: {(string.IsNullOrEmpty(characterName) ? "无" : characterName)}");

                    if (!string.IsNullOrEmpty(characterName))
                    {
                        // 角色打工：设置Head头像
                        string headPath = $"WorkIcon/打工cg-{characterName}";
                        Sprite headSprite = Resources.Load<Sprite>(headPath);
                        Debug.Log($"[JobManager] 加载角色头像: {headPath}, 结果: {(headSprite != null ? "成功" : "失败")}");
                        item.SetCharacterHead(headSprite);
                        // 注意：角色打工不调用 ShowIcon()，因为 SetCharacterHead 已禁用 iconImage

                        int dayReward = CalculateDayReward(currentProgress);
                        item.ShowReward(dayReward);
                    }
                    else
                    {
                        // 普通打工：显示报酬
                        item.SetIcon(sprite);
                        item.ShowIcon();

                        int dayReward = CalculateDayReward(currentProgress);
                        item.ShowReward(dayReward);
                    }
                }
            }

            yield return StartCoroutine(MovePlayerToPosition(currentProgress));

            UpdateDecorationColors(currentProgress);

            yield return new WaitForSeconds(timePerCell);

            currentProgress++;
        }
        
        for (int i = 0; i < currentProgress - 1 && i < cgItems.Count; i++)
        {
            var item = cgItems[i];
            if (item != null)
            {
                item.FadeOut(fadedAlpha);
            }
        }
    }

    private bool IsDayEmptyJob(int dayIndex)
    {
        for (int slot = 0; slot < 3; slot++)
        {
            int slotIndex = dayIndex * 3 + slot;
            if (slotIndex < actjobs.Length)
            {
                var job = actjobs[slotIndex];
                if (job != null && job.isEmpty)
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// 获取某天的角色打工信息
    /// </summary>
    /// <param name="dayIndex">天数索引</param>
    /// <returns>角色打工的npcName，如果没有角色打工则返回null</returns>
    private string GetCharacterJobForDay(int dayIndex)
    {
        for (int slot = 0; slot < 3; slot++)
        {
            int slotIndex = dayIndex * 3 + slot;
            if (slotIndex < actjobs.Length)
            {
                var job = actjobs[slotIndex];
                if (job != null && !job.isEmpty && job.ischar && !string.IsNullOrEmpty(job.npcName))
                {
                    return job.npcName;
                }
            }
        }
        return null;
    }

    
    private void EnableNullChildObject(LittleCGItem item)
    {
        if (item == null) return;
        
        Transform nullTransform = item.transform.Find("Null");
        if (nullTransform != null)
        {
            nullTransform.gameObject.SetActive(true);
        }
    }

    private int CalculateDayReward(int cellIndex)
    {
        int dayReward = 0;
        int dayIndex = cellIndex;
        
        HashSet<int> countedInstanceIds = new HashSet<int>();
        for (int slot = 0; slot < 3; slot++)
        {
            int slotIndex = dayIndex * 3 + slot;
            
            if (slotIndex < actjobs.Length)
            {
                var job = actjobs[slotIndex];
                if (job != null && !job.isEmpty && !countedInstanceIds.Contains(job.instanceId))
                {
                    countedInstanceIds.Add(job.instanceId);
                    int reward = 0;
                    if (job.ischar && !string.IsNullOrEmpty(job.npcName))
                    {
                        int affectionLevel = job.plannedAffectionLevel >= 0 
                            ? job.plannedAffectionLevel 
                            : (AffectionManager.Instance != null ? AffectionManager.Instance.GetAffectionLevel(job.npcName) : 0);
                        int safeLevel = Mathf.Clamp(affectionLevel, 0, job.rewards.Length - 1);
                        reward = job.rewards[safeLevel];
                    }
                    else
                    {
                        JobData originalJob = null;
                        if (_allJobs.TryGetValue(job.name, out originalJob))
                        {
                            int safeLevel = Mathf.Clamp(originalJob.currentLevel, 0, originalJob.rewards.Length - 1);
                            reward = originalJob.rewards[safeLevel];
                        }
                        else
                        {
                            int safeLevel = Mathf.Clamp(job.currentLevel, 0, job.rewards.Length - 1);
                            reward = job.rewards[safeLevel];
                        }
                    }
                    int finalReward = GetFinalReward(reward);
                    dayReward += finalReward;
                }
            }
        }
        
        return dayReward;
    }

    private IEnumerator MovePlayerToPosition(int targetIndex)
    {
        if (playerIcon == null) yield break;
        
        if (targetIndex < 0 || targetIndex >= cgItems.Count) yield break;
        
        if (!playerIcon.gameObject.activeInHierarchy)
        {
            playerIcon.gameObject.SetActive(true);
            yield return null;
        }

        playerIcon.enabled = true;
        Transform targetTransform = cgItems[targetIndex].transform;
        RectTransform targetRect = cgItems[targetIndex].GetComponent<RectTransform>();
        RectTransform playerRect = playerIcon.GetComponent<RectTransform>();
        
        if (playerRect != null && targetRect != null)
        {
            Vector2 targetAnchoredPos = targetRect.anchoredPosition + playerOffset;
            Vector2 targetPivot = targetRect.pivot;
            Vector2 targetAnchorMin = targetRect.anchorMin;
            Vector2 targetAnchorMax = targetRect.anchorMax;
            
            Transform originalParent = playerRect.parent;
            
            playerRect.SetParent(targetRect.parent);
            playerRect.pivot = targetPivot;
            playerRect.anchorMin = targetAnchorMin;
            playerRect.anchorMax = targetAnchorMax;
            
            Vector2 startPos = playerRect.anchoredPosition;
            
            playerRect.SetParent(originalParent);
            
            float elapsed = 0;
            while (elapsed < playerMoveDuration)
            {
                elapsed += Time.deltaTime;
                playerRect.anchoredPosition = Vector2.Lerp(startPos, targetAnchoredPos, elapsed / playerMoveDuration);
                yield return null;
            }
            
            playerRect.anchoredPosition = targetAnchoredPos;
            yield break;
        }

        Vector3 targetWorldPos = targetTransform.position;
        targetWorldPos.z = playerIcon.transform.position.z;
        
        if (currentProgress == 0)
        {
            playerIcon.transform.position = targetWorldPos;
        }
        else
        {
            Vector3 startPos = playerIcon.transform.position;
            float elapsed = 0;

            while (elapsed < playerMoveDuration)
            {
                elapsed += Time.deltaTime;
                playerIcon.transform.position = Vector3.Lerp(startPos, targetWorldPos, elapsed / playerMoveDuration);
                yield return null;
            }

            playerIcon.transform.position = targetWorldPos;
        }
    }

    private void UpdatePlayerAnimation()
    {
        if (playerIcon == null || !playerIcon.enabled)
            return;

        playerAnimTimer += Time.deltaTime;
        if (playerAnimTimer >= playerAnimInterval)
        {
            playerAnimTimer = 0;
            currentPlayerSpriteIndex = (currentPlayerSpriteIndex + 1) % 3;

            if (playerSprites[currentPlayerSpriteIndex] != null)
            {
                playerIcon.sprite = playerSprites[currentPlayerSpriteIndex];
            }
        }
    }

    private IEnumerator MonitorAndRestoreWorkBgmCoroutine()
    {
        while (true)
        {
            if (SoundsManager.Instance != null && !string.IsNullOrEmpty(workBgmPath))
            {
                string currentBgm = SoundsManager.Instance.CurrentMusicPath;
                bool isPlaying = SoundsManager.Instance.IsMusicPlaying;
                
                if (currentBgm != workBgmPath)
                {
                    SoundsManager.Instance.PlayMusic(workBgmPath);
                    yield return null;
                }
                else
                {
                    if (!isPlaying)
                    {
                        SoundsManager.Instance.PlayMusic();
                        yield return null;
                    }
                }
            }
            yield return new WaitForSeconds(0.5f);
        }
    }
    

    void FinishWork()
    {
        StopWorkBgm();
        
        if (ShowMapButton.instance != null && ShowMapButton.instance.isShow)
        {
            ShowMapButton.instance.ShowMap();
        }
        
        currentMoney += totalReward;
        currentMoney -= 1000;
        currentWeek++;
        if (currentWeek > 6 && DemoProgressTracker.Instance != null)
        {
            DemoProgressTracker.Instance.TriggerEnd();
        }
        totalDays += 7-usedDaysInWeek;
        usedDaysInWeek = 0;

        currentDay = (DayOfWeek)6;
        currentTimeSlot = TimeSlot.Morning;
        totalApUsed = 0;
        totalChar = 0;
        System.Array.Clear(actjobs, 0, actjobs.Length);

        if (workUI != null)
        {
            workUI.SetActive(false);
        }
        if (workCGUI != null)
        {
            workCGUI.SetActive(false);
        }
        if (jobDetailPanel != null)
        {
            jobDetailPanel.SetActive(false);
        }

        JobListManager.Nextweek();
        jobLogManager.updatelist(actjobs);
        
        // 保存当前周的运势用于结算面板显示
        settledLuck = currentLuck;
        
        RandomWeekLuck();
        if (PrayLuck.Instance != null)
        {
            PrayLuck.Instance.ResetWeeklyState();
        }

        ShowSettlementPanel();
        updateUI();

        // 周数变化，刷新感叹号状态
        if (StoryDotManager.Instance != null)
            StoryDotManager.Instance.NotifyStateChanged();
    }
    #endregion

    #region 打工动画辅助方法
    void LoadSmallImagesForJob(string imageName, int index)
    {
        currentLeftSpriteName = $"打工cg贴纸-{imageName}-{index}";
        currentRightSpriteName = $"打工表情-{imageName}-{index}";

        if (leftSmallImage != null)
        {
            Sprite leftSprite = GetSmallSprite(currentLeftSpriteName);
            leftSmallImage.sprite = leftSprite;
            leftSmallImage.enabled = leftSprite != null;
        }

        if (rightSmallImage != null)
        {
            Sprite rightSprite = GetSmallSprite(currentRightSpriteName);
            rightSmallImage.sprite = rightSprite;
            rightSmallImage.enabled = rightSprite != null;
        }
    }

    private Sprite GetSmallSprite(string spriteName)
    {
        if (_smallSpriteDict == null)
        {
            InitSmallImageDicts();
        }

        if (_smallSpriteDict != null && _smallSpriteDict.TryGetValue(spriteName, out Sprite sprite))
        {
            return sprite;
        }
        
        return null;
    }

    private void InitSmallImageDicts()
    {
        if (_smallSpriteDict != null) return;

        _smallSpriteDict = new Dictionary<string, Sprite>();

        Sprite[] leftSprites = Resources.LoadAll<Sprite>("WorkIcon/WorkLabel_view_atlas");
        Sprite[] rightSprites = Resources.LoadAll<Sprite>("WorkIcon/WorkHead_view_atlas");

        foreach (var sprite in leftSprites)
        {
            if (!_smallSpriteDict.ContainsKey(sprite.name))
            {
                _smallSpriteDict.Add(sprite.name, sprite);
            }
        }

        foreach (var sprite in rightSprites)
        {
            if (!_smallSpriteDict.ContainsKey(sprite.name))
            {
                _smallSpriteDict.Add(sprite.name, sprite);
            }
        }
    }

    System.Collections.IEnumerator AnimateSmallImages()
    {
        float timer = 0;
        float[] angles = new float[] { 20f, -20f };
        int currentIndex = 0;
        float holdDuration = 0.3f;

        while (true)
        {
            timer += Time.deltaTime;

            if (timer >= holdDuration)
            {
                currentIndex = (currentIndex + 1) % angles.Length;
                timer = 0;

                float angle = angles[currentIndex];

                if (leftSmallImage != null && leftSmallImage.enabled)
                {
                    leftSmallImage.transform.localRotation = Quaternion.Euler(0, 0, angle);
                }
                if (rightSmallImage != null && rightSmallImage.enabled)
                {
                    rightSmallImage.transform.localRotation = Quaternion.Euler(0, 0, -angle);
                }
            }

            yield return new WaitForFixedUpdate();
        }
    }

    System.Collections.IEnumerator FadeIn(GameObject obj, float duration)
    {
        CanvasGroup canvasGroup = obj.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = obj.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 0;
        float elapsed = 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = 1;
    }

    System.Collections.IEnumerator FadeOut(GameObject obj, float duration)
    {
        CanvasGroup canvasGroup = obj.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = obj.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 1;
        float elapsed = 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = 1 - Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = 0;
    }
    #endregion

    #region 打工升级与报酬
    void LevelUp(JobData job)
    {
        if (job.currentLevel < 3)
        {
            job.currentLevel++;
            levelUpJobs.Add(job.name);
        }
    }

    int GetFinalReward(int baseReward)
    {
        int finalReward = currentLuck switch
        {
            LuckType.吉 => Mathf.RoundToInt(baseReward * 1.2f),
            LuckType.晦 => Mathf.RoundToInt(baseReward * 0.8f),
            _ => baseReward
        };
        Debug.Log($"[JobManager] GetFinalReward: 基础奖励={baseReward}, 运势={currentLuck}, 最终奖励={finalReward}");
        return finalReward;
    }
    #endregion

    #region 打工详情
    public void ShowJobDetail(JobData job)
    {
        if (jobDetailPanel == null) return;

        // 角色打工使用好感度等级，普通打工使用currentLevel
        int displayLevel;
        int displayReward;
        if (job.ischar && !string.IsNullOrEmpty(job.npcName))
        {
            displayLevel = AffectionManager.Instance != null 
                ? AffectionManager.Instance.GetAffectionLevel(job.npcName) 
                : 0;
            int safeLevel = Mathf.Clamp(displayLevel, 0, job.rewards.Length - 1);
            displayReward = job.rewards[safeLevel];
        }
        else
        {
            displayLevel = job.currentLevel;
            displayReward = job.rewards[job.currentLevel];
        }

        jobDetailTitle.text = job.name;
        jobDetailText.text = $"工时：{job.apCost}\n" +
                            $"等级：{GetJobLevelName(displayLevel)}\n" +
                            $"薪资：${displayReward}";

        if (jobDetailDescription != null)
        {
            if (!string.IsNullOrEmpty(job.Description))
            {
                jobDetailDescription.text = job.Description;
                jobDetailDescription.gameObject.SetActive(true);
            }
            else
            {
                jobDetailDescription.gameObject.SetActive(false);
            }
        }

        if (jobDetailCondition != null)
        {
            string conditionText = GetJobConditionText(job);
            if (!string.IsNullOrEmpty(conditionText))
            {
                jobDetailCondition.text = conditionText;
                jobDetailCondition.gameObject.SetActive(true);
                if (jobDetailConditionBg != null)
                {
                    int lineCount = conditionText.Split('\n').Length;
                    string bgPath = $"WorkIcon/打工条件-{GetChineseNumber(lineCount)}行";
                    Sprite bgSprite = Resources.Load<Sprite>(bgPath);
                    if (bgSprite != null)
                    {
                        jobDetailConditionBg.sprite = bgSprite;
                        jobDetailConditionBg.enabled = true;
                        jobDetailConditionBg.gameObject.SetActive(true);
                    }
                    else
                    {
                        jobDetailConditionBg.enabled = false;
                        jobDetailConditionBg.gameObject.SetActive(false);
                    }
                }
            }
            else
            {
                jobDetailCondition.gameObject.SetActive(false);
                if (jobDetailConditionBg != null)
                {
                    jobDetailConditionBg.enabled = false;
                    jobDetailConditionBg.gameObject.SetActive(false);
                }
            }
        }

        if (jobDetailImage != null)
        {
            string imageIndex = job.ischar && !string.IsNullOrEmpty(job.npcName) ? job.npcName : job.imageName;
            if (!string.IsNullOrEmpty(imageIndex))
            {
                Sprite sprite = Resources.Load<Sprite>("WorkIcon/打工名片-" + imageIndex);
                if (sprite != null)
                {
                    jobDetailImage.sprite = sprite;
                    jobDetailImage.enabled = true;
                    jobDetailImage.gameObject.SetActive(true);
                    
                    RectTransform rectTransform = jobDetailImage.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        if (job.ischar)
                        {
                            rectTransform.sizeDelta = new Vector2(203, 215);
                        }
                        else
                        {
                            rectTransform.sizeDelta = jobDetailImageOriginalSize;
                        }
                    }
                }
                else
                {
                    jobDetailImage.enabled = false;
                    jobDetailImage.gameObject.SetActive(false);
                }
            }
            else
            {
                jobDetailImage.enabled = false;
                jobDetailImage.gameObject.SetActive(false);
            }
        }

        if (jobDetailExpressionImage != null)
        {
            if (!string.IsNullOrEmpty(job.imageName))
            {
                string spriteName = $"打工表情-{job.imageName}-1";
                Sprite expressionSprite = GetSmallSprite(spriteName);
                if (expressionSprite != null)
                {
                    jobDetailExpressionImage.sprite = expressionSprite;
                    jobDetailExpressionImage.enabled = true;
                    jobDetailExpressionImage.gameObject.SetActive(true);
                }
                else
                {
                    jobDetailExpressionImage.enabled = false;
                    jobDetailExpressionImage.gameObject.SetActive(false);
                }
            }
            else
            {
                jobDetailExpressionImage.enabled = false;
                jobDetailExpressionImage.gameObject.SetActive(false);
            }
        }

        jobDetailPanel.SetActive(true);
    }

    public void HideJobDetail()
    {
    }

    private string GetChineseNumber(int number)
    {
        string[] chineseNumbers = { "", "一", "二", "三", "四", "五", "六", "七", "八", "九", "十" };
        if (number <= 0 || number >= chineseNumbers.Length)
        {
            return number.ToString();
        }
        return chineseNumbers[number];
    }

    private string GetJobConditionText(JobData job)
    {
        System.Text.StringBuilder conditions = new System.Text.StringBuilder();

        if (job.ischar)
        {
            conditions.AppendLine("仅能从白天开始");
            conditions.Append("每周限3次");
            return conditions.ToString().Trim();
        }

        switch (job.timeType)
        {
            case TimeType.NightOnly:
                conditions.AppendLine("仅能从晚上开始");
                break;
            case TimeType.DayOnly:
                conditions.AppendLine("仅能从白天开始");
                break;
            case TimeType.SundayOnly:
                conditions.AppendLine("仅周日可进行");
                break;
            case TimeType.MorningOnly:
                conditions.AppendLine("仅能从上午开始");
                break;
            case TimeType.AfternoonOnly:
                conditions.AppendLine("仅能从下午开始");
                break;
        }

        if (job.cdVal > 0)
        {
            conditions.AppendLine($"每周限{job.cdVal}次");
            conditions.Append("无法连续进行");
        }

        return conditions.ToString().Trim();
    }

    private string GetJobLevelName(int level)
    {
        return level switch
        {
            0 => "新手",
            1 => "熟手",
            2 => "老手",
            3 => "大师",
            _ => "未知等级"
        };
    }
    #endregion

    #region UI更新
    void updateUI()
    {
        if (moneyText != null)
        {
            moneyText.text = $"现金/{currentMoney:N0}\n债务/{totalDebt:N0}";
        }

        if (weekText != null)
        {
            string dayOfWeekStr = GetDayOfWeekString(currentDay);
            weekText.text = $"第{currentWeek}周/（{dayOfWeekStr}）";
        }

        if (monthText != null)
        {
            System.DateTime currentDate = startDate.AddDays(totalDays);
            monthText.text = $"{currentDate.Month}月{currentDate.Day}日";
        }
    }

    private string GetDayOfWeekString(DayOfWeek day)
    {
        switch (day)
        {
            case DayOfWeek.Sun: return "日";
            case DayOfWeek.Mon: return "一";
            case DayOfWeek.Tue: return "二";
            case DayOfWeek.Wed: return "三";
            case DayOfWeek.Thu: return "四";
            case DayOfWeek.Fri: return "五";
            case DayOfWeek.Sat: return "六";
            default: return "日";
        }
    }

    void ShowSettlementPanel()
    {
        if (SettlementPanel == null) return;

        settlementTitleText.text = $"结算--第{currentWeek - 1}周";
        livingCostText.text = "生活费：-$1000";

        // 获取运势显示文本
        string luckText = settledLuck switch
        {
            LuckType.吉 => "（吉）",
            LuckType.晦 => "（晦）",
            _ => "（平）"
        };
        rewardText.text = $"总收入：+${totalReward:N0} {luckText}";
        remainingMoneyText.text = $"剩余：${currentMoney:N0}";

        if (levelUpJobs.Count > 0)
        {
            levelUpText.text = "升级：" + string.Join(", ", levelUpJobs);
        }
        else
        {
            levelUpText.text = "本周无升级";
        }

        SettlementPanel.SetActive(true);
    }
    #endregion

    #region 打工判断逻辑
    private int FindFirstValidSlot(JobData job)
    {
        int requiredSlots = job.apCost;
        int slotIndex = 0;
        
        while (slotIndex < actjobs.Length)
        {
            if (actjobs[slotIndex] == null)
            {
                int dayIndex = slotIndex / 3;
                bool hasEmptyJobInDay = false;
                for (int j = dayIndex * 3; j < dayIndex * 3 + 3 && j < actjobs.Length; j++)
                {
                    if (actjobs[j] != null && actjobs[j].isEmpty)
                    {
                        hasEmptyJobInDay = true;
                        break;
                    }
                }
                
                if (hasEmptyJobInDay)
                {
                    slotIndex = (dayIndex + 1) * 3;
                    continue;
                }
                
                int dayProgress = slotIndex / 3;
                int dayIndexInWeek = dayProgress % 7;
                TimeSlot slotTime = (TimeSlot)(slotIndex % 3);
                
                bool timeMatch;
                if (job.ischar)
                {
                    timeMatch = slotTime == TimeSlot.Morning;
                }
                else
                {
                    timeMatch = job.timeType switch
                    {
                        TimeType.AllDay => true,
                        TimeType.DayOnly => slotTime is TimeSlot.Morning or TimeSlot.Afternoon,
                        TimeType.NightOnly => slotTime == TimeSlot.Night,
                        TimeType.SundayOnly => dayIndexInWeek == 0,
                        TimeType.MorningOnly => slotTime == TimeSlot.Morning,
                        TimeType.CharOnly => slotTime == TimeSlot.Morning,
                        TimeType.AfternoonOnly => slotTime == TimeSlot.Afternoon,
                        _ => true
                    };
                }
                
                if (!timeMatch)
                {
                    slotIndex++;
                    continue;
                }
                
                int availableSlots = 0;
                for (int i = slotIndex; i < actjobs.Length && availableSlots < requiredSlots; i++)
                {
                    int checkDayIndex = i / 3;
                    bool checkHasEmptyJob = false;
                    for (int j = checkDayIndex * 3; j < checkDayIndex * 3 + 3 && j < actjobs.Length; j++)
                    {
                        if (actjobs[j] != null && actjobs[j].isEmpty)
                        {
                            checkHasEmptyJob = true;
                            break;
                        }
                    }
                    
                    if (checkHasEmptyJob)
                    {
                        break;
                    }
                    
                    if (actjobs[i] == null)
                    {
                        availableSlots++;
                    }
                    else
                    {
                        break;
                    }
                }
                
                if (availableSlots >= requiredSlots)
                {
                    return slotIndex;
                }
                
                slotIndex++;
            }
            else
            {
                slotIndex++;
            }
        }
        
        return -1;
    }
    
    public bool CanJobBeTaken(JobData job)
    {
        int firstValidSlot = FindFirstValidSlot(job);
        if (firstValidSlot < 0) return false;

        if (job.ischar)
        {
            if (totalChar >= 3) return false;
            if (!string.IsNullOrEmpty(job.unlockDialogKeyword))
            {
                if (!DialogKeywordDetector.IsKeywordUnlocked(job.unlockDialogKeyword))
                {
                    return false;
                }
            }
            return true;
        }

        if (job.cdVal > 0 && job.curcd >= job.cdVal) return false;

        if (job.cdVal > 0 && job.timeType != TimeType.SundayOnly)
        {
            if (firstValidSlot > 0)
            {
                var prevJob = actjobs[firstValidSlot - 1];
                if (prevJob != null && prevJob.name == job.name)
                {
                    return false;
                }
            }
        }

        return true;
    }

    public void UpdateSwitchButCueState()
    {
        bool hasUnclickedNormalCue = false;
        bool hasUnclickedCharCue = false;
        bool hasUnclickedCue = false;
        
        if (JobListManager.Instance != null)
        {
            hasUnclickedNormalCue = JobListManager.Instance.HasUnclickedCueInJobs();
            hasUnclickedCharCue = JobListManager.Instance.HasUnclickedCueInCharJobs();
        }
        
        hasUnclickedCue = hasUnclickedNormalCue || hasUnclickedCharCue;
        
        if (SwitchButCue != null)
        {
            bool shouldShowSwitchCue = hasUnclickedCue;
            if (!_isFirstSwitchButCueShown)
            {
                shouldShowSwitchCue = true;
            }
            
            SwitchButCue.SetActive(shouldShowSwitchCue);
        }
        
        if (normalButCue != null)
        {
            normalButCue.SetActive(hasUnclickedNormalCue);
        }
        
        if (charWorkButCue != null)
        {
            charWorkButCue.SetActive(hasUnclickedCharCue);
        }
    }
    #endregion

    #region 运势逻辑
    /// <summary>
    /// 重置本周运势为默认值"平"
    /// </summary>
    public void RandomWeekLuck()
    {
        currentLuck = LuckType.平;
        Debug.Log($"[JobManager] 本周运势已重置为默认值: {currentLuck}, 运势索引: {GetLuckIndex()}");
    }

    /// <summary>
    /// 点击雕像后随机抽取运势（从吉、平、晦三个中随机）
    /// </summary>
    public void DrawLuckAfterPray()
    {
        System.Array lucks = System.Enum.GetValues(typeof(LuckType));
        currentLuck = (LuckType)lucks.GetValue(Random.Range(0, lucks.Length));
        Debug.Log($"[JobManager] 祈祷后抽取运势: {currentLuck}, 运势索引: {GetLuckIndex()}");
    }

    /// <summary>
    /// 根据运势返回对应的CG小图标索引
    /// 吉 -> 1, 平 -> 2, 晦 -> 3
    /// </summary>
    private int GetLuckIndex()
    {
        return currentLuck switch
        {
            LuckType.吉 => 1,
            LuckType.平 => 2,
            LuckType.晦 => 3,
            _ => 2
        };
    }

    public void ShowPrayTip(System.Action onComplete = null)
    {
        luckTipPanel.SetActive(true);
        StartCoroutine(ShowPrayTipCoroutine(onComplete));
    }

    private IEnumerator ShowPrayTipCoroutine(System.Action onComplete)
    {
        luckText.text = "划过十字，你闭上眼，十指交扣，在圣母像前祷告。";
        yield return new WaitForSeconds(2f);

        // 点击雕像后随机抽取运势
        DrawLuckAfterPray();

        string luckDesc = currentLuck switch
        {
            LuckType.吉 => "本周运势：大吉（报酬+20%）",
            LuckType.晦 => "本周运势：晦气（报酬-20%）",
            _ => "本周运势：平（报酬不变）"
        };

        Debug.Log($"[JobManager] 显示运势提示: {currentLuck} -> {luckDesc}");

        luckText.text = luckDesc;
        yield return new WaitForSeconds(2f);

        luckTipPanel.SetActive(false);
        onComplete?.Invoke();
    }
    #endregion

    #region 空打工测试
    void AddEmptyJop(int count)
    {
        for (int i = 0; i < count && i < actjobs.Length; i++)
        {
            if (actjobs[i] == null)
            {
                JobData emptyJob = new JobData
                    {
                        name = "空打工",
                        isEmpty = true,
                        apCost = 1,
                        imageName = "",
                        instanceId = ++_instanceIdCounter
                    };
                actjobs[i] = emptyJob;
            }
        }
        jobLogManager.updatelist(actjobs);
        updateUI();
    }

    public void SetDayJobsEmpty(int dayIndex)
    {
        if (dayIndex < 0 || dayIndex > 6) return;

        int startSlot = dayIndex * 3;
        for (int i = 0; i < 3; i++)
        {
            int slotIndex = startSlot + i;
            if (slotIndex < actjobs.Length)
            {
                JobData emptyJob = new JobData
                    {
                        name = "空打工",
                        isEmpty = true,
                        apCost = 1,
                        imageName = "",
                        instanceId = ++_instanceIdCounter
                    };
                actjobs[slotIndex] = emptyJob;
            }
        }
        
        jobLogManager.updatelist(actjobs);
    }


    public void AdvanceTimeByDays(int days)
    {
        if (days <= 0) return;

        int apToAdd = days * 3;
        totalApUsed += apToAdd;
        totalDays += days;

        RefreshTimeFromAp();
        updateUI();
    }
    
    public void AdvanceOneDayAndSetEmpty()
    {
        usedDaysInWeek++;
        int dayIndex = ((int)currentDay + 1) % 7;
        int startSlot = dayIndex * 3;

        JobData emptyJob = new JobData
        {
            name = "拜访",
            isEmpty = true,
            cannotBeRemoved = true,
            apCost = 3,
            imageName = "",
            instanceId = ++_instanceIdCounter
        };

        for (int i = 0; i < 3; i++)
        {
            int slotIndex = startSlot + i;
            if (slotIndex < actjobs.Length)
            {
                actjobs[slotIndex] = emptyJob;
            }
        }
        
        AdvanceTimeByDays(1);

        jobLogManager.updatelist(actjobs);
        updateUI();

        // 天数变化，刷新感叹号状态
        if (StoryDotManager.Instance != null)
            StoryDotManager.Instance.NotifyStateChanged();
    }

    int FindFirstAvailable3APSlot()
    {
        for (int dayIndex = 0; dayIndex < 7; dayIndex++)
        {
            int startSlot = dayIndex * 3;
            
            bool isDayEmpty = true;
            for (int i = 0; i < 3; i++)
            {
                int slotIndex = startSlot + i;
                if (slotIndex >= actjobs.Length || actjobs[slotIndex] != null)
                {
                    isDayEmpty = false;
                    break;
                }
            }
            
            if (isDayEmpty)
            {
                return startSlot;
            }
        }
        
        return -1;
    }
    
    #endregion

    #region 存档支持方法
    /// <summary>
    /// 获取活动打工数组（供存档系统使用）
    /// </summary>
    public JobData[] GetActJobs()
    {
        return actjobs;
    }

    /// <summary>
    /// 清空活动打工数组（供存档系统使用）
    /// </summary>
    public void ClearActJobs()
    {
        System.Array.Clear(actjobs, 0, actjobs.Length);
    }

    /// <summary>
    /// 设置指定槽位的打工（供存档系统使用）
    /// </summary>
    public void SetActJob(int slotIndex, JobData job, int instanceId)
    {
        if (slotIndex >= 0 && slotIndex < actjobs.Length)
        {
            job.instanceId = instanceId;
            actjobs[slotIndex] = job;
        }
    }

    /// <summary>
    /// 获取本周已使用天数（供存档系统使用）
    /// </summary>
    public int GetUsedDaysInWeek()
    {
        return usedDaysInWeek;
    }

    /// <summary>
    /// 设置本周已使用天数（供存档系统使用）
    /// </summary>
    public void SetUsedDaysInWeek(int value)
    {
        usedDaysInWeek = value;
    }

    /// <summary>
    /// 同步保存的状态变量（供存档系统使用）
    /// 防止关闭面板时恢复到错误的状态
    /// </summary>
    public void SyncSavedState()
    {
        _savedDay = currentDay;
        _savedTimeSlot = currentTimeSlot;
        _savedTotalApUsed = totalApUsed;
    }

    /// <summary>
    /// 重新计算角色打工计数器（供存档系统使用）
    /// 因为 SetActJob 不会更新 totalChar，需要在恢复槽位后调用
    /// </summary>
    public void RecalculateTotalChar()
    {
        totalChar = 0;
        foreach (var job in actjobs)
        {
            if (job != null && job.ischar)
            {
                totalChar++;
            }
        }
        Debug.Log($"[JobManager] 重新计算 totalChar={totalChar}");
    }

    /// <summary>
    /// 刷新打工列表UI（供存档系统使用）
    /// </summary>
    public void RefreshJobLogUI()
    {
        if (jobLogManager != null)
        {
            jobLogManager.updatelist(actjobs);
        }
        updateUI();
    }
    #endregion

    #region 测试功能
    public void TestPlayAllSmallTalks()
    {
        if (SmallTalkManager.Instance == null) return;

        StartCoroutine(TestPlayAllSmallTalksCoroutine());
    }

    private IEnumerator TestPlayAllSmallTalksCoroutine()
    {
        HashSet<string> npcNames = new HashSet<string>();
        foreach (var job in _allcharJobs.Values)
        {
            if (!string.IsNullOrEmpty(job.npcName))
            {
                npcNames.Add(job.npcName);
            }
        }

        if (npcNames.Count == 0) yield break;

        int total = 0;
        foreach (string npcName in npcNames)
        {
            for (int i = 1; i <= 2; i++)
            {
                string smallTalkName = $"{npcName}-对话{i}";

                bool finished = false;
                total++;
                bool isLast = (total == npcNames.Count * 2);
                yield return StartCoroutine(SmallTalkManager.Instance.PlayCharacterSideStory(smallTalkName, isLast));
                finished = true;
            }
        }
    }
    #endregion

    /// <summary>
    /// 重置静态状态（由 StaticStateResetManager 在存读档时调用）
    /// </summary>
    public static void ResetStaticState()
    {
        _smallSpriteDict = null;  // 设为 null，让下次使用时重新加载
        _instanceIdCounter = 0;
        Debug.Log("[JobManager] 静态状态已重置: _smallSpriteDict=null, _instanceIdCounter=0");
    }
}
