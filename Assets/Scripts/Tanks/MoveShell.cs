using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Legacy straight-transform projectile used by the original firing experiment, not the current
/// Rigidbody Shell/AIShell combat. Logical pause stops translation; retained for older scenes.
/// </summary>
public class MoveShell : MonoBehaviour {

    public float speed = 1.0f;


    void Update() {

        if(PauseManager.Instance.IsPaused)
        {
            transform.Translate(Vector3.zero);
            return;
        }

        transform.Translate(0.0f, 0.0f, Time.deltaTime * speed);
    }
}
