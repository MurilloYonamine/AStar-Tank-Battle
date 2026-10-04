using UnityEngine;

/// <summary>
/// Plays an authored engine loop only during actual planar movement in an active round.
/// Enemy volume is attenuated from the player, not the elevated isometric camera.
/// The source uses the existing SFX mixer so the menu's sound settings still apply.
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(AudioSource))]
public sealed class TankMovementAudio : MonoBehaviour
{
    [SerializeField] private AudioSource engineSource;
    [SerializeField] private TankHealth health;
    [SerializeField] private TankHealth playerHealth;
    [SerializeField] private BattleSessionController session;
    [SerializeField, Range(0f, 1f)] private float movementVolume = 0.06f;
    [SerializeField, Min(0.01f)] private float minimumSpeed = 0.1f;
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.15f;
    [SerializeField, Min(1f)] private float enemyAudibleDistance = 20f;

    private Rigidbody body;
    private Vector3 previousPosition;
    private float movingUntil;

    private bool CanPlay => session != null && session.IsRoundActive &&
        health != null && !health.IsDead && playerHealth != null && !playerHealth.IsDead &&
        Time.timeScale > 0f && (PauseManager.Instance == null || !PauseManager.Instance.IsPaused);

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        // AudioSource and clip are authored in the scene, never added at runtime.
        if (engineSource == null) engineSource = GetComponent<AudioSource>();
        engineSource.playOnAwake = false;
        engineSource.loop = true;
        engineSource.spatialBlend = 0f;
        engineSource.dopplerLevel = 0f;
        StopEngine();
    }

    private void OnEnable()
    {
        previousPosition = GetComponent<Rigidbody>().position;
        movingUntil = 0f;
        if (health != null) health.Died += OnTankDied;
    }

    private void FixedUpdate()
    {
        Vector3 displacement = body.position - previousPosition;
        previousPosition = body.position;
        if (!CanPlay) { movingUntil = 0f; return; }
        displacement.y = 0f;
        float speed = displacement.magnitude / Time.fixedDeltaTime;
        // Reject spawn teleports and measure movement rather than input/path intent.
        if (speed >= minimumSpeed && speed < 30f)
            movingUntil = Time.unscaledTime + 0.08f;
    }

    private void Update()
    {
        if (!CanPlay)
        {
            previousPosition = body.position;
            movingUntil = 0f;
            StopEngine();
            return;
        }

        float attenuation = 1f;
        if (health != playerHealth)
        {
            Vector3 offset = body.position - playerHealth.transform.position;
            offset.y = 0f;
            attenuation = Mathf.Clamp01(1f - offset.magnitude / enemyAudibleDistance);
            attenuation *= attenuation;
        }
        float targetVolume = Time.unscaledTime < movingUntil ? movementVolume * attenuation : 0f;
        if (targetVolume > 0f && !engineSource.isPlaying && engineSource.clip != null)
            engineSource.Play();
        engineSource.volume = Mathf.MoveTowards(engineSource.volume, targetVolume,
            movementVolume * Time.unscaledDeltaTime / fadeDuration);
        if (targetVolume == 0f && engineSource.volume <= 0.0001f) StopEngine();
    }

    private void OnTankDied(TankHealth tank, GameObject source) => StopEngine();

    private void StopEngine()
    {
        if (engineSource == null) return;
        engineSource.Stop();
        engineSource.volume = 0f;
    }

    private void OnDisable()
    {
        if (health != null) health.Died -= OnTankDied;
        movingUntil = 0f;
        StopEngine();
    }
}
