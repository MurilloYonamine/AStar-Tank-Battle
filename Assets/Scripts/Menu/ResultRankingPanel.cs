using System;
using UnityEngine.UIElements;

/// <summary>
/// Binds the authored victory ranking to the authenticated PHP API after a confirmed save.
/// Leaving the result invalidates callbacks; retrying a read never saves the match again.
/// </summary>
public sealed class ResultRankingPanel : IDisposable
{
    private readonly VisualElement panel;
    private readonly MultiColumnListView list;
    private readonly Label status;
    private readonly Button retry;
    private readonly Label feedback;
    private DataManager.RankingEntry[] entries = Array.Empty<DataManager.RankingEntry>();
    private DataManager backend;
    private int playerId;
    private int requestVersion;
    private string matchId;
    private double completionSeconds;

    public ResultRankingPanel(VisualElement root)
    {
        panel = root.Q("ResultRankingPanel");
        list = root.Q<MultiColumnListView>("ResultRankingList");
        status = root.Q<Label>("ResultRankingStatusLabel");
        retry = root.Q<Button>("RetryRankingButton");
        feedback = root.Q<Label>("ResultRankingNoteLabel");
        if (list == null) return; // Other scenes may not include this optional result panel.
        list.fixedItemHeight = 28;
        list.columns["Posicao"].makeCell = () => new Label();
        list.columns["Posicao"].bindCell = (cell, index) => ((Label)cell).text = entries[index].posicao.ToString();
        list.columns["Jogador"].makeCell = () => new Label();
        list.columns["Jogador"].bindCell = (cell, index) => ((Label)cell).text = entries[index].jogador_nome;
        list.columns["Tempo"].makeCell = () => new Label();
        list.columns["Tempo"].bindCell = (cell, index) => ((Label)cell).text = MatchTime.Format(entries[index].duracao_segundos);
        retry.clicked += Retry;
        Close();
    }

    public void Prepare(bool victory)
    {
        Close();
        if (panel == null || !victory) return;
        panel.style.display = DisplayStyle.Flex;
        status.text = "Salve a partida para atualizar o ranking.";
    }

    public void Load(DataManager dataManager, int authenticatedPlayerId, string savedMatchId = null, double savedDuration = 0)
    {
        if (list == null) return;
        backend = dataManager;
        playerId = authenticatedPlayerId;
        matchId = savedMatchId;
        completionSeconds = savedDuration;
        int version = ++requestVersion;
        status.text = "Carregando melhores tempos...";
        retry.style.display = DisplayStyle.None;
        if (backend == null)
        {
            status.text = "Não foi possível acessar o servidor do ranking.";
            return;
        }
        backend.LoadMatchRanking(playerId, matchId, (response, error) =>
        {
            if (version != requestVersion) return;
            if (error != null)
            {
                status.text = error;
                if (feedback != null) feedback.text = "Partida salva. Não foi possível confirmar sua colocação.";
                retry.style.display = DisplayStyle.Flex;
                return;
            }
            entries = response.ranking;
            list.itemsSource = entries;
            list.Rebuild();
            status.text = entries.Length == 0 ? "Ainda não há vitórias registradas." :
                "Melhor vitória por jogador. Menor tempo é melhor; empates compartilham a posição.";
            if (feedback != null && completionSeconds > 0)
            {
                MatchRankingFeedback.Kind kind = MatchRankingFeedback.Evaluate(response, playerId, completionSeconds, out string text);
                feedback.text = text;
                feedback.EnableInClassList("result-record", kind == MatchRankingFeedback.Kind.NewRecord || kind == MatchRankingFeedback.Kind.FirstRecord);
                feedback.EnableInClassList("result-top-ten", kind == MatchRankingFeedback.Kind.TopTen ||
                    kind == MatchRankingFeedback.Kind.FirstPlace || kind == MatchRankingFeedback.Kind.TiedFirst);
            }
        });
    }

    private void Retry() => Load(backend, playerId, matchId, completionSeconds);

    public void Close()
    {
        requestVersion++;
        backend = null;
        feedback?.RemoveFromClassList("result-record");
        feedback?.RemoveFromClassList("result-top-ten");
        entries = Array.Empty<DataManager.RankingEntry>();
        if (list != null) { list.itemsSource = entries; list.Rebuild(); }
        if (retry != null) retry.style.display = DisplayStyle.None;
        if (panel != null) panel.style.display = DisplayStyle.None;
    }

    public void Dispose()
    {
        Close();
        if (retry != null) retry.clicked -= Retry;
    }
}
