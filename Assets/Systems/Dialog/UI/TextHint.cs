using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(Toggle))]
public class TextHint : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private string text;
    [SerializeField] private GameObject hintPrefab;
    [SerializeField] private Vector3 offset = new Vector3(0, -30, 0);

    private GameObject currentHint;
    private Toggle button;
    private RectTransform _rect;
    private Camera _uiCamera;

    void Start()
    {
        button = GetComponent<Toggle>();
        _rect = GetComponent<RectTransform>();

        Canvas canvas = GetComponentInParent<Canvas>();
        _uiCamera = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? canvas.worldCamera
            : null;
    }

    void Update()
    {
        if (currentHint == null) return;
        if (!RectTransformUtility.RectangleContainsScreenPoint(_rect, Input.mousePosition, _uiCamera))
            HideHint();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (string.IsNullOrEmpty(text)) return;
        ShowHint();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideHint();
    }

    private void ShowHint()
    {
        if (currentHint != null) return;

        if (hintPrefab == null)
        {
            currentHint = new GameObject("HintText");
            TextMeshProUGUI hintText = currentHint.AddComponent<TextMeshProUGUI>();
            hintText.text = text;
            hintText.fontSize = 14;
            hintText.color = Color.white;
            hintText.alignment = TextAlignmentOptions.Center;

            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
                currentHint.transform.SetParent(canvas.transform, false);
        }
        else
        {
            currentHint = Instantiate(hintPrefab, transform.parent);
            TextMeshProUGUI hintText = currentHint.GetComponent<TextMeshProUGUI>();
            if (hintText != null)
                hintText.text = text;
        }

        if (currentHint != null)
        {
            currentHint.transform.position = transform.position + offset;
            currentHint.SetActive(true);
        }
    }

    private void HideHint()
    {
        if (currentHint != null)
        {
            Destroy(currentHint);
            currentHint = null;
        }
    }

    private void OnDestroy()
    {
        HideHint();
    }
}
