// Autor: Murillo Gomes Yonamine
// Data: 27/09/2026

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class DataManager : MonoBehaviour
{
    private const string URL_BASE = "http://localhost/senac/a6gsd/sistema";
    private const string URL_GET = URL_BASE + "/recuperar.php"; // URL para receber os dados
    private const string URL_POST = URL_BASE + "/adicionar.php"; // URL para enviar os dados

    public string jsonData; // vai receber os dados no formato JSON

    public delegate void OnDataLoaded(string jsonData);
    public static event OnDataLoaded onDataLoaded;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void SaveData(string playerName, int score, string date)
    {
        StartCoroutine(InsertNewData(playerName, score, date));
    }


    public void LoadData()
    {
        StartCoroutine(nameof(LoadDataFromJson));
    }

    private IEnumerator LoadDataFromJson()
    {
        WWWForm form = new WWWForm();
        form.AddField("chave_secreta", "123456");

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

    private IEnumerator InsertNewData(string playerName, int score, string date)
    {
        WWWForm form = new WWWForm();
        form.AddField("apelido", playerName);
        form.AddField("pontos", score);
        form.AddField("data", date);

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
}
