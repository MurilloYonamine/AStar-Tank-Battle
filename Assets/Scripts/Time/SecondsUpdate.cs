using UnityEngine;

/// <summary>
/// Original absolute-realtime movement example, independent of the gameplay pause gate.
/// Not attached in UmaTank.unity. BattleSessionController separately subtracts battle pauses
/// for the saved active duration; this demonstration is not that performance timer.
/// </summary>
public class SecondsUpdate : MonoBehaviour {
    float timeStartOffset = 0;
    float speed = 1.0f;
    bool gotStartTime = false;
    void Update()
    {
        if (!gotStartTime) {
            timeStartOffset = Time.realtimeSinceStartup;
            gotStartTime = true;
        }
        transform.position = new Vector3(
            transform.position.x, 
            transform.position.y, 
            (Time.realtimeSinceStartup - timeStartOffset) * speed);
    }
}
