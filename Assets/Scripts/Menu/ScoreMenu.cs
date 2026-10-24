// Autor: Murillo Gomes Yonamine
// Data: 02/10/2026

using System;
using UnityEngine.UIElements;

public class ScoreMenu : IMenu
{
    private VisualElement _scoreScreen;
    private MultiColumnListView _scoreListView;
    private Button _scoreBackButton;

    private MenuManager _menuManager;
    private DataManager _dataManager;
    private Label _statusLabel;
    private DataManager.RankingEntry[] _ranking = Array.Empty<DataManager.RankingEntry>();
    private int _requestVersion;

    public void Initialize(VisualElement root, MenuManager menuManager, DataManager dataManager)
    {
        _menuManager = menuManager;
        _dataManager = dataManager;

        _scoreScreen = root.Q<VisualElement>("ScoreScreen");
        _scoreBackButton = root.Q<Button>("BackButton");

        _scoreListView = root.Q<MultiColumnListView>("ScoreList");
        _statusLabel = root.Q<Label>("RankingStatusLabel");
        _scoreListView.fixedItemHeight = 36;

        _scoreListView.columns["Jogador"].makeCell = () => new Label();
        _scoreListView.columns["Jogador"].bindCell = (element, index) =>
        {
            ((Label)element).text = _ranking[index].jogador_nome;
        };

        _scoreListView.columns["Pontos"].makeCell = () => new Label();
        _scoreListView.columns["Pontos"].bindCell = (element, index) =>
        {
            ((Label)element).text = _ranking[index].pontuacao.ToString();
        };

        _scoreListView.columns["Posicao"].makeCell = () => new Label();
        _scoreListView.columns["Posicao"].bindCell = (element, index) =>
        {
            ((Label)element).text = (index + 1).ToString();
        };

        _scoreBackButton.clicked += OnBackClicked;
    }

    public void Dispose()
    {
        _scoreBackButton.clicked -= OnBackClicked;
        _requestVersion++;
    }

    public void Show()
    {
        _scoreScreen.style.display = DisplayStyle.Flex;
        int requestVersion = ++_requestVersion;
        _ranking = Array.Empty<DataManager.RankingEntry>();
        _scoreListView.itemsSource = _ranking;
        _scoreListView.Rebuild();
        _statusLabel.text = "Carregando ranking de vitórias...";
        int playerId = GameManager.Instance?.PlayerData?.id ?? 0;
        _dataManager.LoadRanking(playerId, (ranking, error) =>
        {
            // Ignore stale responses after leaving/reopening the menu or disposing it.
            if (requestVersion != _requestVersion) return;
            if (error != null)
            {
                _statusLabel.text = error;
                _menuManager.PlayErrorSound();
                return;
            }
            _ranking = ranking;
            _scoreListView.itemsSource = _ranking;
            _scoreListView.Rebuild();
            _statusLabel.text = ranking.Length == 0 ? "Ainda não há vitórias registradas." :
                "Top 10 — melhor pontuação em uma vitória de cada jogador.";
        });
    }

    public void Hide()
    {
        _requestVersion++;
        _scoreScreen.style.display = DisplayStyle.None;
    }

    private void OnBackClicked()
    {
        _menuManager.OpenMainMenu();
    }

}
