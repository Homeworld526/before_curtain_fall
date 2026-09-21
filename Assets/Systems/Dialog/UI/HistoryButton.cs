using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class HistoryButton : MonoBehaviour
{
    //private bool status = false;
    public CanvasGroup target;
    public Image image;
    private Material originalMaterial;
    private Material blurMaterial;
    public Sprite sprite;
    private bool _fromFlipToggle;

    public void FlipToggle()
    {
        _fromFlipToggle = true;
        GetComponent<Toggle>().isOn = !GetComponent<Toggle>().isOn;
        _fromFlipToggle = false;
    }
    
    private void Awake()
    {
        //image = GetComponent<Image>();
        originalMaterial = image.material;
        blurMaterial = new Material(Shader.Find("Unlit/UI_BGBlur"));
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseHistory();
        }
    }
    
    private void OnDestroy()
    {
        if (blurMaterial != null)
        {
            Destroy(blurMaterial);
        }
    }

    public void CloseHistory()
    {
        bool status = GetComponent<Toggle>().isOn;
        if (status)
        {
            GetComponent<Toggle>().isOn = false;
        }
    }
    
    public void ToggleHistory()
    {
        bool status = GetComponent<Toggle>().isOn;
        if (status)
        {
            if (blurMaterial != null)
            {
                Destroy(blurMaterial);
            }
            blurMaterial = new Material(Shader.Find("Unlit/UI_BGBlur"));
            target.alpha = 1;
            target.interactable = true;
            target.blocksRaycasts = true;
            DialogVisual.Instance.IsHistory = true;
            foreach (var ins in GameObject.FindObjectsOfType<ScrollviewAdder>())
            {
                ins.ScrollToBottomView();
            }
            //image.material = blurMaterial;
            //status = true;
        }
        else
        {
            target.alpha = 0;
            target.interactable = false;
            target.blocksRaycasts = false;
            
            if (!_fromFlipToggle)
                DialogVisual.Instance.clickDelay++;
            DialogVisual.Instance.IsHistory = false;
            
            //image.material = originalMaterial;
            //status = false;
            GetComponent<Image>().sprite = sprite;
        }
    }
}
