// Autor: Murillo Gomes Yonamine
// Data: 27/09/2026

#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.InputSystem;

public class CheatManager : MonoBehaviour
{
    private void Update()
    {
        // new input system
        if(Keyboard.current.f1Key.wasPressedThisFrame)
        {
            Debug.Log("Cheat: F1 pressed - Adding 1000 points");
            GameManager.Instance.AddScore(1000);
        }
    }

}
#endif