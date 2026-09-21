using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class ST_TextPrinter :SmallTalkElements, IMoveNextPreventer
{
    private Coroutine PrintRoutine;
    
    public Action OnTextDisplayEnd;
    public Action OnTextDisplayDelay;
    public Action OnTextDisplayStart;

    private TextMeshProUGUI text { get => GetComponent<TextMeshProUGUI>(); }
    public bool PrintOnHold = false;

    private string currentText;
    
    #region PrintTool
    private string unclosedTagCache = "";
    private int CollectFullRichTag(string str, int index)
        {
            int tagStartIndex = index;
            while (index < str.Length && str[index] != '>')
            {
                index++;
                if (index >= str.Length) break;
            }
    
            if (index < str.Length && str[index] == '>')
            {
                string fullTag = str.Substring(tagStartIndex, index - tagStartIndex + 1);
                if (fullTag[1] != '/')
                {
                    unclosedTagCache = fullTag;
                    //inRichTag = true;
                }
                else
                {
                    unclosedTagCache = "";
                    //inRichTag = false;
                }
            }
    
            return index;
        }
    
    public IEnumerator PrintText(TextMeshProUGUI text, string str, float interval)
        {
            yield return new WaitUntil(() => OnTextDisplayStart != null);
            
            OnTextDisplayStart?.Invoke();
            
            
            int i = 0;
            //string currentString;
            bool isTyping;

            if (str == "")
            {
                i = 0x3f3f3f3f;
            }
            else if (!str.StartsWith(text.text))
            {
                text.text = "";
                i = 0;
            }
            else
            {
                i = text.text.Length;
            }

            //Start
            //currentString = "";
            isTyping = true;
            //inRichTag = false;
            //unclosedTagCache = "";
    
            while (i < str.Length)
            {
                
                while (PrintOnHold)
                {
                    yield return null;
                }
    
                if (!isTyping)
                {
                    continue;
                }

                char currentChar = str[i]; //== '^'? '\n' : str[i];
                if (currentChar == '<')
                {
                    //inRichTag = true;
                    i = CollectFullRichTag(str, i) + 1;
                }
    
                text.text = str.Substring(0, i++);
                float alpha = 0f;
                SetCharacterAlpha(text, i - 2, alpha);
                while (alpha < 1f)
                {
                    alpha = Mathf.Clamp01(alpha + (1 / interval) * Time.deltaTime);
                    SetCharacterAlpha(text, i - 2, alpha);
                    yield return null;
                }
    
                //yield return new WaitForSeconds(interval);
            }
    
            text.text = str;
    
            OnTextDisplayEnd?.Invoke();
            yield return new WaitForSeconds(SettingManager.Instance.settings.AutoPlaySpeed);
            OnTextDisplayDelay?.Invoke();
            
            PrintRoutine = null;
    
        }
    
    void SetCharacterAlpha(TextMeshProUGUI tmpText, int charIndex, float alpha)
       {
           // 安全校验：字符索引需在有效范围内
           if (charIndex < 0 || charIndex >= tmpText.textInfo.characterCount)
               return;
   
           TMP_TextInfo textInfo = tmpText.textInfo;
           TMP_CharacterInfo charInfo = textInfo.characterInfo[charIndex];
           int materialIndex = charInfo.materialReferenceIndex; // 字符对应的材质索引
           Color32[] vertexColors = textInfo.meshInfo[materialIndex].colors32; // 顶点颜色数组
   
           // 每个字符由4个顶点组成，需同时修改这4个顶点的Alpha值
           int vertexIndex = charInfo.vertexIndex;
           byte alphaByte = (byte)(alpha * 255); // 转换为0-255的字节值
           vertexColors[vertexIndex + 0].a = alphaByte;
           vertexColors[vertexIndex + 1].a = alphaByte;
           vertexColors[vertexIndex + 2].a = alphaByte;
           vertexColors[vertexIndex + 3].a = alphaByte;
   
           // 标记顶点数据需要更新，让TextMeshPro刷新显示
           tmpText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
       }

    public void ClearPrint()
    {
        if (PrintRoutine != null)
        {
            StopCoroutine(PrintRoutine);
            PrintRoutine = null;
            
            
            //OnTextDisplayStart.Invoke();
            
            OnTextDisplayDelay?.Invoke();
        }
        OnTextDisplayStart.Invoke();
        text.text = "";
    }
    #endregion

    protected override void OnMoveNext(DialogNode node)
    {
        currentText = node.content;
        float speed = 0.05f;
        Debug.Log($"[TextPrinter] PrintSpeed={speed}, 文本=\"{node.content.Substring(0, Mathf.Min(node.content.Length, 20))}...\"");
        PrintRoutine = StartCoroutine(PrintText(text, currentText, speed));
    }

    public bool CanMoveNext()
    {
        return PrintRoutine == null;
    }

    private void Update()
    {
    }

    public void SkipPrint()
    {
        if (PrintRoutine != null)
        {
            StopCoroutine(PrintRoutine);
            PrintRoutine = null;
        }
        text.text = currentText;
        OnTextDisplayEnd?.Invoke();
        OnTextDisplayDelay?.Invoke();
    }
}
