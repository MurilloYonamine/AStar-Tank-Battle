using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Original fixed-step timing demonstration, not the A* hull movement controller.
/// Not attached in UmaTank.unity; current enemy physics movement uses EnemyTankMovement.
/// </summary>
public class FixedUpdateMove : MonoBehaviour
{    
    void FixedUpdate()
    {
        transform.Translate(0, 0, Time.deltaTime);
    }
}
