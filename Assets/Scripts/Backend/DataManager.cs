// Autor: Murillo Gomes Yonamine | Professor Eduardo
// Data: 27/09/2026

using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Unity's HTTP/JSON adapter for Murillo's PHP middleware, not a MySQL client.
/// Requests reach index.php -> Controller (validation/auth) -> Model -> PDO/MySQL.
/// Configure URL_BASE for the installation; localhost refers to the machine running the game.
/// Current login/registration/match/ranking flows require valid server responses; legacy player
/// list/account-point methods remain for compatibility, not as proof that a match was saved.
/// Setup, collected data, retention and project attribution are in BattleSessionController's header.
/// </summary>
public class DataManager : MonoBehaviour
{
    private const string URL_BASE = "http://localhost/gsd";
    private const string URL_GET = URL_BASE + "/?action=api_listar";
    private const string URL_POST = URL_BASE + "/?action=api_inserir";
    private const string URL_UPDATE = URL_BASE + "/?action=api_atualizar";
    private const string URL_LOGIN = URL_BASE + "/?action=api_login";
    private const string URL_MATCH = URL_BASE + "/?action=api_salvar_partida";
    private const string URL_RANKING = URL_BASE + "/?action=api_ranking_partidas";

    // Following the class example, Unity proposes a new token during login; PHP associates it
    // with the verified player for one day. This client keeps the token in memory only.
    public string Token { get; private set; }
    private int _loginRequestId;

    [Serializable]
    private class AuthResponse
    {
        public int id = 0;
        public string name = "";
        public string email = "";
        public string error = "";
    }

    [Serializable]
    private class ApiResponse
    {
        public bool success = false;
        public string error = "";
    }

    [Serializable]
    private class MatchResponse
    {
        public bool success = false;
        public int partida_id = 0;
        public string error = "";
    }

    [Serializable]
    public class RankingEntry
    {
        public int jogador_id;
        public string jogador_nome;
        public double duracao_segundos;
        public int posicao;
    }

    [Serializable]
    public class RankingResponse
    {
        public RankingEntry[] ranking;
        public string error;
        public bool comparacao_recorde_disponivel;
        public double recorde_anterior_segundos;
    }

    /// <summary>
    /// POST api_ranking_partidas with jogador_id/token. Receives ranking entries containing
    /// jogador_id, jogador_nome, duracao_segundos (numeric seconds) and posicao (1, 1, 3 on ties).
    /// The Model returns each player's fastest victory. Never convert duration into score points.
    /// </summary>
    public void LoadRanking(int playerId, Action<RankingEntry[], string> completed)
    {
        StartCoroutine(LoadRankingRequest(playerId, null, (response, error) => completed(response?.ranking, error)));
    }

    /// <summary>
    /// Optional partida_uuid requests a comparison excluding this saved match, without changing
    /// the save API or database. Missing comparison data never authorizes a new-record announcement.
    /// Response adds comparacao_recorde_disponivel and recorde_anterior_segundos when supported;
    /// a read failure is returned to the UI and never changes a successfully saved match.
    /// </summary>
    public void LoadMatchRanking(int playerId, string matchId, Action<RankingResponse, string> completed)
    {
        StartCoroutine(LoadRankingRequest(playerId, matchId, completed));
    }

    private IEnumerator LoadRankingRequest(int playerId, string matchId, Action<RankingResponse, string> completed)
    {
        if (playerId <= 0 || string.IsNullOrEmpty(Token))
        {
            completed(null, "Entre na sua conta para consultar o ranking de vitórias.");
            yield break;
        }
        WWWForm form = new WWWForm();
        form.AddField("jogador_id", playerId);
        form.AddField("token", Token);
        if (!string.IsNullOrEmpty(matchId)) form.AddField("partida_uuid", matchId);
        using (UnityWebRequest www = UnityWebRequest.Post(URL_RANKING, form))
        {
            www.timeout = 10;
            yield return www.SendWebRequest();
            RankingResponse response = null;
            try { response = JsonUtility.FromJson<RankingResponse>(www.downloadHandler.text); }
            catch (ArgumentException) { /* Invalid JSON must not be shown as an empty ranking. */ }
            if (www.result == UnityWebRequest.Result.Success && response?.ranking != null &&
                string.IsNullOrEmpty(response.error))
            {
                // An older server returns only points. Do not present missing durations as records of zero.
                bool valid = Array.TrueForAll(response.ranking, entry => entry != null && entry.jogador_id > 0 &&
                    entry.posicao > 0 && entry.duracao_segundos > 0 &&
                    !double.IsNaN(entry.duracao_segundos) && !double.IsInfinity(entry.duracao_segundos));
                valid &= !response.comparacao_recorde_disponivel ||
                    response.recorde_anterior_segundos >= 0 && !double.IsNaN(response.recorde_anterior_segundos) &&
                    !double.IsInfinity(response.recorde_anterior_segundos);
                completed(valid ? response : null, valid ? null :
                    "O servidor ainda não retornou um ranking por tempo válido. Atualize os arquivos PHP da pasta gsd.");
            }
            else
                completed(null, !string.IsNullOrEmpty(response?.error) ? response.error :
                    "Não foi possível carregar o ranking. Verifique a conexão e abra o placar novamente.");
        }
    }

    /// <summary>
    /// POST api_salvar_partida sends jogador_id/token, partida_uuid, inicio_utc/fim_utc,
    /// duracao_segundos, resultado, pontuacao and inimigos_eliminados as form fields.
    /// Outcome is vitoria, derrota or empate; duration is numeric active seconds (F3), not points.
    /// Receives {success, partida_id, repetida} or {error}. A successful HTTP response plus
    /// success=true and a positive partida_id are required; malformed JSON/HTTP errors/timeouts
    /// call completed(false, error). Retry the SAME matchId/snapshot to avoid a duplicate attempt.
    /// </summary>
    public void SaveMatch(int playerId, string matchId, DateTime startedUtc, DateTime finishedUtc,
        double durationSeconds, string outcome, int score, int enemiesKilled, Action<bool, string> completed)
    {
        StartCoroutine(SaveMatchRequest(playerId, matchId, startedUtc, finishedUtc,
            durationSeconds, outcome, score, enemiesKilled, completed));
    }

    private IEnumerator SaveMatchRequest(int playerId, string matchId, DateTime startedUtc, DateTime finishedUtc,
        double durationSeconds, string outcome, int score, int enemiesKilled, Action<bool, string> completed)
    {
        if (string.IsNullOrEmpty(Token))
        {
            completed(false, "Sessão inválida. Entre novamente para salvar a partida.");
            yield break;
        }

        WWWForm form = new WWWForm();
        form.AddField("jogador_id", playerId);
        form.AddField("token", Token);
        form.AddField("partida_uuid", matchId);
        // The PHP contract uses UTC strings and invariant numeric seconds, independent of locale.
        // Active duration may be less than end-start because wall-clock timestamps include pauses.
        form.AddField("inicio_utc", startedUtc.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        form.AddField("fim_utc", finishedUtc.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        form.AddField("duracao_segundos", durationSeconds.ToString("F3", CultureInfo.InvariantCulture));
        form.AddField("resultado", outcome);
        form.AddField("pontuacao", score);
        form.AddField("inimigos_eliminados", enemiesKilled);

        using (UnityWebRequest www = UnityWebRequest.Post(URL_MATCH, form))
        {
            www.timeout = 10;
            yield return www.SendWebRequest();

            MatchResponse response = null;
            if (!string.IsNullOrEmpty(www.downloadHandler.text))
            {
                try { response = JsonUtility.FromJson<MatchResponse>(www.downloadHandler.text); }
                catch (ArgumentException) { /* An unexpected response cannot confirm persistence. */ }
            }

            if (www.result == UnityWebRequest.Result.Success && response != null &&
                response.success && response.partida_id > 0)
            {
                completed(true, null);
                yield break;
            }

            // HTTP failure, timeout or unexpected JSON cannot authorize a save confirmation.
            string error = "Não foi possível salvar a partida. Verifique a conexão e tente novamente.";
            if (response != null && !string.IsNullOrEmpty(response.error)) error = response.error;
            completed(false, error);
        }
    }

    public string jsonData; // Raw legacy player-list JSON, not a match/ranking response.

    public delegate void OnDataLoaded(string jsonData);
    public static event OnDataLoaded onDataLoaded;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// POST api_login: email/password/new token -> {id,name,email} or {error}.
    /// PHP verifies the stored password hash; the response contains neither password nor token.
    /// Only a valid account response associates the proposed token locally and releases gameplay.
    /// Invalid credentials, HTTP failure and malformed JSON return a null player plus an error.
    /// </summary>
    public void Login(string email, string password, Action<PlayerData, string> completed)
    {
        _loginRequestId++;
        StartCoroutine(LoginRequest(email, password, completed, _loginRequestId));
    }

    /// <summary>Clears client authentication and makes any in-flight login response obsolete.</summary>
    public void CancelLogin()
    {
        _loginRequestId++;
        Token = null;
    }

    private IEnumerator LoginRequest(string email, string password, Action<PlayerData, string> completed, int requestId)
    {
        Token = null;
        string newToken = Guid.NewGuid().ToString("N");
        WWWForm form = new WWWForm();
        form.AddField("email", email);
        form.AddField("password", password);
        form.AddField("token", newToken);
        using (UnityWebRequest www = UnityWebRequest.Post(URL_LOGIN, form))
        {
            yield return www.SendWebRequest();
            // Cancellation/new login must not be undone by an older asynchronous response.
            if (requestId != _loginRequestId) yield break;

            AuthResponse response = null;
            if (!string.IsNullOrEmpty(www.downloadHandler.text))
            {
                try { response = JsonUtility.FromJson<AuthResponse>(www.downloadHandler.text); }
                catch (ArgumentException) { /* Invalid JSON cannot authenticate an account. */ }
            }

            bool requestFailed = www.result != UnityWebRequest.Result.Success;
            bool responseMissing = response == null;
            bool invalidPlayer = !responseMissing && (response.id <= 0 || string.IsNullOrEmpty(response.name) || string.IsNullOrEmpty(response.email));

            if (requestFailed || responseMissing || invalidPlayer)
            {
                string errorMessage;
                if (!responseMissing && !string.IsNullOrEmpty(response.error))
                {
                    errorMessage = response.error;
                }
                else
                {
                    errorMessage = "Não foi possível autenticar. Verifique a conexão com o servidor.";
                }

                completed(null, errorMessage);
                yield break;
            }
            Token = newToken;
            completed(new PlayerData(response.id, response.name, response.email, null, 0, null, null), null);
        }
    }

    /// <summary>
    /// Registers an account via api_inserir, not a match. Sends name/email/password/pontos;
    /// PHP hashes the password and returns {success:true} or {error}. The current menu sends
    /// zero legacy points and waits for the callback before confirming registration.
    /// </summary>
    public void SaveData(string playerName, string email, string password, int score, Action<bool, string> completed = null)
    {
        StartCoroutine(InsertNewData(playerName, email, password, score, completed));
    }

    /// <summary>
    /// Compatibility account-point update (api_atualizar), not the timed-match persistence path.
    /// BattleSessionController uses SaveMatch for every completed outcome instead.
    /// </summary>
    public void UpdatePlayerData(int id, string playerName, string email, string password, int score)
    {
        StartCoroutine(UpdateData(id, playerName, email, password, score));
    }


    /// <summary>
    /// Requests api_listar with an empty POST and forwards its raw JSON player array through
    /// onDataLoaded. This public legacy account list neither authenticates nor supplies the
    /// time ranking; the ranking menu uses the separate protected LoadRanking contract.
    /// </summary>
    public void LoadData()
    {
        StartCoroutine(nameof(LoadDataFromJson));
    }

    // Legacy public player list retained for existing consumers. ScoreMenu uses LoadRanking.
    private IEnumerator LoadDataFromJson()
    {
        WWWForm form = new WWWForm();
        
        UnityWebRequest www = UnityWebRequest.Post(URL_GET, form);
        www.certificateHandler = new BypassHTTPSCertificate();
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Erro ao carregar dados: " + www.error);
        }
        else
        {
            jsonData = www.downloadHandler.text;
            onDataLoaded?.Invoke(jsonData);
            Debug.Log("Dados carregados com sucesso: " + jsonData);
        }
    }

    // Registration requires both successful HTTP and the business-level success field.
    private IEnumerator InsertNewData(string playerName, string email, string password, int score, Action<bool, string> completed)
    {
        WWWForm form = new WWWForm();
        form.AddField("name", playerName);
        form.AddField("email", email);
        form.AddField("password", password);
        form.AddField("pontos", score);

        using (UnityWebRequest www = UnityWebRequest.Post(URL_POST, form))
        {
            yield return www.SendWebRequest();

            ApiResponse response = null;
            try { response = JsonUtility.FromJson<ApiResponse>(www.downloadHandler.text); }
            catch (ArgumentException) { /* Treat malformed JSON as a failed response below. */ }
            
            bool success = www.result == UnityWebRequest.Result.Success && response != null && response.success;
            
            string error = response != null && !string.IsNullOrEmpty(response.error)
                ? response.error : "Não foi possível cadastrar. Verifique a conexão com o servidor.";
            
            if (!success) Debug.LogError(error);
            completed?.Invoke(success, success ? null : error);
        }
    }

    // POST api_atualizar sends id/name/token/pontos and receives {success} or {error}.
    // This legacy log-only consumer is not used to acknowledge current match persistence.
    // It creates no partidas row; BattleSessionController saves wins, losses AND draws via SaveMatch.
    private IEnumerator UpdateData(int id, string playerName, string email, string password, int score)
    {
        WWWForm form = new WWWForm();
        form.AddField("id", id);
        form.AddField("name", playerName);
        form.AddField("token", Token ?? "");
        form.AddField("pontos", score);

        using (UnityWebRequest www = UnityWebRequest.Post(URL_UPDATE, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Erro ao atualizar dados: " + www.error);
            }
            else
            {
                Debug.Log("Dados atualizados com sucesso: " + www.downloadHandler.text);
            }
        }
    }
}
