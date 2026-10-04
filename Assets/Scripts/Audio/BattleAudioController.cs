using UnityEngine;

/// <summary>
/// Applies the existing audio API after scene initialization, when the mixer is ready.
/// Music and SFX sources, mixer routing and menu clips are authored in the scene.
/// </summary>
public sealed class BattleAudioController : MonoBehaviour
{
    [Header("Looping music")]
    [SerializeField] private AudioClip menuMusicClip;
    [SerializeField] private AudioClip gameplayMusicClip;
    [Tooltip("Existing music source owned by AudioManager; separate gains preserve the menu's mixer sliders.")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField, Range(0f, 1f)] private float menuMusicVolume = 0.4f;
    [SerializeField, Range(0f, 1f)] private float gameplayMusicVolume = 1f;

    [Header("Character voices")]
    [Tooltip("Dedicated authored source: switching characters interrupts only the previous voice.")]
    [SerializeField] private AudioSource characterVoiceSource;
    [SerializeField, Range(0f, 1f)] private float characterVoiceVolume = 0.65f;
    [Tooltip("Chance of the player's character speaking after an elimination credited to the player.")]
    [SerializeField, Range(0f, 1f)] private float eliminationVoiceChance = 0.3f;
    [SerializeField, Min(0f)] private float eliminationVoiceCooldown = 5f;

    private float nextEliminationVoiceTime;

    [Header("Combat effects")]
    [SerializeField] private AudioClip playerShotClip;
    [SerializeField] private AudioClip enemyShotClip;
    [SerializeField] private AudioClip deathClip;
    [SerializeField, Range(0f, 1f)] private float playerShotVolume = 0.3f;
    [SerializeField, Range(0f, 1f)] private float enemyShotVolume = 0.15f;

    [Header("Interface cues (existing group assets)")]
    [SerializeField] private AudioClip selectionChangeClip;
    [SerializeField] private AudioClip countdownClip;
    [SerializeField] private AudioClip countdownSecondClip;
    [SerializeField] private AudioClip countdownThirdClip;
    [SerializeField] private AudioClip matchStartClip;
    [SerializeField] private AudioClip resultClip;
    [SerializeField, Range(0f, 1f)] private float cueVolume = 0.65f;
    [Tooltip("Volume multiplier for the three countdown cues and GO, without changing other effects.")]
    [SerializeField, Range(0f, 1f)] private float countdownVolume = 0.3f;

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
        PlayMenuMusic();
    }

    /// <summary>Restarts the menu loop when returning from a run or cancelling selection.</summary>
    public static void PlayMenuMusic() => PlayMusic(instance != null ? instance.menuMusicClip : null,
        instance != null ? instance.menuMusicVolume : 1f);

    /// <summary>Starts the gameplay loop only after the intro countdown has finished.</summary>
    public static void PlayGameplayMusic()
    {
        if (instance != null) instance.nextEliminationVoiceTime = 0f;
        PlayMusic(instance != null ? instance.gameplayMusicClip : null,
            instance != null ? instance.gameplayMusicVolume : 1f);
    }

    public static void StopMusic()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.StopMusic();
    }

    private static void PlayMusic(AudioClip clip, float volume)
    {
        AudioManager audio = AudioManager.Instance;
        if (audio == null) return;
        if (instance != null && instance.musicSource != null) instance.musicSource.volume = volume;
        // Reuse Murilo's single music source and mixer; missing clips must not leave an old loop playing.
        if (clip != null) audio.PlayMusic(clip, true);
        else audio.StopMusic();
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

    /// <summary>Replaces the previous spoken cue without interrupting music or shared SFX.</summary>
    public static void PlayCharacterVoice(AudioClip clip)
    {
        StopCharacterVoice();
        if (instance == null || instance.characterVoiceSource == null || clip == null) return;
        AudioSource source = instance.characterVoiceSource;
        source.clip = clip;
        source.loop = false;
        source.volume = instance.characterVoiceVolume;
        source.Play();
    }

    /// <summary>Random spoken feedback for a credited kill; never cuts another voice or stacks multi-kills.</summary>
    public static bool TryPlayEliminationVoice(AudioClip clip)
    {
        if (instance == null || instance.characterVoiceSource == null || clip == null ||
            instance.characterVoiceSource.isPlaying || Time.unscaledTime < instance.nextEliminationVoiceTime ||
            instance.eliminationVoiceChance <= 0f ||
            (instance.eliminationVoiceChance < 1f && Random.value >= instance.eliminationVoiceChance)) return false;

        PlayCharacterVoice(clip);
        instance.nextEliminationVoiceTime = Time.unscaledTime + instance.eliminationVoiceCooldown;
        return true;
    }

    /// <summary>Stops dialogue before countdown, cancellation, player death or a result preview.</summary>
    public static void StopCharacterVoice()
    {
        if (instance == null || instance.characterVoiceSource == null) return;
        instance.characterVoiceSource.Stop();
        instance.characterVoiceSource.clip = null;
    }

    /// <summary>Plays the cue on the same frame as its countdown label, using the first cue as a fallback.</summary>
    public static void PlayCountdown(int number = 1)
    {
        if (instance == null) return;
        AudioClip clip = number == 2 ? instance.countdownSecondClip :
            number == 3 ? instance.countdownThirdClip : instance.countdownClip;
        PlayCue(clip != null ? clip : instance.countdownClip, instance.countdownVolume);
    }
    public static void PlayMatchStart()
    {
        if (instance != null) PlayCue(instance.matchStartClip, instance.countdownVolume);
    }
    public static void PlayResult() => PlayCue(instance != null ? instance.resultClip : null);

    /// <summary>Called once after creating a projectile, never by aiming or cooldown updates.</summary>
    public static void PlayShot(bool enemyShot = false)
    {
        if (instance == null) return;
        PlayCue(enemyShot ? instance.enemyShotClip : instance.playerShotClip,
            enemyShot ? instance.enemyShotVolume : instance.playerShotVolume);
    }

    /// <summary>The persistent SFX source lets the cue finish after the dead tank is deactivated.</summary>
    public static void PlayDeath() => PlayCue(instance != null ? instance.deathClip : null);

    private static bool PlayCue(AudioClip clip, float volumeScale = 1f)
    {
        AudioManager audio = AudioManager.Instance;
        if (audio == null || clip == null) return false;
        audio.PlaySFX(clip, (instance != null ? instance.cueVolume : 1f) * volumeScale);
        return true;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            StopCharacterVoice();
            instance = null;
        }
    }
}
