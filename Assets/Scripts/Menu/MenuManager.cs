// Autor: Murillo Gomes Yonamine
// Data: 27/09/2026

using System.Collections.Generic;
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
    private Button _startButton;
    private Button _scoreButton;
    private Button _exitButton;
    private Button _scoreBackButton;
    private Button _playBackButton;

    private MultiColumnListView _scoreListView;
    private TextField _playerNameField;
    private Label _playerNameError;

    [Header("Visual Elements")]
    private VisualElement _mainMenu;
    private VisualElement _scoreScreen;
    private VisualElement _playScreen;

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
    }

    private void OnEnable()
    {
        DataManager.onDataLoaded += OnDataLoaded;

        var root = _uiDocument.rootVisualElement;

        _mainMenu = root.Q<VisualElement>("MainMenu");
        _scoreScreen = root.Q<VisualElement>("ScoreScreen");
        _playScreen = root.Q<VisualElement>("PlayScreen");

        _mainMenu.style.display = DisplayStyle.Flex;
        _scoreScreen.style.display = DisplayStyle.None;
        _playScreen.style.display = DisplayStyle.None;

        _scoreListView = root.Q<MultiColumnListView>("ScoreList");
        _scoreListView.fixedItemHeight = 36;

        _scoreListView.columns["Jogador"].makeCell = () => new Label();
        _scoreListView.columns["Jogador"].bindCell = (element, index) =>
        {
            ((Label)element).text = _players[index].apelido;
        };

        _scoreListView.columns["Pontos"].makeCell = () => new Label();
        _scoreListView.columns["Pontos"].bindCell = (element, index) =>
        {
            ((Label)element).text = _players[index].pontos.ToString();
        };

        _scoreListView.columns["Data"].makeCell = () => new Label();
        _scoreListView.columns["Data"].bindCell = (element, index) =>
        {
            ((Label)element).text = _players[index].data;
        };

        _playButton = root.Q<Button>("PlayButton");
        _scoreButton = root.Q<Button>("ScoreButton");
        _exitButton = root.Q<Button>("ExitButton");
        _scoreBackButton = root.Q<Button>("BackButton");
        _startButton = root.Q<Button>("StartButton");
        _playBackButton = root.Q<Button>("PlayBackButton");
        _playerNameField = root.Q<TextField>("PlayerNameField");
        _playerNameError = root.Q<Label>("PlayerNameError");
        _playerNameError.style.display = DisplayStyle.None;

        // Foca o campo de nome do jogador após um pequeno atraso para garantir que o UI esteja pronto
        _playerNameField.schedule.Execute(() => _playerNameField.Focus()).StartingIn(100);

        _playButton.clicked += OnPlayClicked;
        _startButton.clicked += OnStartClicked;
        _scoreButton.clicked += OnScoreClicked;
        _exitButton.clicked += OnExitClicked;
        _playerNameField.RegisterValueChangedCallback(OnPlayerNameChanged);

        _scoreBackButton.clicked += OnBackClicked;
        _playBackButton.clicked += OnPlayBackClicked;
    }

    private void OnDisable()
    {
        DataManager.onDataLoaded -= OnDataLoaded;

        _playButton.clicked -= OnPlayClicked;
        _startButton.clicked -= OnStartClicked;
        _scoreButton.clicked -= OnScoreClicked;
        _exitButton.clicked -= OnExitClicked;

        _scoreBackButton.clicked -= OnBackClicked;
        _playBackButton.clicked -= OnPlayBackClicked;
        _playerNameField.UnregisterValueChangedCallback(OnPlayerNameChanged);
    }

    private void OnPlayClicked()
    {
        _mainMenu.style.display = DisplayStyle.None;
        _playScreen.style.display = DisplayStyle.Flex;

        // Foca o campo de nome do jogador após um pequeno atraso para garantir que o UI esteja pronto
        _playerNameField.schedule.Execute(() => _playerNameField.Focus()).StartingIn(100);
    }

    private void OnStartClicked()
    {
        string playerName = _playerNameField.value.Trim();

        if (string.IsNullOrEmpty(playerName))
        {
            _playerNameError.text = "O nome do jogador não pode estar vazio.";
            _playerNameError.style.display = DisplayStyle.Flex;
            _playerNameField.Focus();
            return;
        }

        _playerNameError.style.display = DisplayStyle.None;
        _playScreen.style.display = DisplayStyle.None;
        GameManager.Instance.StartGame(playerName);
    }

    private void OnPlayerNameChanged(ChangeEvent<string> changeEvent)
    {
        if (!string.IsNullOrWhiteSpace(changeEvent.newValue))
        {
            _playerNameError.style.display = DisplayStyle.None;
        }
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

        PlayerRootObject playerDataList =
            JsonUtility.FromJson<PlayerRootObject>(wrappedJson);

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

    private void OnPlayBackClicked()
    {
        _playScreen.style.display = DisplayStyle.None;
        _mainMenu.style.display = DisplayStyle.Flex;
        _playerNameError.style.display = DisplayStyle.None;
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
