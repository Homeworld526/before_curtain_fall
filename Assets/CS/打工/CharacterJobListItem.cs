using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using GameSystem;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class CharacterJobListItem : MonoBehaviour, IPointerEnterHandler
{
    public Button button;
    public Text jobName;
    public Text jobPrice;
    public TextMeshProUGUI favorText;
    public TextMeshProUGUI favorValueText;
    public TextMeshProUGUI levelText;
    public Image icon;
    public Image headImage;
    public Image itemImage;
    public JobData job;
    private int jobCD;
    private int jobLevel;
    bool iscan = true;
    private static Dictionary<string, Sprite> _spriteDict;
    private GameObject cueObject;

    private void Start()
    {
        if (button == null)
        {
            Debug.LogError($"CharacterJobListItem 在 {gameObject.name} 上缺少 Button 组件引用！");
            return;
        }

        button.onClick.AddListener(Addjob);
        Transform cueTransform = transform.Find("Cue");
        if (cueTransform != null)
        {
            cueObject = cueTransform.gameObject;
            Debug.Log($"[CharacterJobListItem] {gameObject.name} 找到Cue对象");
        }
        else
        {
            Debug.LogWarning($"[CharacterJobListItem] {gameObject.name} 未找到Cue子对象");
        }
        
        if (_spriteDict == null)
        {
            _spriteDict = new Dictionary<string, Sprite>();
            Sprite[] allSprites = Resources.LoadAll<Sprite>("WorkIcon/workUI_view_atlas");
            foreach (var sprite in allSprites)
            {
                if (!_spriteDict.ContainsKey(sprite.name))
                {
                    _spriteDict.Add(sprite.name, sprite);
                }
            }
        }
        
        if (AffectionManager.Instance != null)
        {
            AffectionManager.OnAffectionChanged += OnAffectionChanged;
        }
    }

    private void OnDestroy()
    {
        // OnAffectionChanged 是静态事件，不需要检查 AffectionManager.Instance
        // 直接取消订阅，避免事件指向已销毁的对象
        AffectionManager.OnAffectionChanged -= OnAffectionChanged;
    }

    private void OnAffectionChanged(string characterId)
    {
        // 检查自身是否已被销毁（Unity 假 null）
        if (this == null) return;

        try
        {
            if (job != null)
            {
                string normalizedNpcName = AffectionManager.NormalizeCharacterName(job.npcName);
                if (normalizedNpcName == characterId)
                {
                    UpdateDisplay();
                }
            }
        }
        catch (MissingReferenceException)
        {
            // 对象已被销毁但事件仍触发，取消订阅以防止后续错误
            Debug.LogWarning("[CharacterJobListItem] OnAffectionChanged 触发时对象已销毁，取消订阅");
            AffectionManager.OnAffectionChanged -= OnAffectionChanged;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (job != null)
        {
            JobManager.Instance.ShowJobDetail(job);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        JobManager.Instance.HideJobDetail();
    }

    private void Addjob()
    {
        if (job != null && !job.hasClickedCue)
        {
            job.hasClickedCue = true;
            Debug.Log($"[CharacterJobListItem.Addjob] {job.name} 已被点击，设置hasClickedCue为true");
            if (cueObject != null)
            {
                cueObject.SetActive(false);
            }
            JobManager.Instance.UpdateSwitchButCueState();
        }
        
        if (JobManager.Instance.totalApUsed + job.apCost > 21)
        {
            TipDialog.EnsureInstance();
            TipDialog.Instance.Show();
            return;
        }

        if (!iscan)
        {
            TipDialog.EnsureInstance();
            TipDialog.Instance.Show();
            return;
        }

        JobManager.Instance.Addjob(job);
        JobListManager.Instance.RefreshAll();
    }

    public void UpdateDisplay()
    {
        if (job == null)
        {
            Debug.LogWarning("JobData 为空，无法刷新UI");
            return;
        }

        iscan = JobManager.Instance.CanJobBeTaken(job);
        
        if (cueObject == null)
        {
            Transform cueTransform = transform.Find("Cue");
            if (cueTransform != null)
            {
                cueObject = cueTransform.gameObject;
                Debug.Log($"[CharacterJobListItem.UpdateDisplay] {gameObject.name} 找到Cue对象");
            }
        }
        
        if (cueObject != null)
        {
            bool shouldShowCue = !job.hasClickedCue;
            cueObject.SetActive(shouldShowCue);
            Debug.Log($"[CharacterJobListItem.UpdateDisplay] {job.name}, hasClickedCue={job.hasClickedCue}, Cue显示={shouldShowCue}");
        }
        else
        {
            Debug.LogWarning($"[CharacterJobListItem.UpdateDisplay] {job.name} cueObject为空");
        }
        
        if (!iscan)
        {
            itemImage.color = new Color(0.819f, 0.878f, 0.957f, 1f); // D1E0F4
        }
        else if (!string.IsNullOrEmpty(job.BG))
        {
            if (ColorUtility.TryParseHtmlString("#" + job.BG, out Color bgColor))
            {
                itemImage.color = bgColor;
            }
            else
            {
                itemImage.color = Color.white;
            }
        }

        jobName.text = job.name;
        
        // 角色打工的报酬基于好感度等级，而不是currentLevel
        int affectionLevel = AffectionManager.Instance.GetAffectionLevel(job.npcName);
        int safeLevel = Mathf.Clamp(affectionLevel, 0, job.rewards.Length - 1);
        jobPrice.text = "报酬：$<color=#912710>" + job.rewards[safeLevel] + "</color>";
        
        int favorValue = AffectionManager.Instance.GetAffection(job.npcName);
        favorText.text = "好感：<color=#912710>" + favorValue + "</color>";
        favorValueText.text = favorValue.ToString();

        // 更新等级文本（角色打工使用好感度等级）
        if (levelText != null)
        {
            levelText.text = "等级：" + GetJobLevelName(affectionLevel);
        }

        if (!string.IsNullOrEmpty(job.BG_2))
        {
            Color favorColor;
            string colorString = job.BG_2.StartsWith("#") ? job.BG_2 : "#" + job.BG_2;
            if (ColorUtility.TryParseHtmlString(colorString, out favorColor))
            {
                favorValueText.color = favorColor;
            }
        }

        // 加载head图片
        if (!string.IsNullOrEmpty(job.head))
        {
            Sprite sprite = GetSprite(job.head);
            if (sprite != null)
            {
                headImage.sprite = sprite;
            }
        }

        // 加载好感度图片
        if (!string.IsNullOrEmpty(job.name))
        {
            Sprite iconSprite = Resources.Load<Sprite>("WorkIcon/" + job.name);
            if (iconSprite != null)
            {
                icon.sprite = iconSprite;
            }
        }
    }
    
    private Sprite GetSprite(string key)
    {
        if (string.IsNullOrEmpty(key))
            return null;
            
        if (_spriteDict == null || _spriteDict.Count == 0)
        {
            LoadSpriteDict();
        }
        
        if (_spriteDict != null && _spriteDict.TryGetValue(key, out Sprite sprite))
        {
            return sprite;
        }
        
        if (_spriteDict != null)
        {
            foreach (var pair in _spriteDict)
            {
                if (pair.Key != null && pair.Key.Contains(key))
                {
                    return pair.Value;
                }
            }
        }
        
        return null;
    }
    
    private void LoadSpriteDict()
    {
        if (_spriteDict == null)
        {
            _spriteDict = new Dictionary<string, Sprite>();
        }

        if (_spriteDict.Count > 0)
            return;

        Sprite[] allSprites = Resources.LoadAll<Sprite>("WorkIcon/workUI_view_atlas");
        foreach (var sprite in allSprites)
        {
            if (!_spriteDict.ContainsKey(sprite.name))
            {
                _spriteDict.Add(sprite.name, sprite);
            }
        }
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

    /// <summary>
    /// 重置静态状态（由 StaticStateResetManager 在存读档时调用）
    /// </summary>
    public static void ResetStaticState()
    {
        _spriteDict = null;  // 设为 null，让下次使用时重新加载
        Debug.Log("[CharacterJobListItem] 静态状态已重置: _spriteDict=null");
    }
}
