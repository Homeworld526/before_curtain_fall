using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.EventSystems;

public class AffectionUI : MonoBehaviour
{
    [Header("好感度界面根对象")]
    public Transform affectionPanelRoot;

    [Header("好感度每格数值")]
    public const int AFFECTION_PER_LEVEL = 20;

    [Header("最大好感等级")]
    public const int MAX_LEVEL = 5;

    [Header("关闭按钮")]
    public Button closeButton;

    [Header("开场动画对象")]
    public GameObject openingAnimation;

    [Header("内容面板对象")]
    public GameObject contentPanel;

    [Header("跳过动画提示文本")]
    public GameObject skipHintText;

    private Dictionary<string, Transform> characterUIMap = new Dictionary<string, Transform>();
    private bool isPlayingAnimation = false;
    private bool skipAnimationRequested = false;

    private void Start()
    {
        if (AffectionManager.Instance != null)
        {
            AffectionManager.OnAffectionChanged += RefreshAffectionUI;
            AffectionManager.OnOpenAffectionUI += RefreshAllAffectionUI;
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseButtonClicked);
        }
    }

    private void OnDestroy()
    {
        // OnAffectionChanged / OnOpenAffectionUI 是静态事件，不需要检查 AffectionManager.Instance
        // 直接取消订阅，避免事件指向已销毁的对象
        AffectionManager.OnAffectionChanged -= RefreshAffectionUI;
        AffectionManager.OnOpenAffectionUI -= RefreshAllAffectionUI;

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(OnCloseButtonClicked);
        }
    }

    private void Update()
    {
        // 检测点击跳过动画
        if (isPlayingAnimation && !skipAnimationRequested)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Debug.Log("[AffectionUI] 检测到鼠标点击");

                // 检查是否点击在任何按钮上
                if (EventSystem.current != null)
                {
                    PointerEventData pointerData = new PointerEventData(EventSystem.current);
                    pointerData.position = Input.mousePosition;

                    List<RaycastResult> results = new List<RaycastResult>();
                    EventSystem.current.RaycastAll(pointerData, results);

                    Debug.Log($"[AffectionUI] Raycast 结果数量: {results.Count}");

                    foreach (var result in results)
                    {
                        Debug.Log($"[AffectionUI] 点击到对象: {result.gameObject.name}");

                        // 检查是否点击在按钮上
                        if (result.gameObject.GetComponent<Button>() != null)
                        {
                            Debug.Log($"[AffectionUI] 点击在按钮上，不跳过动画");
                            return; // 点击在按钮上，不跳过
                        }
                    }
                }

                Debug.Log("[AffectionUI] 点击不在按钮上，跳过动画");
                SkipOpeningAnimation();
            }
        }
    }

    /// <summary>
    /// 跳过开场动画
    /// </summary>
    public void SkipOpeningAnimation()
    {
        if (!isPlayingAnimation) return;

        skipAnimationRequested = true;
        Debug.Log("[AffectionUI] 跳过开场动画");
    }

    public void RefreshAllAffectionUI()
    {
        if (this == null) return;

        if (affectionPanelRoot == null)
        {
            Debug.LogWarning("[AffectionUI] 未指定好感度界面根对象");
            return;
        }

        characterUIMap.Clear();

        if (contentPanel != null)
        {
            CanvasGroup canvasGroup = contentPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = contentPanel.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = 0f;
        }

        foreach (Transform child in affectionPanelRoot)
        {
            if (child.name.StartsWith("好感等级-"))
            {
                string fullName = child.name;
                int dashIndex = fullName.IndexOf('-');
                string surname = (dashIndex >= 0) ? fullName.Substring(dashIndex + 1) : fullName.Substring(4);
                
                characterUIMap[fullName] = child;
                RefreshCharacterAffection(child, surname);
            }
        }
    }

    public void RefreshAffectionUI(string characterId)
    {
        if (this == null) return;

        if (characterId == null || characterId.Length == 0)
            return;

        string surname = GetSurname(characterId);
        string uiName = $"好感等级-{surname}";

        if (affectionPanelRoot != null)
        {
            Transform characterUI = affectionPanelRoot.Find(uiName);
            if (characterUI != null)
            {
                RefreshCharacterAffection(characterUI, surname);
            }
        }
    }

    private void RefreshCharacterAffection(Transform characterUI, string surname)
    {
        if (characterUI == null)
            return;

        string uiName = characterUI.name;

        Transform progressBar = characterUI.Find("好感进度条");
        if (progressBar == null)
            return;

        string characterId = GetCharacterIdFromSurname(surname);
        if (characterId == null)
            characterId = surname;

        int currentAffection = AffectionManager.Instance.GetAffection(characterId);
        int filledLevels = Mathf.Clamp(currentAffection / AFFECTION_PER_LEVEL, 0, MAX_LEVEL);

        List<Transform> children = new List<Transform>();
        foreach (Transform child in progressBar)
        {
            children.Add(child);
        }

        if (children.Count == 0)
            return;

        for (int i = 0; i < children.Count; i++)
        {
            Transform child = children[i];
            Image childImage = child.GetComponent<Image>();
            if (childImage != null)
                childImage.enabled = false;
        }

        for (int i = 0; i < children.Count && i < MAX_LEVEL && i < filledLevels; i++)
        {
            Transform child = children[children.Count - 1 - i];
            Image childImage = child.GetComponent<Image>();
            if (childImage != null)
                childImage.enabled = true;
        }
    }

    private string GetSurname(string characterName)
    {
        if (string.IsNullOrEmpty(characterName))
            return "";

        return characterName.Substring(characterName.Length - 1);
    }

    private string GetCharacterIdFromSurname(string surname)
    {
        if (AffectionManager.Instance == null)
            return null;

        var configMap = typeof(AffectionManager)
            .GetField("configMap", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .GetValue(AffectionManager.Instance) as Dictionary<string, AffectionConfig>;

        if (configMap != null)
        {
            foreach (var kvp in configMap)
            {
                if (kvp.Key.EndsWith(surname))
                    return kvp.Key;
            }
        }

        return null;
    }

    public void OpenAffectionPanel()
    {
        RefreshAllAffectionUI();
        gameObject.SetActive(true);

        if (contentPanel != null)
        {
            contentPanel.SetActive(true);
        }

        if (openingAnimation != null && !isPlayingAnimation)
        {
            StartCoroutine(PlayOpeningAnimation());
        }
        else
        {
            StartCoroutine(FadeInContent());
        }
    }

    private IEnumerator PlayOpeningAnimation()
    {
        isPlayingAnimation = true;
        skipAnimationRequested = false;

        if (contentPanel != null)
        {
            contentPanel.SetActive(false);
        }

        // 显示跳过提示
        if (skipHintText != null)
        {
            skipHintText.SetActive(true);
        }

        openingAnimation.SetActive(true);
        yield return new WaitForSeconds(0.1f);

        VideoPlayer videoPlayer = openingAnimation.GetComponent<VideoPlayer>();

        if (videoPlayer != null)
        {
            videoPlayer.Prepare();

            int waitFrames = 0;
            while (!videoPlayer.isPrepared && waitFrames < 60 && !skipAnimationRequested)
            {
                waitFrames++;
                yield return null;
            }

            if (videoPlayer.isPrepared && !skipAnimationRequested)
            {
                videoPlayer.Play();

                while (videoPlayer.isPlaying && !skipAnimationRequested)
                {
                    yield return null;
                }

                if (skipAnimationRequested)
                {
                    videoPlayer.Stop();
                }
            }
        }
        else
        {
            Animator animator = openingAnimation.GetComponent<Animator>();
            if (animator != null)
            {
                float clipLength = 0f;
                RuntimeAnimatorController runtimeController = animator.runtimeAnimatorController;

                if (runtimeController != null && runtimeController.animationClips.Length > 0)
                {
                    clipLength = runtimeController.animationClips[0].length;
                }

                float elapsed = 0f;
                while (elapsed < clipLength && !skipAnimationRequested)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }
            else
            {
                Animation animation = openingAnimation.GetComponent<Animation>();
                if (animation != null && animation.clip != null)
                {
                    float elapsed = 0f;
                    while (elapsed < animation.clip.length && !skipAnimationRequested)
                    {
                        elapsed += Time.deltaTime;
                        yield return null;
                    }
                }
            }
        }

        // 隐藏跳过提示
        if (skipHintText != null)
        {
            skipHintText.SetActive(false);
        }

        openingAnimation.SetActive(false);

        if (contentPanel != null)
        {
            contentPanel.SetActive(true);
            StartCoroutine(FadeInContent());
        }

        isPlayingAnimation = false;
        skipAnimationRequested = false;
    }

    private IEnumerator FadeInContent()
    {
        CanvasGroup canvasGroup = contentPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = contentPanel.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 0f;

        float fadeSpeed = 2f;
        while (canvasGroup.alpha < 1f)
        {
            canvasGroup.alpha += Time.deltaTime * fadeSpeed;
            if (canvasGroup.alpha > 1f)
                canvasGroup.alpha = 1f;
            yield return null;
        }
    }

    public void OnCloseButtonClicked()
    {
        CloseAffectionPanel();
    }

    public void CloseAffectionPanel()
    {
        // 重置动画状态
        isPlayingAnimation = false;

        // 禁用开场动画对象
        if (openingAnimation != null)
        {
            openingAnimation.SetActive(false);
        }

        // 停止视频播放
        VideoPlayer videoPlayer = openingAnimation?.GetComponent<VideoPlayer>();
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }

        gameObject.SetActive(false);
    }
}
