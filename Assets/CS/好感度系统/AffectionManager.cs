using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class AffectionManager : MonoBehaviour
{
    public static AffectionManager Instance;
    
    public string selectcharacterId = "";
    [SerializeField] private AffectionConfig[] characterConfigs;
    
    private static Dictionary<string, string> _characterNameMapping = new Dictionary<string, string>
    {
        { "陈玉澍", "陈玉树" },
        { "陳玉樹", "陈玉树" },
        { "陳玉澍", "陈玉树" }
    };

    [Header("打开好感度界面按钮")]
    public Button openAffectionUIButton;

    [Header("好感度界面UI组件")]
    public AffectionUI affectionUI;

    private Dictionary<string, CharData> CharDatas = new Dictionary<string, CharData>();
    private Dictionary<string, AffectionConfig> configMap = new Dictionary<string, AffectionConfig>();
    public static event Action<string> OnAffectionChanged;
    public static event Action OnOpenAffectionUI;
    public event Action<string, int> onStatChange;

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
        Initialize();
    }

    private void OnEnable()
    {
        if (StatEventCenter.Instance != null)
        {
            StatEventCenter.Instance.onStatChange += OnStatChange;
        }
    }

    private void OnDisable()
    {
        if (StatEventCenter.Instance != null)
        {
            StatEventCenter.Instance.onStatChange -= OnStatChange;
        }
    }

    private void Start()
    {
        if (openAffectionUIButton != null)
        {
            openAffectionUIButton.onClick.AddListener(OpenAffectionUI);
        }
    }

    private void OnDestroy()
    {
        // 清空静态变量，避免指向已销毁的对象
        if (Instance == this)
        {
            Instance = null;
        }
        
        if (openAffectionUIButton != null)
        {
            openAffectionUIButton.onClick.RemoveListener(OpenAffectionUI);
        }
    }

    /// <summary>
    /// 重置静态状态（由 StaticStateResetManager 在存读档时调用）
    /// 注意：不清空静态事件，因为订阅者是实例对象，会在 OnDestroy 中自动取消订阅
    /// </summary>
    public static void ResetStaticState()
    {
        // 不清空静态事件，保留订阅关系
        // OnAffectionChanged = null;
        // OnOpenAffectionUI = null;
        Debug.Log("[AffectionManager] 静态状态已重置（保留事件订阅）");
    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.Escape)
            && affectionUI != null
            && affectionUI.gameObject.activeInHierarchy)
        {
            var deco = CharacterDecorationManager.Instance;
            if (deco != null && deco.characterDetailsPanel != null && deco.characterDetailsPanel.activeSelf)
            {
                deco.HideCharacterDetails();
            }
            else
            {
                affectionUI.CloseAffectionPanel();
            }
        }
    }

    public static string NormalizeCharacterName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;
        
        if (_characterNameMapping.TryGetValue(name, out string normalized))
            return normalized;
        
        return name;
    }

    private void Initialize()
    {
        Debug.Log($"[AffectionManager] 开始初始化，共 {characterConfigs.Length} 个角色配置");
        foreach (var config in characterConfigs)
        {
            string normalizedName = NormalizeCharacterName(config.characterName);
            Debug.Log($"[AffectionManager] 注册角色: {config.characterName} -> {normalizedName}");
            configMap[normalizedName] = config;

            if (!CharDatas.ContainsKey(normalizedName))
            {
                CharDatas[normalizedName] = new CharData()
                {
                    characterId = normalizedName,
                    currentAffection = 0,
                };
            }
        }
        Debug.Log($"[AffectionManager] 初始化完成，共注册 {CharDatas.Count} 个角色");
    }

    private void OnStatChange(string statName, int value)
    {
        if (statName.EndsWith("好感度"))
        {
            string characterId = statName.Substring(0, statName.Length - 3);
            AddAffection(characterId, value);
        }
    }

    public void AddAffection(string characterId, int amount)
    {
        string normalizedId = NormalizeCharacterName(characterId);
        
        if (!CharDatas.ContainsKey(normalizedId))
        {
            Debug.LogWarning($"角色 {characterId} (规范化后: {normalizedId}) 不存在");
            return;
        }
        var data = CharDatas[normalizedId];
        int oldAffection = data.currentAffection;
        data.currentAffection += amount;
        Debug.Log($"[AffectionManager] 角色 {characterId} (规范化后: {normalizedId}) 好感度变化: {oldAffection} -> {data.currentAffection} (增加 {amount})");
        OnAffectionChanged?.Invoke(normalizedId);
        onStatChange?.Invoke(normalizedId + "好感度", amount);
    }
    public int GetAffectionLevel(string characterId)
    {
        string normalizedId = NormalizeCharacterName(characterId);
        
        if (!CharDatas.ContainsKey(normalizedId))
            return 0;

        int value = CharDatas[normalizedId].currentAffection;

        if (value >= 80)
            return 3;
        if (value >= 50)
            return 2;
        if (value >= 20)
            return 1;
        
        return 0;
    }

    public int GetAffection(string characterId)
    {
        string normalizedId = NormalizeCharacterName(characterId);
        
        if (CharDatas.ContainsKey(normalizedId))
            return CharDatas[normalizedId].currentAffection;
        return 0;
    }

    public List<CharData> GetAllAffections()
    {
        var list = new List<CharData>();
        foreach (var kvp in CharDatas)
            list.Add(new CharData { characterId = kvp.Key, currentAffection = kvp.Value.currentAffection });
        return list;
    }

    public void LoadAffections(List<CharData> data)
    {
        foreach (var entry in data)
        {
            if (CharDatas.ContainsKey(entry.characterId))
                CharDatas[entry.characterId].currentAffection = entry.currentAffection;
        }
    }

    public void OpenAffectionUI()
    {
        if (affectionUI != null)
        {
            affectionUI.OpenAffectionPanel();
        }
        else
        {
            Debug.LogWarning("[AffectionManager] 未设置 AffectionUI 组件");
        }
        OnOpenAffectionUI?.Invoke();
    }

    [ContextMenu("随机增加10好感度")]
    public void RandomAddAffectionTest()
    {
        if (CharDatas.Count == 0)
        {
            Debug.LogWarning("[AffectionManager] 没有已注册的角色");
            return;
        }
        
        List<string> characterIds = new List<string>(CharDatas.Keys);
        int randomIndex = UnityEngine.Random.Range(0, characterIds.Count);
        string randomCharacter = characterIds[randomIndex];
        
        StatEventCenter.Instance.ChangeStat(randomCharacter + "好感度", 10);
        Debug.Log($"[AffectionManager] 测试：通过StatEventCenter为 {randomCharacter} 增加10好感度");
    }
}

