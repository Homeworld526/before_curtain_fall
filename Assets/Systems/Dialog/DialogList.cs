using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public interface IDialogAction
{
    void AddAction();
}

public class DiaPlayer : MonoBehaviour
{

}

public class DialogList : SingleCase<DialogList> 
{
    public delegate void DialogAction(string param);
    public DialogVisual visual;
    public List<TextAsset> dialogs;
    public List<DialogAction> act = new List<DialogAction>();
    public List<Transform> hosts;
    public List<bool> isMiddle;
    public DiaPlayer player;
    private List<DialogStatMonitorBase> monitors = new List<DialogStatMonitorBase>();
    private Dictionary<string, int> monitorNames = new Dictionary<string, int>();
    private List<DialogStatChangerBase> changers = new List<DialogStatChangerBase>();
    private Dictionary<string, int> changerNames = new Dictionary<string, int>();

    public DialogStatDataSO statData;

    #region SmallTalk

        public GameObject smallTalkPrefab;
        public SmallTalkController smallTalk;
        public void StartSmallTalk(string name, System.Action onFinish)
        {
            int index = GetDialogNum(name);
            StartSmallTalk(index, onFinish);
        }

        public void StartSmallTalk(int index, System.Action onFinish)
        {
            smallTalk.gameObject.SetActive(true);
            smallTalk.StartTalk(dialogs[index], onFinish);
        }
    
    #endregion
    
    
    public int GetDialogNum(string name)
    {
        for (int i = 0; i < dialogs.Count; i++)
        {
            if (dialogs[i].name == name)
                return i;
        }
        return 0;
    }
    
    public void ReloadGame(int index)
    {
        InvokeDialog(PlayerPrefs.GetInt("dialogIndex"), PlayerPrefs.GetInt("dialogLine"), index);
    }
    
    
    public void AddMonitor<T>(string name, MonoBehaviour host) where T : DialogStatMonitorBase, new()
    {
        monitors.Add(new T());
        monitors[monitors.Count - 1]._host = host;
        monitorNames.Add(name, monitors.Count - 1);
    }
    public void AddChanger<T>(string name, MonoBehaviour host) where T : DialogStatChangerBase, new()
    {
        changers.Add(new T());
        changers[changers.Count - 1]._host = host;
        changerNames.Add(name, changers.Count - 1);
    }

    public bool CheckMonitor(string name, int value)
    {
        int val = statData != null ? statData.GetInt(name, -1) : PlayerPrefs.GetInt(name, -1);
        Debug.Log("获取数值" + name + ": " + val);
        Debug.Log("需求数值: " + value);
        Debug.Log("比较结果" + (val >= value));
        return val >= value;
    }

    public void ChangeValue(string name, int value)
    {
        Debug.Log("修改数值" + name + ": " + value);
        if (statData != null)
            statData.SetInt(name, value);
        else
            PlayerPrefs.SetInt(name, value);
        StatEventCenter.Instance.ChangeStat(name, value);
    }

    #region debug

    public bool debbug;
    public int debbugnum;

    #endregion
    
    
    private void Start()
    {
        Resources.UnloadUnusedAssets();
        //visual = GetComponent<DialogVisual>();
        act.Clear();
        for(int i = 0; i < hosts.Count; i++)
        {
             hosts[i].GetComponent<IDialogAction>().AddAction();
        }
        DialogAssembler.Instance.Init();
    }

    public int currentIndex { get; private set; }
    // 静态镜像，确保 Instance 被 disable 后 PrologueUIManager 等仍能读到对话索引
    public static int CurrentIndex { get; private set; }

    /// <summary>
    /// 存档加载期间设为 true，阻止 OnEnable 自动启动对话。
    /// 由 SaveSystem 在加载流程中控制。
    /// </summary>
    public static bool SuppressAutoDialog { get; set; }

    private void OnEnable()
    {
        if (dialogs.Count == 0) return;

        // 存档加载期间不自动启动对话，由 SaveSystem 负责恢复
        if (SuppressAutoDialog)
        {
            return;
        }

        if (PlayerPrefs.GetInt("SmallTalk", 0) > 0)
        {
            return;
        }

        if (debbug)
        {
            StartSmallTalk(debbugnum, null);
            return;
        }

        int line = PlayerPrefs.GetInt("dialogLine", 0);

        InvokeDialog(PlayerPrefs.GetInt("dialogIndex"), line);
    }

    [SerializeField] private Toggle skipbut;
    
    public void InvokeEndAct(string param)
    {
        if(skipbut.isOn) skipbut.isOn = false;
        StartCoroutine(EndDelay(param));
    }

    private IEnumerator EndDelay(string param)
    {
        SaveSystem.Instance.WaitForSaveBlockToClear = true;
        yield return new WaitForSeconds(0.6f);
        act[_pendingActIndex].Invoke(param);
        StatEventCenter.Instance.onDialogEnd?.Invoke(param);
        Resources.UnloadUnusedAssets();
        SaveSystem.Instance.WaitForSaveBlockToClear = false;
    }

    // 直接读 Dialog GameObject 的激活状态，无需手动维护布尔值
    public static bool IsInDialog =>
        DialogLocator.Instance != null && DialogLocator.Instance.Dialog != null && DialogLocator.Instance.Dialog.activeSelf;

    private int _pendingActIndex;

    public void InvokeDialog(int index)
    {
        currentIndex = index;
        CurrentIndex = index;
        _pendingActIndex = index;
        var x = player;
        //var x = GameObject.FindFirstObjectByType<IPlayer>();
        if(x!=null)  x.enabled = false;

        
        if (index < dialogs.Count && index >= 0)
        {
            //Debug.Log("yes");
            
            visual.dialogFile = dialogs[index];
            //visual.startIndex = 0;
            visual.DialogStart();
            if (isMiddle[index])
            {
                PicRSetMiddleEffect.Instance.StartEffect(true);
            }
            else
            {
                PicRSetMiddleEffect.Instance.StartEffect(false);
            }
        }
    }
    public void InvokeDialog(int index, int line)
    {
        currentIndex = index;
        CurrentIndex = index;
        _pendingActIndex = index;
        var x = player;
        //var x = GameObject.FindFirstObjectByType<IPlayer>();
        if (x != null) x.enabled = false;


        if (index < dialogs.Count && index >= 0)
        {
            //Debug.Log("yes");
            visual.dialogFile = dialogs[index];
            visual.startIndex = line;
            visual.DialogStart();
            if (isMiddle[index])
            {
                PicRSetMiddleEffect.Instance.StartEffect(true);
            }
            else
            {
                PicRSetMiddleEffect.Instance.StartEffect(false);
            }
            
        }
    }
    public void InvokeDialog(int index, int startline, int line)
    {
        Debug.LogWarning("重载"+index+" "+ startline+" "+ line);
        currentIndex = index;
        CurrentIndex = index;
        _pendingActIndex = index;
        var x = player;
        //var x = GameObject.FindFirstObjectByType<IPlayer>();
        if (x != null) x.enabled = false;


        if (index < dialogs.Count && index >= 0)
        {
            //Debug.Log("yes");
            visual.dialogFile = dialogs[index];
            visual.startIndex = startline;
            visual.DialogStart(line);
            if (isMiddle[index])
            {
                PicRSetMiddleEffect.Instance.StartEffect(true);
            }
            else
            {
                PicRSetMiddleEffect.Instance.StartEffect(false);
            }
            
        }
    }

    /// <summary>
    /// 重置静态状态（由 StaticStateResetManager 在存读档时调用）
    /// </summary>
    public static void ResetStaticState()
    {
        CurrentIndex = 0;
        SuppressAutoDialog = false;
        Debug.Log("[DialogList] 静态状态已重置: CurrentIndex=0, SuppressAutoDialog=false");
    }
}
