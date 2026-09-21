using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ST_BlackScreen : SmallTalkElements, IMoveNextPreventer
{
    public CanvasGroup cgGroup;
    
    //private PicSwaper swaper {get => GetComponent<PicSwaper>(); }

    protected override void OnMoveNext(DialogNode node)
    {
        Debug.Log($"[ST_BlackScreen] OnMoveNext: node.note = {node.note}");
        
        bool hasBlackTransition = false;
        if (!string.IsNullOrEmpty(node.note))
        {
            foreach (var VARIABLE in node.note.Split('+'))
            {
                Debug.Log($"[ST_BlackScreen] 检查节点: {VARIABLE}");
                if (VARIABLE == "黑屏转场")
                {
                    hasBlackTransition = true;
                    if (cgGroup.alpha < 1f)
                    {
                        Debug.Log("[ST_BlackScreen] 触发黑屏淡入");
                        StartCoroutine(Delay());
                        CanvasGroupFader.FadeTo(cgGroup, 1f, 0.5f, () => { 
                            Debug.Log("[ST_BlackScreen] 淡入完成回调触发");
                            inFade = false; 
                        });
                        return;
                    }
                }
            }
        }

        if (!hasBlackTransition && cgGroup.alpha > 0f)
        {
            Debug.Log("[ST_BlackScreen] 触发黑屏淡出");
            StartCoroutine(Delay());
            CanvasGroupFader.FadeTo(cgGroup, 0f, 0.5f, () => {
                Debug.Log("[ST_BlackScreen] 淡出完成回调触发");
                inFade = false;
            });
        }
        
        
    }

    private IEnumerator Delay()
    {
        yield return null;
        inFade = true;
    }
    
    private bool inFade = false;
    public bool CanMoveNext()
    {
        if (inFade)
        {
            Debug.LogWarning("[ST_BlackScreen] 正在淡入淡出，阻止推进");
        }
        return !inFade;
    }
    
    public void ForceReset()
    {
        Debug.Log("[ST_BlackScreen] ForceReset - 重置状态");
        inFade = false;
        StopAllCoroutines();
        CanvasGroupFader.CancelFade(cgGroup, 0f);
    }
    
    public bool GetInFadeState()
    {
        return inFade;
    }
}
