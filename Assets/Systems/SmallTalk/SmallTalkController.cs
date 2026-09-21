using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public interface IMoveNextPreventer
{
    public bool CanMoveNext();
}

public class SmallTalkElements : MonoBehaviour
{
    public SmallTalkController host;
    
    
    public void OnEnable()
    {
        host.OnMoveNext += OnMoveNext;
    }

    private void OnDisable()
    {
        if (host.enabled)
        {
            host.OnMoveNext -= OnMoveNext;
        }
    }

    protected virtual void OnMoveNext(DialogNode node)
    {
        
    }
}

public class SmallTalkController : MonoBehaviour
{
    public Transform Panel;
    public GameObject invObject;
    
    private DialogTree dialogTree = new DialogTree();
    private bool hasRefreshedInv = false;

    public List<Transform> AdditionalPreventer = new List<Transform>();
    
    public Action<DialogNode> OnMoveNext;

    public delegate bool Preventer();
    
    public Preventer CanMoveNext;

    private CanvasGroup _canvasGroup;
    
    private bool GetPreventers()
    {
        bool res = true;
        foreach (IMoveNextPreventer children in GetComponentsInChildren<IMoveNextPreventer>())
        {
            bool canMove = children.CanMoveNext();
            if (!canMove)
            {
                Debug.Log($"[GetPreventers] {children.GetType().Name} 阻止推进");
            }
            res &= canMove;
        }

        foreach (var VARIABLE in AdditionalPreventer)
        {
            bool canMove = VARIABLE.GetComponent<IMoveNextPreventer>().CanMoveNext();
            if (!canMove)
            {
                Debug.Log($"[GetPreventers] AdditionalPreventer {VARIABLE.name} 阻止推进");
            }
            res &= canMove;
        }
        return res;
    }
    
    private void OnEnable()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        CanMoveNext = null;
        foreach (IMoveNextPreventer children in GetComponentsInChildren<IMoveNextPreventer>())
        {
            CanMoveNext += children.CanMoveNext;
            CanMoveNext -= children.CanMoveNext;
        }
    }

    private bool init = false;
    private System.Action onfinish;
    private bool isPrinting = false;
    private bool waitForClick = false;
    private int lineCount = 0;
    private bool skipRequested = false; // 跳过请求标志

    private void Update()
    {
        if (!init) return;
        
        // 检查跳过请求
        if (skipRequested)
        {
            skipRequested = false;
            EndTalk();
            return;
        }
        
        RefreshInvMaterials();

        DialogNode current = dialogTree.ShowDialog();
        if (current.type == DialogType.End)
        {
            Debug.Log($"[SmallTalk] 播放结束，共 {lineCount} 句");
            EndTalk();
            return;
        }

        if (isPrinting)
        {
            foreach (ST_TextPrinter printer in GetComponentsInChildren<ST_TextPrinter>())
            {
                if (!printer.CanMoveNext())
                    return;
            }
            isPrinting = false;
            waitForClick = true;
            return; // 打印完成当帧不检测点击，避免残留输入导致自动跳过
        }

        if (waitForClick && Input.GetKeyDown(KeyCode.Mouse0))
        {
            waitForClick = false;
            Progress();
            lineCount++;
            isPrinting = true;
        }
    }
    
    private void RefreshInvMaterials()
    {
        if (invObject != null)
        {
            Image[] images = invObject.GetComponentsInChildren<Image>(true);
            foreach (Image img in images)
            {
                if (img.material == null)
                {
                    Debug.LogWarning($"[SmallTalkController] Image {img.name} 材质丢失，重新赋值");
                    img.material = img.defaultMaterial;
                }
                
                if (img.sprite == null)
                {
                    Debug.LogWarning($"[SmallTalkController] Image {img.name} Sprite 丢失");
                }
            }
        }
    }

    public void StartTalk(TextAsset asset, System.Action onFinish, bool autoPlay = true)
    {
        Debug.Log("[StartTalk] 开始新对话 (手动点击播放模式)");
        init = true;
        dialogTree.BuildTree(asset);
        dialogTree.StartTree(0);
        
        
        foreach (IMoveNextPreventer preventer in GetComponentsInChildren<IMoveNextPreventer>())
        {
            if (preventer is ST_BlackScreen)
            {
                ST_BlackScreen blackScreen = preventer as ST_BlackScreen;
                Debug.Log("[StartTalk] 重置 ST_BlackScreen 状态");
                blackScreen.ForceReset();
            }
        }
        
        StartCoroutine(RestartInvObjectCoroutine());
        
        OnMoveNext.Invoke(dialogTree.ShowDialog());
        isPrinting = true;
        _canvasGroup.blocksRaycasts = true;
        CanvasGroupFader.FadeTo(_canvasGroup, 1f, 0.5f);
        onfinish = onFinish;
    }

    public void EndTalk()
    {
        Debug.Log("[EndTalk] 开始结束对话");
        init = false;
        _canvasGroup.blocksRaycasts = false;
        
        foreach (IMoveNextPreventer preventer in GetComponentsInChildren<IMoveNextPreventer>())
        {
            if (preventer is ST_BlackScreen)
            {
                ST_BlackScreen blackScreen = preventer as ST_BlackScreen;
                Debug.Log($"[EndTalk] 调用 ForceReset() 前的 inFade 状态: {blackScreen.GetInFadeState()}");
                blackScreen.ForceReset();
                Debug.Log($"[EndTalk] 调用 ForceReset() 后的 inFade 状态: {blackScreen.GetInFadeState()}");
            }
        }

        Resources.UnloadUnusedAssets();
        
        CanvasGroupFader.FadeTo(_canvasGroup, 0f, 0.3f, () => {
            Debug.Log("[EndTalk] 淡入淡出完成，触发回调");
            Panel.gameObject.SetActive(false); 
            onfinish?.Invoke(); 
        });
    }
    
    /// <summary>
    /// 跳过当前小剧场
    /// </summary>
    public void Skip()
    {
        Debug.Log($"[SmallTalkController] Skip() 被调用, init={init}");
        if (init)
        {
            Debug.Log("[SmallTalkController] 设置 skipRequested = true");
            skipRequested = true;
        }
        else
        {
            Debug.LogWarning("[SmallTalkController] init 为 false，无法跳过");
        }
    }
    
    private void Progress()
    {
        DialogNode currentDialog = dialogTree.ShowDialog();
        Debug.Log($"[Progress] 当前对话类型: {currentDialog.type}, 内容: \"{currentDialog.content}\"");
        
        if (currentDialog.type == DialogType.End)
        {
            Debug.Log("[Progress] 当前是 End 节点，调用 EndTalk()");
            EndTalk();
            return;
        }
        
        dialogTree.Proceed();
        
        DialogNode nextDialog = dialogTree.ShowDialog();
        Debug.Log($"[Progress] 下一个节点类型: {nextDialog.type}, 内容: \"{nextDialog.content}\"");
        
        if (nextDialog.type == DialogType.End || string.IsNullOrEmpty(nextDialog.content))
        {
            Debug.Log("[Progress] 下一个节点是 End 或内容为空，直接结束");
            EndTalk();
            return;
        }
        
        OnMoveNext.Invoke(nextDialog);
    }
    
    private IEnumerator RestartInvObjectCoroutine()
    {
        yield return null;
        
        if (invObject != null)
        {
            Debug.Log("[SmallTalkController] 重启 Inv 对象并刷新材质");
            
            Image[] images = invObject.GetComponentsInChildren<Image>(true);
            foreach (Image img in images)
            {
                if (img.material == null)
                {
                    Debug.Log($"[SmallTalkController] Image {img.name} 材质为空，尝试刷新");
                    img.material = img.defaultMaterial;
                }
            }
            
            invObject.SetActive(false);
            yield return null;
            invObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[SmallTalkController] invObject 未赋值");
        }
    }
}
