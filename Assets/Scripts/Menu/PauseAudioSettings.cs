using System;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Binds authored pause sliders to Murilo's existing audio API and saved preferences.
/// Keeps menu/pause views synchronized without changing AudioManager or SettingsMenu.
/// </summary>
public sealed class PauseAudioSettings : IDisposable
{
    private readonly string[] parameters = { AudioManager.MASTER_VOLUME_PARAM, AudioManager.MUSIC_VOLUME_PARAM, AudioManager.SFX_VOLUME_PARAM };
    private readonly string[] names = { "Master", "Music", "SFX" };
    private readonly Slider[] menuSliders = new Slider[3], pauseSliders = new Slider[3];
    private readonly Label[] menuLabels = new Label[3], pauseLabels = new Label[3];
    private readonly EventCallback<ChangeEvent<float>>[] menuCallbacks = new EventCallback<ChangeEvent<float>>[3];
    private readonly EventCallback<ChangeEvent<float>>[] pauseCallbacks = new EventCallback<ChangeEvent<float>>[3];

    public PauseAudioSettings(VisualElement menuRoot, VisualElement pauseRoot)
    {
        for (int i = 0; i < names.Length; i++)
        {
            int channel = i;
            menuSliders[i] = menuRoot?.Q<Slider>(names[i] + "Slider");
            pauseSliders[i] = pauseRoot?.Q<Slider>("Pause" + names[i] + "Slider");
            menuLabels[i] = menuRoot?.Q<Label>(names[i] + "ValueLabel");
            pauseLabels[i] = pauseRoot?.Q<Label>("Pause" + names[i] + "ValueLabel");
            menuCallbacks[i] = _ => Refresh(); // The original menu callback has already saved this value.
            pauseCallbacks[i] = change => SetVolume(channel, change.newValue);
            menuSliders[i]?.RegisterValueChangedCallback(menuCallbacks[i]);
            pauseSliders[i]?.RegisterValueChangedCallback(pauseCallbacks[i]);
        }
        Refresh();
    }

    public void Refresh()
    {
        AudioManager audio = AudioManager.Instance;
        for (int i = 0; i < names.Length; i++)
        {
            float value = audio != null ? Mathf.Clamp01(audio.GetSavedVolume(parameters[i])) : 1f;
            menuSliders[i]?.SetValueWithoutNotify(value);
            pauseSliders[i]?.SetValueWithoutNotify(value);
            pauseSliders[i]?.SetEnabled(audio != null);
            string text = Mathf.RoundToInt(value * 100f) + "%";
            if (menuLabels[i] != null) menuLabels[i].text = text;
            if (pauseLabels[i] != null) pauseLabels[i].text = text;
        }
    }

    private void SetVolume(int channel, float value)
    {
        AudioManager audio = AudioManager.Instance;
        if (audio == null) return;
        value = Mathf.Clamp01(value);
        if (channel == 0) audio.SetMasterVolume(value);
        else if (channel == 1) audio.SetMusicVolume(value);
        else audio.SetSFXVolume(value);
        Refresh();
    }

    public void Dispose()
    {
        for (int i = 0; i < names.Length; i++)
        {
            menuSliders[i]?.UnregisterValueChangedCallback(menuCallbacks[i]);
            pauseSliders[i]?.UnregisterValueChangedCallback(pauseCallbacks[i]);
        }
    }
}
