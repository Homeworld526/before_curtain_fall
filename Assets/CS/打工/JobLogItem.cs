using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameSystem;

public class JobLogItem : MonoBehaviour
{
    public Button button;
    public TextMeshProUGUI jobName;
    public TextMeshProUGUI orderText;
    public Image jobImage;
    public Image backgroundImage;
    public JobData job;

    private static Sprite[] _allSprites;

    private void Start()
    {
        if (button != null)
            button.onClick.AddListener(removejob);

        if (_allSprites == null)
        {
            _allSprites = Resources.LoadAll<Sprite>("WorkIcon/workUI_view_atlas");
        }
    }

    public void UpdateDisplay(int colorIndex, bool shouldChangeColor)
    {
        // ==============================
        // 空数据 → 完全透明、不显示任何东西
        // ==============================
        if (job == null)
        {
            if (jobName != null) jobName.text = "";
            if (backgroundImage != null) backgroundImage.enabled = false;
            if (jobImage != null) jobImage.enabled = false;
            return;
        }

        // ==============================
        // 空打工（isEmpty=true）→ 显示名称但隐藏图片和背景
        // ==============================
        if (job.isEmpty)
        {
            if (jobName != null) jobName.text = job.name;
            if (backgroundImage != null) backgroundImage.enabled = false;
            if (jobImage != null) jobImage.enabled = false;
            if (jobName != null) jobName.color = Color.black;
            return;
        }

        // 启用背景（只有非空才显示）
        if (backgroundImage != null) backgroundImage.enabled = true;
        if (jobImage != null) jobImage.enabled = true;

        Color textColor = Color.black;

        // 角色打工
        if (job.ischar)
        {
            if (!string.IsNullOrEmpty(job.head) && jobImage != null)
            {
                // 确保 _allSprites 已加载
                if (_allSprites == null)
                {
                    _allSprites = Resources.LoadAll<Sprite>("WorkIcon/workUI_view_atlas");
                }

                Sprite sprite = null;
                foreach (var s in _allSprites)
                {
                    if (s.name == job.head || s.name.Contains(job.head))
                    {
                        sprite = s;
                        break;
                    }
                }

                if (sprite != null)
                {
                    jobImage.sprite = sprite;
                    jobImage.enabled = true;
                }
                else
                {
                    jobImage.enabled = false;
                }
            }

            // 角色打工使用其指定的颜色
            if (backgroundImage != null && !string.IsNullOrEmpty(job.BG))
            {
                if (ColorUtility.TryParseHtmlString("#" + job.BG, out Color bgColor))
                {
                    backgroundImage.color = bgColor;
                    backgroundImage.enabled = true;
                }
                else
                {
                    backgroundImage.enabled = false;
                }
            }
        }
        // 普通打工
        else
        {
            if (jobImage != null) jobImage.enabled = false;

            // 普通打工使用交替颜色（原色和B9B0A7）
            if (backgroundImage != null)
            {
                Color altColor = ((colorIndex - 1) % 2 == 0)
                    ? new Color(1f, 1f, 1f) // 原色（白色）
                    : new Color(0.725f, 0.69f, 0.655f); // B9B0A7
                backgroundImage.color = altColor;
                backgroundImage.enabled = true;

                // 背景为第二个颜色(B9B0A7)时，文字变为白色
                if (altColor.r < 0.8f)
                {
                    textColor = Color.white;
                }
            }
        }

        // 设置文字颜色
        if (jobName != null)
        {
            jobName.text = job.name;
            jobName.color = textColor;
        }
    }

    private void removejob()
    {
        if (job == null || job.isEmpty) return;
        JobManager.Instance.Removejob(job);
        JobListManager.Instance.RefreshAll();
    }

    /// <summary>
    /// 重置静态状态（由 StaticStateResetManager 在存读档时调用）
    /// </summary>
    public static void ResetStaticState()
    {
        _allSprites = null;  // 设为 null，让下次使用时重新加载
        Debug.Log("[JobLogItem] 静态状态已重置: _allSprites=null");
    }
}