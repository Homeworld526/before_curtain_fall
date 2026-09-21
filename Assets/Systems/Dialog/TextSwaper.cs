using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class TextSwaper : MonoBehaviour
{
    public float transitionTime = 2.5f;

    private void OnEnable()
    {
        StartCoroutine(TextShow());
    }

    private IEnumerator TextShow()
    {
        float t = 0;
        float alpha = 0;
        for (int i = 0; i < GetComponent<TextMeshProUGUI>().text.Length; i++)
        {
            SetCharacterAlpha(GetComponent<TextMeshProUGUI>(), i, alpha);
        }
        while (t < transitionTime)
        {
            alpha = Mathf.Clamp01(alpha + (1 / transitionTime) * Time.deltaTime);
            for(int i = 0;i < GetComponent<TextMeshProUGUI>().text.Length;i++)
            {
                SetCharacterAlpha(GetComponent<TextMeshProUGUI>(), i, alpha);
            }
            yield return null;
        }
        alpha = 1;
        for (int i = 0; i < GetComponent<TextMeshProUGUI>().text.Length; i++)
        {
            SetCharacterAlpha(GetComponent<TextMeshProUGUI>(), i, alpha);
        }
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
}

