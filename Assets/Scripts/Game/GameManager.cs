// Autor: Murillo Gomes Yonamine
// Data: 27/09/2026

using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private DataManager _dataManager;

    public PlayerData PlayerData { get; private set; }

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

    private void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            PauseManager.Instance.SetPause(true);
            EndGame();
            Application.Quit();
        }
    }

    public void StartGame(string playerName)
    {
        PauseManager.Instance.SetPause(false);

        PlayerData = new PlayerData(playerName, score: 0, System.DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
        Debug.Log("Jogo iniciado para o jogador: " + playerName);
    }

    public void AddScore(int score)
    {
        PlayerData.pontos += score;
        Debug.Log("Pontuação atual: " + PlayerData.pontos);
    }

    public void EndGame()
    {
        Debug.Log("Fim de jogo. Pontuação final: " + PlayerData.pontos);
        _dataManager.SaveData(PlayerData.apelido, PlayerData.pontos, PlayerData.data);
    }
}
