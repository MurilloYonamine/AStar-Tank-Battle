using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Original LateUpdate timing demonstration: translates after regular frame updates.
/// Not attached in UmaTank.unity; battle model anchoring uses TankCharacterAnimation instead.
/// </summary>
public class LateUpdateMove : MonoBehaviour
{    
    void LateUpdate()
    {
        transform.Translate(0, 0, Time.deltaTime);
    }
}
