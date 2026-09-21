using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneAdder : MonoBehaviour
{
    public static SceneAdder instance;
    public bool SceneIsAdded = false;
    string currentSceneAdded = null;
    //public PlayerController playerController;

    public void Awake()
    {
        instance = this;
    }

    public void GetPlayer()
    {
        //playerController = GameObject.FindWithTag("Player").GetComponent<PlayerController>();
        //Debug.Log(playerController.gameObject.name);
    }

    public void AddScene(string sceneName)
    {
        //playerController.PlayerFreeze();
        SceneIsAdded = true;
        if(currentSceneAdded == null)
        {
            SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
            currentSceneAdded = sceneName;
        }
        else if(currentSceneAdded != sceneName)
        {
            SceneManager.UnloadSceneAsync(currentSceneAdded);
            SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
            currentSceneAdded = sceneName;
        }
        else
        {
            return;
        }
    }
    public void RemoveScene()
    {
        //playerController.PlayerUnFreeze(); ;
        if (currentSceneAdded == null)
        {
            return;
        }
        else{
            SceneManager.UnloadSceneAsync(currentSceneAdded);
            currentSceneAdded = null;
            SceneIsAdded = false;
        }
    }
}
