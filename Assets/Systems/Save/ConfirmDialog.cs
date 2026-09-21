using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConfirmDialog : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private Action onConfirmCallback;
    public event Action OnCancel;

    private void Start()
    {
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmClicked);
        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelClicked);
    }

    public void Show(string message, Action onConfirm)
    {
        if (messageText != null)
            messageText.text = message;

        onConfirmCallback = onConfirm;
        gameObject.SetActive(true);
    }

    private void OnConfirmClicked()
    {
        onConfirmCallback?.Invoke();
        Destroy(gameObject);
    }

    private void OnCancelClicked()
    {
        OnCancel?.Invoke();
        Destroy(gameObject);
    }
}
