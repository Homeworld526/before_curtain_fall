using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class startgame : MonoBehaviour
{
    public void StartGame()
    {
        SceneCenter.Instance.ChangeScene("SampleScene");
    }
    
}
