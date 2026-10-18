// Autor: Murillo Gomes Yonamine | Professor Eduardo
// Data: 27/09/2026

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class DataManager : MonoBehaviour
{
    private const string URL_BASE = "http://localhost/gsd";
    private const string URL_GET = URL_BASE + "/?action=api_listar";
    private const string URL_POST = URL_BASE + "/?action=api_inserir";
    private const string URL_UPDATE = URL_BASE + "/?action=api_atualizar";
    private const string URL_LOGIN = URL_BASE + "/?action=api_login";

    // Como no exemplo da aula, a Unity envia o token ao login e o PHP o associa ao jogador.
    // Mantido apenas na memória; não é salvo em disco nem enviado no placar.
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

    public string jsonData; // vai receber os dados no formato JSON

    public delegate void OnDataLoaded(string jsonData);
    public static event OnDataLoaded onDataLoaded;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    // POST api_login: email/password/token -> {id,name,email} ou {error}.
    // O PHP confere a senha; a Unity só libera o gameplay depois da resposta válida.
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

    // POST api_inserir: name/email/password só confirma cadastro após {success:true}.
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
