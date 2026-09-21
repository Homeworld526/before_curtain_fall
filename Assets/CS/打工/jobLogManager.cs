using System.Collections.Generic;
using UnityEngine;
using GameSystem;

public class jobLogManager : MonoBehaviour
{
    [Header("UI 引用")]
    public GameObject Content;
    public GameObject normalJobPrefab;
    public GameObject charJobPrefab;

    [Header("对象池管理")]
    public List<JobLogItem> normalJobLogItems = new List<JobLogItem>();
    public List<JobLogItem> charJobLogItems = new List<JobLogItem>();
    public List<JobLogItem> actJobLogItems = new List<JobLogItem>();

    private void Start()
    {
        for (int i = 0; i < 15; i++)
        {
            GameObject newItemObj = Instantiate(normalJobPrefab, Content.transform, false);
            JobLogItem item = newItemObj.GetComponent<JobLogItem>();
            normalJobLogItems.Add(item);
            newItemObj.SetActive(false);
        }
        
        for (int i = 0; i < 6; i++)
        {
            GameObject newItemObj = Instantiate(charJobPrefab, Content.transform, false);
            JobLogItem item = newItemObj.GetComponent<JobLogItem>();
            charJobLogItems.Add(item);
            newItemObj.SetActive(false);
        }

        JobData[] emptyJobs = new JobData[21];
        updatelist(emptyJobs);
    }
    
    public void updatelist(JobData[] jobs)
    {
        foreach (var item in actJobLogItems)
        {
            item.gameObject.SetActive(false);
            if (item.job != null && item.job.ischar)
                charJobLogItems.Add(item);
            else
                normalJobLogItems.Add(item);
        }
        actJobLogItems.Clear();

        int colorIndex = 0;
        int i = 0;
        
        while (i < jobs.Length)
        {
            JobData job = jobs[i];
            JobLogItem item = null;
            
            if (job != null)
            {
                colorIndex++;
                
                if (job.ischar)
                {
                    if (charJobLogItems.Count > 0)
                    {
                        int lastIndex = charJobLogItems.Count - 1;
                        item = charJobLogItems[lastIndex];
                        charJobLogItems.RemoveAt(lastIndex);
                    }
                    else
                    {
                        item = Instantiate(charJobPrefab, Content.transform, false).GetComponent<JobLogItem>();
                    }

                    item.job = job;
                    item.UpdateDisplay(colorIndex, true);
                    item.gameObject.SetActive(true);
                    item.transform.SetSiblingIndex(i);
                    actJobLogItems.Add(item);
                    i += 3;
                }
                else
                {
                    int slots = job.apCost;
                    for (int j = 0; j < slots && i < jobs.Length; j++)
                    {
                        if (normalJobLogItems.Count > 0)
                        {
                            int lastIndex = normalJobLogItems.Count - 1;
                            item = normalJobLogItems[lastIndex];
                            normalJobLogItems.RemoveAt(lastIndex);
                        }
                        else
                        {
                            item = Instantiate(normalJobPrefab, Content.transform, false).GetComponent<JobLogItem>();
                        }

                        item.job = job;
                        item.UpdateDisplay(colorIndex, true);
                        item.gameObject.SetActive(true);
                        item.transform.SetSiblingIndex(i);
                        actJobLogItems.Add(item);
                        i++;
                    }
                }
            }
            else
            {
                // ==============================
                // 空位：创建但设置为空、透明
                // ==============================
                if (normalJobLogItems.Count > 0)
                {
                    int lastIndex = normalJobLogItems.Count - 1;
                    item = normalJobLogItems[lastIndex];
                    normalJobLogItems.RemoveAt(lastIndex);
                }
                else
                {
                    item = Instantiate(normalJobPrefab, Content.transform, false).GetComponent<JobLogItem>();
                }

                // 关键：设置为 NULL，让 JobLogItem 隐藏背景
                item.job = null;
                item.UpdateDisplay(0, false);
                item.gameObject.SetActive(true);
                item.transform.SetSiblingIndex(i);
                actJobLogItems.Add(item);
                i++;
            }
        }
    }
}