using UnityEngine;

public class SyncShow : MonoBehaviour
{
    [Header("把你要同步显示的【教学按钮】拖到这里")]
    public GameObject tutorialButton;

    [Header("存档key：当前已解锁的教学阶段")]
    public string currentStageKey = "CurrentTutorialStage";

    // 当挂载这个脚本的物体被 SetActive(true) 显示时，Unity会自动执行这里
    private void OnEnable()
    {
        if (tutorialButton != null)
        {
            tutorialButton.SetActive(true); // 同步显示教学按钮
        }
    }

    // 当挂载这个脚本的物体被 SetActive(false) 隐藏时，Unity会自动执行这里
    private void OnDisable()
    {
        if (tutorialButton != null)
        {
            tutorialButton.SetActive(false); // 同步隐藏教学按钮
        }
    }

    // 按钮点击时调用此方法呼出教学
    public void ShowTutorial()
    {
        Debug.Log("[教学] ShowTutorial 被调用, Instance=" + (TutorialManager.Instance != null));
        if (TutorialManager.Instance != null)
        {
            TutorialStepRevealer.nextOpenByButton = true;
            TutorialManager.Instance.ShowManualTutorial();
        }
    }
}