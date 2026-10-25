using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Original frame-update timing demonstration: translates forward at one unit per second.
/// Not attached in UmaTank.unity and not the pause-adjusted match clock.
/// </summary>
public class UpdateMove : MonoBehaviour
{    
    void Update()
    {
        transform.Translate(0, 0, Time.deltaTime);
    }
}
