using UnityEngine;
using UnityEngine.UI;

public class TestAdvanceDay : MonoBehaviour
{
    public Button advanceDayBtn;

    private void Awake()
    {
        if (advanceDayBtn != null)
        {
            advanceDayBtn.onClick.AddListener(OnAdvanceDayClick);
            var text = advanceDayBtn.GetComponentInChildren<Text>();
            if (text != null) text.text = "推进一天(空打工)";
        }
    }

    public void OnAdvanceDayClick()
    {
        if (JobManager.Instance != null)
        {
            JobManager.Instance.AdvanceOneDayAndSetEmpty();
            Debug.Log("[TestAdvanceDay] 已调用 AdvanceOneDayAndSetEmpty()");
        }
        else
        {
            Debug.LogError("[TestAdvanceDay] JobManager.Instance 为空！");
        }
    }
}
