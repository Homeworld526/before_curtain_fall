using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // 添加对EventSystem的引用

public class NPC : MonoBehaviour
{
    public CharData charData;
    public string characterName;
    public int currentAffection;
    public AffectionConfig affectionConfig;
    public GameObject uiPanel; // 要显示的 UI 面板
    private bool isShowingPanel; // 记录面板是否正在显示
    public TextMeshProUGUI AffectionText; // 显示当前好感度的 Text
    public TextMeshProUGUI NameText; // 显示角色名称的 Text

    private void OnEnable()
    {

    }
    private void OnDisable()
    {
        AffectionManager.OnAffectionChanged -= UpdateUI;
    }
    void Start()
    {
        AffectionManager.OnAffectionChanged += UpdateUI;
        characterName = affectionConfig.characterName;
        Debug.Log("角色名称：" + characterName);
        currentAffection = AffectionManager.Instance.GetAffection(charData.characterId);
    }

    private void OnDestroy()
    {
        // OnAffectionChanged 是静态事件，直接取消订阅，避免事件指向已销毁的对象
        AffectionManager.OnAffectionChanged -= UpdateUI;
    }

    void Update()
    {

        if (Input.GetMouseButtonDown(0))
        {
            // 检查鼠标是否悬停在UI元素上
            if (EventSystem.current.IsPointerOverGameObject())
            {
                return; // 如果点击事件被UI元素阻挡，直接返回不处理
            }
            Camera mainCamera = Camera.main;

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // 使用LayerMask来限制检测的层
            int layerMask = 1 << gameObject.layer; // 只检测对象所在层

            if (Physics.Raycast(ray, out hit, 100f, layerMask))
            {
                Debug.Log($"击中对象: {hit.collider.gameObject.name}", hit.collider.gameObject);

                if (hit.collider.gameObject == gameObject)
                {
                    AffectionManager.Instance.selectcharacterId = characterName;
                    ToggleUIPanel();
                }
            }
            else
            {
                Debug.Log("未击中任何对象");
            }
        }
    }


    void UpdateUI(string characterName)
    {
        if (this == null) return;

        if (AffectionText != null)
        {
            int affection = AffectionManager.Instance.GetAffection(characterName);
            Debug.Log("从AffectionManager获取的好感度：" + affection);
            currentAffection = affection;
            AffectionText.text = "好感度： " + currentAffection.ToString();
        }
    }



    void ToggleUIPanel()
    {
        isShowingPanel = !isShowingPanel;

        if (uiPanel != null)
        {
            uiPanel.SetActive(isShowingPanel);
            int affection = AffectionManager.Instance.GetAffection(characterName);
            currentAffection = affection;
            AffectionText.text = "好感度： " + currentAffection.ToString();
            NameText.text = "角色：" + characterName;
            Debug.Log(isShowingPanel ? "显示角色信息面板" : "隐藏角色信息面板");
        }
        else
        {
            Debug.LogError("uiPanel未分配！");
        }
    }
}
