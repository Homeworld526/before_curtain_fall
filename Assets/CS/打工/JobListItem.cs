using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using GameSystem;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class JobListItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Button button;
    public Text jobName;
    public Text Order;
    public Text jobPrice;
    public Text JobLevelText;
    public Image icon;
    public JobData job;
    private int jobCD;
    private int jobLevel;
    bool iscan = true;
    private GameObject cueObject;

    private void Start()
    {
        if (button == null)
        {
            Debug.LogError($"JobListItem 在 {gameObject.name} 上缺少 Button 组件引用！");
            return;
        }

        button.onClick.AddListener(Addjob);
        Transform cueTransform = transform.Find("Cue");
        if (cueTransform != null)
        {
            cueObject = cueTransform.gameObject;
            Debug.Log($"[JobListItem] {gameObject.name} 找到Cue对象");
        }
        else
        {
            Debug.LogWarning($"[JobListItem] {gameObject.name} 未找到Cue子对象");
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
            Debug.Log($"[JobListItem.Addjob] {job.name} 已被点击，设置hasClickedCue为true");
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

    public void UpdateDisplay(int order = 0)
    {
        if (job == null)
        {
            Debug.LogWarning("JobData 为空，无法刷新UI");
            return;
        }

        iscan = JobManager.Instance.CanJobBeTaken(job);
        var image = GetComponent<Image>();

        if (!iscan)
        {
            image.color = new Color(0.819f, 0.878f, 0.957f, 1f); // D1E0F4
        }
        else
        {
            image.color = Color.white;
        }

        if (cueObject == null)
        {
            Transform cueTransform = transform.Find("Cue");
            if (cueTransform != null)
            {
                cueObject = cueTransform.gameObject;
                Debug.Log($"[JobListItem.UpdateDisplay] {gameObject.name} 找到Cue对象");
            }
        }
        
        if (cueObject != null)
        {
            bool shouldShowCue = !job.hasClickedCue;
            cueObject.SetActive(shouldShowCue);
            Debug.Log($"[JobListItem.UpdateDisplay] {job.name}, hasClickedCue={job.hasClickedCue}, Cue显示={shouldShowCue}");
        }
        else
        {
            Debug.LogWarning($"[JobListItem.UpdateDisplay] {job.name} cueObject为空");
        }

        jobName.text = job.name;
       // jobDescription.text = "消耗：" + job.apCost.ToString() + "AP";
        jobPrice.text = "报酬：$<color=#912710>" + job.rewards[job.currentLevel]+ "</color>";
        //icon.sprite = Resources.Load<Sprite>("WorkIcon/" + job.imageName);
        JobLevelText.text = GetJobLevelName(job.currentLevel);
        
        if (Order != null)
        {
            Order.text = order > 0 ? order.ToString() : "";
            Order.gameObject.SetActive(order > 0);
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
            _ => "未知等级" // 防止等级超出范围
        };
    }
}
