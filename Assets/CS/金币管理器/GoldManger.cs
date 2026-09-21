using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GoldManger : MonoBehaviour
{
    public int gold;
    public static GoldManger Instance;
    public TextMeshProUGUI goldText;

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
    public void AddGold(int amount)
    {
        gold += amount;
        UpdateUI();
    }

    public void RemoveGold(int amount)
    {
        gold -= amount;
        UpdateUI();
    }
    void UpdateUI()
    {
        goldText.text = gold.ToString() + "\n" + 100000;
    }
}
