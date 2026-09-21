using UnityEngine;

public class InteractionUIManager : MonoBehaviour
{
    public static InteractionUIManager Instance;

    public InteractionUI uiPrefab;
    private InteractionUI ui;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 场景中创建一个唯一的 UI 实例
        ui = Instantiate(uiPrefab, transform);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void ShowUI(Transform target, string message)
    {
        ui.Show(target, message);
    }

    public void HideUI()
    {
        ui.Hide();
    }
}