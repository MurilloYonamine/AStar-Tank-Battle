// Autor: Murillo Gomes Yonamine
// Data: 02/10/2026

using UnityEngine;
using UnityEngine.UIElements;

public class ScoreMenu : IMenu
{
    private VisualElement _scoreScreen;
    private MultiColumnListView _scoreListView;
    private Button _scoreBackButton;

    private MenuManager _menuManager;
    private DataManager _dataManager;
    private PlayerData[] _players;

    public void Initialize(VisualElement root, MenuManager menuManager, DataManager dataManager)
    {
        _menuManager = menuManager;
        _dataManager = dataManager;

        _scoreScreen = root.Q<VisualElement>("ScoreScreen");
        _scoreBackButton = root.Q<Button>("BackButton");

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

        _scoreBackButton.clicked += OnBackClicked;
        DataManager.onDataLoaded += OnDataLoaded;
    }

    public void Dispose()
    {
        _scoreBackButton.clicked -= OnBackClicked;
        DataManager.onDataLoaded -= OnDataLoaded;
    }

    public void Show()
    {
        _scoreScreen.style.display = DisplayStyle.Flex;
        _dataManager.LoadData();
    }

    public void Hide()
    {
        _scoreScreen.style.display = DisplayStyle.None;
    }

    private void OnBackClicked()
    {
        _menuManager.OpenMainMenu();
    }

    private void OnDataLoaded(string jsonData)
    {
        string wrappedJson = "{\"players\":" + jsonData + "}";
        PlayerRootObject playerDataList = JsonUtility.FromJson<PlayerRootObject>(wrappedJson);

        if (playerDataList == null || playerDataList.players == null)
        {
            _menuManager.PlayErrorSound();
            Debug.LogError("A lista de jogadores está nula.");
            return;
        }

        Debug.Log("Quantidade de jogadores: " + playerDataList.players.Length);

        _players = playerDataList.players;
        _scoreListView.itemsSource = _players;
        _scoreListView.Rebuild();

        _menuManager.UpdatePlayers(_players);
    }
}
