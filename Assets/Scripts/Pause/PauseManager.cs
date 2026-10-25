// Autor: Murillo Gomes Yonamine
// Data: 27/09/2026

using UnityEngine;

/// <summary>
/// Persistent logical gameplay gate used by menu, selection and the battle controllers.
/// It does not change Unity's timeScale or freeze physics by itself; BattleSessionController
/// owns reversible battle-body/animation suspension and pause-adjusted match timing.
/// </summary>
public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    private bool _isPaused = false;

    public bool IsPaused => _isPaused;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetPause(bool pause)
    {
        _isPaused = pause;
        Debug.Log("O jogo está pausado: " + _isPaused);
    }
}
