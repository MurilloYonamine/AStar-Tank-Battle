// Autor: Murillo Gomes Yonamine
// Data: 02/10/2026

using UnityEngine;
using UnityEngine.UIElements;

public class SettingsMenu : IMenu
{
    private VisualElement _settingsScreen;
    private Button _settingsBackButton;

    private Slider _masterSlider;
    private Slider _musicSlider;
    private Slider _sfxSlider;

    private Label _masterValueLabel;
    private Label _musicValueLabel;
    private Label _sfxValueLabel;

    private MenuManager _menuManager;

    public void Initialize(VisualElement root, MenuManager menuManager)
    {
        _menuManager = menuManager;

        _settingsScreen = root.Q<VisualElement>("SettingsScreen");
        _settingsBackButton = root.Q<Button>("SettingsBackButton");

        _masterSlider = root.Q<Slider>("MasterSlider");
        _musicSlider = root.Q<Slider>("MusicSlider");
        _sfxSlider = root.Q<Slider>("SFXSlider");

        _masterValueLabel = root.Q<Label>("MasterValueLabel");
        _musicValueLabel = root.Q<Label>("MusicValueLabel");
        _sfxValueLabel = root.Q<Label>("SFXValueLabel");

        _settingsBackButton.clicked += OnBackClicked;

        _masterSlider.RegisterValueChangedCallback(OnMasterVolumeChanged);
        _musicSlider.RegisterValueChangedCallback(OnMusicVolumeChanged);
        _sfxSlider.RegisterValueChangedCallback(OnSFXVolumeChanged);

        InitAudioSettings();
    }

    public void Dispose()
    {
        _settingsBackButton.clicked -= OnBackClicked;

        _masterSlider.UnregisterValueChangedCallback(OnMasterVolumeChanged);
        _musicSlider.UnregisterValueChangedCallback(OnMusicVolumeChanged);
        _sfxSlider.UnregisterValueChangedCallback(OnSFXVolumeChanged);
    }

    public void Show()
    {
        _settingsScreen.style.display = DisplayStyle.Flex;
    }

    public void Hide()
    {
        _settingsScreen.style.display = DisplayStyle.None;
    }

    private void OnBackClicked()
    {
        _menuManager.OpenMainMenu();
    }

    private void OnMasterVolumeChanged(ChangeEvent<float> evt)
    {
        AudioManager.Instance.SetMasterVolume(evt.newValue);
        _masterValueLabel.text = Mathf.RoundToInt(evt.newValue * 100f) + "%";
    }

    private void OnMusicVolumeChanged(ChangeEvent<float> evt)
    {
        AudioManager.Instance.SetMusicVolume(evt.newValue);
        _musicValueLabel.text = Mathf.RoundToInt(evt.newValue * 100f) + "%";
    }

    private void OnSFXVolumeChanged(ChangeEvent<float> evt)
    {
        AudioManager.Instance.SetSFXVolume(evt.newValue);
        _sfxValueLabel.text = Mathf.RoundToInt(evt.newValue * 100f) + "%";
    }

    private void InitAudioSettings()
    {
        float masterVal = AudioManager.Instance.GetSavedVolume(AudioManager.MASTER_VOLUME_PARAM);
        float musicVal = AudioManager.Instance.GetSavedVolume(AudioManager.MUSIC_VOLUME_PARAM);
        float sfxVal = AudioManager.Instance.GetSavedVolume(AudioManager.SFX_VOLUME_PARAM);

        _masterSlider.SetValueWithoutNotify(masterVal);
        _musicSlider.SetValueWithoutNotify(musicVal);
        _sfxSlider.SetValueWithoutNotify(sfxVal);

        _masterValueLabel.text = Mathf.RoundToInt(masterVal * 100f) + "%";
        _musicValueLabel.text = Mathf.RoundToInt(musicVal * 100f) + "%";
        _sfxValueLabel.text = Mathf.RoundToInt(sfxVal * 100f) + "%";
    }
}
