using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class GraphicFlipper : MonoBehaviour
{
    [SerializeField] private Sprite FalseSprite;
    [SerializeField] private Sprite TrueSprite;
    
    public void Flip(bool flip)
    {
        GetComponent<Image>().sprite = flip ? TrueSprite : FalseSprite;
        DialogVisual.Instance.clickDelay++;
    }
}
