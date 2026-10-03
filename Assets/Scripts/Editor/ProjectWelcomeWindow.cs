// Autor: Murillo Gomes Yonamine
// Data: 02/10/2026

using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public class ProjectWelcomeWindow : EditorWindow
{
    private const string SCENE_PATH = "Assets/Scenes/WaifuTanks.unity";
    private const string BACKEND_FOLDER_PATH = "Assets/Backend";
    private static string ShowAtStartupKey => "ShowWelcomeAtStartup_" + Application.dataPath.GetHashCode();

    private bool _dontShowAgain;

    static ProjectWelcomeWindow()
    {
        EditorApplication.delayCall += CheckStartupOpen;
    }

    private static void CheckStartupOpen()
    {
        if (EditorPrefs.GetBool(ShowAtStartupKey, true))
        {
            ShowWindow();
        }
    }

    [MenuItem("Window/Configuração Inicial")]
    public static void ShowWindow()
    {
        ProjectWelcomeWindow window = GetWindow<ProjectWelcomeWindow>(false, "Configuração Inicial", true);
        window.minSize = new Vector2(510, 450);
        window.maxSize = new Vector2(510, 450);
        window.Show();
    }

    private void OnEnable()
    {
        _dontShowAgain = !EditorPrefs.GetBool(ShowAtStartupKey, true);
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(12);

        // Cabeçalho
        EditorGUILayout.LabelField("Bem-vindo ao Projeto Batalha de Tanques", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Os arquivos do servidor PHP e o banco de dados estão na pasta 'Assets/Backend'. Siga as instruções abaixo para preparar o ambiente local antes de jogar:", MessageType.Info);

        EditorGUILayout.Space(12);

        // Checklist / Instruções
        EditorGUILayout.LabelField("Passos para Configuração do Ambiente:", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        EditorGUILayout.LabelField("1. Copie a pasta 'gsd' de Assets/Backend para a pasta 'htdocs' do XAMPP.", EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space(3);

        EditorGUILayout.LabelField("2. Inicie os módulos Apache e MySQL no painel de controle do XAMPP.", EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space(3);

        EditorGUILayout.LabelField("3. No phpMyAdmin (http://localhost/phpmyadmin), importe diretamente o arquivo 'tank_gamers.sql'.", EditorStyles.wordWrappedLabel);

        EditorGUILayout.Space(16);

        // Botão para abrir a pasta do backend no Explorer
        if (GUILayout.Button("Abrir Pasta Assets/Backend no Explorador", GUILayout.Height(34)))
        {
            AbrirPastaBackend();
        }

        EditorGUILayout.Space(8);

        // Botão para carregar a cena principal
        GUI.backgroundColor = new Color(0.2f, 0.6f, 0.9f);
        if (GUILayout.Button("Abrir Cena Principal (WaifuTanks.unity)", GUILayout.Height(38)))
        {
            AbrirCenaPrincipal();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(14);

        // Opção para não mostrar novamente
        EditorGUI.BeginChangeCheck();
        _dontShowAgain = EditorGUILayout.ToggleLeft("Não mostrar esta janela automaticamente ao abrir o projeto", _dontShowAgain);
        if (EditorGUI.EndChangeCheck())
        {
            EditorPrefs.SetBool(ShowAtStartupKey, !_dontShowAgain);
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Você pode reabrir esta janela a qualquer momento em Window > Configuração Inicial.", EditorStyles.miniLabel);
    }

    private static void AbrirPastaBackend()
    {
        string fullPath = Path.GetFullPath(BACKEND_FOLDER_PATH);
        if (Directory.Exists(fullPath))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = fullPath,
                UseShellExecute = true
            });
        }
        else
        {
            EditorUtility.DisplayDialog("Aviso", "Pasta 'Assets/Backend' não foi encontrada no projeto.", "OK");
        }
    }

    private static void AbrirCenaPrincipal()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(SCENE_PATH);
        }
    }
}
