//using Sirenix.Serialization;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]

public class SceneSwitch : MonoBehaviour
{
    private Collider2D _collider;
    public string sceneName;
    public int sceneIndex;
    public bool isProgress;
    //public Vector3 playerPos;

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
    }

    private void Update()
    {
        
    }

    private void OnTriggerEnter2D()
    {
       if(isProgress)
        {
            PlayerPrefs.SetInt("CurrentScene", sceneIndex);
            PlayerPrefs.DeleteKey("CurrentProgress");
        }
        SceneCenter.Instance.ChangeScene(sceneName);
    }
}
