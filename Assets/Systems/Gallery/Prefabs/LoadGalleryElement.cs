using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class LoadGalleryElement : MonoBehaviour
{
    public int galleryIndex;
    public Image preview;
    public Transform prevvisual;
    public int page = 1;

    public Transform Textele;
    public TextMeshProUGUI Text;
    public Transform LockedText;

    public void RefreshGalleryPage()
    {
        int ind = galleryIndex + (6 * (page - 1));
        if (ind >= CGUnlockSystem.Instance.filename.Count)
        {
            UnInit("");
            return;
        }

        string mainFile = CGUnlockSystem.Instance.GetFile(ind);
        string resourceName = CGUnlockSystem.IsVideo(mainFile)
            ? mainFile.Substring("VIDEO_".Length)
            : mainFile;
        if (Resources.Load(resourceName) == null)
        {
            UnInit(mainFile);
            return;
        }
        if (!CGUnlockSystem.Instance.IsUnlock(ind))
        {
            UnInit(mainFile);
            return;
        }

        if (CGUnlockSystem.IsVideo(mainFile))
        {
            Sprite cover = CGUnlockSystem.Instance.FindCoverSprite(ind);
            if (cover == null)
            {
                UnInit(mainFile);
                return;
            }
            Init(mainFile, cover);
        }
        else
        {
            Init(mainFile, Resources.Load<Sprite>(mainFile));
        }
    }

    private void OnEnable()
    {
        RefreshGalleryPage();
    }

    public void Init(string file, Sprite coverSprite)
    {
        int ind = galleryIndex + (6 * (page - 1));
        GetComponent<Button>().onClick.AddListener(() => ShowBigPic.Instance.Show(file, ind, 0));
        prevvisual.gameObject.SetActive(true);
        preview.sprite = coverSprite;
        LockedText.gameObject.SetActive(false);
        Textele.gameObject.SetActive(true);
        Text.text = CGUnlockSystem.Instance.GetDes(ind);
    }

    public void UnInit(string spriteName)
    {
        GetComponent<Button>().onClick.RemoveAllListeners();
        prevvisual.gameObject.SetActive(false);
        LockedText.gameObject.SetActive(true);
        Textele.gameObject.SetActive(false);
    }
}
