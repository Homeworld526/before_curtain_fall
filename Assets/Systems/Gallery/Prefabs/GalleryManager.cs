using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GalleryManager : SingleCase<GalleryManager>
{
    public GameObject slotPrefab;
    public List<GameObject> slots;
    
    
    private void OnEnable()
    {
        foreach (var VARIABLE in slots)
        {
            Destroy(VARIABLE);
        }
        slots.Clear();
        for (int i = 0; i < 6; i++)
        {
            slots.Add(Instantiate(slotPrefab, new Vector3(0, 0, 0), Quaternion.identity, this.transform));
        }
    }
    
    
}
