// Autor original: Murillo Gomes Yonamine
// Modificações: João Pedro Ribeiro Vivas
// Data: 02/10/2026

using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Janela do Editor (só existe dentro da Unity, não vai para o jogo compilado) que aparece
/// ao abrir o projeto e explica como começar. Fica em Window > Configuração Inicial.
/// </summary>
[InitializeOnLoad] // faz a classe ser inicializada assim que a Unity abre/recompila
public class ProjectWelcomeWindow : EditorWindow
{
    // Cena principal do jogo.
    private const string SCENE_PATH = "Assets/Scenes/UmaTank.unity";

    // Nome do arquivo de dados. PRECISA ser igual ao FILE_NAME do DataManager.
    private const string DATA_FILE_NAME = "tank_gamers_local.json";

    // Chave usada para lembrar (por projeto) se a janela deve abrir sozinha.
    private static string ShowAtStartupKey => "ShowWelcomeAtStartup_" + Application.dataPath.GetHashCode();

    private bool _dontShowAgain;

    // Construtor estático: roda quando a Unity carrega. Agenda a checagem para
    // depois que o Editor terminar de iniciar (delayCall).
    static ProjectWelcomeWindow()
    {
        EditorApplication.delayCall += CheckStartupOpen;
    }

    // Abre a janela automaticamente, a menos que o usuário tenha marcado "não mostrar".
    private static void CheckStartupOpen()
    {
        if (EditorPrefs.GetBool(ShowAtStartupKey, true))
        {
            ShowWindow();
        }
    }

    // Cria o item de menu Window > Configuração Inicial e abre a janela (tamanho fixo).
    [MenuItem("Window/Configuração Inicial")]
    public static void ShowWindow()
    {
        ProjectWelcomeWindow window = GetWindow<ProjectWelcomeWindow>(false, "Configuração Inicial", true);
        window.minSize = new Vector2(510, 400);
        window.maxSize = new Vector2(510, 400);
        window.Show();
    }

    // Lê do EditorPrefs o estado da caixinha "Não mostrar novamente".
    private void OnEnable()
    {
        _dontShowAgain = !EditorPrefs.GetBool(ShowAtStartupKey, true);
    }

    // Desenha o conteúdo da janela (chamado várias vezes por segundo pela Unity).
    private void OnGUI()
    {
        EditorGUILayout.Space(12);

        // Cabeçalho
        EditorGUILayout.LabelField("Bem-vindo ao Projeto UmaTank", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Contas, partidas e ranking agora são salvos localmente, num arquivo JSON no seu computador. " +
            "Não é mais necessário XAMPP, Apache, PHP ou MySQL.",
            MessageType.Info);

        EditorGUILayout.Space(12);

        // Passos
        EditorGUILayout.LabelField("Como começar:", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        EditorGUILayout.LabelField("1. Abra a cena principal (botão azul abaixo) e aperte Play.", EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space(3);

        EditorGUILayout.LabelField("2. Cadastre uma conta na tela de login e entre para jogar.", EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space(3);

        EditorGUILayout.LabelField(
            "3. Os dados ficam em '" + DATA_FILE_NAME + "' (botão abaixo abre a pasta). " +
            "Cada computador tem o seu próprio arquivo, então contas e ranking não são compartilhados entre os colegas.",
            EditorStyles.wordWrappedLabel);

        EditorGUILayout.Space(16);

        // Botão: abre no Explorador a pasta onde o JSON é salvo.
        if (GUILayout.Button("Abrir Pasta dos Dados Locais no Explorador", GUILayout.Height(34)))
        {
            AbrirPastaDados();
        }

        EditorGUILayout.Space(8);

        // Botão (azul): carrega a cena principal.
        GUI.backgroundColor = new Color(0.2f, 0.6f, 0.9f);
        if (GUILayout.Button("Abrir Cena Principal (UmaTank.unity)", GUILayout.Height(38)))
        {
            AbrirCenaPrincipal();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(14);

        // Caixinha para não mostrar a janela automaticamente nas próximas vezes.
        EditorGUI.BeginChangeCheck();
        _dontShowAgain = EditorGUILayout.ToggleLeft("Não mostrar esta janela automaticamente ao abrir o projeto", _dontShowAgain);
        if (EditorGUI.EndChangeCheck())
        {
            EditorPrefs.SetBool(ShowAtStartupKey, !_dontShowAgain);
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Você pode reabrir esta janela a qualquer momento em Window > Configuração Inicial.", EditorStyles.miniLabel);
    }

    // Abre a pasta persistentDataPath (onde o DataManager grava o JSON) no Explorador de Arquivos.
    private static void AbrirPastaDados()
    {
        // Mesma pasta usada pelo DataManager (Application.persistentDataPath).
        string folder = Application.persistentDataPath;

        // A pasta só existe depois que o jogo grava algo; criamos para o botão sempre funcionar.
        Directory.CreateDirectory(folder);

        Process.Start(new ProcessStartInfo
        {
            FileName = folder,
            UseShellExecute = true // faz o sistema operacional abrir a pasta com o programa padrão
        });
    }

    // Abre a cena principal, perguntando antes se quer salvar alterações da cena atual.
    private static void AbrirCenaPrincipal()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(SCENE_PATH);
        }
    }
}
