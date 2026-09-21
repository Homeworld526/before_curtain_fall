using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GameSystem;

public class JobListManager : MonoBehaviour
{
    public static JobListManager Instance { get; private set; }
    [Header("设置")]
    public GameObject Content;          // 父对象容器
    public GameObject normalJobPrefab;  // 普通打工预制体
    public GameObject characterJobPrefab; // 角色打工预制体

    public List<JobListItem> _jobSlots = new List<JobListItem>();
    public List<CharacterJobListItem> _characterJobSlots = new List<CharacterJobListItem>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void RefreshAll()
    {
        for (int i = 0; i < _jobSlots.Count; i++)
        {
            if (_jobSlots[i] != null) 
            {
                _jobSlots[i].UpdateDisplay(i + 1);
            }
        }
        
        foreach (var slot in _characterJobSlots)
        {
            if (slot != null) slot.UpdateDisplay();
        }
    }
    public void Nextweek()
    {
        foreach (var slot in _jobSlots)
        {
            slot.job.curcd = 0;
        }
        
        foreach (var slot in _characterJobSlots)
        {
            slot.job.curcd = 0;
        }
        
        RefreshAll();
    }
    public void InitList(Dictionary<string, JobData> _allJobs, bool isCharacterJobs = false)
    {
        _jobSlots.Clear();
        _characterJobSlots.Clear();

        for (int i = Content.transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(Content.transform.GetChild(i).gameObject);
        }

        List<JobData> jobsToProcess = new List<JobData>();
        
        foreach (var jobData in _allJobs)
        {
            if (jobData.Key == "躺平")
            {
                continue;
            }

            bool shouldUseCharacterPrefab = isCharacterJobs || jobData.Value.ischar;
            
            if (shouldUseCharacterPrefab)
            {
                if (!IsCharacterJobUnlocked(jobData.Value))
                {
                    continue;
                }
                
                if (characterJobPrefab == null)
                {
                    continue;
                }
                GameObject newItemObj = Instantiate(characterJobPrefab);
                newItemObj.transform.SetParent(Content.transform, false);
                CharacterJobListItem item = newItemObj.GetComponent<CharacterJobListItem>();
                if (item != null)
                {
                    item.job = jobData.Value;
                    _characterJobSlots.Add(item);
                }
            }
            else
            {
                if (!IsJobUnlockedByDialog(jobData.Value))
                {
                    continue;
                }
                
                if (normalJobPrefab == null)
                {
                    continue;
                }
                GameObject newItemObj = Instantiate(normalJobPrefab);
                newItemObj.transform.SetParent(Content.transform, false);
                JobListItem item = newItemObj.GetComponent<JobListItem>();
                if (item != null)
                {
                    item.job = jobData.Value;
                    _jobSlots.Add(item);
                }
            }
        }
        RefreshAll();
        JobManager.Instance.UpdateSwitchButCueState();
    }

    private Dictionary<string, bool> _jobUnlockedState = new Dictionary<string, bool>();
    
    /// <summary>
    /// 清空打工解锁状态缓存（供存档系统使用）
    /// </summary>
    public void ClearJobUnlockedStateCache()
    {
        _jobUnlockedState.Clear();
    }
    
    /// <summary>
    /// 预填充所有打工的解锁状态缓存（供存档系统使用）
    /// 避免读档后第一次刷新列表时误判为"刚解锁"而重置 hasClickedCue
    /// </summary>
    public void PreFillUnlockedStateCache()
    {
        if (JobManager.Instance == null) return;

        foreach (var kvp in JobManager.Instance._allJobs)
        {
            JobData job = kvp.Value;
            bool isUnlocked = true;
            if (!string.IsNullOrEmpty(job.unlockDialogKeyword))
            {
                isUnlocked = DialogKeywordDetector.IsKeywordUnlocked(job.unlockDialogKeyword);
            }
            _jobUnlockedState[job.name] = isUnlocked;
        }

        foreach (var kvp in JobManager.Instance._allcharJobs)
        {
            JobData job = kvp.Value;
            bool isUnlocked = true;
            if (!string.IsNullOrEmpty(job.unlockDialogKeyword))
            {
                isUnlocked = DialogKeywordDetector.IsKeywordUnlocked(job.unlockDialogKeyword);
            }
            _jobUnlockedState[job.name] = isUnlocked;
        }
    }
    
    bool IsCharacterJobUnlocked(JobData job)
    {
        bool wasUnlocked = _jobUnlockedState.ContainsKey(job.name) && _jobUnlockedState[job.name];
        bool isNowUnlocked = true;
        
        if (!string.IsNullOrEmpty(job.unlockDialogKeyword))
        {
            if (!DialogKeywordDetector.IsKeywordUnlocked(job.unlockDialogKeyword))
            {
                Debug.Log($"[IsCharacterJobUnlocked] {job.name} 需要完成剧情: {job.unlockDialogKeyword}");
                isNowUnlocked = false;
            }
        }
        
        if (!wasUnlocked && isNowUnlocked)
        {
            job.hasClickedCue = false;
            Debug.Log($"[IsCharacterJobUnlocked] {job.name} 刚解锁，设置hasClickedCue为false");
        }
        
        _jobUnlockedState[job.name] = isNowUnlocked;
        return isNowUnlocked;
    }
    
    bool IsJobUnlockedByDialog(JobData job)
    {
        bool wasUnlocked = _jobUnlockedState.ContainsKey(job.name) && _jobUnlockedState[job.name];
        bool isNowUnlocked = true;
        
        if (!string.IsNullOrEmpty(job.unlockDialogKeyword))
        {
            if (!DialogKeywordDetector.IsKeywordUnlocked(job.unlockDialogKeyword))
            {
                Debug.Log($"[IsJobUnlockedByDialog] {job.name} 需要完成剧情: {job.unlockDialogKeyword}");
                isNowUnlocked = false;
            }
        }
        
        if (!wasUnlocked && isNowUnlocked)
        {
            job.hasClickedCue = false;
            Debug.Log($"[IsJobUnlockedByDialog] {job.name} 刚解锁，设置hasClickedCue为false");
        }
        
        _jobUnlockedState[job.name] = isNowUnlocked;
        return isNowUnlocked;
    }
    
    public bool HasUnclickedCueInJobs()
    {
        if (JobManager.Instance == null) return false;
        
        foreach (var job in JobManager.Instance._allJobs.Values)
        {
            if (job != null && job.name != "躺平" && IsJobUnlockedByDialog(job) && !job.hasClickedCue)
            {
                return true;
            }
        }
        return false;
    }
    
    public bool HasUnclickedCueInCharJobs()
    {
        if (JobManager.Instance == null) return false;
        
        foreach (var job in JobManager.Instance._allcharJobs.Values)
        {
            if (job != null && IsCharacterJobUnlocked(job) && !job.hasClickedCue)
            {
                return true;
            }
        }
        return false;
    }

    private void OnDestroy()
    {
        // 清空静态变量，避免指向已销毁的对象
        if (Instance == this)
        {
            Instance = null;
        }
    }
}