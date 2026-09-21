using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartingDialog : MonoBehaviour
{
    public int line = 0;

    void Start()
    {
        PlayerPrefs.SetInt("dialogIndex", 0);
        PlayerPrefs.SetInt("dialogLine", line);
        DialogLocator.Instance.Dialog.SetActive(true);   
    }

    
}
