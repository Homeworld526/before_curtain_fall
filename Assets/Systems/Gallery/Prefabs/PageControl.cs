using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PageControl : MonoBehaviour
{
    public List<LoadGalleryElement> elements;
    public int page = 1;

    public void SetGalleryOrder(int index)
    {
        page = 1;
        CGUnlockSystem.Instance.SetGalleryOrder(index);
        RefreshSlots();
    }

    private void RefreshSlots()
    {
        foreach (var VARIABLE in elements)
        {
            VARIABLE.page = page;
            VARIABLE.RefreshGalleryPage();
        }
    }

    public void AddPage()
    {
        if (page * 6 < CGUnlockSystem.Instance.filename.Count)
        {
            page++;
            RefreshSlots();
        }
    }

    public void MinusPage()
    {
        if (page > 1)
        {
            page--;
            RefreshSlots();
        }
    }

    [SerializeField]
    private TextMeshProUGUI pageText;
    
    void Update()
    {
        pageText.text =
            page.ToString() + "/" + (CGUnlockSystem.Instance.filename.Count / 6 + 1).ToString();
    }
}
