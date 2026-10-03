// Autor: Murillo Gomes Yonamine
// Data: 02/10/2026

using System;
using UnityEngine.UIElements;

public class AuthMenu : IMenu
{
    private VisualElement _playScreen;
    private VisualElement _loginContainer;
    private VisualElement _registerContainer;

    private TextField _loginEmailField;
    private TextField _loginPasswordField;
    private Label _loginError;

    private TextField _registerNameField;
    private TextField _registerEmailField;
    private TextField _registerPasswordField;
    private Label _registerError;

    private Button _playBackButton;
    private Button _skipLoginButton;
    private Button _openRegisterButton;
    private Button _startButton;
    private Button _registerBackButton;
    private Button _registerSubmitButton;

    private MenuManager _menuManager;
    private DataManager _dataManager;
    private PlayerData[] _players;

    public void Initialize(VisualElement root, MenuManager menuManager, DataManager dataManager)
    {
        _menuManager = menuManager;
        _dataManager = dataManager;

        _playScreen = root.Q<VisualElement>("PlayScreen");
        _loginContainer = root.Q<VisualElement>("LoginContainer");
        _registerContainer = root.Q<VisualElement>("RegisterContainer");

        _loginEmailField = root.Q<TextField>("LoginEmailField");
        _loginPasswordField = root.Q<TextField>("LoginPasswordField");
        _loginError = root.Q<Label>("LoginError");

        _registerNameField = root.Q<TextField>("RegisterNameField");
        _registerEmailField = root.Q<TextField>("RegisterEmailField");
        _registerPasswordField = root.Q<TextField>("RegisterPasswordField");
        _registerError = root.Q<Label>("RegisterError");

        _playBackButton = root.Q<Button>("PlayBackButton");
        _skipLoginButton = root.Q<Button>("SkipLoginButton");
        _openRegisterButton = root.Q<Button>("OpenRegisterButton");
        _startButton = root.Q<Button>("StartButton");

        _registerBackButton = root.Q<Button>("RegisterBackButton");
        _registerSubmitButton = root.Q<Button>("RegisterSubmitButton");

        _playBackButton.clicked += OnPlayBackClicked;
        _skipLoginButton.clicked += OnSkipLoginClicked;
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

    public void Dispose()
    {
        _playBackButton.clicked -= OnPlayBackClicked;
        _skipLoginButton.clicked -= OnSkipLoginClicked;
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

    public void Show()
    {
        _playScreen.style.display = DisplayStyle.Flex;
        _loginContainer.style.display = DisplayStyle.Flex;
        _registerContainer.style.display = DisplayStyle.None;
        _loginError.style.display = DisplayStyle.None;

        _loginEmailField.schedule.Execute(() => _loginEmailField.Focus()).StartingIn(100);
    }

    public void Hide()
    {
        _playScreen.style.display = DisplayStyle.None;
        _loginError.style.display = DisplayStyle.None;
        _registerError.style.display = DisplayStyle.None;
    }

    public void SetPlayers(PlayerData[] players)
    {
        _players = players;
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
        _menuManager.OpenMainMenu();
    }

    private void OnSkipLoginClicked()
    {
        Hide();
        GameManager.Instance.StartGame("Convidado");
    }

    private void OnStartClicked()
    {
        string email = _loginEmailField.value.Trim();
        string password = _loginPasswordField.value;

        if (string.IsNullOrEmpty(email))
        {
            _menuManager.PlayErrorSound();
            _loginError.text = "O e-mail não pode estar vazio.";
            _loginError.style.display = DisplayStyle.Flex;
            _loginEmailField.Focus();
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            _menuManager.PlayErrorSound();
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
                _menuManager.PlayErrorSound();
                _loginError.text = "Usuário não encontrado. Realize o cadastro primeiro.";
                _loginError.style.display = DisplayStyle.Flex;
                return;
            }

            if (matched.password != password)
            {
                _menuManager.PlayErrorSound();
                _loginError.text = "Senha incorreta.";
                _loginError.style.display = DisplayStyle.Flex;
                _loginPasswordField.Focus();
                return;
            }

            Hide();
            GameManager.Instance.StartGame(matched);
            return;
        }

        Hide();
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
            _menuManager.PlayErrorSound();
            _registerError.text = "O nome não pode estar vazio.";
            _registerError.style.display = DisplayStyle.Flex;
            _registerNameField.Focus();
            return;
        }

        if (string.IsNullOrEmpty(email) || !email.Contains("@"))
        {
            _menuManager.PlayErrorSound();
            _registerError.text = "Informe um e-mail válido.";
            _registerError.style.display = DisplayStyle.Flex;
            _registerEmailField.Focus();
            return;
        }

        if (string.IsNullOrEmpty(password) || password.Length < 3)
        {
            _menuManager.PlayErrorSound();
            _registerError.text = "A senha deve ter pelo menos 3 caracteres.";
            _registerError.style.display = DisplayStyle.Flex;
            _registerPasswordField.Focus();
            return;
        }

        if (_players != null && Array.Exists(_players, p => string.Equals(p.email, email, StringComparison.OrdinalIgnoreCase)))
        {
            _menuManager.PlayErrorSound();
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
}
