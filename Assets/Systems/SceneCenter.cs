using System.Collections;
using System.Collections.Generic;
//using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
//using Cinemachine;

public class SceneCenter : MonoBehaviour
{
    //public BlackScreen bs;
    public static SceneCenter Instance { get; private set; }
    //public Transform player;
    //public int dialogIndex;
    //public Sprite dialogProfile;
    //public TextAsset dialogFile;
    //public tp dialogendFunc;
    //public tp closeDialog;

    //public int PPTsI;
    //public int PPTeI;
    //public tp PPTedel;

    //public CinemachineVirtualCamera cam;
    // public NPCProgresser np;

    //public Transform tutorial2;
    public void Init()
    {
        //bs.InstantBlack();
        //SceneManager.LoadScene("GeneralUI", LoadSceneMode.Additive);
        //SceneAdder.instance.AddScene("Dialogue");
        //dialogendFunc = new tp(bs.InstantUnBlack);

        //closeDialog = new tp(SceneAdder.instance.RemoveScene);
    }

    private void OnEnable()
    {
        if (Instance != null) Destroy(Instance.gameObject);
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
        currentScene = SceneManager.GetActiveScene().name;
        //GetComponent<PPTLoader>().OpenPPT();
    }

    public List<string> scenes;
    public string currentScene;

    public void ChangeScene(string sceneName)
    {
        if (scenes.Contains(sceneName) && currentScene != sceneName)
        {
            // 切换场景前清理所有静态状态，避免残留旧状态
            StaticStateResetManager.ResetAllStaticState();
            
            SceneManager.LoadScene(sceneName);
            //player.Translate(playerPos - player.position);
            currentScene = sceneName;
        }
    }

    public void ClearAfterLoad(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= ClearAfterLoad;
        StartCoroutine(DelClearAfterLoad());
    }

    private IEnumerator DelClearAfterLoad()
    {
        yield return new WaitUntil(() =>
        {
            return (TutorialManager.Instance != null && PrologueUIManager.Instance != null &&
                    StoryDotManager.Instance != null);
        });
        if (TutorialManager.Instance != null)
            TutorialManager.Instance.ResetForNewGame();
        if (PrologueUIManager.Instance != null)
            PrologueUIManager.Instance.ResetForNewGame();
        TutorialStepRevealer.ResetForNewGame();
        if (StoryDotManager.Instance != null)
            StoryDotManager.Instance.ResetForNewGame();
    }
}
