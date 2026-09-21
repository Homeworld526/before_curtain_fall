using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIHider : MonoBehaviour
{
    [SerializeField] private List<GameObject> elements;
    [SerializeField] private Toggle toggle;

    private bool isHidden = false;
    private DialogVisual _dialogVisual;
    private AutoButton[] _autoButtons;

    void Start()
    {
        _dialogVisual = FindObjectOfType<DialogVisual>();
        _autoButtons = FindObjectsOfType<AutoButton>();
        SyncToggle();
    }

    void Update()
    {
        /*if (Input.GetKeyDown(KeyCode.Space))
        {
            // 取消 Toggle 焦点，防止 EventSystem Submit 再触发一次
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);

            if (toggle != null)
                toggle.isOn = !toggle.isOn;
            else
            {
                isHidden = !isHidden;
                ApplyElements();
            }
        }*/
    }

    public void ToggleUI()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        if (toggle != null)
            toggle.isOn = !toggle.isOn;
        else
        {
            isHidden = !isHidden;
            ApplyElements();
        }
    }
    
    
    private void OnToggleChanged(bool value)
    {
        isHidden = !value;
        ApplyElements();
    }

    private void ApplyElements()
    {
        bool show = !isHidden;
        foreach (var element in elements)
            element.SetActive(show);
        if (_dialogVisual != null)
            _dialogVisual.IsUIHidden = isHidden;
        if (isHidden && _autoButtons != null)
            foreach (var btn in _autoButtons)
                if (btn.GetComponent<Toggle>().isOn)
                    btn.GetComponent<Toggle>().isOn = false;
    }

    private void SyncToggle()
    {
        if (toggle == null) return;
        toggle.onValueChanged.RemoveListener(OnToggleChanged);
        toggle.isOn = !isHidden;
        toggle.onValueChanged.AddListener(OnToggleChanged);
    }
}
