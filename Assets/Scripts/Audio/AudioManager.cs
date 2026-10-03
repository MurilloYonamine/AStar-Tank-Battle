// Autor: Murillo Gomes Yonamine
// Data: 02/10/2026

using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public const string MASTER_VOLUME_PARAM = "MasterVolume";
    public const string MUSIC_VOLUME_PARAM = "MusicVolume";
    public const string SFX_VOLUME_PARAM = "SFXVolume";

    [Header("Mixer")]
    [SerializeField] private AudioMixer _audioMixer;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource _musicSource;
    [SerializeField] private AudioSource _sfxSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetMasterVolume(GetSavedVolume(MASTER_VOLUME_PARAM));
            SetMusicVolume(GetSavedVolume(MUSIC_VOLUME_PARAM));
            SetSFXVolume(GetSavedVolume(SFX_VOLUME_PARAM));
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public float GetSavedVolume(string paramName)
    {
        return PlayerPrefs.GetFloat(paramName, 1f);
    }

    public void SetMasterVolume(float linear)
    {
        SetVolume(MASTER_VOLUME_PARAM, linear);
    }

    public void SetMusicVolume(float linear)
    {
        SetVolume(MUSIC_VOLUME_PARAM, linear);
    }

    public void SetSFXVolume(float linear)
    {
        SetVolume(SFX_VOLUME_PARAM, linear);
    }

    private void SetVolume(string exposedParam, float linear)
    {
        float db = linear > 0.0001f ? Mathf.Log10(linear) * 20f : -80f;
        _audioMixer.SetFloat(exposedParam, db);
        PlayerPrefs.SetFloat(exposedParam, linear);
    }

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        _musicSource.clip = clip;
        _musicSource.loop = loop;
        _musicSource.Play();
    }

    public void StopMusic()
    {
        _musicSource.Stop();
    }

    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        _sfxSource.PlayOneShot(clip, volumeScale);
    }
}
