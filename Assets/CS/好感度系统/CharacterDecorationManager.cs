using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

/// <summary>
/// 人物装饰管理器
/// 用于管理人物界面的装饰点亮逻辑和角色详情显示
/// 通过订阅 DialogKeywordDetector 的 OnDecorationLit 事件来点亮装饰
/// 自动配置：查找"人物UI"对象下的"好感进度条-{姓氏}"子对象
/// </summary>
public class CharacterDecorationManager : MonoBehaviour
{
    /// <summary>
    /// 单个角色的装饰配置
    /// </summary>
    [Serializable]
    public class CharacterDecorationConfig
    {
        public string characterName;           // 角色名
        public Transform decorationParent;     // 装饰父节点
        public Color inactiveColor;            // 未激活颜色
        public Color activeColor;              // 激活颜色
        public List<Image> decorations;        // 装饰图片列表
        
        [TextArea(3, 10)]
        public string description;             // 角色介绍文本
    }

    /// <summary>
    /// 角色描述预配置（在Inspector中配置）
    /// </summary>
    [Serializable]
    public class CharacterDescriptionConfig
    {
        [Tooltip("角色名")]
        public string characterName;

        [TextArea(3, 10)]
        [Tooltip("角色介绍文本")]
        public string description;
    }

    [Header("人物UI父对象（始终激活）")]
    public Transform characterUIParent;

    [Header("人物UI根对象名称")]
    public string characterUIRootName = "人物UI";

    [Header("装饰父对象命名格式")]
    public string decorationParentPrefix = "好感进度条-";

    [Header("角色描述配置（在这里配置介绍文本）")]
    [Tooltip("角色介绍文本配置")]
    public List<CharacterDescriptionConfig> characterDescriptions = new List<CharacterDescriptionConfig>();

    [Header("角色装饰配置列表（自动生成）")]
    public List<CharacterDecorationConfig> decorationConfigs = new List<CharacterDecorationConfig>();

    [Header("默认颜色配置")]
    public Color defaultInactiveColor = new Color(0.6f, 0.6f, 0.6f, 1f);
    public Color defaultActiveColor = Color.white;

    [Header("角色详情UI")]
    [Tooltip("角色详情页面")]
    public GameObject characterDetailsPanel;

    [Tooltip("角色名字图片")]
    public Image nameImage;

    [Tooltip("角色介绍文本")]
    public TMP_Text descriptionText;

    [Tooltip("角色背景图片")]
    public Image backgroundImage;

    [Tooltip("角色背景视频播放器")]
    public VideoPlayer backgroundVideoPlayer;

    [Tooltip("关闭按钮")]
    public Button closeButton;

    [Header("资源路径配置")]
    [Tooltip("角色详情图片路径（Resources下）")]
    public string charIconPath = "CharIcon/";

    private Dictionary<string, CharacterDecorationConfig> _configMap = new Dictionary<string, CharacterDecorationConfig>();
    private Transform _characterUIRoot;
    private string _currentCharacterName;

    public static CharacterDecorationManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // 等待 DialogKeywordDetector 加载完成后再配置
        StartCoroutine(InitializeAfterDetectorReady());
    }

    private System.Collections.IEnumerator InitializeAfterDetectorReady()
    {
        // 等待 DialogKeywordDetector 实例存在
        while (DialogKeywordDetector.Instance == null)
        {
            yield return null;
        }

        // 等待 DialogKeywordDetector 加载角色配置
        while (DialogKeywordDetector.Instance.characterKeywordConfigs == null || 
               DialogKeywordDetector.Instance.characterKeywordConfigs.Count == 0)
        {
            yield return null;
        }

        Debug.Log("[CharacterDecorationManager] DialogKeywordDetector 已加载角色配置，开始自动配置");

        // 查找人物UI根对象
        FindCharacterUIRoot();
        
        // 自动配置角色装饰
        AutoConfigureDecorations();
        
        // 自动查找详情UI引用
        AutoFindDetailsUIReferences();
        
        // 自动绑定角色按钮
        AutoBindCharacterButtons();
        
        // 订阅 DialogKeywordDetector 的装饰点亮事件
        DialogKeywordDetector.OnDecorationLit += OnDecorationLit;
        
        // 初始化装饰状态
        RefreshAllDecorations();
        
        // 默认隐藏详情页面
        if (characterDetailsPanel != null)
        {
            characterDetailsPanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        DialogKeywordDetector.OnDecorationLit -= OnDecorationLit;

        if (Instance == this)
        {
            Instance = null;
        }
    }

    #region UI引用查找

    /// <summary>
    /// 查找人物UI根对象（支持禁用状态的对象）
    /// </summary>
    private void FindCharacterUIRoot()
    {
        // 方式1：从父对象查找（可以找到禁用的子对象）
        if (characterUIParent != null)
        {
            _characterUIRoot = characterUIParent.Find(characterUIRootName);
        }
        
        // 方式2：如果父对象未指定，尝试全局查找（只能找到激活的对象）
        if (_characterUIRoot == null)
        {
            _characterUIRoot = GameObject.Find(characterUIRootName)?.transform;
        }
        
        if (_characterUIRoot == null)
        {
            Debug.LogWarning($"[CharacterDecorationManager] 未找到人物UI根对象: {characterUIRootName}");
            Debug.LogWarning($"[CharacterDecorationManager] 请在Inspector中指定 characterUIParent，或将人物UI对象激活");
        }
        else
        {
            Debug.Log($"[CharacterDecorationManager] 找到人物UI根对象: {characterUIRootName}");
        }
    }

    /// <summary>
    /// 自动查找角色详情UI引用
    /// </summary>
    private void AutoFindDetailsUIReferences()
    {
        if (_characterUIRoot == null)
        {
            Debug.LogWarning("[CharacterDecorationManager] 人物UI根对象为空，无法查找详情UI");
            return;
        }

        // 查找角色详情页面
        if (characterDetailsPanel == null)
        {
            Transform details = FindChildRecursive(_characterUIRoot, "Character Details");
            if (details != null)
            {
                characterDetailsPanel = details.gameObject;
                Debug.Log("[CharacterDecorationManager] 找到角色详情页面: Character Details");
            }
            else
            {
                Debug.LogWarning("[CharacterDecorationManager] 未找到角色详情页面: Character Details");
                return; // 没找到详情页面，后续无法查找
            }
        }

        // 在详情页面中查找子元素
        Transform detailsTransform = characterDetailsPanel.transform;

        // 查找名字图片
        if (nameImage == null)
        {
            Transform nameTrans = FindChildRecursive(detailsTransform, "Name");
            if (nameTrans != null)
            {
                nameImage = nameTrans.GetComponent<Image>();
                if (nameImage != null)
                    Debug.Log("[CharacterDecorationManager] 找到名字图片: Name");
                else
                    Debug.LogWarning("[CharacterDecorationManager] Name 对象没有 Image 组件");
            }
            else
            {
                Debug.LogWarning("[CharacterDecorationManager] 未找到名字图片: Name");
            }
        }

        // 查找介绍文本
        if (descriptionText == null)
        {
            Transform desTrans = FindChildRecursive(detailsTransform, "Des");
            if (desTrans != null)
            {
                descriptionText = desTrans.GetComponent<TMP_Text>();
                if (descriptionText != null)
                    Debug.Log("[CharacterDecorationManager] 找到介绍文本: Des");
                else
                    Debug.LogWarning("[CharacterDecorationManager] Des 对象没有 TMP_Text 组件");
            }
            else
            {
                Debug.LogWarning("[CharacterDecorationManager] 未找到介绍文本: Des");
            }
        }

        // 查找背景图片
        if (backgroundImage == null)
        {
            Transform bgTrans = FindChildRecursive(detailsTransform, "BG");
            if (bgTrans != null)
            {
                backgroundImage = bgTrans.GetComponent<Image>();
                if (backgroundImage != null)
                    Debug.Log("[CharacterDecorationManager] 找到背景图片: BG");
                else
                    Debug.LogWarning("[CharacterDecorationManager] BG 对象没有 Image 组件");
            }
            else
            {
                Debug.LogWarning("[CharacterDecorationManager] 未找到背景图片: BG");
            }
        }

        // 查找背景视频播放器
        if (backgroundVideoPlayer == null)
        {
            Transform bgTrans = FindChildRecursive(detailsTransform, "BG");
            if (bgTrans != null)
            {
                backgroundVideoPlayer = bgTrans.GetComponent<VideoPlayer>();
                if (backgroundVideoPlayer != null)
                    Debug.Log("[CharacterDecorationManager] 找到背景视频播放器: BG");
            }
        }

        // 查找关闭按钮
        if (closeButton == null)
        {
            Transform closeTrans = FindChildRecursive(detailsTransform, "Close");
            if (closeTrans != null)
            {
                closeButton = closeTrans.GetComponent<Button>();
            }
        }

        // 绑定关闭按钮事件
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners(); // 移除之前的监听器
            closeButton.onClick.AddListener(HideCharacterDetails);
            Debug.Log("[CharacterDecorationManager] 绑定关闭按钮事件");
        }
        else
        {
            Debug.LogWarning("[CharacterDecorationManager] 关闭按钮未找到或未配置");
        }
    }

    /// <summary>
    /// 递归查找子对象（支持任意层级）
    /// </summary>
    private Transform FindChildRecursive(Transform parent, string name)
    {
        // 先直接查找
        Transform found = parent.Find(name);
        if (found != null)
            return found;

        // 递归查找子对象
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            found = FindChildRecursive(child, name);
            if (found != null)
                return found;
        }

        return null;
    }

    #endregion

    #region 装饰配置

    /// <summary>
    /// 自动配置角色装饰
    /// 从 DialogKeywordDetector 获取角色列表，自动查找对应的UI对象
    /// </summary>
    private void AutoConfigureDecorations()
    {
        decorationConfigs.Clear();
        _configMap.Clear();

        if (_characterUIRoot == null)
        {
            Debug.LogWarning("[CharacterDecorationManager] 人物UI根对象未找到，无法自动配置");
            return;
        }

        // 从 DialogKeywordDetector 获取角色关键词配置
        var charConfigs = DialogKeywordDetector.Instance?.characterKeywordConfigs;
        if (charConfigs == null || charConfigs.Count == 0)
        {
            Debug.LogWarning("[CharacterDecorationManager] DialogKeywordDetector 未加载角色配置");
            return;
        }

        foreach (var charConfig in charConfigs)
        {
            string characterName = charConfig.characterName;

            // 查找角色按钮（按钮名称 = 角色名）
            Transform buttonTrans = FindChildRecursive(_characterUIRoot, characterName);
            if (buttonTrans != null)
            {
                Button button = buttonTrans.GetComponent<Button>();
                if (button != null)
                {
                    string capturedName = characterName; // 闭包捕获
                    button.onClick.AddListener(() => ShowCharacterDetails(capturedName));
                    Debug.Log($"[CharacterDecorationManager] 绑定角色按钮: {characterName}");
                }
                else
                {
                    Debug.LogWarning($"[CharacterDecorationManager] 角色按钮 {characterName} 没有 Button 组件");
                }
            }
            else
            {
                Debug.LogWarning($"[CharacterDecorationManager] 未找到角色按钮: {characterName}");
            }

            // 查找装饰父对象
            string surname = GetSurname(characterName);
            string decorationParentName = decorationParentPrefix + surname;
            Transform decorationParent = FindChildRecursive(_characterUIRoot, decorationParentName);
            
            // 创建配置（即使没有装饰父对象也创建，用于显示详情）
            CharacterDecorationConfig config = new CharacterDecorationConfig
            {
                characterName = characterName,
                decorationParent = decorationParent,
                inactiveColor = defaultInactiveColor,
                activeColor = defaultActiveColor,
                decorations = new List<Image>(),
                description = GetDescriptionFromConfig(characterName)
            };

            if (decorationParent != null)
            {
                CollectDecorationsFromParent(config);
            }

            decorationConfigs.Add(config);
            _configMap[characterName] = config;
            Debug.Log($"[CharacterDecorationManager] 配置角色 {characterName}: 装饰数量 {config.decorations.Count}");
        }

        Debug.Log($"[CharacterDecorationManager] 自动配置完成，共 {decorationConfigs.Count} 个角色");
    }

    /// <summary>
    /// 从角色名获取姓氏（取第一个字）
    /// </summary>
    private string GetSurname(string characterName)
    {
        if (string.IsNullOrEmpty(characterName))
            return "";

        return characterName.Substring(0, 1);
    }

    /// <summary>
    /// 从预配置中获取角色描述文本
    /// </summary>
    private string GetDescriptionFromConfig(string characterName)
    {
        foreach (var config in characterDescriptions)
        {
            if (config.characterName == characterName)
            {
                return config.description;
            }
        }
        return "";
    }

    /// <summary>
    /// 从父节点收集装饰图片
    /// </summary>
    private void CollectDecorationsFromParent(CharacterDecorationConfig config)
    {
        config.decorations.Clear();
        
        Transform parent = config.decorationParent;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            Image image = child.GetComponent<Image>();
            
            if (image != null)
            {
                // 跳过 Mask 组件的图片
                if (child.GetComponent<Mask>() != null)
                    continue;
                
                config.decorations.Add(image);
                
                // 初始状态：禁用所有装饰的 Image
                image.enabled = false;
            }
        }

        Debug.Log($"[CharacterDecorationManager] 从 {parent.name} 收集到 {config.decorations.Count} 个装饰");
    }

    #endregion

    #region 角色按钮绑定

    /// <summary>
    /// 自动绑定角色按钮
    /// </summary>
    private void AutoBindCharacterButtons()
    {
        if (_characterUIRoot == null) return;

        foreach (var config in decorationConfigs)
        {
            Transform buttonTrans = FindChildRecursive(_characterUIRoot, config.characterName);
            if (buttonTrans != null)
            {
                // 添加或获取 CharacterButton 组件
                CharacterButton charBtn = buttonTrans.GetComponent<CharacterButton>();
                if (charBtn == null)
                {
                    charBtn = buttonTrans.gameObject.AddComponent<CharacterButton>();
                }

                // 绑定点击事件
                string characterName = config.characterName;
                charBtn.OnButtonClicked = () => OnCharacterButtonClicked(characterName);

                Debug.Log($"[CharacterDecorationManager] 绑定角色按钮: {config.characterName}");
            }
            else
            {
                Debug.LogWarning($"[CharacterDecorationManager] 未找到角色按钮: {config.characterName}");
            }
        }
    }

    /// <summary>
    /// 角色按钮点击回调
    /// </summary>
    private void OnCharacterButtonClicked(string characterName)
    {
        Debug.Log($"[CharacterDecorationManager] 点击角色按钮: {characterName}");
        ShowCharacterDetails(characterName);
    }

    #endregion

    #region 角色详情显示

    /// <summary>
    /// 显示角色详情
    /// </summary>
    public void ShowCharacterDetails(string characterName)
    {
        _currentCharacterName = characterName;

        // 显示详情页面
        if (characterDetailsPanel != null)
        {
            characterDetailsPanel.SetActive(true);
        }

        // 更新名字图片
        UpdateNameImage(characterName);

        // 更新介绍文本
        UpdateDescription(characterName);

        // 更新背景图片
        UpdateBackgroundImage(characterName);
    }

    /// <summary>
    /// 隐藏角色详情
    /// </summary>
    public void HideCharacterDetails()
    {
        Debug.Log("[CharacterDecorationManager] HideCharacterDetails 被调用");

        if (characterDetailsPanel != null)
        {
            characterDetailsPanel.SetActive(false);
            Debug.Log("[CharacterDecorationManager] 隐藏角色详情面板");
        }
        else
        {
            Debug.LogWarning("[CharacterDecorationManager] characterDetailsPanel 为空");
        }

        // 清空背景视频
        if (backgroundVideoPlayer != null)
        {
            backgroundVideoPlayer.Stop();
            backgroundVideoPlayer.clip = null;
        }

        _currentCharacterName = null;
    }

    /// <summary>
    /// 更新名字图片
    /// </summary>
    private void UpdateNameImage(string characterName)
    {
        if (nameImage == null) return;

        // 加载名字图片：角色名+名字（如"张海心名字"）
        string imageName = characterName + "名字";
        string fullPath = charIconPath + imageName;

        Sprite sprite = Resources.Load<Sprite>(fullPath);
        if (sprite != null)
        {
            nameImage.sprite = sprite;
            Debug.Log($"[CharacterDecorationManager] 加载名字图片: {fullPath}");
        }
        else
        {
            Debug.LogWarning($"[CharacterDecorationManager] 未找到名字图片: {fullPath}");
        }

        // 根据名字长度设置宽度
        int nameLength = characterName.Length;
        float targetWidth;
        
        if (nameLength == 2)
        {
            targetWidth = 370f; // 两个字名字宽度
        }
        else
        {
            targetWidth = 550f; // 三个字及以上名字宽度
        }

        // 设置 RectTransform 宽度
        RectTransform rectTransform = nameImage.rectTransform;
        if (rectTransform != null)
        {
            Vector2 sizeDelta = rectTransform.sizeDelta;
            sizeDelta.x = targetWidth;
            rectTransform.sizeDelta = sizeDelta;
            Debug.Log($"[CharacterDecorationManager] 设置名字图片宽度: {targetWidth} (名字长度: {nameLength})");
        }

        // 淡入动画
        StartCoroutine(FadeInImage(nameImage, 1f));
    }

    /// <summary>
    /// 更新介绍文本
    /// </summary>
    private void UpdateDescription(string characterName)
    {
        if (descriptionText == null) return;

        if (_configMap.TryGetValue(characterName, out var config))
        {
            descriptionText.text = config.description;
            Debug.Log($"[CharacterDecorationManager] 更新介绍文本: {characterName}");
        }
        else
        {
            descriptionText.text = "暂无介绍";
            Debug.LogWarning($"[CharacterDecorationManager] 未找到角色介绍配置: {characterName}");
        }

        // 根据名字长度设置宽度
        int nameLength = characterName.Length;
        float targetWidth;

        if (nameLength == 2)
        {
            targetWidth = 500f; // 两个字名字宽度
        }
        else
        {
            targetWidth = 625f; // 三个字及以上名字宽度
        }

        // 设置 RectTransform 宽度
        RectTransform rectTransform = descriptionText.rectTransform;
        if (rectTransform != null)
        {
            Vector2 sizeDelta = rectTransform.sizeDelta;
            sizeDelta.x = targetWidth;
            rectTransform.sizeDelta = sizeDelta;
            Debug.Log($"[CharacterDecorationManager] 设置描述文本宽度: {targetWidth} (名字长度: {nameLength})");
        }

        // 淡入动画
        StartCoroutine(FadeInText(descriptionText, 1f));
    }

    /// <summary>
    /// 图片淡入协程
    /// </summary>
    private System.Collections.IEnumerator FadeInImage(Image image, float duration)
    {
        if (image == null) yield break;

        Color color = image.color;
        color.a = 0f;
        image.color = color;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Clamp01(elapsed / duration);
            image.color = color;
            yield return null;
        }

        color.a = 1f;
        image.color = color;
    }

    /// <summary>
    /// 文本淡入协程
    /// </summary>
    private System.Collections.IEnumerator FadeInText(TMP_Text text, float duration)
    {
        if (text == null) yield break;

        Color color = text.color;
        color.a = 0f;
        text.color = color;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Clamp01(elapsed / duration);
            text.color = color;
            yield return null;
        }

        color.a = 1f;
        text.color = color;
    }

    /// <summary>
    /// 更新背景图片/视频
    /// </summary>
    private void UpdateBackgroundImage(string characterName)
    {
        StartCoroutine(UpdateBackgroundVideoAsync(characterName));
    }

    /// <summary>
    /// 异步更新背景视频（带淡入效果避免闪烁）
    /// </summary>
    private System.Collections.IEnumerator UpdateBackgroundVideoAsync(string characterName)
    {
        // 加载背景视频：角色名-动效（如"张海心-动效"）
        if (backgroundVideoPlayer != null)
        {
            string videoName = characterName + "-动效";
            string fullPath = charIconPath + videoName;

            VideoClip videoClip = Resources.Load<VideoClip>(fullPath);
            if (videoClip != null)
            {
                // 获取或添加 CanvasGroup 来控制透明度
                CanvasGroup canvasGroup = backgroundVideoPlayer.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = backgroundVideoPlayer.gameObject.AddComponent<CanvasGroup>();
                }

                // 先隐藏视频
                canvasGroup.alpha = 0f;

                backgroundVideoPlayer.clip = videoClip;
                backgroundVideoPlayer.Play();

                // 等待几帧让视频开始渲染（避免闪烁）
                for (int i = 0; i < 30; i++)
                {
                    yield return null;
                }

                // 淡入显示视频
                float fadeDuration = 0.2f;
                float elapsed = 0f;
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.deltaTime;
                    canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                    yield return null;
                }
                canvasGroup.alpha = 1f;

                Debug.Log($"[CharacterDecorationManager] 加载背景视频: {fullPath}");
            }
            else
            {
                Debug.LogWarning($"[CharacterDecorationManager] 未找到背景视频: {fullPath}");
            }
        }

        // 兼容旧版背景图片
        if (backgroundImage != null)
        {
            string imageName = characterName + "-不带字";
            string fullPath = charIconPath + imageName;

            Sprite sprite = Resources.Load<Sprite>(fullPath);
            if (sprite != null)
            {
                backgroundImage.sprite = sprite;
                Debug.Log($"[CharacterDecorationManager] 加载背景图片: {fullPath}");
            }
            else
            {
                Debug.LogWarning($"[CharacterDecorationManager] 未找到背景图片: {fullPath}");
            }
        }
    }

    #endregion

    #region 装饰点亮逻辑

    /// <summary>
    /// 装饰点亮回调
    /// </summary>
    private void OnDecorationLit(string characterName, int decorationIndex)
    {
        if (!_configMap.TryGetValue(characterName, out CharacterDecorationConfig config))
        {
            Debug.LogWarning($"[CharacterDecorationManager] 未找到角色 {characterName} 的装饰配置");
            return;
        }

        LightDecoration(config, decorationIndex);
    }

    /// <summary>
    /// 点亮指定装饰（从下往上）
    /// decorationIndex 0 表示第一个触发的关键词，点亮最后一个装饰
    /// decorationIndex 1 表示第二个触发的关键词，点亮倒数第二个装饰
    /// </summary>
    private void LightDecoration(CharacterDecorationConfig config, int decorationIndex)
    {
        // 从下往上点亮：索引0点亮最后一个，索引1点亮倒数第二个...
        int actualIndex = config.decorations.Count - 1 - decorationIndex;
        
        if (actualIndex < 0 || actualIndex >= config.decorations.Count)
        {
            Debug.LogWarning($"[CharacterDecorationManager] 装饰索引超出范围: decorationIndex={decorationIndex}, actualIndex={actualIndex}");
            return;
        }

        Image decoration = config.decorations[actualIndex];
        if (decoration != null)
        {
            // 启用 Image 组件
            decoration.enabled = true;
            Debug.Log($"[CharacterDecorationManager] 点亮角色 {config.characterName} 的装饰: 第 {actualIndex + 1} 个（从上往下），触发次数 {decorationIndex + 1}");
            
            // 播放点亮动画
            PlayLightAnimation(decoration);
        }
    }

    /// <summary>
    /// 播放点亮动画
    /// </summary>
    private void PlayLightAnimation(Image decoration)
    {
        // 简单的缩放动画
        StartCoroutine(AnimateDecoration(decoration));
    }

    private System.Collections.IEnumerator AnimateDecoration(Image decoration)
    {
        if (decoration == null)
            yield break;

        Transform transform = decoration.transform;
        Vector3 originalScale = transform.localScale;
        Vector3 targetScale = originalScale * 1.2f;
        
        float duration = 0.3f;
        float elapsed = 0f;

        // 放大
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }

        // 缩小回原大小
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            yield return null;
        }

        transform.localScale = originalScale;
    }

    /// <summary>
    /// 刷新所有装饰状态
    /// </summary>
    public void RefreshAllDecorations()
    {
        foreach (var kvp in _configMap)
        {
            string characterName = kvp.Key;
            CharacterDecorationConfig config = kvp.Value;
            
            // 使用 DialogKeywordDetector 的静态方法获取触发次数
            int triggeredCount = DialogKeywordDetector.GetCharacterTriggeredCount(characterName);
            
            RefreshCharacterDecorations(config, triggeredCount);
        }
    }

    /// <summary>
    /// 刷新单个角色的装饰状态（从下往上启用）
    /// </summary>
    private void RefreshCharacterDecorations(CharacterDecorationConfig config, int triggeredCount)
    {
        for (int i = 0; i < config.decorations.Count; i++)
        {
            Image decoration = config.decorations[i];
            if (decoration == null)
                continue;

            // 从下往上启用：触发1次启用最后一个，触发2次启用倒数两个...
            // 计算该装饰应该启用的最小触发次数
            // 例如：5个装饰，索引0（最上）需要触发5次才启用，索引4（最下）只需触发1次就启用
            int minTriggerToEnable = config.decorations.Count - i;
            
            // 当前触发次数 >= 该装饰需要的最小触发次数时，启用
            bool shouldEnable = triggeredCount >= minTriggerToEnable;
            decoration.enabled = shouldEnable;
        }

        Debug.Log($"[CharacterDecorationManager] 角色 {config.characterName} 装饰状态刷新: {triggeredCount} 次触发，从下往上启用 {triggeredCount} 个装饰");
    }

    /// <summary>
    /// 获取角色的点亮装饰数量
    /// </summary>
    public int GetLitCount(string characterName)
    {
        if (!_configMap.TryGetValue(characterName, out CharacterDecorationConfig config))
            return 0;

        int count = 0;
        foreach (var decoration in config.decorations)
        {
            if (decoration != null && decoration.enabled)
            {
                count++;
            }
        }
        return count;
    }

    #endregion

    #region 调试方法

    /// <summary>
    /// 重置所有装饰状态（禁用所有 Image）
    /// </summary>
    [ContextMenu("重置所有装饰")]
    public void ResetAllDecorations()
    {
        foreach (var kvp in _configMap)
        {
            CharacterDecorationConfig config = kvp.Value;
            
            foreach (var decoration in config.decorations)
            {
                if (decoration != null)
                {
                    decoration.enabled = false;
                }
            }
        }

        Debug.Log("[CharacterDecorationManager] 已重置所有装饰（禁用所有 Image）");
    }

    /// <summary>
    /// 重新自动配置（用于调试）
    /// </summary>
    [ContextMenu("重新自动配置")]
    public void ReconfigureDecorations()
    {
        FindCharacterUIRoot();
        AutoConfigureDecorations();
        AutoFindDetailsUIReferences();
        AutoBindCharacterButtons();
        RefreshAllDecorations();
        Debug.Log("[CharacterDecorationManager] 已重新自动配置");
    }

    /// <summary>
    /// 测试点亮所有装饰（用于调试）
    /// </summary>
    [ContextMenu("测试点亮所有装饰")]
    public void TestLightAllDecorations()
    {
        if (decorationConfigs.Count == 0)
        {
            Debug.LogWarning("[CharacterDecorationManager] 装饰配置为空，请先执行'重新自动配置'");
            return;
        }

        foreach (var config in decorationConfigs)
        {
            Debug.Log($"[CharacterDecorationManager] 测试点亮角色 {config.characterName} 的所有装饰，共 {config.decorations.Count} 个");
            
            for (int i = 0; i < config.decorations.Count; i++)
            {
                Image decoration = config.decorations[i];
                if (decoration != null)
                {
                    decoration.enabled = true;
                    Debug.Log($"[CharacterDecorationManager] 点亮装饰 {i}: {decoration.name}");
                }
            }
        }
    }

    /// <summary>
    /// 测试点亮第一个装饰（用于调试）
    /// </summary>
    [ContextMenu("测试点亮第一个装饰")]
    public void TestLightFirstDecoration()
    {
        if (decorationConfigs.Count == 0)
        {
            Debug.LogWarning("[CharacterDecorationManager] 装饰配置为空，请先执行'重新自动配置'");
            return;
        }

        foreach (var config in decorationConfigs)
        {
            if (config.decorations.Count > 0)
            {
                // 从下往上点亮第一个（最后一个装饰）
                int lastIndex = config.decorations.Count - 1;
                Image decoration = config.decorations[lastIndex];
                if (decoration != null)
                {
                    decoration.enabled = true;
                    Debug.Log($"[CharacterDecorationManager] 点亮角色 {config.characterName} 的第一个装饰（索引 {lastIndex}）: {decoration.name}");
                }
            }
        }
    }

    /// <summary>
    /// 测试显示第一个角色详情
    /// </summary>
    [ContextMenu("测试显示第一个角色")]
    public void TestShowFirstCharacter()
    {
        if (decorationConfigs.Count > 0)
        {
            ShowCharacterDetails(decorationConfigs[0].characterName);
        }
    }

    /// <summary>
    /// 隐藏详情页面
    /// </summary>
    [ContextMenu("隐藏详情页面")]
    public void TestHideDetails()
    {
        HideCharacterDetails();
    }

    #endregion
}
