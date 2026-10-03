// Autor: Murillo Gomes Yonamine
// Data: 02/10/2026

using System;
using UnityEngine;
using UnityEngine.UIElements;

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance { get; private set; }

    [SerializeField] private UIDocument _uiDocument;
    [SerializeField] private DataManager _dataManager;
    private PlayerData[] _players;

    [Header("Buttons")]
    private Button _playButton;
    private Button _scoreButton;
    private Button _exitButton;
    private Button _scoreBackButton;

    private Button _playBackButton;
    private Button _openRegisterButton;
    private Button _startButton;

    private Button _registerBackButton;
    private Button _registerSubmitButton;

    [Header("Visual Elements")]
    private VisualElement _mainMenu;
    private VisualElement _scoreScreen;
    private VisualElement _playScreen;
    private VisualElement _loginContainer;
    private VisualElement _registerContainer;

    private MultiColumnListView _scoreListView;

    private TextField _loginEmailField;
    private TextField _loginPasswordField;
    private Label _loginError;

    private TextField _registerNameField;
    private TextField _registerEmailField;
    private TextField _registerPasswordField;
    private Label _registerError;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        PauseManager.Instance.SetPause(true);
        _dataManager.LoadData();
    }

    private void OnEnable()
    {
        DataManager.onDataLoaded += OnDataLoaded;

        var root = _uiDocument.rootVisualElement;

        _mainMenu = root.Q<VisualElement>("MainMenu");
        _scoreScreen = root.Q<VisualElement>("ScoreScreen");
        _playScreen = root.Q<VisualElement>("PlayScreen");
        _loginContainer = root.Q<VisualElement>("LoginContainer");
        _registerContainer = root.Q<VisualElement>("RegisterContainer");

        _mainMenu.style.display = DisplayStyle.Flex;
        _scoreScreen.style.display = DisplayStyle.None;
        _playScreen.style.display = DisplayStyle.None;
        _loginContainer.style.display = DisplayStyle.Flex;
        _registerContainer.style.display = DisplayStyle.None;

        _scoreListView = root.Q<MultiColumnListView>("ScoreList");
        _scoreListView.fixedItemHeight = 36;

        _scoreListView.columns["Jogador"].makeCell = () => new Label();
        _scoreListView.columns["Jogador"].bindCell = (element, index) =>
        {
            ((Label)element).text = _players[index].name;
        };

        _scoreListView.columns["Pontos"].makeCell = () => new Label();
        _scoreListView.columns["Pontos"].bindCell = (element, index) =>
        {
            ((Label)element).text = _players[index].pontos.ToString();
        };

        _scoreListView.columns["Data"].makeCell = () => new Label();
        _scoreListView.columns["Data"].bindCell = (element, index) =>
        {
            ((Label)element).text = _players[index].created_at;
        };

        // Main Menu
        _playButton = root.Q<Button>("PlayButton");
        _scoreButton = root.Q<Button>("ScoreButton");
        _exitButton = root.Q<Button>("ExitButton");
        _scoreBackButton = root.Q<Button>("BackButton");

        // Login
        _loginEmailField = root.Q<TextField>("LoginEmailField");
        _loginPasswordField = root.Q<TextField>("LoginPasswordField");
        _loginError = root.Q<Label>("LoginError");
        _playBackButton = root.Q<Button>("PlayBackButton");
        _openRegisterButton = root.Q<Button>("OpenRegisterButton");
        _startButton = root.Q<Button>("StartButton");
        _loginError.style.display = DisplayStyle.None;

        // Register
        _registerNameField = root.Q<TextField>("RegisterNameField");
        _registerEmailField = root.Q<TextField>("RegisterEmailField");
        _registerPasswordField = root.Q<TextField>("RegisterPasswordField");
        _registerError = root.Q<Label>("RegisterError");
        _registerBackButton = root.Q<Button>("RegisterBackButton");
        _registerSubmitButton = root.Q<Button>("RegisterSubmitButton");
        _registerError.style.display = DisplayStyle.None;

        _playButton.clicked += OnPlayClicked;
        _scoreButton.clicked += OnScoreClicked;
        _exitButton.clicked += OnExitClicked;
        _scoreBackButton.clicked += OnBackClicked;

        _playBackButton.clicked += OnPlayBackClicked;
        _openRegisterButton.clicked += OnOpenRegisterClicked;
        _startButton.clicked += OnStartClicked;

        _registerBackButton.clicked += OnRegisterBackClicked;
        _registerSubmitButton.clicked += OnRegisterSubmitClicked;

        _loginEmailField.RegisterValueChangedCallback(OnFieldValueChanged);
        _loginPasswordField.RegisterValueChangedCallback(OnFieldValueChanged);
        _registerNameField.RegisterValueChangedCallback(OnFieldValueChanged);
        _registerEmailField.RegisterValueChangedCallback(OnFieldValueChanged);
        _registerPasswordField.RegisterValueChangedCallback(OnFieldValueChanged);
    }

    private void OnDisable()
    {
        DataManager.onDataLoaded -= OnDataLoaded;

        _playButton.clicked -= OnPlayClicked;
        _scoreButton.clicked -= OnScoreClicked;
        _exitButton.clicked -= OnExitClicked;
        _scoreBackButton.clicked -= OnBackClicked;

        _playBackButton.clicked -= OnPlayBackClicked;
        _openRegisterButton.clicked -= OnOpenRegisterClicked;
        _startButton.clicked -= OnStartClicked;

        _registerBackButton.clicked -= OnRegisterBackClicked;
        _registerSubmitButton.clicked -= OnRegisterSubmitClicked;

        _loginEmailField.UnregisterValueChangedCallback(OnFieldValueChanged);
        _loginPasswordField.UnregisterValueChangedCallback(OnFieldValueChanged);
        _registerNameField.UnregisterValueChangedCallback(OnFieldValueChanged);
        _registerEmailField.UnregisterValueChangedCallback(OnFieldValueChanged);
        _registerPasswordField.UnregisterValueChangedCallback(OnFieldValueChanged);
    }

    private void OnPlayClicked()
    {
        _mainMenu.style.display = DisplayStyle.None;
        _playScreen.style.display = DisplayStyle.Flex;
        _loginContainer.style.display = DisplayStyle.Flex;
        _registerContainer.style.display = DisplayStyle.None;
        _loginError.style.display = DisplayStyle.None;

        _loginEmailField.schedule.Execute(() => _loginEmailField.Focus()).StartingIn(100);
    }

    private void OnOpenRegisterClicked()
    {
        _loginContainer.style.display = DisplayStyle.None;
        _registerContainer.style.display = DisplayStyle.Flex;
        _registerError.style.display = DisplayStyle.None;

        _registerNameField.schedule.Execute(() => _registerNameField.Focus()).StartingIn(100);
    }

    private void OnRegisterBackClicked()
    {
        _registerContainer.style.display = DisplayStyle.None;
        _loginContainer.style.display = DisplayStyle.Flex;
        _loginError.style.display = DisplayStyle.None;

        _loginEmailField.schedule.Execute(() => _loginEmailField.Focus()).StartingIn(100);
    }

    private void OnPlayBackClicked()
    {
        _playScreen.style.display = DisplayStyle.None;
        _mainMenu.style.display = DisplayStyle.Flex;
        _loginError.style.display = DisplayStyle.None;
        _registerError.style.display = DisplayStyle.None;
    }

    private void OnStartClicked()
    {
        string email = _loginEmailField.value.Trim();
        string password = _loginPasswordField.value;

        if (string.IsNullOrEmpty(email))
        {
            _loginError.text = "O e-mail não pode estar vazio.";
            _loginError.style.display = DisplayStyle.Flex;
            _loginEmailField.Focus();
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            _loginError.text = "A senha não pode estar vazia.";
            _loginError.style.display = DisplayStyle.Flex;
            _loginPasswordField.Focus();
            return;
        }

        if (_players != null && _players.Length > 0)
        {
            PlayerData matched = Array.Find(_players, p => string.Equals(p.email, email, StringComparison.OrdinalIgnoreCase));

            if (matched == null)
            {
                _loginError.text = "Usuário não encontrado. Realize o cadastro primeiro.";
                _loginError.style.display = DisplayStyle.Flex;
                return;
            }

            if (matched.password != password)
            {
                _loginError.text = "Senha incorreta.";
                _loginError.style.display = DisplayStyle.Flex;
                _loginPasswordField.Focus();
                return;
            }

            _loginError.style.display = DisplayStyle.None;
            _playScreen.style.display = DisplayStyle.None;
            GameManager.Instance.StartGame(matched);
            return;
        }

        _loginError.style.display = DisplayStyle.None;
        _playScreen.style.display = DisplayStyle.None;
        string fallbackName = email.Contains("@") ? email.Split('@')[0] : email;
        GameManager.Instance.StartGame(fallbackName, email, password);
    }

    private void OnRegisterSubmitClicked()
    {
        string name = _registerNameField.value.Trim();
        string email = _registerEmailField.value.Trim();
        string password = _registerPasswordField.value;

        if (string.IsNullOrEmpty(name))
        {
            _registerError.text = "O nome não pode estar vazio.";
            _registerError.style.display = DisplayStyle.Flex;
            _registerNameField.Focus();
            return;
        }

        if (string.IsNullOrEmpty(email) || !email.Contains("@"))
        {
            _registerError.text = "Informe um e-mail válido.";
            _registerError.style.display = DisplayStyle.Flex;
            _registerEmailField.Focus();
            return;
        }

        if (string.IsNullOrEmpty(password) || password.Length < 3)
        {
            _registerError.text = "A senha deve ter pelo menos 3 caracteres.";
            _registerError.style.display = DisplayStyle.Flex;
            _registerPasswordField.Focus();
            return;
        }

        if (_players != null && Array.Exists(_players, p => string.Equals(p.email, email, StringComparison.OrdinalIgnoreCase)))
        {
            _registerError.text = "Este e-mail já está cadastrado.";
            _registerError.style.display = DisplayStyle.Flex;
            _registerEmailField.Focus();
            return;
        }

        _dataManager.SaveData(name, email, password, 0);
        _dataManager.LoadData();

        _registerError.style.display = DisplayStyle.None;
        _registerContainer.style.display = DisplayStyle.None;
        _loginContainer.style.display = DisplayStyle.Flex;

        _loginEmailField.value = email;
        _loginPasswordField.value = "";

        _loginError.text = "<color=#70FF70>Cadastro realizado! Digite a senha para entrar.</color>";
        _loginError.style.display = DisplayStyle.Flex;
        _loginPasswordField.schedule.Execute(() => _loginPasswordField.Focus()).StartingIn(100);
    }

    private void OnFieldValueChanged(ChangeEvent<string> evt)
    {
        _loginError.style.display = DisplayStyle.None;
        _registerError.style.display = DisplayStyle.None;
    }

    private void OnScoreClicked()
    {
        _mainMenu.style.display = DisplayStyle.None;
        _scoreScreen.style.display = DisplayStyle.Flex;
        _dataManager.LoadData();
    }

    private void OnDataLoaded(string jsonData)
    {
        string wrappedJson = "{\"players\":" + jsonData + "}";

        PlayerRootObject playerDataList = JsonUtility.FromJson<PlayerRootObject>(wrappedJson);

        if (playerDataList == null || playerDataList.players == null)
        {
            Debug.LogError("A lista de jogadores está nula.");
            return;
        }

        Debug.Log("Quantidade de jogadores: " + playerDataList.players.Length);

        _players = playerDataList.players;
        _scoreListView.itemsSource = _players;
        _scoreListView.Rebuild();
    }

    private void OnBackClicked()
    {
        _scoreScreen.style.display = DisplayStyle.None;
        _mainMenu.style.display = DisplayStyle.Flex;
    }


    private void OnExitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
