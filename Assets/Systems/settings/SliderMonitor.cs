using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(TextMeshProUGUI))]
public class SliderMonitor : MonoBehaviour
{
    private TextMeshProUGUI text;
    public float modifier = 1f;
    
    void Start()
    {
        text = GetComponent<TextMeshProUGUI>();
    }
    public Slider slider;
    void Update()
    {
        text.text = Mathf.FloorToInt(slider.value * modifier).ToString();  
    }
}
