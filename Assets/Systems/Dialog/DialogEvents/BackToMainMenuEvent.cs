using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackToMainMenuEvent : DialogEventHost
{
    public GameObject Dialog;
    public CanvasGroup EndingScreen;
    public GameObject Home;
    public GameObject PrepRoom;
    public GameObject Player;

    [ContextMenu("跳过序章")]
    public void SkipPrologue()
    {
        DialogEndEvent("1");
    }
    
    protected override void DialogEndEvent(string param)
    {
        //Debug.LogWarning(param);
        //base.DialogEndEvent();
        if (param == "跳转主界面")
        {
            Debug.Log("触发");
            PlayerPrefs.SetInt("dialogIndex", 0);
            PlayerPrefs.SetInt("dialogLine", 0);
            PlayerPrefs.SetInt("Endings",1);
            //SceneCenter.Instance.ChangeScene("MainMenu");
            StartCoroutine(DialogEnd());
            
        }
        else
        {
            Dialog.SetActive(false);
            PrepRoom.SetActive(false);
            Home.SetActive(true);
            foreach (var button in FindObjectOfType<ShowDialog>().Buttons)
            {
                if (!button.activeInHierarchy)
                {
                    button.SetActive(true);
                }
            }
            FindObjectOfType<ShowDialog>().Hide2();
            //Player?.SetActive(false);
            SoundsManager.Instance.PlayMusic("小平房间循环曲");
            // 教学只在序章第一次结束时弹出（后续角色剧情结束不再弹）
            if (PrologueUIManager.Instance != null && !PrologueUIManager.IsPrologueCompleted)
            {
                PrologueUIManager.Instance.CompletePrologue();
            }
        }

    }

    
    
    private IEnumerator DialogEnd()
    {
        yield return FadeInOut.FadeIn(EndingScreen,1f,0.5f, this);
        while (!Input.GetKeyUp(KeyCode.Mouse0))
        {
            yield return null;
        }
        yield return null;
        SceneCenter.Instance.ChangeScene("Start");
    }
}
