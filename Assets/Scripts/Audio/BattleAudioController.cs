using UnityEngine;

/// <summary>
/// Applies the existing audio API after scene initialization, when the mixer is ready.
/// Music and SFX sources, mixer routing and menu clips are authored in the scene.
/// </summary>
public sealed class BattleAudioController : MonoBehaviour
{
    [Header("Interface cues (existing group assets)")]
    [SerializeField] private AudioClip selectionChangeClip;
    [SerializeField] private AudioClip countdownClip;
    [SerializeField] private AudioClip matchStartClip;
    [SerializeField] private AudioClip resultClip;
    [SerializeField, Range(0f, 1f)] private float cueVolume = 0.65f;

    private static BattleAudioController instance;

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    private void Start()
    {
        AudioManager audio = AudioManager.Instance;
        if (audio == null) return;
        audio.SetMasterVolume(audio.GetSavedVolume(AudioManager.MASTER_VOLUME_PARAM));
        audio.SetMusicVolume(audio.GetSavedVolume(AudioManager.MUSIC_VOLUME_PARAM));
        audio.SetSFXVolume(audio.GetSavedVolume(AudioManager.SFX_VOLUME_PARAM));
    }

    public static void PlayButtonClick()
    {
        // Reuse Murilo's public menu API for the same click sound and SFX mixer routing.
        if (AudioManager.Instance != null && MenuManager.Instance != null)
            MenuManager.Instance.PlayButtonClickSound();
    }

    public static void PlaySelectionChange()
    {
        if (!PlayCue(instance != null ? instance.selectionChangeClip : null)) PlayButtonClick();
    }

    public static void PlayCountdown() => PlayCue(instance != null ? instance.countdownClip : null);
    public static void PlayMatchStart() => PlayCue(instance != null ? instance.matchStartClip : null);
    public static void PlayResult() => PlayCue(instance != null ? instance.resultClip : null);

    private static bool PlayCue(AudioClip clip)
    {
        AudioManager audio = AudioManager.Instance;
        if (audio == null || clip == null) return false;
        audio.PlaySFX(clip, instance != null ? instance.cueVolume : 1f);
        return true;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
