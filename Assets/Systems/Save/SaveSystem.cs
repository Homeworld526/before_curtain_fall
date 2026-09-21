using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using GameSystem;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 存档数据基类 - 所有需要保存的数据都应继承此类
/// </summary>
[System.Serializable]
public abstract class SaveData
{
    public string SaveVersion = "1.0";
}

/// <summary>
/// 玩家存档数据示例
/// </summary>
[System.Serializable]
public class PlayerSaveData : SaveData
{
    public int dialogIndex = 0;
    public int dialogLine = 0;
    public bool isInDialog = false;
    public int dialogStartLine = 0;
    public int currentWeek;
    public int totalDays;
}



[System.Serializable]
public class UnlockedKeywordData
{
    public string unlockKey;
    public bool isUnlocked;
}



[System.Serializable]
public class ActiveDialogTriggerSaveData
{
    public string characterName = "";
    public int dialogIndex = -1;
}

/// <summary>
/// 打工数据存档 - 用于保存打工等级、经验、冷却等运行时数据
/// </summary>
[System.Serializable]
public class JobSaveData
{
    public string jobId;           // 打工ID（使用name作为标识）
    public int currentLevel;       // 当前等级
    public int exp;                // 经验值
    public int curcd;              // 当前冷却
    public bool hasClickedCue;     // 是否已点击过Cue（红点提示）
}

/// <summary>
/// 活动打工槽位数据 - 用于保存actjobs数组状态
/// </summary>
[System.Serializable]
public class ActiveJobSlotData
{
    public int slotIndex;          // 槽位索引
    public string jobName;         // 打工名称
    public bool isChar;            // 是否角色打工
    public bool isEmpty;           // 是否空打工（拜访）
    public int instanceId;         // 实例ID
}

/// <summary>
/// 角色关键词触发数据 - 用于保存人物界面装饰点亮状态
/// </summary>
[System.Serializable]
public class TriggeredCharKeywordData
{
    public string characterName;           // 角色名
    public List<string> triggeredKeywords; // 已触发的关键词列表
}

/// <summary>
/// 完整存档数据 - 包含所有模块的存档数据
/// </summary>
[System.Serializable]
public class CompleteSaveData
{
    public string SaveTime;
    public int SaveSlotIndex;
    public PlayerSaveData PlayerData;
    public string Pic;
    public List<UnlockedKeywordData> UnlockedKeywords = new List<UnlockedKeywordData>();
    public List<string> CompletedStoryIds = new List<string>();
    public long CurrentMoney;
    public long TotalDebt;
    public List<CharData> Affections = new List<CharData>();
    public List<bool> GameObjectActiveStates = new List<bool>();
    public ActiveDialogTriggerSaveData ActiveDialogTrigger = new ActiveDialogTriggerSaveData();
    public List<DialogStatEntry> DialogStats = new List<DialogStatEntry>();
    public bool GuideCompleted;
    public bool PrologueCompleted;     // 序章是否已完成
    public bool TutorialShown;         // 教学册子是否已弹出过
    public bool HasPrayedThisWeek;     // 本周是否已祈祷
    public List<string> DemoProgressKeys = new List<string>(); // DemoProgressTracker 已完成的剧情 key
    public List<JobSaveData> NormalJobsData = new List<JobSaveData>();    // 普通打工数据
    public List<JobSaveData> CharacterJobsData = new List<JobSaveData>(); // 角色打工数据
    public List<ActiveJobSlotData> ActiveJobSlots = new List<ActiveJobSlotData>(); // 活动打工槽位
    public int TotalApUsed;           // 总行动力使用量
    public int UsedDaysInWeek;        // 本周已使用天数
    public List<TriggeredCharKeywordData> TriggeredCharKeywords = new List<TriggeredCharKeywordData>(); // 角色关键词触发状态（装饰点亮）

    public CompleteSaveData()
    {
        SaveTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        PlayerData = new PlayerSaveData();
        UnlockedKeywords = new List<UnlockedKeywordData>();
        Affections = new List<CharData>();
        NormalJobsData = new List<JobSaveData>();
        CharacterJobsData = new List<JobSaveData>();
        ActiveJobSlots = new List<ActiveJobSlotData>();
        TriggeredCharKeywords = new List<TriggeredCharKeywordData>();
    }
}

/// <summary>
/// 存档系统
/// </summary>
public class SaveSystem : SingleCase<SaveSystem>
{
    public Transform Dialog;
    public List<GameObject> TrackedGameObjects = new List<GameObject>();
    public bool WaitForSaveBlockToClear;

    // 存档文件路径
    private string saveFolderPath;
    private string screenshotsFolderPath;

    public Canvas DialogCanvas;
    public CanvasGroup SettingsCanvas;
    
    // 当前加载的存档数据
    private CompleteSaveData currentSaveData;
    
    // 最大存档槽位数
    [SerializeField] private int maxSaveSlots = 18;
    
    // 存档文件夹名称
    private const string SAVE_FOLDER_NAME = "Saves";

    // 存档文件扩展名
    private const string SAVE_FILE_EXTENSION = ".json";

    // 截图文件夹名称
    private const string SCREENSHOTS_FOLDER_NAME = "Screenshots";

    // 截图目标分辨率
    private const int SCREENSHOT_WIDTH = 480;
    private const int SCREENSHOT_HEIGHT = 270;
    private const int SCREENSHOT_JPEG_QUALITY = 75;

    private void Start()
    {
        InitializeSaveFolder();
        InitializeScreenshotsFolder();
        
#if UNITY_EDITOR
        // 编辑器模式下注册播放模式状态变化事件
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
    }
    
    protected override void OnDestroy()
    {
#if UNITY_EDITOR
        // 清理事件注册
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
#endif
        base.OnDestroy();
    }
    
#if UNITY_EDITOR
    /// <summary>
    /// 编辑器播放模式状态变化时调用
    /// </summary>
    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        // 当从播放模式切换到编辑模式时（停止播放）
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            Debug.Log("[SaveSystem] 编辑器模式下停止播放，删除所有存档");
            DeleteAllSaves();
        }
    }
#endif

    /// <summary>
    /// 初始化存档文件夹
    /// </summary>
    private void InitializeSaveFolder()
    {
        saveFolderPath = Path.Combine(Application.persistentDataPath, SAVE_FOLDER_NAME);
        if (!Directory.Exists(saveFolderPath))
        {
            Directory.CreateDirectory(saveFolderPath);
            Debug.Log($"[SaveSystem] 创建存档文件夹: {saveFolderPath}");
        }
    }


    
    /// <summary>
    /// 初始化截图文件夹
    /// </summary>
    private void InitializeScreenshotsFolder()
    {
        screenshotsFolderPath = Path.Combine(Application.persistentDataPath, SCREENSHOTS_FOLDER_NAME);
        if (!Directory.Exists(screenshotsFolderPath))
        {
            Directory.CreateDirectory(screenshotsFolderPath);
            Debug.Log($"[SaveSystem] 创建截图文件夹: {screenshotsFolderPath}");
        }
    }

    /// <summary>
    /// 获取存档文件路径
    /// </summary>
    private string GetSaveFilePath(int slotIndex)
    {
        return Path.Combine(saveFolderPath, $"SaveSlot_{slotIndex}{SAVE_FILE_EXTENSION}");
    }

    private string GetDialogScenePath(int slotIndex)
    {
        return Path.Combine(saveFolderPath, $"DialogScene_{slotIndex}.json");
    }

    /// <summary>
    /// 检查存档是否存在
    /// </summary>
    public bool IsSaveExists(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= maxSaveSlots)
        {
            Debug.LogError($"[SaveSystem] 存档槽位无效: {slotIndex}");
            return false;
        }
        return File.Exists(GetSaveFilePath(slotIndex));
    }

    /// <summary>
    /// 获取所有存档信息
    /// </summary>
    public List<SaveInfo> GetAllSaveInfos()
    {
        List<SaveInfo> saveInfos = new List<SaveInfo>();
        for (int i = 0; i < maxSaveSlots; i++)
        {
            if (IsSaveExists(i))
            {
                CompleteSaveData saveData = LoadFromFile(i);
                if (saveData != null)
                {
                    // 将相对路径转换为完整文件路径
                    string fullPicPath = string.IsNullOrEmpty(saveData.Pic) 
                        ? "" 
                        : Path.Combine(Application.persistentDataPath, saveData.Pic);

                    saveInfos.Add(new SaveInfo
                    {
                        SlotIndex = i,
                        SaveTime = saveData.SaveTime,
                        Pic = fullPicPath,
                        currentWeek = saveData.PlayerData.currentWeek,
                        dialogIndex = saveData.PlayerData.isInDialog ? saveData.PlayerData.dialogIndex : -1,
                    });
                }
            }
            else
            {
                saveInfos.Add(new SaveInfo
                {
                    SlotIndex = i,
                    SaveTime = "",
                    Pic = "",
                });
            }
        }
        return saveInfos;
    }

    public Coroutine saveRoutine;
    
    /// <summary>
    /// 保存游戏
    /// </summary>
    public void SaveGame(int slotIndex, System.Action<bool, int> onComplete = null)
    {
        if (DialogVisual.Instance != null) DialogVisual.Instance.clickDelay++;
        
        if (slotIndex < 0 || slotIndex >= maxSaveSlots)
        {
            Debug.LogError($"[SaveSystem] 存档槽位无效: {slotIndex}");
            onComplete?.Invoke(false, slotIndex);
            return;
        }

        saveRoutine = StartCoroutine(SaveGameCoroutine(slotIndex, onComplete));
    }

    /// <summary>
    /// 保存游戏协程 - 包含截图
    /// </summary>
    private IEnumerator SaveGameCoroutine(int slotIndex, System.Action<bool,int> onComplete)
    {
        if (slotIndex < 0 || slotIndex >= maxSaveSlots)
        {
            Debug.LogError($"[SaveSystem] 存档槽位无效: {slotIndex}");
            onComplete?.Invoke(false, slotIndex);
            yield break;
        }

        yield return new WaitUntil(() => !WaitForSaveBlockToClear &&
            (DialogVisual.Instance == null || !DialogVisual.Instance.ForceNoClickSkip));

        // 如果没有当前存档数据，创建新的
        if (currentSaveData == null)
        {
            currentSaveData = new CompleteSaveData();
        }

        // 更新存档槽位和时间
        currentSaveData.SaveSlotIndex = slotIndex;
        currentSaveData.SaveTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // 收集需要保存的数据
        CollectSaveData();

        // 截取屏幕截图（在try块外执行）
        bool screenshotCompleted = false;
        string screenshotPath = null;
        
        yield return StartCoroutine(CaptureScreenshot(slotIndex, (path) => {
            screenshotPath = path;
            screenshotCompleted = true;
        }));

        // 等待截图完成
        while (!screenshotCompleted)
        {
            yield return null;
        }
        
        currentSaveData.Pic = screenshotPath;

        try
        {
            // 序列化并保存
            string json = JsonUtility.ToJson(currentSaveData, true);
            string filePath = GetSaveFilePath(slotIndex);
            File.WriteAllText(filePath, json);

            DialogSerializer.SaveRuntimeScene(GetDialogScenePath(slotIndex));

            Debug.Log($"[SaveSystem] 存档成功 - 槽位: {slotIndex}, 时间: {currentSaveData.SaveTime}");
            onComplete?.Invoke(true, slotIndex);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveSystem] 存档失败: {e.Message}");
            onComplete?.Invoke(false, slotIndex);
        }
        
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        
        saveRoutine = null;
    }
    

    /// <summary>
    /// 加载游戏
    /// </summary>
    public bool LoadGame(int slotIndex)
    {
        if(DialogVisual.Instance != null) DialogVisual.Instance.clickDelay++;

        if (slotIndex < 0 || slotIndex >= maxSaveSlots)
        {
            Debug.LogError($"[SaveSystem] 存档槽位无效: {slotIndex}");
            return false;
        }

        if (!IsSaveExists(slotIndex))
        {
            Debug.LogWarning($"[SaveSystem] 存档不存在: 槽位 {slotIndex}");
            return false;
        }

        try
        {
            currentSaveData = LoadFromFile(slotIndex);
            if (currentSaveData == null)
            {
                Debug.LogError($"[SaveSystem] 加载存档失败: 槽位 {slotIndex}");
                return false;
            }

            Debug.Log($"[SaveSystem] 读档成功 - 槽位: {slotIndex}, 时间: {currentSaveData.SaveTime}");

            // 阻止 DialogList.OnEnable 在场景加载期间自动启动对话
            DialogList.SuppressAutoDialog = true;

            // 检查当前场景是否已在 SampleScene
            bool alreadyInSampleScene = SceneManager.GetActiveScene().name == "SampleScene";

            if (!currentSaveData.PlayerData.isInDialog)
            {
                if (alreadyInSampleScene)
                {
                    // 已在 SampleScene，直接应用存档数据
                    Debug.Log("[SaveSystem] 已在 SampleScene，直接应用存档数据");
                    // 先清理静态状态
                    StaticStateResetManager.ResetAllStaticState();
                    // 停止 BlackoutTransition 残留协程，防止回调在读档后重新激活 Dialog
                    if (BlackoutTransition.Instance != null)
                        BlackoutTransition.Instance.ResetState();
                    // 关闭 Dialog GameObject，否则 DialogList.IsInDialog 持续为 true，
                    // ShowMapButton.ShowMap() 会被 isDialogPlaying 拦截导致地图打不开
                    if (DialogLocator.Instance != null && DialogLocator.Instance.Dialog != null)
                        DialogLocator.Instance.Dialog.SetActive(false);
                    if (Dialog != null) Dialog.gameObject.SetActive(false);
                    // 重置所有 DialogTrigger 实例的 isPlaying 状态
                    // 访问角色时 isPlaying=true 且物体会因关闭地图而 SetActive(false)，
                    // ResetStaticState 只清空静态变量 PlayingTrigger，无法重置实例字段 isPlaying，
                    // 必须遍历所有实例（包含 inactive）调用 ResetPlaying，否则该角色将永远点击无反应
                    foreach (var trigger in FindObjectsOfType<DialogTrigger>(true))
                        trigger.ResetPlaying();
                    // 直接应用存档数据
                    ApplyLoadData();
                    DialogList.SuppressAutoDialog = false;
                }
                else
                {
                    // 不在 SampleScene，需要切换场景
                    void OnSceneLoadedNoDialog(Scene scene, LoadSceneMode mode)
                    {
                        SceneManager.sceneLoaded -= OnSceneLoadedNoDialog;
                        SceneCenter.Instance.StartCoroutine(LoadGameNoDialogDelay(slotIndex));
                    }
                    SceneManager.sceneLoaded += OnSceneLoadedNoDialog;
                    SceneCenter.Instance.ChangeScene("SampleScene");
                }
            }
            else
            {
                // 在对话中读档时，必须先重置所有 DialogTrigger 实例的 isPlaying 状态。
                // 场景：正在播放 A 的剧情时读档加载 B 的剧情存档，若不重置 A：
                //   1. A 的 isPlaying=true 残留，A 的 OnDialogEnded 仍订阅 normalEndEvent.DialogEnded
                //   2. B 的剧情结束时触发 DialogEnded，A 的 OnDialogEnded 被错误调用
                //   3. MarkCompleted(A) 被执行 → A 被标记已完成 → 感叹号消失、无法拜访
                // ResetPlaying 会清理 isPlaying、_currentStoryDialogIndex 并取消订阅 DialogEnded，
                // 随后 ApplyActiveDialogTriggerData → RestorePlayingState 会正确恢复存档中的 B。
                foreach (var trigger in FindObjectsOfType<DialogTrigger>(true))
                    trigger.ResetPlaying();
                ApplyLoadData();
                string dialogScenePath = GetDialogScenePath(slotIndex);
                if (SceneManager.GetActiveScene().name != "SampleScene")
                {
                    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
                    {
                        SceneManager.sceneLoaded -= OnSceneLoaded;
                        SceneCenter.Instance.StartCoroutine(LoadGameDelay(slotIndex));
                    }
                    SceneManager.sceneLoaded += OnSceneLoaded;
                    SceneCenter.Instance.ChangeScene("SampleScene");
                }
                else
                {
                    Dialog.gameObject.SetActive(false);
                    DialogSerializer.LoadRuntimeScene(dialogScenePath);
                    Dialog.gameObject.SetActive(true);
                    
                    // 重新启动对话以加载内容（人物立绘、对话文本等）
                    StartCoroutine(RestoreDialogContentAfterLoad());
                }
                // 对话存档已在 ApplyLoadData 中恢复完毕，解除抑制
                DialogList.SuppressAutoDialog = false;
            }
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveSystem] 读档失败: {e.Message}");
            DialogList.SuppressAutoDialog = false;
            return false;
        }
    }

    private IEnumerator LoadGameDelay(int slotIndex)
    {
        // 等待新场景的 SaveSystem Start() 执行完毕（saveFolderPath 被初始化）
        yield return new WaitUntil(() => Instance != null && !string.IsNullOrEmpty(Instance.saveFolderPath));
        // 确保抑制标志在新实例的 LoadGame 中仍然生效
        DialogList.SuppressAutoDialog = true;
        Instance.LoadGame(slotIndex);
        
        // 如果是对话存档，等待一帧后再恢复对话内容
        yield return null;
        if (Instance.currentSaveData != null && Instance.currentSaveData.PlayerData.isInDialog)
        {
            yield return Instance.StartCoroutine(Instance.RestoreDialogContentAfterLoad());
        }
    }
    
    /// <summary>
    /// 读档后重新启动对话以加载内容（人物立绘、对话文本等）
    /// </summary>
    public IEnumerator RestoreDialogContentAfterLoad()
    {
        // 等待 DialogList 和 DialogVisual 初始化完成
        yield return new WaitUntil(() => DialogList.Instance != null);
        yield return new WaitUntil(() => DialogVisual.Instance != null);
        yield return null; // 等待一帧确保初始化完毕
        
        // 从 PlayerPrefs 中读取对话位置（已在 ApplyPlayerData 中设置）
        if (currentSaveData != null && currentSaveData.PlayerData.isInDialog)
        {
            int dialogIndex = PlayerPrefs.GetInt("dialogIndex");
            int dialogLine = PlayerPrefs.GetInt("dialogLine");
            int dialogStartLine = PlayerPrefs.GetInt("dialogStartLine");
            
            Debug.Log($"[SaveSystem] ========== 对话恢复诊断信息 ========== ");
            Debug.Log($"[SaveSystem] 存档数据: dialogIndex={currentSaveData.PlayerData.dialogIndex}, dialogLine={currentSaveData.PlayerData.dialogLine}, dialogStartLine={currentSaveData.PlayerData.dialogStartLine}");
            Debug.Log($"[SaveSystem] PlayerPrefs: dialogIndex={dialogIndex}, dialogLine={dialogLine}, dialogStartLine={dialogStartLine}");
            Debug.Log($"[SaveSystem] DialogList.dialogs.Count={DialogList.Instance.dialogs.Count}");
            
            // 正确的恢复方式：直接从存档位置继续，而不是从开头跳转
            // 使用 InvokeDialog(index, line) 方法，从存档位置开始继续对话
            // 这样可以避免跳转过程自动选择分支的问题
            Debug.Log($"[SaveSystem] 调用 InvokeDialog({dialogIndex}, {dialogLine}) - 从存档位置直接继续");
            DialogList.Instance.InvokeDialog(dialogIndex, dialogLine);
            
            Debug.Log($"[SaveSystem] ========== 对话恢复调用完成 ========== ");
        }
        else
        {
            Debug.LogWarning($"[SaveSystem] RestoreDialogContentAfterLoad: currentSaveData为null或不在对话中");
        }
    }

    private IEnumerator LoadGameNoDialogDelay(int slotIndex)
    {
        // 等待 SaveSystem 初始化完成
        yield return new WaitUntil(() => Instance != null && !string.IsNullOrEmpty(Instance.saveFolderPath));

        // 等待 ConfigManager 初始化完成（JobManager 需要用到）
        float configWaitTime = 0f;
        while (ConfigManager.Instance == null)
        {
            yield return null;
            configWaitTime += Time.deltaTime;
            if (configWaitTime > 5f)
            {
                Debug.LogWarning("[SaveSystem] ConfigManager 初始化超时，继续执行");
                break;
            }
        }

        // 等待 JobManager 初始化完成（确保 Start() 已执行）
        float jobWaitTime = 0f;
        while (JobManager.Instance == null)
        {
            yield return null;
            jobWaitTime += Time.deltaTime;
            if (jobWaitTime > 5f)
            {
                Debug.LogWarning("[SaveSystem] JobManager 初始化超时，继续执行");
                break;
            }
        }
        yield return null; // 等待一帧，确保 JobManager.Start() 执行完毕

        // 等待 AffectionManager 初始化完成（角色打工需要用到）
        yield return new WaitUntil(() => AffectionManager.Instance != null);

        // 等待 DialogList 初始化完成（小剧场需要用到）
        yield return new WaitUntil(() => DialogList.Instance != null);

        Instance.currentSaveData = Instance.LoadFromFile(slotIndex);
        if (Instance.currentSaveData == null)
        {
            DialogList.SuppressAutoDialog = false;
            yield break;
        }
        Instance.ApplyLoadData();

        // 存档数据已应用完毕，解除对话自动启动抑制
        DialogList.SuppressAutoDialog = false;

        // 设置 Dialog 为非激活状态（带空值检查）
        if (Instance.Dialog != null)
            Instance.Dialog.gameObject.SetActive(false);

        // 确保 DialogLocator 中的 Dialog 也被设置为非激活状态
        // 因为 IsInDialog 检查的是 DialogLocator.Instance.Dialog.activeSelf
        if (DialogLocator.Instance != null && DialogLocator.Instance.Dialog != null)
        {
            DialogLocator.Instance.Dialog.SetActive(false);
        }

        DialogSerializer.LoadRuntimeScene(Instance.GetDialogScenePath(slotIndex));
        var backEvent = FindObjectOfType<BackToMainMenuEvent>();
        if (backEvent != null) backEvent.SkipPrologue();
        foreach (var trigger in FindObjectsOfType<DialogTrigger>())
            trigger.ResetPlaying();

        // 等待 ShowDialog 初始化完成
        float showDialogWaitTime = 0f;
        while (ShowDialog.Instance == null)
        {
            yield return null;
            showDialogWaitTime += Time.deltaTime;
            if (showDialogWaitTime > 3f)
            {
                Debug.LogWarning("[SaveSystem] ShowDialog 初始化超时，继续执行");
                break;
            }
        }
        yield return null;

        // 设置 ShowDialog 的序章完成状态（避免后续 Hide() 因序章未完成而不显示UI）
        if (ShowDialog.Instance != null && Instance.currentSaveData.PrologueCompleted)
        {
            ShowDialog.Instance.SetPrologueCompleted();
        }

        // 显示 OtherUI（包括打工按钮）
        // 因为 ShowDialog.Start() 在存档加载前就执行了，导致 OtherUI 被错误隐藏
        if (ShowDialog.Instance != null && !Instance.currentSaveData.PlayerData.isInDialog)
        {
            ShowDialog.Instance.Hide2();
            Debug.Log("[SaveSystem] 已恢复 ShowDialog 的 OtherUI 显示");
        }

        //PrologueUIManager.Instance.ForceUnlockAll();
    }

    /// <summary>
    /// 删除存档
    /// </summary>
    public bool DeleteSave(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= maxSaveSlots)
        {
            Debug.LogError($"[SaveSystem] 存档槽位无效: {slotIndex}");
            return false;
        }

        try
        {
            string filePath = GetSaveFilePath(slotIndex);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);

                string dialogScenePath = GetDialogScenePath(slotIndex);
                if (File.Exists(dialogScenePath)) File.Delete(dialogScenePath);

                // 同时删除对应的截图文件
                DeleteScreenshot(slotIndex);

                Debug.Log($"[SaveSystem] 删除存档成功 - 槽位: {slotIndex}");
                return true;
            }
            Debug.LogWarning($"[SaveSystem] 存档不存在: 槽位 {slotIndex}");
            return false;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveSystem] 删除存档失败: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 删除所有存档
    /// </summary>
    public void DeleteAllSaves()
    {
        try
        {
            int deletedCount = 0;
            for (int i = 0; i < maxSaveSlots; i++)
            {
                if (IsSaveExists(i))
                {
                    string filePath = GetSaveFilePath(i);
                    File.Delete(filePath);
                    string dialogScenePath = GetDialogScenePath(i);
                    if (File.Exists(dialogScenePath)) File.Delete(dialogScenePath);
                    DeleteScreenshot(i);
                    deletedCount++;
                }
            }
            Debug.Log($"[SaveSystem] 删除所有存档完成，共删除 {deletedCount} 个存档");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveSystem] 删除所有存档失败: {e.Message}");
        }
    }

    /// <summary>
    /// 截取屏幕截图并保存为图片
    /// </summary>
    private IEnumerator CaptureScreenshot(int slotIndex, System.Action<string> onScreenshotSaved)
    {
        // 检查是否有DialogCanvas需要包含
        bool hasDialogCanvas = DialogCanvas != null && DialogCanvas.gameObject.activeInHierarchy;


            // 使用自定义方法捕获场景和UI
            yield return StartCoroutine(CaptureSceneAndUI(slotIndex, onScreenshotSaved));
        /*}
        else
        {
            yield return new WaitForEndOfFrame();

            string screenshotsFolderPath = Path.Combine(Application.persistentDataPath, SCREENSHOTS_FOLDER_NAME);
            string screenshotFileName = $"Screenshot_Slot{slotIndex}_{System.DateTime.Now:yyyyMMdd_HHmmss}.jpg";
            string screenshotFilePath = Path.Combine(screenshotsFolderPath, screenshotFileName);

            Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();
            SaveScaledScreenshot(screenshot, screenshotFilePath);
            Object.Destroy(screenshot);

            Debug.Log($"[SaveSystem] 截图保存成功: {screenshotFilePath}");

            string relativePath = Path.Combine(SCREENSHOTS_FOLDER_NAME, screenshotFileName);
            onScreenshotSaved?.Invoke(relativePath);
        }*/
    }

    /// <summary>
    /// 捕获场景和UI的截图
    /// </summary>
    private IEnumerator CaptureSceneAndUI(int slotIndex, System.Action<string> onScreenshotSaved)
    {
        // 保存SettingsCanvas的原始状态
        bool settingsCanvasWasActive = false;
        if (SettingsCanvas != null)
        {
            settingsCanvasWasActive = SettingsCanvas.gameObject.activeInHierarchy;
            // 临时隐藏SettingsCanvas
            SettingsCanvas.gameObject.SetActive(false);
        }

        yield return new WaitForEndOfFrame();

        // 生成截图文件路径
        string screenshotsFolderPath = Path.Combine(Application.persistentDataPath, SCREENSHOTS_FOLDER_NAME);
        string screenshotFileName = $"Screenshot_Slot{slotIndex}_{System.DateTime.Now:yyyyMMdd_HHmmss}.jpg";
        string screenshotFilePath = Path.Combine(screenshotsFolderPath, screenshotFileName);

        Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();
        SaveScaledScreenshot(screenshot, screenshotFilePath);
        Object.Destroy(screenshot);

        // 恢复SettingsCanvas的原始状态
        if (SettingsCanvas != null && settingsCanvasWasActive)
        {
            SettingsCanvas.gameObject.SetActive(true);
        }

        Debug.Log($"[SaveSystem] 截图保存成功: {screenshotFilePath}");

        // 返回相对路径（相对于persistentDataPath）
        string relativePath = Path.Combine(SCREENSHOTS_FOLDER_NAME, screenshotFileName);
        onScreenshotSaved?.Invoke(relativePath);
    }

    private void SaveScaledScreenshot(Texture2D source, string filePath)
    {
        RenderTexture rt =
            RenderTexture.GetTemporary(SCREENSHOT_WIDTH, SCREENSHOT_HEIGHT, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(source, rt);
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D scaled = new Texture2D(SCREENSHOT_WIDTH, SCREENSHOT_HEIGHT, TextureFormat.RGB24, false);
        scaled.ReadPixels(new Rect(0, 0, SCREENSHOT_WIDTH, SCREENSHOT_HEIGHT), 0, 0);
        scaled.Apply();
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        File.WriteAllBytes(filePath, scaled.EncodeToJPG(SCREENSHOT_JPEG_QUALITY));
        Object.Destroy(scaled);
    }

    /// <summary>
    /// 删除指定存档槽位的截图
    /// </summary>
    private void DeleteScreenshot(int slotIndex)
    {
        // 先尝试从存档数据中获取截图路径
        CompleteSaveData saveData = LoadFromFile(slotIndex);
        if (saveData != null && !string.IsNullOrEmpty(saveData.Pic))
        {
            // 如果存档中有截图路径，直接删除该文件
            string fullScreenshotPath = Path.Combine(Application.persistentDataPath, saveData.Pic);
            if (File.Exists(fullScreenshotPath))
            {
                try
                {
                    File.Delete(fullScreenshotPath);
                    Debug.Log($"[SaveSystem] 删除截图: {fullScreenshotPath}");
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[SaveSystem] 删除截图失败: {fullScreenshotPath}, 错误: {e.Message}");
                }
            }
        }
        else
        {
            // 如果存档中没有截图路径，则查找并删除所有匹配的截图文件
            string screenshotsFolderPath = Path.Combine(Application.persistentDataPath, SCREENSHOTS_FOLDER_NAME);
            if (Directory.Exists(screenshotsFolderPath))
            {
                string[] files = Directory.GetFiles(screenshotsFolderPath, $"Screenshot_Slot{slotIndex}_*.jpg");
                foreach (string file in files)
                {
                    try
                    {
                        File.Delete(file);
                        Debug.Log($"[SaveSystem] 删除截图: {file}");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[SaveSystem] 删除截图失败: {file}, 错误: {e.Message}");
                    }
                }
            }
        }
    }

    /// <summary>
    /// 从文件加载存档数据
    /// </summary>
    private CompleteSaveData LoadFromFile(int slotIndex)
    {
        string filePath = GetSaveFilePath(slotIndex);
        if (!File.Exists(filePath))
        {
            return null;
        }

        string json = File.ReadAllText(filePath);
        return JsonUtility.FromJson<CompleteSaveData>(json);
    }

    /// <summary>
    /// 读档前清空所有运行时状态，防止多次读档后旧数据残留
    /// 注意：不调用 StaticStateResetManager，因为静态状态已在 SceneCenter.ChangeScene 中清理
    /// </summary>
    private void ResetRuntimeState()
    {
        // 静态状态已在 SceneCenter.ChangeScene 中清理，这里只清理需要被存档覆盖的状态

        // 清空已完成剧情（存档会通过 ApplyStoryDotData 重新恢复）
        StoryDotManager.Instance?.ResetForNewGame();

        // 清空教学/序章状态（存档会通过 ApplyTutorialData 重新恢复）
        TutorialGuideManager.ResetForNewGame();
        PrologueUIManager.Instance?.ResetForNewGame();
        TutorialManager.SetTutorialShown(false);

        // 清空祈祷状态（存档会通过 ApplyPrayData 重新恢复）
        PrayLuck.Instance?.ResetWeeklyState();
    }

    /// <summary>
    /// 收集所有需要保存的数据
    /// 扩展点：在这里添加新的数据收集逻辑
    /// </summary>
    private void CollectSaveData()
    {
        // 收集玩家数据
        CollectPlayerData();

        // 收集设置数据
        CollectSettingsData();

        // 收集解锁关键词状态
        CollectUnlockedKeywordsData();

        // 收集红点剧情状态
        CollectStoryDotData();

        // 收集金钱数据
        CollectMoneyData();

        // 收集好感度数据
        CollectAffectionData();

        // 收集GameObject激活状态
        CollectGameObjectActiveData();

        // 收集正在播放的 DialogTrigger 状态
        CollectActiveDialogTriggerData();

        // 收集对话数值数据
        CollectDialogStatsData();

        // 收集教学引导完成状态
        CollectTutorialData();

        // 收集打工数据
        CollectJobsData();

        // 收集角色关键词触发状态（装饰点亮）
        CollectTriggeredCharKeywordsData();

        // 收集 DemoProgressTracker 数据
        CollectDemoProgressData();

        // 收集祈祷状态
        CollectPrayData();

        // CollectInventoryData();
        // CollectQuestData();
        // CollectAchievementData();
    }

    /// <summary>
    /// 收集玩家数据
    /// </summary>
    private void CollectPlayerData()
    {
        if (DialogVisual.Instance != null)
        {
            currentSaveData.PlayerData.isInDialog = DialogVisual.Instance.gameObject.activeInHierarchy;
            currentSaveData.PlayerData.dialogLine = DialogVisual.Instance.GetType() == DialogType.Choice ? DialogVisual.Instance.currentIndex - 1 : DialogVisual.Instance.currentIndex;
            currentSaveData.PlayerData.dialogStartLine = DialogVisual.Instance.startIndex;
        }
        else
        {
            currentSaveData.PlayerData.isInDialog = false;
            currentSaveData.PlayerData.dialogLine = 0;
            currentSaveData.PlayerData.dialogStartLine = 0;
        }

        if (DialogList.Instance != null)
        {
            currentSaveData.PlayerData.dialogIndex = DialogList.Instance.currentIndex;
        }
        else
        {
            currentSaveData.PlayerData.dialogIndex = 0;
        }

        var jobManager = JobManager.Instance ?? FindObjectOfType<JobManager>();
        if (jobManager != null)
        {
            currentSaveData.PlayerData.currentWeek = jobManager.currentWeek;
            currentSaveData.PlayerData.totalDays = jobManager.totalDays;
        }
    }

    /// <summary>
    /// 收集设置数据
    /// </summary>
    private void CollectSettingsData()
    {
        // 示例：从SettingsManager收集设置数据
        // if (SettingsManager.Instance != null)
        // {
        //     currentSaveData.SettingsData.MusicVolume = SettingsManager.Instance.MusicVolume;
        //     currentSaveData.SettingsData.SFXVolume = SettingsManager.Instance.SFXVolume;
        //     currentSaveData.SettingsData.IsFullscreen = SettingsManager.Instance.IsFullscreen;
        // }
    }

    /// <summary>
    /// 收集解锁关键词状态
    /// </summary>
    private void CollectUnlockedKeywordsData()
    {
        currentSaveData.UnlockedKeywords.Clear();

        var unlockedKeywords = DialogKeywordDetector.GetAllUnlockedKeywords();
        foreach (var kvp in unlockedKeywords)
        {
            currentSaveData.UnlockedKeywords.Add(new UnlockedKeywordData
            {
                unlockKey = kvp.Key,
                isUnlocked = kvp.Value
            });
        }

        Debug.Log($"[SaveSystem] 收集了 {currentSaveData.UnlockedKeywords.Count} 个解锁关键词状态");
    }


    /// <summary>
    /// 应用加载的数据
    /// 扩展点：在这里添加新的数据应用逻辑
    /// </summary>
    private void ApplyLoadData()
    {
        // 读档前清空运行时状态，防止多次读档后旧数据残留
        ResetRuntimeState();

        // 应用玩家数据
        ApplyPlayerData();

        // 应用设置数据
        ApplySettingsData();

        // 应用解锁关键词状态
        ApplyUnlockedKeywordsData();

        // 应用红点剧情状态
        ApplyStoryDotData();

        // 应用金钱数据
        ApplyMoneyData();

        // 应用好感度数据
        ApplyAffectionData();

        // 应用GameObject激活状态
        ApplyGameObjectActiveData();

        // 恢复正在播放的 DialogTrigger 订阅状态
        ApplyActiveDialogTriggerData();

        // 应用对话数值数据
        ApplyDialogStatsData();

        // 恢复教学引导完成状态
        ApplyTutorialData();

        // 恢复 ShowDialog 的序章完成状态
        // 确保对话结束后 Hide() 能正确显示 OtherUI
        if (ShowDialog.Instance != null && currentSaveData.PrologueCompleted)
        {
            ShowDialog.Instance.SetPrologueCompleted();
        }

        // 应用打工数据
        ApplyJobsData();

        // 应用角色关键词触发状态（装饰点亮）
        ApplyTriggeredCharKeywordsData();

        // 应用 DemoProgressTracker 数据
        ApplyDemoProgressData();

        // 应用祈祷状态
        ApplyPrayData();

        // ApplyInventoryData();
        // ApplyQuestData();
        // ApplyAchievementData();
    }

    /// <summary>
    /// 应用玩家数据
    /// </summary>
    private void ApplyPlayerData()
    {
        if (JobManager.Instance != null)
        {
            JobManager.Instance.currentWeek = currentSaveData.PlayerData.currentWeek;
            JobManager.Instance.totalDays = currentSaveData.PlayerData.totalDays;
        }

        // 只有在对话中保存的存档才恢复对话状态，否则清空防止 DialogList.OnEnable 误启动对话
        if (currentSaveData.PlayerData.isInDialog)
        {
            PlayerPrefs.SetInt("dialogIndex", currentSaveData.PlayerData.dialogIndex);
            PlayerPrefs.SetInt("dialogLine", currentSaveData.PlayerData.dialogLine);
            PlayerPrefs.SetInt("dialogStartLine", currentSaveData.PlayerData.dialogStartLine);
        }
        else
        {
            PlayerPrefs.SetInt("dialogIndex", 0);
            PlayerPrefs.SetInt("dialogLine", 0);
            PlayerPrefs.SetInt("dialogStartLine", 0);
        }
    }

    /// <summary>
    /// 应用设置数据
    /// </summary>
    private void ApplySettingsData()
    {
        // 示例：应用设置数据
        // if (SettingsManager.Instance != null)
        // {
        //     SettingsManager.Instance.MusicVolume = currentSaveData.SettingsData.MusicVolume;
        //     SettingsManager.Instance.SFXVolume = currentSaveData.SettingsData.SFXVolume;
        //     SettingsManager.Instance.IsFullscreen = currentSaveData.SettingsData.IsFullscreen;
        // }
    }

    /// <summary>
    /// 应用解锁关键词状态
    /// </summary>
    private void ApplyUnlockedKeywordsData()
    {
        Dictionary<string, bool> keywords = new Dictionary<string, bool>();

        foreach (var keywordData in currentSaveData.UnlockedKeywords)
        {
            keywords[keywordData.unlockKey] = keywordData.isUnlocked;
        }

        DialogKeywordDetector.LoadUnlockedKeywords(keywords);
        
        // 清空打工解锁状态缓存，确保重新计算解锁状态
        if (JobListManager.Instance != null)
        {
            JobListManager.Instance.ClearJobUnlockedStateCache();
        }

        Debug.Log($"[SaveSystem] 应用了 {currentSaveData.UnlockedKeywords.Count} 个解锁关键词状态");
    }

    private void CollectStoryDotData()
    {
        if (StoryDotManager.Instance != null)
            currentSaveData.CompletedStoryIds = StoryDotManager.Instance.GetCompletedStoryIds();
        else
            currentSaveData.CompletedStoryIds = new List<string>();
        Debug.Log($"[SaveSystem] 收集了 {currentSaveData.CompletedStoryIds.Count} 个剧情完成状态");
    }

    private void ApplyStoryDotData()
    {
        if (currentSaveData.CompletedStoryIds != null)
            StoryDotManager.LoadCompletedStoryIdsStatic(currentSaveData.CompletedStoryIds);
        Debug.Log($"[SaveSystem] 应用了 {currentSaveData.CompletedStoryIds?.Count ?? 0} 个剧情完成状态");
    }

    private void CollectMoneyData()
    {
        if (JobManager.Instance != null)
        {
            currentSaveData.CurrentMoney = JobManager.Instance.currentMoney;
            currentSaveData.TotalDebt = JobManager.Instance.totalDebt;
        }
    }

    private void ApplyMoneyData()
    {
        if (JobManager.Instance != null)
        {
            JobManager.Instance.currentMoney = currentSaveData.CurrentMoney;
            JobManager.Instance.totalDebt = currentSaveData.TotalDebt;
        }
    }

    private void CollectAffectionData()
    {
        if (AffectionManager.Instance != null)
            currentSaveData.Affections = AffectionManager.Instance.GetAllAffections();
        else
            currentSaveData.Affections = new List<CharData>();
    }

    private void ApplyAffectionData()
    {
        if (AffectionManager.Instance != null && currentSaveData.Affections != null)
            AffectionManager.Instance.LoadAffections(currentSaveData.Affections);
    }

    private void CollectGameObjectActiveData()
    {
        currentSaveData.GameObjectActiveStates.Clear();
        foreach (var go in TrackedGameObjects)
        {
            if (go != null)
                currentSaveData.GameObjectActiveStates.Add(go.activeSelf);
        }
    }

    private void ApplyGameObjectActiveData()
    {
        for (int i = 0; i < TrackedGameObjects.Count && i < currentSaveData.GameObjectActiveStates.Count; i++)
        {
            if (TrackedGameObjects[i] != null)
                TrackedGameObjects[i].SetActive(currentSaveData.GameObjectActiveStates[i]);
        }
    }


    private void CollectActiveDialogTriggerData()
    {
        var playing = DialogTrigger.PlayingTrigger;
        if (playing != null)
        {
            currentSaveData.ActiveDialogTrigger.characterName = playing.GetCharacterName();
            currentSaveData.ActiveDialogTrigger.dialogIndex = playing.CurrentStoryDialogIndex;
        }
        else
        {
            currentSaveData.ActiveDialogTrigger.characterName = "";
            currentSaveData.ActiveDialogTrigger.dialogIndex = -1;
        }
    }

    private void ApplyActiveDialogTriggerData()
    {
        var data = currentSaveData.ActiveDialogTrigger;
        if (string.IsNullOrEmpty(data.characterName) || data.dialogIndex < 0) return;

        foreach (var trigger in FindObjectsOfType<DialogTrigger>(true))
        {
            if (trigger.GetCharacterName() == data.characterName)
            {
                trigger.RestorePlayingState(data.dialogIndex);
                break;
            }
        }
    }

    private void CollectTutorialData()
    {
        currentSaveData.GuideCompleted = TutorialGuideManager.IsGuideCompleted();
        currentSaveData.PrologueCompleted = PrologueUIManager.IsPrologueCompleted;
        currentSaveData.TutorialShown = TutorialManager.HasTutorialBeenShown();
    }

    private void ApplyTutorialData()
    {
        if (currentSaveData.GuideCompleted)
            TutorialGuideManager.SetGuideCompleted();
        if (currentSaveData.PrologueCompleted)
            PrologueUIManager.SetPrologueCompleted();
        if (currentSaveData.TutorialShown)
            TutorialManager.SetTutorialShown(true);
    }

    private void CollectTriggeredCharKeywordsData()
    {
        currentSaveData.TriggeredCharKeywords.Clear();

        var allTriggered = DialogKeywordDetector.GetAllTriggeredCharKeywords();
        foreach (var kvp in allTriggered)
        {
            if (kvp.Value.Count > 0)
            {
                currentSaveData.TriggeredCharKeywords.Add(new TriggeredCharKeywordData
                {
                    characterName = kvp.Key,
                    triggeredKeywords = new List<string>(kvp.Value)
                });
            }
        }

        Debug.Log($"[SaveSystem] 收集了 {currentSaveData.TriggeredCharKeywords.Count} 个角色的关键词触发状态");
    }

    private void ApplyTriggeredCharKeywordsData()
    {
        // 先清空现有状态，防止读档后残留旧数据
        DialogKeywordDetector.LoadTriggeredCharKeywords(new Dictionary<string, HashSet<string>>());

        // 刷新人物界面装饰显示（无论是否有存档数据都要刷新）
        var decoManager = FindObjectOfType<CharacterDecorationManager>();

        if (currentSaveData.TriggeredCharKeywords == null || currentSaveData.TriggeredCharKeywords.Count == 0)
        {
            Debug.Log("[SaveSystem] 无角色关键词触发状态，已清空现有状态");
            
            if (decoManager != null)
            {
                decoManager.RefreshAllDecorations();
            }
            return;
        }

        // 转换为 Dictionary<string, HashSet<string>> 格式
        Dictionary<string, HashSet<string>> data = new Dictionary<string, HashSet<string>>();
        foreach (var entry in currentSaveData.TriggeredCharKeywords)
        {
            data[entry.characterName] = new HashSet<string>(entry.triggeredKeywords);
        }

        DialogKeywordDetector.LoadTriggeredCharKeywords(data);

        if (decoManager != null)
        {
            decoManager.RefreshAllDecorations();
        }

        Debug.Log($"[SaveSystem] 应用了 {currentSaveData.TriggeredCharKeywords.Count} 个角色的关键词触发状态");
    }

    private void CollectPrayData()
    {
        currentSaveData.HasPrayedThisWeek = PrayLuck.Instance != null && PrayLuck.Instance.HasPrayedThisWeek;
    }

    private void ApplyPrayData()
    {
        if (PrayLuck.Instance != null && currentSaveData.HasPrayedThisWeek)
            PrayLuck.Instance.SetPrayed();
    }

    private void CollectDemoProgressData()
    {
        if (DemoProgressTracker.Instance != null)
            currentSaveData.DemoProgressKeys = DemoProgressTracker.Instance.GetCompletedKeys();
        else
            currentSaveData.DemoProgressKeys = new List<string>();
        Debug.Log($"[SaveSystem] 收集了 {currentSaveData.DemoProgressKeys.Count} 个 DemoProgress 进度");
    }

    private void ApplyDemoProgressData()
    {
        if (DemoProgressTracker.Instance != null && currentSaveData.DemoProgressKeys != null)
            DemoProgressTracker.Instance.LoadCompletedKeys(currentSaveData.DemoProgressKeys);
        Debug.Log($"[SaveSystem] 应用了 {currentSaveData.DemoProgressKeys?.Count ?? 0} 个 DemoProgress 进度");
    }

    /// <summary>
    /// 收集打工数据（等级、经验、冷却）
    /// </summary>
    private void CollectJobsData()
    {
        currentSaveData.NormalJobsData.Clear();
        currentSaveData.CharacterJobsData.Clear();
        currentSaveData.ActiveJobSlots.Clear();

        if (JobManager.Instance != null)
        {
            // 收集普通打工数据
            foreach (var kvp in JobManager.Instance._allJobs)
            {
                JobData job = kvp.Value;
                currentSaveData.NormalJobsData.Add(new JobSaveData
                {
                    jobId = job.name,
                    currentLevel = job.currentLevel,
                    exp = job.exp,
                    curcd = job.curcd,
                    hasClickedCue = job.hasClickedCue
                });
            }

            // 收集角色打工数据
            foreach (var kvp in JobManager.Instance._allcharJobs)
            {
                JobData job = kvp.Value;
                currentSaveData.CharacterJobsData.Add(new JobSaveData
                {
                    jobId = job.name,
                    currentLevel = job.currentLevel,
                    exp = job.exp,
                    curcd = job.curcd,
                    hasClickedCue = job.hasClickedCue
                });
            }

            // 收集活动打工槽位数据
            var actjobs = JobManager.Instance.GetActJobs();
            for (int i = 0; i < actjobs.Length; i++)
            {
                if (actjobs[i] != null)
                {
                    currentSaveData.ActiveJobSlots.Add(new ActiveJobSlotData
                    {
                        slotIndex = i,
                        jobName = actjobs[i].name,
                        isChar = actjobs[i].ischar,
                        isEmpty = actjobs[i].isEmpty,
                        instanceId = actjobs[i].instanceId
                    });
                }
            }

            // 收集时间相关数据
            currentSaveData.TotalApUsed = JobManager.Instance.totalApUsed;
            currentSaveData.UsedDaysInWeek = JobManager.Instance.GetUsedDaysInWeek();

            Debug.Log($"[SaveSystem] 收集了 {currentSaveData.NormalJobsData.Count} 个普通打工数据, {currentSaveData.CharacterJobsData.Count} 个角色打工数据, {currentSaveData.ActiveJobSlots.Count} 个活动槽位, UsedDaysInWeek={currentSaveData.UsedDaysInWeek}, TotalApUsed={currentSaveData.TotalApUsed}");
        }
        else
        {
            Debug.LogWarning("[SaveSystem] JobManager.Instance 为 null，无法收集打工数据！");
        }
    }

    /// <summary>
    /// 应用打工数据（等级、经验、冷却）
    /// </summary>
    private void ApplyJobsData()
    {
        if (JobManager.Instance == null) return;

        // 先预填充解锁状态缓存，避免后续检查时误判为"刚解锁"而重置 hasClickedCue
        // 这必须在恢复 hasClickedCue 之前调用，因为 JobManager.Start() 中可能已经调用了 UpdateSwitchButCueState()
        if (JobListManager.Instance != null)
        {
            JobListManager.Instance.PreFillUnlockedStateCache();
        }

        // 应用普通打工数据
        foreach (var jobData in currentSaveData.NormalJobsData)
        {
            if (JobManager.Instance._allJobs.TryGetValue(jobData.jobId, out JobData job))
            {
                job.currentLevel = jobData.currentLevel;
                job.exp = jobData.exp;
                job.curcd = jobData.curcd;
                job.hasClickedCue = jobData.hasClickedCue;
            }
        }

        // 应用角色打工数据
        foreach (var jobData in currentSaveData.CharacterJobsData)
        {
            if (JobManager.Instance._allcharJobs.TryGetValue(jobData.jobId, out JobData job))
            {
                job.currentLevel = jobData.currentLevel;
                job.exp = jobData.exp;
                job.curcd = jobData.curcd;
                job.hasClickedCue = jobData.hasClickedCue;
            }
        }

        // 应用活动打工槽位数据
        JobManager.Instance.ClearActJobs();
        foreach (var slotData in currentSaveData.ActiveJobSlots)
        {
            JobData jobTemplate = null;
            if (slotData.isEmpty)
            {
                // 创建空打工（拜访）
                jobTemplate = new JobData
                {
                    name = slotData.jobName,
                    isEmpty = true,
                    cannotBeRemoved = true,
                    apCost = 3,
                    imageName = "",
                    instanceId = slotData.instanceId
                };
            }
            else if (slotData.isChar)
            {
                if (JobManager.Instance._allcharJobs.TryGetValue(slotData.jobName, out JobData original))
                {
                    jobTemplate = new JobData
                    {
                        name = original.name,
                        ischar = original.ischar,
                        apCost = original.apCost,
                        timeType = original.timeType,
                        cdVal = original.cdVal,
                        rewards = original.rewards,
                        unlockWeek = original.unlockWeek,
                        imageName = original.imageName,
                        head = original.head,
                        BG = original.BG,
                        npcName = original.npcName,
                        Refs = original.Refs,
                        Description = original.Description
                    };
                }
            }
            else
            {
                if (JobManager.Instance._allJobs.TryGetValue(slotData.jobName, out JobData original))
                {
                    jobTemplate = new JobData
                    {
                        name = original.name,
                        ischar = original.ischar,
                        apCost = original.apCost,
                        timeType = original.timeType,
                        cdVal = original.cdVal,
                        rewards = original.rewards,
                        unlockWeek = original.unlockWeek,
                        imageName = original.imageName,
                        Refs = original.Refs,
                        Description = original.Description
                    };
                }
            }

            if (jobTemplate != null)
            {
                JobManager.Instance.SetActJob(slotData.slotIndex, jobTemplate, slotData.instanceId);
            }
        }

        // 应用时间相关数据
        JobManager.Instance.totalApUsed = currentSaveData.TotalApUsed;
        JobManager.Instance.SetUsedDaysInWeek(currentSaveData.UsedDaysInWeek);
        JobManager.Instance.RefreshTimeFromAp();

        Debug.Log($"[SaveSystem] 应用时间数据: UsedDaysInWeek={currentSaveData.UsedDaysInWeek}, TotalApUsed={currentSaveData.TotalApUsed}");

        // 重新计算 totalChar（因为 SetActJob 不会更新 totalChar）
        JobManager.Instance.RecalculateTotalChar();

        // 同步更新保存的状态变量（防止关闭面板时恢复错误状态）
        JobManager.Instance.SyncSavedState();

        // 预填充解锁状态缓存，避免读档后第一次刷新列表时误判为"刚解锁"而重置 hasClickedCue
        if (JobListManager.Instance != null)
        {
            JobListManager.Instance.PreFillUnlockedStateCache();
        }

        // 刷新打工列表UI
        JobManager.Instance.RefreshJobLogUI();

        // 更新 Cue 显示状态
        JobManager.Instance.UpdateSwitchButCueState();

        Debug.Log($"[SaveSystem] 应用了 {currentSaveData.NormalJobsData?.Count ?? 0} 个普通打工数据, {currentSaveData.CharacterJobsData?.Count ?? 0} 个角色打工数据, {currentSaveData.ActiveJobSlots?.Count ?? 0} 个活动槽位");
    }

    /// <summary>
    /// 获取当前加载的存档数据（用于外部访问）
    /// </summary>
    public CompleteSaveData GetCurrentSaveData()
    {
        return currentSaveData;
    }

    /// <summary>
    /// 清空当前存档数据
    /// </summary>
    public void ClearCurrentSaveData()
    {
        currentSaveData = null;
    }

    /// <summary>
    /// 创建新游戏存档
    /// </summary>
    public void CreateNewSaveData()
    {
        currentSaveData = new CompleteSaveData();
    }

    private void CollectDialogStatsData()
    {
        if (DialogList.Instance != null && DialogList.Instance.statData != null)
            currentSaveData.DialogStats = DialogList.Instance.statData.GetAllEntries();
        else
            currentSaveData.DialogStats = new List<DialogStatEntry>();
    }

    private void ApplyDialogStatsData()
    {
        if (DialogList.Instance != null && DialogList.Instance.statData != null)
            DialogList.Instance.statData.LoadFromEntries(currentSaveData.DialogStats);
    }
}

/// <summary>
/// 存档信息（用于显示存档列表）
/// </summary>
[System.Serializable]
public class SaveInfo
{
    public int SlotIndex;
    public string SaveTime;
    public string Pic = "";
    public int currentWeek = 0;
    public int dialogIndex = -1; // -1 表示不在对话中
}
