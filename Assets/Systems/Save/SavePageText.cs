using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class SavePageText : MonoBehaviour
{
    [SerializeField] private string format = "{0}/{1}";
    private TextMeshProUGUI _text;

    private void Awake()
    {
        _text = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        SaveManager.Instance.OnPageChanged += Refresh;
        Refresh(SaveManager.Instance.CurrentPage, SaveManager.Instance.TotalPages);
    }

    private void OnDisable()
    {
        SaveManager.Instance.OnPageChanged -= Refresh;
    }

    private void Refresh(int current, int total)
    {
        _text.text = string.Format(format, current + 1, total);
    }
}
