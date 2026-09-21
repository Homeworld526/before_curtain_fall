using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DateManager : MonoBehaviour
{
    public static DateManager Instance;

    public int Day = 0;
    public TextMeshProUGUI DayText;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    public void DayChanged()
    {
        Day++;
        UpdateUI();
    }
    void UpdateUI()
    {
        DayText.text = 11 + "/" + (15 + Day);
    }
}
