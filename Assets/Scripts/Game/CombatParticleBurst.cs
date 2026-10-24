using UnityEngine;

/// <summary>Bounds cosmetic particle cost and lifetime without changing Time.timeScale.</summary>
public sealed class CombatParticleBurst : MonoBehaviour
{
    private ParticleSystem[] systems;
    private float remaining;

    public void Initialize(float duration)
    {
        remaining = duration;
        systems = GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem particles in systems)
        {
            particles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.loop = false;
            main.stopAction = ParticleSystemStopAction.None;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = Mathf.Min(main.maxParticles, 96);
            var collision = particles.collision;
            collision.enabled = false;
            particles.Play(false);
        }
    }

    private void Update()
    {
        bool paused = PauseManager.Instance != null && PauseManager.Instance.IsPaused;
        foreach (ParticleSystem particles in systems)
        {
            if (paused && particles.isPlaying) particles.Pause(false);
            else if (!paused && particles.isPaused) particles.Play(false);
        }
        if (paused) return;
        remaining -= Time.deltaTime;
        if (remaining <= 0f) Destroy(gameObject);
    }
}
