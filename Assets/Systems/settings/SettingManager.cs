using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingManager : SingleCase<SettingManager>
{
    public SettingsSO settings;

    public Slider MasterVolumeSlider;
    public Slider MusicVolumeSlider;
    public Slider SFXVolumeSlider;

    public Slider PrintSpeedSlider;
    public Slider AutoPlaySpeedSlider;

    public TMP_Dropdown AutoOptionDropDown;

    private void Start()
    {
        LoadSettings();

        if (MasterVolumeSlider != null) MasterVolumeSlider.onValueChanged.AddListener(SoundsManager.Instance.UpdateVolumes);
        SFXVolumeSlider.onValueChanged.AddListener(SoundsManager.Instance.UpdateVolumes);
        MusicVolumeSlider.onValueChanged.AddListener(SoundsManager.Instance.UpdateVolumes);
        if (AutoOptionDropDown != null) AutoOptionDropDown.onValueChanged.AddListener(OnAutoOptionChanged);
    }

    private void Update()
    {
        if (MasterVolumeSlider != null)
            settings.MasterVolume = MasterVolumeSlider.value;
        if (MusicVolumeSlider != null)
            settings.MusicVolume = MusicVolumeSlider.value;
        if (SFXVolumeSlider != null)
            settings.SFXVolume = SFXVolumeSlider.value;
        if (AutoPlaySpeedSlider != null)
            settings.AutoPlaySpeed = AutoPlaySpeedSlider.value;
        if (PrintSpeedSlider != null)
            settings.PrintSpeed = 0.1f / PrintSpeedSlider.value;
        if (AutoOptionDropDown != null)
            settings.OnlySkipReadDialog = AutoOptionDropDown.value == 0;

    }

    /// <summary>
    /// 保存设置到持久化存储
    /// </summary>
    public void SaveSettings()
    {
        settings.SaveToDisk();
    }

    /// <summary>
    /// 从持久化存储加载设置
    /// </summary>
    public void LoadSettings()
    {
        settings.LoadFromDisk();

        // 更新UI滑块的值
        if (MasterVolumeSlider != null)
            MasterVolumeSlider.value = settings.MasterVolume;
        if (MusicVolumeSlider != null)
            MusicVolumeSlider.value = settings.MusicVolume;
        if (SFXVolumeSlider != null)
            SFXVolumeSlider.value = settings.SFXVolume;
        if (PrintSpeedSlider != null)
            PrintSpeedSlider.value = settings.PrintSpeed;
        if (AutoPlaySpeedSlider != null)
            AutoPlaySpeedSlider.value = settings.AutoPlaySpeed;
        if (AutoOptionDropDown != null)
            AutoOptionDropDown.value = settings.OnlySkipReadDialog ? 0 : 1;
    }

    /// <summary>
    /// 重置为默认设置
    /// </summary>
    public void ResetToDefaults()
    {
        settings.MasterVolume = 1f;
        settings.MusicVolume = 1f;
        settings.SFXVolume = 1f;
        settings.PrintSpeed = 1f;
        settings.AutoPlaySpeed = 1f;
        settings.OnlySkipReadDialog = false;

        // 更新UI
        if (MasterVolumeSlider != null)
            MasterVolumeSlider.value = 1f;
        if (MusicVolumeSlider != null)
            MusicVolumeSlider.value = 1f;
        if (SFXVolumeSlider != null)
            SFXVolumeSlider.value = 1f;
        if (PrintSpeedSlider != null)
            PrintSpeedSlider.value = 1f;
        if (AutoPlaySpeedSlider != null)
            AutoPlaySpeedSlider.value = 1f;
        if (AutoOptionDropDown != null)
            AutoOptionDropDown.value = 1;

        // 保存默认设置
        SaveSettings();
    }

    private void OnAutoOptionChanged(int optionIndex)
    {
        settings.OnlySkipReadDialog = optionIndex == 0;
    }

    protected override void OnDestroy()
    {
        SaveSettings();
        base.OnDestroy();
    }

    protected override void OnApplicationQuit()
    {
        SaveSettings();
        base.OnApplicationQuit();
    }
    
}
