using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


[RequireComponent(typeof(Toggle))]
public class SettingButton : MonoBehaviour
{
    public GameObject settings;
    public GameObject blur;

    public Toggle History;
    public JobManager job;
    public ShowMapButton map;

    private void Start()
    {
        SaveManager.Instance.reloadActions += ForceToggle;
    }

    private void TurnOffAutoButtons()
    {
        foreach (var auto in FindObjectsOfType<AutoButton>())
        {
            var toggle = auto.GetComponent<Toggle>();
            if (toggle != null && toggle.isOn)
                toggle.isOn = false;
        }
    }

    private void Update()
    {
        bool affectionOpen = AffectionManager.Instance != null
            && AffectionManager.Instance.affectionUI != null
            && AffectionManager.Instance.affectionUI.gameObject.activeInHierarchy;

        if (!History.isOn && !job.isShow && !map.isShow && !affectionOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || (Input.GetKeyUp(KeyCode.Mouse1) && GetComponent<Toggle>().isOn))
            {
                GetComponent<Toggle>().isOn = !GetComponent<Toggle>().isOn;
            }
        }
    }

    public void toggleSettings()
    {
        bool status = GetComponent<Toggle>().isOn;
        if (status)
        {
           // blur.SetActive(true);
            settings.SetActive(true);
            TurnOffAutoButtons();
        }
        else
        {
            //blur.SetActive(false);
            settings.SetActive(false);
        }
        DialogVisual.Instance.IsPause = status;
    }

    public void ForceToggle()
    {
        GetComponent<Toggle>().isOn = !GetComponent<Toggle>().isOn;
    }
}
