using UnityEngine;

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
