using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BackpackButton : MonoBehaviour
{
    public Button button;
    public GameObject backpack;
    private bool isBackpackOpen = false; // 添加一个变量来控制背包的开关状态

    void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(ToggleBackpack); // 将打开背包的逻辑改为切换背包的逻辑
    }

    public void ToggleBackpack()
    {
        if (isBackpackOpen)
        {
            CloseBackpack();
        }
        else
        {
            OpenBackpack();
        }
        isBackpackOpen = !isBackpackOpen; // 更新背包的开关状态
    }

    public void OpenBackpack()
    {
        backpack.SetActive(true);
    }

    public void CloseBackpack()
    {
        backpack.SetActive(false);
    }
}
