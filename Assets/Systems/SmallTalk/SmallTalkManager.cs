using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmallTalkManager : SingleCase<SmallTalkManager>
{
    public GameObject dialog;
    public GameObject invObject;
    public GameObject mainDialogRoot;

    private CanvasGroup _mainDialogCG;

    private CanvasGroup GetMainDialogCG()
    {
        if (mainDialogRoot == null) return null;
        if (_mainDialogCG == null)
            _mainDialogCG = mainDialogRoot.GetComponent<CanvasGroup>()
                            ?? mainDialogRoot.AddComponent<CanvasGroup>();
        return _mainDialogCG;
    }

    public void HideMainDialogRoot()
    {
        var cg = GetMainDialogCG();
        if (cg == null) return;
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
    }

    public void RestoreMainDialogRoot()
    {
        var cg = GetMainDialogCG();
        if (cg == null) return;
        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;
    }

    private void OnDisable()
    {
        RestoreMainDialogRoot();
    }

    public IEnumerator PlayCharacterSideStory(string name, bool unloadResources = true)
    {
        Debug.Log("[SmallTalkManager] 开始播放小剧场: " + name);

        RestartInvObject();

        if (DialogVisual.Instance != null)
        {
            Debug.Log("[SmallTalkManager] 禁用 DialogVisual");
            DialogVisual.Instance.enabled = false;
        }

        HideMainDialogRoot();

        dialog.GetComponent<Canvas>().sortingOrder = 2;
        dialog.SetActive(true);
        bool finished = false;
        DialogList.Instance.StartSmallTalk(name, () => {
            Debug.Log("[SmallTalkManager] 回调触发: finished = true");
            finished = true;
            if (unloadResources)
            {
                Resources.UnloadUnusedAssets();
            }
        });

        Debug.Log("[SmallTalkManager] 等待小剧场结束...");
        while (!finished)
        {
            yield return null;
        }

        Debug.Log("[SmallTalkManager] 小剧场结束，重新启用 DialogVisual");
        if (DialogVisual.Instance != null)
        {
            DialogVisual.Instance.enabled = true;
        }
        RestoreMainDialogRoot();
        dialog.GetComponent<Canvas>().sortingOrder = 0;
        Debug.Log("[SmallTalkManager] 小剧场播放完成");
    }

    public void SkipCurrentSmallTalk()
    {
        Debug.Log("[SmallTalkManager] SkipCurrentSmallTalk 被调用");

        if (DialogList.Instance == null)
        {
            Debug.LogWarning("[SmallTalkManager] DialogList.Instance 为 null");
            return;
        }

        if (DialogList.Instance.smallTalk == null)
        {
            Debug.LogWarning("[SmallTalkManager] DialogList.Instance.smallTalk 为 null");
            return;
        }

        Debug.Log("[SmallTalkManager] 调用 smallTalk.Skip()");
        DialogList.Instance.smallTalk.Skip();
    }

    public bool IsPlayingSmallTalk()
    {
        bool result = DialogList.Instance != null && DialogList.Instance.smallTalk != null && DialogList.Instance.smallTalk.isActiveAndEnabled;
        Debug.Log($"[SmallTalkManager] IsPlayingSmallTalk: {result}");
        return result;
    }

    private void RestartInvObject()
    {
        GameObject targetInv = null;

        if (invObject != null)
        {
            targetInv = invObject;
        }
        else
        {
            Transform invTransform = dialog.transform.Find("Inv");
            if (invTransform != null)
            {
                targetInv = invTransform.gameObject;
            }
            else if (DialogVisual.Instance != null)
            {
                Transform visualInv = DialogVisual.Instance.transform.Find("Inv");
                if (visualInv != null)
                {
                    targetInv = visualInv.gameObject;
                }
            }
            else
            {
                targetInv = GameObject.Find("Inv");
            }
        }

        if (targetInv != null)
        {
            Debug.Log("[SmallTalkManager] 重启 Inv 对象");
            targetInv.SetActive(false);
            targetInv.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[SmallTalkManager] 未找到 Inv 对象");
        }
    }
}
