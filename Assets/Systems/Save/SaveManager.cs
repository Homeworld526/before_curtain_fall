using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class SaveManager: SingleCase<SaveManager>
{
    public List<SaveVisual> slots;
    public List<SaveVisual> slotsLoad;
    public GameObject textPrefab;
    public GameObject confirmDialogPrefab;

    public Action reloadActions;

    private const int PageSize = 6;
    private int _currentPage = 0;
    public int TotalPages = 3;

    public event Action<int, int> OnPageChanged; // (currentPage, totalPages)
    public int CurrentPage => _currentPage;

    public void NextPage()
    {
        if (_currentPage < TotalPages - 1)
        {
            _currentPage++;
            ApplyPage();
        }
    }

    public void PrevPage()
    {
        if (_currentPage > 0)
        {
            _currentPage--;
            ApplyPage();
        }
    }

    private void ApplyPage()
    {
        for (int i = 0; i < slots.Count; i++)
            slots[i].SetPage(_currentPage * PageSize + i);
        for (int i = 0; i < slotsLoad.Count; i++)
            slotsLoad[i].SetPage(_currentPage * PageSize + i);
        OnPageChanged?.Invoke(_currentPage, TotalPages);
    }

    public void OnComplete(bool complete, int saveSlot)
    {
        if (!complete) return;
        int slotIndex = saveSlot - _currentPage * PageSize;
        if (slotIndex >= 0 && slotIndex < slots.Count)
            slots[slotIndex].UpdateVisual(SaveSystem.Instance.GetAllSaveInfos()[saveSlot].Pic, SaveSystem.Instance.GetAllSaveInfos()[saveSlot]);
    }

    public void UpdateAllVisuals()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            int saveSlot = _currentPage * PageSize + i;
            slots[i].UpdateVisual(SaveSystem.Instance.GetAllSaveInfos()[saveSlot].Pic, SaveSystem.Instance.GetAllSaveInfos()[saveSlot]);
        }
    }
    
    public void AddSave(int saveSlot)
    {
        ShowConfirmDialog("确认保存？", () => PerformSave(saveSlot));
    }

    public void RemoveSave(int saveSlot)
    {
        SaveSystem.Instance.DeleteSave(saveSlot);
        slots[saveSlot].UpdateVisual("",SaveSystem.Instance.GetAllSaveInfos()[saveSlot]);
    }

    public void LoadSave(int saveSlot)
    {
        ShowConfirmDialog("确认读取？", () => PerformLoad(saveSlot));
    }

    private void PerformSave(int saveSlot)
    {
        if (BlackoutTransition.Instance != null && BlackoutTransition.Instance.IsTransitioning) return;
        SaveSystem.Instance.SaveGame(saveSlot, OnComplete);
    }

    private void PerformLoad(int saveSlot)
    {
        SaveSystem.Instance.LoadGame(saveSlot);
        reloadActions?.Invoke();
    }

    private bool _isConfirmDialogOpen = false;

    private void ShowConfirmDialog(string message, Action onConfirm)
    {
        if (confirmDialogPrefab == null)
        {
            onConfirm?.Invoke();
            return;
        }

        if (_isConfirmDialogOpen) return;
        _isConfirmDialogOpen = true;

        Canvas canvas = slots[0].GetComponentInParent<Canvas>();
        GameObject dialogInstance = Instantiate(confirmDialogPrefab, canvas.transform);
        ConfirmDialog confirmDialog = dialogInstance.GetComponent<ConfirmDialog>();
        if (confirmDialog != null)
        {
            confirmDialog.Show(message, () =>
            {
                _isConfirmDialogOpen = false;
                onConfirm?.Invoke();
            });
            confirmDialog.OnCancel += () => _isConfirmDialogOpen = false;
        }
        else
        {
            Debug.LogError("ConfirmDialog component not found on prefab!");
            Destroy(dialogInstance);
            _isConfirmDialogOpen = false;
        }
    }
    
}
