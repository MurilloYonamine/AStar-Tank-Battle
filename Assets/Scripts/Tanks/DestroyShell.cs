using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
