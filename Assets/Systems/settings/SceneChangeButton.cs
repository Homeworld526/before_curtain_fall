using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneChangeButton : MonoBehaviour
{
    public void OnPressed()
    {
        SceneCenter.Instance.ChangeScene("Start");
    }
}
