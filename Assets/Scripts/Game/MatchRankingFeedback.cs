using System;

/// <summary>Pure presentation rules. A ranked account does not imply its latest run set that mark.</summary>
public static class MatchRankingFeedback
{
    public enum Kind { Saved, PreviousBest, TopTen, FirstPlace, TiedFirst, NewRecord, FirstRecord }

    public static Kind Evaluate(DataManager.RankingResponse response, int playerId, double duration, out string message)
    {
        DataManager.RankingEntry own = Array.Find(response.ranking, entry => entry.jogador_id == playerId);
        if (own == null)
        {
            message = "Partida salva! Continue tentando para entrar no TOP 10.";
            return Kind.Saved;
        }
        if (Milliseconds(duration) != Milliseconds(own.duracao_segundos))
        {
            message = "Partida salva! Seu melhor tempo continua sendo " + MatchTime.Format(own.duracao_segundos) + ".";
            return Kind.PreviousBest;
        }
        if (own.posicao == 1)
        {
            bool tied = Array.Exists(response.ranking, entry => entry.jogador_id != playerId && entry.posicao == 1);
            if (tied)
            {
                message = "Parabéns! Seu tempo empatou em 1º lugar no ranking!";
                return Kind.TiedFirst;
            }
            if (response.comparacao_recorde_disponivel && response.recorde_anterior_segundos == 0)
            {
                message = "PRIMEIRO RECORDE! Você marcou o melhor tempo do ranking!";
                return Kind.FirstRecord;
            }
            if (response.comparacao_recorde_disponivel &&
                Milliseconds(duration) < Milliseconds(response.recorde_anterior_segundos))
            {
                message = "NOVO RECORDE! Você quebrou a melhor marca e conquistou o 1º lugar!";
                return Kind.NewRecord;
            }
            message = "MELHOR TEMPO! Parabéns pelo 1º lugar no ranking!";
            return Kind.FirstPlace;
        }
        message = "Parabéns! Seu tempo entrou no TOP 10 — " + own.posicao + "º lugar!";
        return Kind.TopTen;
    }

    private static long Milliseconds(double seconds) => (long)Math.Round(seconds * 1000d, MidpointRounding.AwayFromZero);
}
