using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CloseDialog : MonoBehaviour
{
    public GameObject dialog; // 对话框对象
    void Start()
    {
        // 获取按钮组件并添加点击事件监听器
        Button btn = GetComponent<Button>();
        btn.onClick.AddListener(Hide);
    }
    public void Show()
    {
        // 显示对话框
        if (dialog != null)
        {
            dialog.SetActive(true);
        }
        else
        {
            Debug.LogError("对话框对象未设置");
        }
    }
    public void Hide()
    {
        // 隐藏对话框
        if (dialog != null)
        {
            dialog.SetActive(false);
        }
        else
        {
            Debug.LogError("对话框对象未设置");
        }

        // 重置对话状态，否则其他UI会被阻塞
        if (ShowDialog.Instance != null)
        {
            //ShowDialog.Instance.isDialogPlaying = false;
        }
    }
}
