using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class HistoryBlockDistributer : MonoBehaviour
{
    public bool IsEnding = false;
    public TextMeshProUGUI Name;
    public TextMeshProUGUI Content;
    public Image Face;
    public Image Icon;

    private string _content;
    private Coroutine _printCoroutine; 
    
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

    public void Addtext(string added, float time)
    {
        int l = Content.text.Length;
        Content.text += added;
        for (int i = l; i < Content.text.Length; i++)
        {
            SetCharacterAlpha(Content, i, 0f);
        }
        StartCoroutine(FadeIn(l, time));
    }

    private IEnumerator FadeIn(int l, float time)
    {
       
        float t = 0;
        while (t < time)
        {
            for (int i = l; i < Content.text.Length; i++)
            {
                SetCharacterAlpha(Content, i, Mathf.Clamp01(t/time));
            }
            t+= Time.deltaTime;
            yield return null;
        }
        for (int i = l; i < Content.text.Length; i++)
        {
            SetCharacterAlpha(Content, i, 1);
        }
        
    }
    
    
    public void HideIcon()
    {
        Color color = Icon.color;
        color.a = 0;
        Icon.color = color;
    }

    /// <summary>
    /// 协程：控制Content透明度从0逐渐增大到1
    /// </summary>
    private IEnumerator FadeInContent(float duration = 0.5f)
    {
        Color color = Content.color;
        float startAlpha = color.a;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, 1f, elapsed / duration);
            color.a = alpha;
            Content.color = color;
            yield return null;
        }

        // 确保最终透明度为1
        color.a = 1f;
        Content.color = color;
        _printCoroutine = null;
    }

    private void Update()
    {
        if(_printCoroutine != null && Input.GetKeyUp(KeyCode.Mouse0))
        {
            StopCoroutine(_printCoroutine);
            _printCoroutine = null;
            Color tmp = Content.color;
            tmp.a = 1;
            Content.color = tmp;
        }
    }

    public void Distribute(Sprite face, string name, string content)
    {
        _content = content;
        if (IsEnding)
        {
            if(_content.Contains('>')) _content = _content.Split('>')[1];
        }
        Color Namec = Name.color;
        Color Facec = Face.color;
        Namec.a = 1f;
        if(face == null) Facec.a = 0f;
        Name.color = Namec;
        Face.color = Facec;

        Name.text = name;
        if (IsEnding)
        {
            // 初始透明度为0
            Color tmp = Content.color;
            tmp.a = 0f;
            Content.color = tmp;
            Content.text = _content;
            _printCoroutine = StartCoroutine(FadeInContent());
        }
        else Content.text = _content;
        Face.sprite = face;

        if(name == KeyWords.protagonist)
        {
            Content.color = new Color32(0xff,0xed,0xb2,0xff);
            Name.color = new Color32(0xff, 0xed, 0xb2, 0xff);
        }
    }

    public void Distribute(string name, string content)
    {
        _content = content;
        if (IsEnding) _content = _content.Split('>')[1];
        Color Namec = Name.color;
        Color Facec = Face.color;
        Namec.a = 0f;
        Facec.a = 0f;
        Name.color = Namec;
        Face.color = Facec;
        if (IsEnding)
        {
            
            // 初始透明度为0
            Color tmp = Content.color;
            tmp.a = 0f;
            Content.color = tmp;
            Content.text = _content;
            _printCoroutine = StartCoroutine(FadeInContent());
        }
        else Content.text = _content;
        if(name == KeyWords.pb) if(!IsEnding)Content.color = new Color32(0xbe,0xbe,0xbe,0xff);
        if(name == KeyWords.protagonist)
        {
            Content.color = new Color32(0xff,0xed,0xb2,0xff);
            //Name.color = new Color32(0xff, 0xed, 0xb2, 0xff);
        }
    }
    
    
}
