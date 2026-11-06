using UnityEngine;
using System.Collections;                              // [NOVO] IEnumerator para a coroutine de voz
using UnityEngine.AddressableAssets;                   // [NOVO] AssetReferenceT e Addressables
using UnityEngine.ResourceManagement.AsyncOperations;  // [NOVO] AsyncOperationHandle

/// <summary>
/// Applies the existing audio API after scene initialization, when the mixer is ready.
/// Music and SFX sources, mixer routing and menu clips are authored in the scene.
/// Menu/gameplay music loop; the optional defeat track plays ONCE. Victory/draw use the result
/// cue. Clip gains stay separate from Murillo's Master/Music/SFX mixer sliders/preferences.
/// Character voices use a dedicated source and bounded random kill feedback, never one source
/// per enemy or a new audio framework. Audio provenance is in Assets/Audio/*/Credits.txt and
/// BattleSessionController's project header; assigning a clip does not establish usage rights.
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

    [Header("Defeat music")]
    [Tooltip("Optional track played once on the defeat result screen. An empty field leaves defeat without music.")]
    [SerializeField] private AudioClip defeatMusicClip;
    [SerializeField, Range(0f, 1f)] private float defeatMusicVolume = 0.5f;

    [Header("Character voices")]
    [Tooltip("Dedicated authored source: switching characters interrupts only the previous voice.")]
    [SerializeField] private AudioSource characterVoiceSource;
    [SerializeField, Range(0f, 1f)] private float characterVoiceVolume = 0.65f;
    [Tooltip("Chance of the player's character speaking after an elimination credited to the player.")]
    [SerializeField, Range(0f, 1f)] private float eliminationVoiceChance = 0.3f;
    [SerializeField, Min(0f)] private float eliminationVoiceCooldown = 5f;

    private float nextEliminationVoiceTime;
    private AsyncOperationHandle<AudioClip> voiceHandle; // [NOVO] handle da voz carregada no momento (um só por vez)
    private int voiceVersion;                            // [NOVO] versão: pedidos antigos que chegam atrasados são descartados
    private bool voiceLoading;                           // [NOVO] true enquanto uma voz está sendo carregada

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

    private static void PlayMusic(AudioClip clip, float volume, bool loop = true)
    {
        AudioManager audio = AudioManager.Instance;
        if (audio == null) return;
        if (instance != null && instance.musicSource != null) instance.musicSource.volume = volume;
        // Reuse Murilo's single music source and mixer; missing clips must not leave an old loop playing.
        if (clip != null) audio.PlayMusic(clip, loop);
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

    /// <summary>Carrega a voz sob demanda, toca e libera a anterior (padrão IsValid + Release da aula).</summary>
    public static void PlayCharacterVoice(AssetReferenceT<AudioClip> voiceRef) {
        StopCharacterVoice();                                                    // para a voz atual e libera o clipe anterior
        if (instance == null || instance.characterVoiceSource == null ||
            voiceRef == null || !voiceRef.RuntimeKeyIsValid()) return;           // sem fonte ou sem referência válida: nada a fazer
        instance.StartCoroutine(instance.LoadAndPlayVoice(voiceRef, instance.voiceVersion)); // inicia o carregamento assíncrono
    }

    /// <summary>Mesma regra de chance/cooldown de antes, mas com a voz vinda do Addressables.</summary>
    public static bool TryPlayEliminationVoice(AssetReferenceT<AudioClip> voiceRef) {
        if (instance == null || instance.characterVoiceSource == null || voiceRef == null ||
            !voiceRef.RuntimeKeyIsValid() || instance.voiceLoading ||            // não empilha com um carregamento em andamento
            instance.characterVoiceSource.isPlaying || Time.unscaledTime < instance.nextEliminationVoiceTime ||
            instance.eliminationVoiceChance <= 0f ||
            (instance.eliminationVoiceChance < 1f && Random.value >= instance.eliminationVoiceChance)) return false;

        PlayCharacterVoice(voiceRef);                                            // carrega e toca
        instance.nextEliminationVoiceTime = Time.unscaledTime + instance.eliminationVoiceCooldown; // reinicia o cooldown
        return true;
    }

    private IEnumerator LoadAndPlayVoice(AssetReferenceT<AudioClip> voiceRef, int version) {
        voiceLoading = true;                                                     // marca carregamento em andamento
        voiceHandle = Addressables.LoadAssetAsync<AudioClip>(voiceRef.RuntimeKey); // pede o clipe pelo Addressables (RuntimeKey evita o estado interno do AssetReference)
        AsyncOperationHandle<AudioClip> handle = voiceHandle;                    // cópia local para comparar depois
        yield return handle;                                                     // espera sem travar o jogo

        if (version != voiceVersion)                                             // outro pedido ou Stop aconteceu enquanto carregava
        {
            if (handle.IsValid()) Addressables.Release(handle);                  // descarta o clipe atrasado
            yield break;                                                         // sai sem tocar
        }

        voiceLoading = false;                                                    // este pedido terminou
        if (handle.Status != AsyncOperationStatus.Succeeded)                     // falha: avisa e não trava o fluxo
        {
            Debug.LogError($"[Addressables] Falha ao carregar a voz: {handle.OperationException}");
            if (handle.IsValid()) Addressables.Release(handle);                  // libera o handle com erro
            yield break;
        }

        AudioSource source = characterVoiceSource;                               // fonte dedicada de voz
        source.clip = handle.Result;                                             // usa o clipe carregado
        source.loop = false;                                                     // voz não repete
        source.volume = characterVoiceVolume;                                    // volume configurado
        source.Play();                                                           // toca
    }

    /// <summary>Stops dialogue before countdown, cancellation, player death or a result preview.</summary>
    public static void StopCharacterVoice() {
        if (instance == null) return;                                // sem instância, nada a parar
        instance.voiceVersion++;                                     // [NOVO] cancela carregamentos pendentes
        instance.voiceLoading = false;                               // [NOVO] nada carregando
        if (instance.characterVoiceSource != null)                   // existe fonte de voz
        {
            instance.characterVoiceSource.Stop();                    // para o áudio
            instance.characterVoiceSource.clip = null;               // solta o clipe ANTES de liberar o handle
        }
        if (instance.voiceHandle.IsValid()) Addressables.Release(instance.voiceHandle); // [NOVO] libera o clipe da voz anterior
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
    /// <summary>
    /// Defeat plays its optional music once on the existing bus (no loop); an absent clip stops
    /// music. Victory/draw retain the authored one-shot result cue. Returning to menu/replaying
    /// selects the corresponding music through the existing service.
    /// </summary>
    public static void PlayResult(string outcome = "vitoria")
    {
        if (outcome == "derrota")
        {
            PlayMusic(instance != null ? instance.defeatMusicClip : null,
                instance != null ? instance.defeatMusicVolume : 1f, false);
            return;
        }
        PlayCue(instance != null ? instance.resultClip : null);
    }

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
