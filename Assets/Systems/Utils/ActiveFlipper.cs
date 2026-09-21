using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActiveFlipper : MonoBehaviour
{
    public void ActiveFlip(GameObject obj)
    {
        obj.SetActive(!obj.activeSelf);
    }
}
