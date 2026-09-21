using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CGTracker : MonoBehaviour
{

    private string currentpic;

    void OnEnable()
    {
        currentpic = GetComponent<Image>().sprite.name;
        DialogVisual.Instance.OnTextDisplayStart += UpdateCG;
    }
    
    void OnDisable()
    {
        currentpic = GetComponent<Image>().sprite.name;
        DialogVisual.Instance.OnTextDisplayStart -= UpdateCG;
        GetComponent<PicSwaper>().CleanClones();
    }
    
    
    void UpdateCG()
    {
        string newCG = DialogVisual.Instance.GetCG();
        if (currentpic != newCG)
        {
            if (DialogVisual.LoadSprite(newCG) == null)
            {
                return;
            }
            if (GetComponent<PicSwaper>())
            {
                currentpic = newCG;
                GetComponent<PicSwaper>().PicChange(DialogVisual.LoadSprite(newCG));
            }
            else
            {
                currentpic = newCG;
                GetComponent<Image>().sprite = DialogVisual.LoadSprite(newCG);
            }
        }
    }
}
