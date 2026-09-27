// Autor: Murillo Gomes Yonamine
// Data: 27/09/2026

using UnityEngine;

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
