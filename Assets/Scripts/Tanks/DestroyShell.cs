using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bounds a projectile object's lifetime in active gameplay seconds, excluding logical pauses.
/// Impact/explosion code may destroy it earlier; this fallback prevents missed shots living forever.
/// </summary>
public class DestroyShell : MonoBehaviour
{
    // Lifetime counts combat time, never time spent in the pause menu.
    [SerializeField, Min(0.1f)] private float lifetimeSeconds = 3f;

    private void Update()
    {
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused) return;
        lifetimeSeconds -= Time.deltaTime;
        if (lifetimeSeconds <= 0f) Destroy(gameObject);
    }

}
