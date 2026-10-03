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

    public string jsonData; // vai receber os dados no formato JSON

    public delegate void OnDataLoaded(string jsonData);
    public static event OnDataLoaded onDataLoaded;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void SaveData(string playerName, string email, string password, int score)
    {
        StartCoroutine(InsertNewData(playerName, email, password, score));
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

    private IEnumerator InsertNewData(string playerName, string email, string password, int score)
    {
        WWWForm form = new WWWForm();
        form.AddField("name", playerName);
        form.AddField("nome", playerName);
        form.AddField("email", email);
        form.AddField("password", password);
        form.AddField("pontos", score);

        using (UnityWebRequest www = UnityWebRequest.Post(URL_POST, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Erro ao enviar dados: " + www.error);
            }
            else
            {
                Debug.Log("Dados enviados com sucesso: " + www.downloadHandler.text);
            }
        }
    }

    private IEnumerator UpdateData(int id, string playerName, string email, string password, int score)
    {
        WWWForm form = new WWWForm();
        form.AddField("id", id);
        form.AddField("name", playerName);
        form.AddField("nome", playerName);
        form.AddField("email", email);
        form.AddField("password", password);
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
