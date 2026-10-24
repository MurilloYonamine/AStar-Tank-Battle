// Autor: Murillo Gomes Yonamine | Professor Eduardo
// Data: 27/09/2026

using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Comunica a Unity com o middleware PHP por UnityWebRequest e interpreta respostas JSON.
/// A Unity nunca acessa o MySQL diretamente: index.php -> Controller -> Model -> banco.
/// URL_BASE aponta para o XAMPP local; ao mudar de servidor, configure o endereço aqui.
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

    // Como no exemplo da aula, a Unity cria o token no login e o PHP o associa ao jogador
    // por um dia. Guardamos o token só em memória para enviá-lo nas rotas protegidas.
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
        public int pontuacao;
    }

    [Serializable]
    private class RankingResponse
    {
        public RankingEntry[] ranking;
        public string error;
    }

    /// <summary>Loads the PHP top ten: each player's best winning match, authenticated by token.</summary>
    public void LoadRanking(int playerId, Action<RankingEntry[], string> completed)
    {
        StartCoroutine(LoadRankingRequest(playerId, completed));
    }

    private IEnumerator LoadRankingRequest(int playerId, Action<RankingEntry[], string> completed)
    {
        if (playerId <= 0 || string.IsNullOrEmpty(Token))
        {
            completed(null, "Entre na sua conta para consultar o ranking de vitórias.");
            yield break;
        }
        WWWForm form = new WWWForm();
        form.AddField("jogador_id", playerId);
        form.AddField("token", Token);
        using (UnityWebRequest www = UnityWebRequest.Post(URL_RANKING, form))
        {
            www.timeout = 10;
            yield return www.SendWebRequest();
            RankingResponse response = null;
            try { response = JsonUtility.FromJson<RankingResponse>(www.downloadHandler.text); }
            catch (ArgumentException) { /* Invalid JSON must not be shown as an empty ranking. */ }
            if (www.result == UnityWebRequest.Result.Success && response?.ranking != null &&
                string.IsNullOrEmpty(response.error))
                completed(response.ranking, null);
            else
                completed(null, !string.IsNullOrEmpty(response?.error) ? response.error :
                    "Não foi possível carregar o ranking. Verifique a conexão e abra o placar novamente.");
        }
    }

    /// <summary>
    /// POST api_salvar_partida: jogador_id, token, partida_uuid, inicio_utc, fim_utc,
    /// duracao_segundos, resultado, pontuacao e inimigos_eliminados. A API responde
    /// {success, partida_id, repetida} ou {error}. Result is vitoria, derrota or empate.
    /// O mesmo matchId é reutilizado ao tentar novamente para não criar outra linha.
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
        // O PHP espera datas UTC nesse formato e a duração numérica em segundos.
        // Ela pode ser menor que fim - início porque a pausa não entra na duração.
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
                catch (ArgumentException) { /* Resposta inesperada nao confirma salvamento. */ }
            }

            if (www.result == UnityWebRequest.Result.Success && response != null &&
                response.success && response.partida_id > 0)
            {
                completed(true, null);
                yield break;
            }

            // Falha HTTP, timeout ou JSON inesperado não autorizam confirmar o salvamento.
            string error = "Não foi possível salvar a partida. Verifique a conexão e tente novamente.";
            if (response != null && !string.IsNullOrEmpty(response.error)) error = response.error;
            completed(false, error);
        }
    }

    public string jsonData; // vai receber os dados no formato JSON

    public delegate void OnDataLoaded(string jsonData);
    public static event OnDataLoaded onDataLoaded;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// POST api_login: email/password/token -> {id,name,email} ou {error}.
    /// Somente o PHP confere a senha; a resposta não contém senha nem devolve o token.
    /// O menu só libera o gameplay após receber o jogador autenticado.
    /// </summary>
    public void Login(string email, string password, Action<PlayerData, string> completed)
    {
        _loginRequestId++;
        StartCoroutine(LoginRequest(email, password, completed, _loginRequestId));
    }

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
            // Ignora resposta atrasada se o usuário cancelou ou iniciou outro login.
            if (requestId != _loginRequestId) yield break;

            AuthResponse response = null;
            if (!string.IsNullOrEmpty(www.downloadHandler.text))
            {
                try { response = JsonUtility.FromJson<AuthResponse>(www.downloadHandler.text); }
                catch (ArgumentException) { /* Uma resposta inválida não autentica o jogador. */ }
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

    public void SaveData(string playerName, string email, string password, int score, Action<bool, string> completed = null)
    {
        StartCoroutine(InsertNewData(playerName, email, password, score, completed));
    }

    public void UpdatePlayerData(int id, string playerName, string email, string password, int score)
    {
        StartCoroutine(UpdateData(id, playerName, email, password, score));
    }


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

    // POST api_inserir: name/email/password/pontos; PHP salva senha com hash e responde
    // {success:true} ou {error}. A UI só confirma o cadastro após o retorno de sucesso.
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
            catch (ArgumentException) { /* Tratar abaixo como erro de resposta. */ }
            
            bool success = www.result == UnityWebRequest.Result.Success && response != null && response.success;
            
            string error = response != null && !string.IsNullOrEmpty(response.error)
                ? response.error : "Não foi possível cadastrar. Verifique a conexão com o servidor.";
            
            if (!success) Debug.LogError(error);
            completed?.Invoke(success, success ? null : error);
        }
    }

    // POST api_atualizar: id/token/pontos atualiza o placar antigo do jogador autenticado.
    // Não cria registros em partidas; BattleSessionController usa SaveMatch ao morrer.
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
