using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloseTab : MonoBehaviour
{
    public GameObject tab;
    public void Close()
    {
        
        tab.SetActive(false);
    }
}
