using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PageController : MonoBehaviour
{
    public List<GameObject> pages = new List<GameObject>();
    public TextMeshProUGUI pageText;

    private int _currentPage = 0;

    private void OnEnable()
    {
        for (int i = 0; i < pages.Count; i++)
        {
            if (pages[i] != null && pages[i].activeSelf)
            {
                _currentPage = i;
                UpdateText();
                return;
            }
        }
    }

    private void Start()
    {
        ShowPage(_currentPage);
    }

    public void NextPage()
    {
        if (_currentPage >= pages.Count - 1) return;
        ShowPage(_currentPage + 1);
    }

    public void PrevPage()
    {
        if (_currentPage <= 0) return;
        ShowPage(_currentPage - 1);
    }

    private void ShowPage(int index)
    {
        for (int i = 0; i < pages.Count; i++)
            if (pages[i] != null) pages[i].SetActive(i == index);

        _currentPage = index;
        UpdateText();
    }

    private void UpdateText()
    {
        if (pageText != null)
            pageText.text = $"{_currentPage + 1}/{pages.Count}";
    }
}
