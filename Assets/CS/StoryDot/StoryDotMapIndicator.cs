using UnityEngine;

public class StoryDotMapIndicator : MonoBehaviour
{
    [Tooltip("地图上的感叹号 GameObject")]
    public GameObject exclamationMark;

    private void OnEnable()
    {
        if (StoryDotManager.Instance != null)
        {
            StoryDotManager.Instance.OnStoryStateChanged += Refresh;
        }
        Refresh();
    }

    private void OnDisable()
    {
        if (StoryDotManager.Instance != null)
        {
            StoryDotManager.Instance.OnStoryStateChanged -= Refresh;
        }
    }

    public void Refresh()
    {
        if (exclamationMark == null) return;
        bool hasAny = StoryDotManager.Instance != null && StoryDotManager.Instance.HasAnyAvailable();
        exclamationMark.SetActive(hasAny);
    }
}
