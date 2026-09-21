using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TitleMatching : MonoBehaviour, IEffect
{

    public bool IsRunning()
    {
        throw new System.NotImplementedException();
    }

    public void StartEffect(string param)
    {
        if (param == "左对齐")
        {
            GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;
        }
        else if (param == "中对齐")
        {
            StartCoroutine(midAlign());
        }
    }

    private IEnumerator midAlign()
    {
        yield return new WaitForSeconds(0.25f);
        GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
    }
    
    public void PauseEffect()
    {
        throw new System.NotImplementedException();
    }

    public void ContinueEffect()
    {
        throw new System.NotImplementedException();
    }

    public void ResetEffect()
    {
        throw new System.NotImplementedException();
    }
}
