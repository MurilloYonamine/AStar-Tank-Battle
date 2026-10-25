// Autor: Murillo Gomes Yonamine
// Data: 27/09/2026

#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Editor-only legacy F1 account-points test. Excluded from player builds by UNITY_EDITOR;
/// changing pontos does not award eliminations or improve the time-based match ranking.
/// </summary>
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
