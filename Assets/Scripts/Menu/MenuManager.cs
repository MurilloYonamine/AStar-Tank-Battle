// Autor: Murillo Gomes Yonamine
// Data: 02/10/2026

using UnityEngine;
using UnityEngine.UIElements;

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance { get; private set; }

    [SerializeField] private UIDocument _uiDocument;
    [SerializeField] private DataManager _dataManager;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip _buttonClickClip;
    [SerializeField] private AudioClip _errorClip;

    private VisualElement _mainMenu;
    private Button _playButton;
    private Button _scoreButton;
    private Button _settingsButton;
    private Button _exitButton;

    private readonly AuthMenu _authMenu = new AuthMenu();
    private readonly ScoreMenu _scoreMenu = new ScoreMenu();
    private readonly SettingsMenu _settingsMenu = new SettingsMenu();
    private IMenu[] _menus;

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
        var root = _uiDocument.rootVisualElement;

        _mainMenu = root.Q<VisualElement>("MainMenu");
        _playButton = root.Q<Button>("PlayButton");
        _scoreButton = root.Q<Button>("ScoreButton");
        _settingsButton = root.Q<Button>("SettingsButton");
        _exitButton = root.Q<Button>("ExitButton");

        _playButton.clicked += OpenPlayScreen;
        _scoreButton.clicked += OpenScoreScreen;
        _settingsButton.clicked += OpenSettingsScreen;
        _exitButton.clicked += OnExitClicked;

        root.Query<Button>().ForEach(btn => btn.clicked += PlayButtonClickSound);

        _menus = new IMenu[] { _authMenu, _scoreMenu, _settingsMenu };

        _authMenu.Initialize(root, this, _dataManager);
        _scoreMenu.Initialize(root, this, _dataManager);
        _settingsMenu.Initialize(root, this);

        OpenMainMenu();
    }

    private void OnDisable()
    {
        _playButton.clicked -= OpenPlayScreen;
        _scoreButton.clicked -= OpenScoreScreen;
        _settingsButton.clicked -= OpenSettingsScreen;
        _exitButton.clicked -= OnExitClicked;

        foreach (var menu in _menus)
        {
            menu.Dispose();
        }
    }

    public void OpenMainMenu()
    {
        _mainMenu.style.display = DisplayStyle.Flex;
        foreach (var menu in _menus)
        {
            menu.Hide();
        }
    }

    private void ShowMenu(IMenu targetMenu)
    {
        _mainMenu.style.display = DisplayStyle.None;
        foreach (var menu in _menus)
        {
            menu.Hide();
        }
        targetMenu.Show();
    }

    public void OpenPlayScreen() => ShowMenu(_authMenu);
    public void OpenScoreScreen() => ShowMenu(_scoreMenu);
    public void OpenSettingsScreen() => ShowMenu(_settingsMenu);
    public void PlayButtonClickSound() => AudioManager.Instance.PlaySFX(_buttonClickClip);
    public void PlayErrorSound() => AudioManager.Instance.PlaySFX(_errorClip);

    public void UpdatePlayers(PlayerData[] players)
    {
        _authMenu.SetPlayers(players);
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
