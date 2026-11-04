// Autor original: Murillo Gomes Yonamine
// Modificações: João Pedro Ribeiro Vivas
// Data: 27/09/2026

using System;
using System.Collections;
using System.IO;
using UnityEngine;

/// <summary>
/// ============================================================================
/// O QUE MUDOU NESTE ARQUIVO
/// ============================================================================
/// ANTES: este script montava formulários e os enviava por HTTP
///        (UnityWebRequest -> http://localhost/gsd) para o PHP, que falava com o MySQL.
///
/// AGORA: este script chama diretamente a classe LocalGameStore (que grava num arquivo
///        JSON na máquina do jogador). Não existe mais Apache, PHP, MySQL, XAMPP,
///        URL_BASE, certificado HTTPS nem token trafegando pela rede.
///
/// COMO FICOU COMPATÍVEL COM O RESTO DO JOGO
/// Todos os métodos públicos mantêm o MESMO nome, os MESMOS parâmetros e o MESMO jeito
/// de responder (callbacks). Por isso AuthMenu, ScoreMenu, ResultRankingPanel,
/// BattleSessionController, GameManager e MenuManager NÃO precisam ser alterados.
///
/// POR QUE AINDA USAMOS COROUTINES (yield return null)?
/// Com HTTP, a resposta chegava "depois" (assíncrona). As telas foram escritas
/// assumindo isso (ex.: o AuthMenu liga _loginPending = true ANTES de chamar Login).
/// Para não quebrar essa suposição, cada operação espera 1 frame antes de responder,
/// simulando o comportamento assíncrono. Isso também não trava a tela.
///
/// ONDE FICAM OS DADOS
/// Application.persistentDataPath/tank_gamers_local.json
/// (no Windows: %USERPROFILE%\AppData\LocalLow\NomeDaEmpresa\NomeDoProjeto\).
/// No Editor e no jogo compilado o caminho é o mesmo, então contas criadas em um
/// aparecem no outro. Clique com o botão direito neste componente no Inspector
/// para ver o caminho ou apagar o banco (menus "Mostrar caminho..." e "APAGAR...").
/// </summary>
public class DataManager : MonoBehaviour
{
    // Nome do arquivo onde o "banco de dados" é salvo.
    private const string FILE_NAME = "tank_gamers_local.json";

    // O "servidor" local. Criado sob demanda (ver propriedade Store logo abaixo).
    private LocalGameStore _store;

    /// <summary>
    /// Acesso ao banco local. Cria o LocalGameStore na primeira vez que é usado
    /// ("lazy"), assim não importa a ordem em que os scripts do Unity acordam.
    /// </summary>
    private LocalGameStore Store
    {
        get
        {
            if (_store == null)
            {
                string path = Path.Combine(Application.persistentDataPath, FILE_NAME);
                _store = new LocalGameStore(path);
            }
            return _store;
        }
    }

    // ------------------------------------------------------------------------
    // SESSÃO (substitui o token do PHP)
    // ------------------------------------------------------------------------
    // Antes: o Unity inventava um token, o PHP o associava ao jogador verificado
    // e o conferia em toda requisição. Agora a "sessão" é só: "quem está logado agora".
    // Mantivemos a propriedade Token (e a regra "sem token = sem acesso") para que o
    // resto do jogo continue funcionando igual: só quem fez login consegue salvar
    // partida ou ver o ranking.
    public string Token { get; private set; }

    // Id da conta que fez login. 0 = ninguém logado.
    private int _sessionPlayerId;

    // Contador de logins: se o jogador cancelar ou iniciar outro login, o contador muda
    // e a resposta do login antigo é descartada (mesma lógica do código original).
    private int _loginRequestId;

    // ------------------------------------------------------------------------
    // TIPOS DE RESPOSTA PÚBLICOS (usados pelas telas de ranking)
    // ------------------------------------------------------------------------

    /// <summary>Uma linha do ranking: posição, jogador e tempo da melhor vitória.</summary>
    [Serializable]
    public class RankingEntry
    {
        public int jogador_id;
        public string jogador_nome;
        public double duracao_segundos;
        public int posicao;
    }

    /// <summary>
    /// Resposta completa do ranking: a lista + dados para decidir se houve "novo recorde".
    /// comparacao_recorde_disponivel = true quando recorde_anterior_segundos é confiável.
    /// </summary>
    [Serializable]
    public class RankingResponse
    {
        public RankingEntry[] ranking;
        public string error;
        public bool comparacao_recorde_disponivel;
        public double recorde_anterior_segundos;
    }

    // ========================================================================
    // RANKING
    // ========================================================================

    /// <summary>
    /// Carrega o Top 10 de menores tempos de vitória (usado pelo menu de ranking).
    /// Precisa de jogador logado. O resultado chega no callback: (lista, erro).
    /// Se der certo, erro é null; se falhar, a lista é null e erro traz o motivo.
    /// </summary>
    public void LoadRanking(int playerId, Action<RankingEntry[], string> completed)
    {
        StartCoroutine(LoadRankingRoutine(playerId, null, (response, error) => completed(response?.ranking, error)));
    }

    /// <summary>
    /// Igual ao LoadRanking, mas para a tela de resultado: informando o id da partida que
    /// acabou de ser salva, também calcula o recorde ANTERIOR (sem contar essa partida),
    /// permitindo anunciar "NOVO RECORDE!" corretamente.
    /// </summary>
    public void LoadMatchRanking(int playerId, string matchId, Action<RankingResponse, string> completed)
    {
        StartCoroutine(LoadRankingRoutine(playerId, matchId, completed));
    }

    private IEnumerator LoadRankingRoutine(int playerId, string matchId, Action<RankingResponse, string> completed)
    {
        // Só jogador logado (e que seja o dono da sessão) pode consultar o ranking.
        if (playerId <= 0 || string.IsNullOrEmpty(Token) || playerId != _sessionPlayerId)
        {
            completed(null, "Entre na sua conta para consultar o ranking de vitórias.");
            yield break;
        }

        yield return null; // simula a "espera da resposta" (ver comentário no topo do arquivo)

        // A consulta propriamente dita (o antigo SELECT do MySQL) acontece aqui.
        RankingEntry[] entries = Store.BuildRanking(matchId, out bool comparison, out double previousRecord);

        completed(new RankingResponse
        {
            ranking = entries,
            error = "",
            comparacao_recorde_disponivel = comparison,
            recorde_anterior_segundos = previousRecord
        }, null);
    }

    // ========================================================================
    // SALVAR PARTIDA
    // ========================================================================

    /// <summary>
    /// Salva uma tentativa de partida (vitória, derrota ou empate).
    /// O BattleSessionController chama isto quando a rodada termina.
    ///
    /// Parâmetros:
    ///   playerId        -> id do jogador logado
    ///   matchId         -> UUID ÚNICO da tentativa (reenviar o mesmo UUID NÃO duplica a partida)
    ///   startedUtc/finishedUtc -> início e fim pelo relógio (UTC)
    ///   durationSeconds -> tempo ATIVO de jogo (sem pausas); é o tempo do ranking
    ///   outcome         -> "vitoria", "derrota" ou "empate"
    ///   score           -> campo legado (o jogo manda 0)
    ///   enemiesKilled   -> inimigos eliminados
    ///   completed       -> callback (deu certo?, mensagem de erro)
    /// </summary>
    public void SaveMatch(int playerId, string matchId, DateTime startedUtc, DateTime finishedUtc,
        double durationSeconds, string outcome, int score, int enemiesKilled, Action<bool, string> completed)
    {
        StartCoroutine(SaveMatchRoutine(playerId, matchId, startedUtc, finishedUtc,
            durationSeconds, outcome, score, enemiesKilled, completed));
    }

    private IEnumerator SaveMatchRoutine(int playerId, string matchId, DateTime startedUtc, DateTime finishedUtc,
        double durationSeconds, string outcome, int score, int enemiesKilled, Action<bool, string> completed)
    {
        // Sem login válido não salva (mesma regra do token no PHP).
        if (string.IsNullOrEmpty(Token) || playerId != _sessionPlayerId)
        {
            completed(false, "Sessão inválida. Entre novamente para salvar a partida.");
            yield break;
        }

        yield return null; // simula a espera da resposta

        // Grava no arquivo. "error" vem null quando tudo deu certo.
        string error = Store.SaveMatch(playerId, matchId, startedUtc, finishedUtc, durationSeconds,
            outcome, score, enemiesKilled, out int savedMatchId, out bool alreadySaved);

        if (error == null)
        {
            if (alreadySaved) Debug.Log("Partida " + matchId + " já estava salva; reenvio ignorado (sem duplicar).");
            completed(true, null);
        }
        else
        {
            Debug.LogError("Erro ao salvar partida: " + error);
            completed(false, error);
        }
    }

    // ========================================================================
    // LISTA LEGADA DE JOGADORES
    // ========================================================================

    public string jsonData; // JSON bruto da lista de jogadores (legado; não é ranking nem partida).

    public delegate void OnDataLoaded(string jsonData);
    public static event OnDataLoaded onDataLoaded; // quem quiser pode "assinar" para receber a lista

    private void Awake()
    {
        // Mantém este objeto vivo ao trocar de cena, para não perder a sessão (login).
        DontDestroyOnLoad(gameObject);
    }

    // ========================================================================
    // LOGIN / CADASTRO
    // ========================================================================

    /// <summary>
    /// Entra numa conta com e-mail e senha. No callback: (dados do jogador, erro).
    /// Login correto -> jogador preenchido e erro null (e a sessão/Token é criada).
    /// Login incorreto -> jogador null e erro com o motivo.
    /// </summary>
    public void Login(string email, string password, Action<PlayerData, string> completed)
    {
        _loginRequestId++; // invalida qualquer tentativa de login anterior ainda pendente
        StartCoroutine(LoginRoutine(email, password, completed, _loginRequestId));
    }

    /// <summary>Cancela login em andamento e "sai da conta" (apaga a sessão local).</summary>
    public void CancelLogin()
    {
        _loginRequestId++;
        Token = null;
        _sessionPlayerId = 0;
    }

    private IEnumerator LoginRoutine(string email, string password, Action<PlayerData, string> completed, int requestId)
    {
        // Começar um login novo derruba a sessão anterior.
        Token = null;
        _sessionPlayerId = 0;

        yield return null; // simula a espera da resposta

        // Se o jogador cancelou/voltou/iniciou outro login nesse meio tempo, descarta este.
        if (requestId != _loginRequestId) yield break;

        // Confere e-mail e senha (o hash é verificado dentro do LocalGameStore).
        LocalGameStore.PlayerRecord account = Store.Authenticate(email, password);
        if (account == null)
        {
            completed(null, "E-mail ou senha incorretos.");
            yield break;
        }

        // Login válido: cria a sessão. O Token é só um texto aleatório que prova "estou logado".
        Token = Guid.NewGuid().ToString("N");
        _sessionPlayerId = account.id;

        // Devolve os dados da conta SEM senha (igual ao comportamento antigo).
        completed(new PlayerData(account.id, account.name, account.email, null, 0, null, null), null);
    }

    /// <summary>
    /// Cadastra uma conta nova. A senha é guardada como hash, nunca em texto puro.
    /// O callback é opcional: (deu certo?, mensagem de erro).
    /// </summary>
    public void SaveData(string playerName, string email, string password, int score, Action<bool, string> completed = null)
    {
        StartCoroutine(RegisterRoutine(playerName, email, password, score, completed));
    }

    private IEnumerator RegisterRoutine(string playerName, string email, string password, int score, Action<bool, string> completed)
    {
        yield return null; // simula a espera da resposta

        string error = Store.Register(playerName, email, password, score); // null = sucesso
        bool success = error == null;

        if (!success) Debug.LogError(error);
        completed?.Invoke(success, error);
    }

    // ========================================================================
    // MÉTODOS LEGADOS (mantidos só para não quebrar quem ainda os chama)
    // ========================================================================

    /// <summary>
    /// Atualiza nome e pontos da conta (fluxo antigo de "pontos da conta").
    /// O GameManager.EndGame chama isto ao apertar Esc e logo em seguida faz Application.Quit().
    /// Por isso aqui a gravação é IMEDIATA (sem coroutine): uma coroutine poderia nem
    /// chegar a rodar antes do jogo fechar. Em disco local isso é instantâneo.
    /// Os parâmetros email e password existem só por compatibilidade e são ignorados.
    /// </summary>
    public void UpdatePlayerData(int id, string playerName, string email, string password, int score)
    {
        if (string.IsNullOrEmpty(Token) || id != _sessionPlayerId)
        {
            Debug.LogError("Erro ao atualizar dados: sessão inválida.");
            return;
        }

        string error = Store.UpdatePlayer(id, playerName, score);
        if (error != null) Debug.LogError("Erro ao atualizar dados: " + error);
        else Debug.Log("Dados atualizados com sucesso.");
    }

    /// <summary>
    /// Carrega a lista (legada) de jogadores e avisa quem assinou onDataLoaded.
    /// Não é o ranking. O MenuManager chama isto ao abrir o menu e o AuthMenu após cadastrar.
    /// </summary>
    public void LoadData()
    {
        StartCoroutine(nameof(LoadDataRoutine));
    }

    private IEnumerator LoadDataRoutine()
    {
        yield return null; // simula a espera da resposta

        jsonData = Store.ExportPlayersJson();
        onDataLoaded?.Invoke(jsonData);
        Debug.Log("Dados carregados com sucesso: " + jsonData);
    }

    // ========================================================================
    // FERRAMENTAS DE DESENVOLVIMENTO (menu de contexto do componente no Inspector)
    // ========================================================================

    [ContextMenu("Mostrar caminho do arquivo de dados")]
    private void LogDataPath()
    {
        Debug.Log("Arquivo de dados local: " + Store.FilePath);
    }

    [ContextMenu("APAGAR banco local (cuidado!)")]
    private void DeleteLocalDatabase()
    {
        Store.ResetAll();
        Token = null;
        _sessionPlayerId = 0;
        Debug.LogWarning("Banco local apagado: " + Store.FilePath);
    }
}
