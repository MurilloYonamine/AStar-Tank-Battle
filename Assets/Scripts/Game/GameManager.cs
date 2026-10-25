// Autor: Murillo Gomes Yonamine
// Data: 27/09/2026

using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Murillo's persistent account/menu bridge and original account-points API.
/// UmaTank retains StartGame/PlayerData/DataManager but BattleSessionController owns the
/// timed match and disables this legacy Update (Escape -> EndGame/quit). Current match
/// persistence must not call EndGame, which updates an account instead of inserting an attempt.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private DataManager _dataManager;

    public PlayerData PlayerData { get; private set; }
    public DataManager DataManager => _dataManager;

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

    public void StartGame(string playerName, string email = "", string password = "")
    {
        PauseManager.Instance.SetPause(false);

        PlayerData = new PlayerData(playerName, email, password, 0);

        Debug.Log("Jogo iniciado para o jogador: " + playerName);
    }

    public void StartGame(PlayerData player)
    {
        PauseManager.Instance.SetPause(false);

        PlayerData = player;

        Debug.Log("Jogo iniciado para o jogador: " + player.name);
    }


    public void AddScore(int score)
    {
        PlayerData.pontos += score;
        Debug.Log("Pontuação atual: " + PlayerData.pontos);
    }

    public void EndGame()
    {
        // Legacy account-point flow only. The current battle saves each completed attempt
        // (victory, loss or draw) through DataManager.SaveMatch without calling this method.
        Debug.Log("Fim de jogo. Pontuação final: " + PlayerData.pontos);

        if (PlayerData.id > 0)
        {
            _dataManager.UpdatePlayerData(
                PlayerData.id,
                PlayerData.name,
                PlayerData.email,
                PlayerData.password,
                PlayerData.pontos
            );
        }
        else
        {
            _dataManager.SaveData(
                PlayerData.name,
                PlayerData.email,
                PlayerData.password,
                PlayerData.pontos
            );
        }
    }
}
