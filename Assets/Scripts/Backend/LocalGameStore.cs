// LocalGameStore.cs  (ARQUIVO NOVO)
//
// ============================================================================
// O QUE ESTA CLASSE FAZ
// ============================================================================
// Antes, o fluxo era:
//
//   Unity (DataManager) --HTTP--> Apache/PHP (XAMPP) --PDO--> MySQL (tank_gamers)
//
// Agora o "servidor" inteiro vira esta classe, que roda DENTRO do próprio jogo:
//
//   Unity (DataManager) ---chamada de método---> LocalGameStore ---> arquivo .json
//
// Ela cumpre o papel das 3 camadas do PHP ao mesmo tempo:
//   - Controller (validação / autenticação)  -> métodos Register, Authenticate, SaveMatch...
//   - Model (consultas SQL)                  -> listas em memória + LINQ (BuildRanking)
//   - Banco MySQL (tabelas)                  -> arquivo JSON em disco
//
// Tabelas do MySQL que existiam e como ficaram aqui:
//   jogadores -> lista "jogadores" (PlayerRecord)
//   partidas  -> lista "partidas"  (MatchRecord)
//   tokens    -> não existe mais em disco: o token de sessão agora vive só na
//                memória do DataManager (o PHP já dizia que o cliente o guardava
//                só em memória; o "expira em 1 dia" deixa de fazer sentido
//                porque a sessão acaba quando o jogo fecha).
//
// Esta classe NÃO é um MonoBehaviour: é C# puro. Quem a usa é o DataManager.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;

public class LocalGameStore
{
    // ------------------------------------------------------------------------
    // REGISTROS (equivalentes às linhas das tabelas do MySQL)
    // ------------------------------------------------------------------------
    // [Serializable] é obrigatório: o JsonUtility da Unity só converte para JSON
    // classes marcadas com esse atributo (mesma regra explicada no PlayerData.cs).

    /// <summary>Uma conta de jogador (linha da tabela "jogadores").</summary>
    [Serializable]
    public class PlayerRecord
    {
        public int id;               // chave primária (auto incremento, como no MySQL)
        public string name;          // nome exibido no ranking
        public string email;         // identifica o jogador no login (único)
        public string passwordHash;  // NUNCA a senha em texto puro, só o hash (ver HashPassword)
        public int pontos;           // campo legado, mantido por compatibilidade
        public string created_at;    // data de criação (UTC)
        public string updated_at;    // data da última alteração (UTC)
    }

    /// <summary>Uma tentativa de partida (linha da tabela "partidas").</summary>
    [Serializable]
    public class MatchRecord
    {
        public int id;                   // chave primária
        public int jogador_id;           // de quem é a partida (chave estrangeira)
        public string partida_uuid;      // identificador único da tentativa (evita duplicar ao reenviar)
        public string inicio_utc;        // quando começou (relógio, pode incluir pausas)
        public string fim_utc;           // quando terminou (relógio)
        public double duracao_segundos;  // tempo ATIVO de jogo (sem pausas); é o que vale pro ranking
        public string resultado;         // "vitoria", "derrota" ou "empate"
        public int pontuacao;            // campo legado (fica 0)
        public int inimigos_eliminados;  // quantos inimigos o jogador derrubou
        public string created_at;        // quando o registro foi gravado
    }

    /// <summary>
    /// O "banco de dados inteiro". É isto que vira o arquivo JSON.
    /// Os contadores next* simulam o AUTO_INCREMENT do MySQL.
    /// </summary>
    [Serializable]
    private class Database
    {
        public int nextPlayerId = 1;
        public int nextMatchId = 1;
        public List<PlayerRecord> jogadores = new List<PlayerRecord>();
        public List<MatchRecord> partidas = new List<MatchRecord>();
    }

    // ------------------------------------------------------------------------
    // CONFIGURAÇÕES DO HASH DE SENHA
    // ------------------------------------------------------------------------
    private const int HashIterations = 100000; // quanto mais, mais difícil de quebrar por força bruta
    private const int SaltBytes = 16;           // "sal" aleatório: duas senhas iguais geram hashes diferentes
    private const int HashBytes = 32;           // tamanho do hash resultante

    private const int RankingSize = 10;         // o ranking mostra o Top 10

    private readonly string _filePath;          // onde o JSON fica no disco
    private Database _db;                       // cópia do banco mantida em memória
    private bool _loadFailed;                   // true se não deu pra LER o arquivo (protege contra sobrescrever)

    /// <summary>Caminho completo do arquivo de dados (útil para mostrar ao usuário/depurar).</summary>
    public string FilePath => _filePath;

    /// <summary>Abre (ou cria) o banco no caminho informado.</summary>
    public LocalGameStore(string filePath)
    {
        _filePath = filePath;
        _db = LoadFromDisk();
    }

    // ========================================================================
    // LEITURA / GRAVAÇÃO DO ARQUIVO
    // ========================================================================

    /// <summary>
    /// Lê o JSON do disco. Se o arquivo não existe (primeira execução), começa um banco vazio.
    /// Se existe mas está corrompido, guarda uma cópia ".corrompido-..." e começa vazio,
    /// assim nenhum dado é apagado em silêncio.
    /// </summary>
    private Database LoadFromDisk()
    {
        if (!File.Exists(_filePath)) return new Database();

        try
        {
            string json = File.ReadAllText(_filePath);
            Database db = JsonUtility.FromJson<Database>(json); // lança ArgumentException se o JSON for inválido
            if (db == null) throw new ArgumentException("Arquivo vazio.");

            // Garante que as listas existam mesmo se o JSON veio incompleto.
            if (db.jogadores == null) db.jogadores = new List<PlayerRecord>();
            if (db.partidas == null) db.partidas = new List<MatchRecord>();
            return db;
        }
        catch (ArgumentException)
        {
            // JSON quebrado: preserva o arquivo antigo com outro nome para recuperação manual.
            string backup = _filePath + ".corrompido-" +
                DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            try { File.Move(_filePath, backup); } catch (IOException) { }
            Debug.LogError("[LocalGameStore] Arquivo de dados inválido. Cópia salva em: " + backup);
            return new Database();
        }
        catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
        {
            // Não conseguimos ler (arquivo travado, sem permissão...). Bloqueia gravações
            // para não sobrescrever dados bons com um banco vazio.
            _loadFailed = true;
            Debug.LogError("[LocalGameStore] Não foi possível ler " + _filePath + ": " + e.Message);
            return new Database();
        }
    }

    /// <summary>
    /// Grava o banco inteiro no disco. Escreve primeiro num arquivo temporário e só depois
    /// copia por cima do definitivo; se algo falhar no meio da escrita, o arquivo bom continua intacto.
    /// Retorna false (e uma mensagem) se não conseguiu gravar.
    /// </summary>
    private bool TryPersist(out string error)
    {
        error = null;
        if (_loadFailed)
        {
            error = "Não foi possível acessar o arquivo de dados local.";
            return false;
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath));
            string tempPath = _filePath + ".tmp";
            File.WriteAllText(tempPath, JsonUtility.ToJson(_db, true)); // true = JSON indentado (legível)
            File.Copy(tempPath, _filePath, true);                       // true = sobrescreve
            File.Delete(tempPath);
            return true;
        }
        catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
        {
            Debug.LogError("[LocalGameStore] Falha ao gravar dados: " + e.Message);
            error = "Não foi possível gravar os dados locais.";
            return false;
        }
    }

    /// <summary>Apaga TODO o banco (arquivo e memória). Só para testes.</summary>
    public void ResetAll()
    {
        _db = new Database();
        _loadFailed = false;
        if (File.Exists(_filePath)) File.Delete(_filePath);
    }

    // ========================================================================
    // CONTAS (substitui api_inserir, api_login e api_atualizar do PHP)
    // ========================================================================

    /// <summary>
    /// Cadastra um jogador. Retorna null se deu certo, ou o texto do erro.
    /// (No PHP: api_inserir. A senha é guardada como hash, igual ao PHP fazia.)
    /// </summary>
    public string Register(string name, string email, string password, int pontos)
    {
        name = (name ?? "").Trim();
        email = NormalizeEmail(email);

        // Mesmas regras que o AuthMenu já valida na tela; repetimos aqui porque
        // a "camada servidor" nunca deve confiar apenas na validação do cliente.
        if (name.Length == 0) return "O nome não pode estar vazio.";
        if (email.Length == 0 || !email.Contains("@")) return "Informe um e-mail válido.";
        if (string.IsNullOrEmpty(password) || password.Length < 3) return "A senha deve ter pelo menos 3 caracteres.";

        // Equivalente à restrição UNIQUE do e-mail no MySQL.
        if (_db.jogadores.Any(p => p.email == email)) return "Este e-mail já está cadastrado.";

        string now = NowUtcString();
        PlayerRecord player = new PlayerRecord
        {
            id = _db.nextPlayerId,
            name = name,
            email = email,
            passwordHash = HashPassword(password),
            pontos = pontos,
            created_at = now,
            updated_at = now
        };

        _db.nextPlayerId++;
        _db.jogadores.Add(player);

        if (!TryPersist(out string error))
        {
            // Desfaz a alteração em memória para não ficar diferente do disco.
            _db.jogadores.Remove(player);
            _db.nextPlayerId--;
            return error;
        }
        return null;
    }

    /// <summary>
    /// Confere e-mail + senha. Retorna a conta se estiver correta, ou null se não.
    /// (No PHP: api_login.) Não diferenciamos "e-mail não existe" de "senha errada"
    /// de propósito, para não revelar quais e-mails estão cadastrados.
    /// </summary>
    public PlayerRecord Authenticate(string email, string password)
    {
        email = NormalizeEmail(email);
        PlayerRecord player = _db.jogadores.FirstOrDefault(p => p.email == email);
        if (player == null || string.IsNullOrEmpty(password)) return null;
        return VerifyPassword(password, player.passwordHash) ? player : null;
    }

    /// <summary>
    /// Atualiza nome e pontos de uma conta (legado, no PHP: api_atualizar).
    /// Retorna null se deu certo, ou o texto do erro.
    /// </summary>
    public string UpdatePlayer(int id, string name, int pontos)
    {
        PlayerRecord player = _db.jogadores.FirstOrDefault(p => p.id == id);
        if (player == null) return "Jogador não encontrado.";

        // Guardamos os valores antigos para desfazer se a gravação falhar.
        string oldName = player.name;
        int oldPontos = player.pontos;
        string oldUpdated = player.updated_at;

        if (!string.IsNullOrWhiteSpace(name)) player.name = name.Trim();
        player.pontos = pontos;
        player.updated_at = NowUtcString();

        if (!TryPersist(out string error))
        {
            player.name = oldName;
            player.pontos = oldPontos;
            player.updated_at = oldUpdated;
            return error;
        }
        return null;
    }

    /// <summary>
    /// Lista de contas SEM senha, em JSON no formato {"players":[...]}.
    /// (No PHP: api_listar. Mantido só por compatibilidade com DataManager.LoadData.)
    /// </summary>
    public string ExportPlayersJson()
    {
        PlayerRootObject root = new PlayerRootObject
        {
            players = _db.jogadores
                .Select(p => new PlayerData(p.id, p.name, p.email, null, p.pontos, p.created_at, p.updated_at))
                .ToArray()
        };
        return JsonUtility.ToJson(root);
    }

    // ========================================================================
    // PARTIDAS (substitui api_salvar_partida do PHP)
    // ========================================================================

    /// <summary>
    /// Grava uma tentativa de partida. Retorna null se deu certo, ou o texto do erro.
    ///
    /// IDEMPOTENTE: se o mesmo jogador reenviar a mesma partida_uuid (ex.: o jogador
    /// clicou em "tentar novamente"), NÃO cria outra linha; só confirma de novo.
    /// No MySQL isso era uma chave única (jogador_id + partida_uuid).
    /// </summary>
    public string SaveMatch(int playerId, string matchUuid, DateTime startedUtc, DateTime finishedUtc,
        double durationSeconds, string outcome, int score, int enemiesKilled,
        out int matchId, out bool alreadySaved)
    {
        matchId = 0;
        alreadySaved = false;

        // --- validações (o "Controller" do PHP) ---
        if (!_db.jogadores.Any(p => p.id == playerId)) return "Jogador não encontrado.";
        if (string.IsNullOrWhiteSpace(matchUuid)) return "Identificador da partida inválido.";
        if (outcome != "vitoria" && outcome != "derrota" && outcome != "empate") return "Resultado da partida inválido.";
        if (double.IsNaN(durationSeconds) || double.IsInfinity(durationSeconds) || durationSeconds <= 0)
            return "Duração da partida inválida.";
        if (enemiesKilled < 0) return "Quantidade de eliminações inválida.";

        // --- reenvio da mesma tentativa? ---
        MatchRecord existing = _db.partidas.FirstOrDefault(m => m.jogador_id == playerId && m.partida_uuid == matchUuid);
        if (existing != null)
        {
            matchId = existing.id;
            alreadySaved = true;
            return null;
        }

        // --- cria a linha nova ---
        MatchRecord match = new MatchRecord
        {
            id = _db.nextMatchId,
            jogador_id = playerId,
            partida_uuid = matchUuid,
            // CultureInfo.InvariantCulture evita que o formato mude conforme o idioma do Windows.
            inicio_utc = startedUtc.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            fim_utc = finishedUtc.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            duracao_segundos = Math.Round(durationSeconds, 3, MidpointRounding.AwayFromZero), // 3 casas, como no banco
            resultado = outcome,
            pontuacao = score,
            inimigos_eliminados = enemiesKilled,
            created_at = NowUtcString()
        };

        _db.nextMatchId++;
        _db.partidas.Add(match);

        if (!TryPersist(out string error))
        {
            _db.partidas.Remove(match);
            _db.nextMatchId--;
            return error;
        }

        matchId = match.id;
        return null;
    }

    // ========================================================================
    // RANKING (substitui api_ranking_partidas do PHP; era uma consulta SQL)
    // ========================================================================

    /// <summary>
    /// Monta o Top 10 de MENOR tempo de vitória, contando só a melhor vitória de cada jogador.
    /// Empates dividem a posição (1, 1, 3...).
    ///
    /// Se currentMatchUuid for informado, também calcula o recorde ANTERIOR ignorando essa
    /// partida. É isso que permite ao MatchRankingFeedback decidir entre "NOVO RECORDE!" e
    /// "MELHOR TEMPO!" (ele só anuncia recorde quando comparacao_recorde_disponivel = true).
    /// </summary>
    public DataManager.RankingEntry[] BuildRanking(string currentMatchUuid,
        out bool comparisonAvailable, out double previousRecordSeconds)
    {
        // 1) Melhor (menor) tempo de vitória de cada jogador.
        //    É o "GROUP BY jogador_id ... MIN(duracao_segundos)" do SQL.
        Dictionary<int, double> bestByPlayer = new Dictionary<int, double>();
        foreach (MatchRecord match in _db.partidas)
        {
            if (match.resultado != "vitoria") continue; // derrotas e empates não entram no ranking
            if (!bestByPlayer.TryGetValue(match.jogador_id, out double current) ||
                Milliseconds(match.duracao_segundos) < Milliseconds(current))
            {
                bestByPlayer[match.jogador_id] = match.duracao_segundos;
            }
        }

        // 2) Junta com o nome do jogador (o "JOIN" com a tabela jogadores) e ordena:
        //    menor tempo primeiro; em empate de tempo, ordem alfabética só para ficar estável.
        var ordered = bestByPlayer
            .Select(pair => new
            {
                Player = _db.jogadores.FirstOrDefault(p => p.id == pair.Key),
                Seconds = pair.Value
            })
            .Where(x => x.Player != null)
            .OrderBy(x => Milliseconds(x.Seconds))
            .ThenBy(x => x.Player.name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // 3) Atribui as posições (empate = mesma posição) e corta no Top 10.
        List<DataManager.RankingEntry> result = new List<DataManager.RankingEntry>();
        for (int i = 0; i < ordered.Count && result.Count < RankingSize; i++)
        {
            // Aqui result.Count == i, então result[i - 1] é o item anterior da lista.
            bool tiedWithPrevious = i > 0 &&
                Milliseconds(ordered[i].Seconds) == Milliseconds(ordered[i - 1].Seconds);

            result.Add(new DataManager.RankingEntry
            {
                jogador_id = ordered[i].Player.id,
                jogador_nome = ordered[i].Player.name,
                duracao_segundos = ordered[i].Seconds,
                posicao = tiedWithPrevious ? result[i - 1].posicao : i + 1 // 1, 1, 3...
            });
        }

        // 4) Recorde anterior (melhor vitória de qualquer jogador, EXCLUINDO a partida atual).
        comparisonAvailable = !string.IsNullOrEmpty(currentMatchUuid);
        previousRecordSeconds = 0; // 0 significa "ainda não havia nenhuma vitória" (=> PRIMEIRO RECORDE)
        if (comparisonAvailable)
        {
            foreach (MatchRecord match in _db.partidas)
            {
                if (match.resultado != "vitoria" || match.partida_uuid == currentMatchUuid) continue;
                if (previousRecordSeconds == 0 || match.duracao_segundos < previousRecordSeconds)
                    previousRecordSeconds = match.duracao_segundos;
            }
        }

        return result.ToArray();
    }

    // ========================================================================
    // FUNÇÕES AUXILIARES
    // ========================================================================

    /// <summary>Tempo em milissegundos inteiros. Comparar assim evita erros de ponto flutuante.</summary>
    private static long Milliseconds(double seconds) =>
        (long)Math.Round(seconds * 1000d, MidpointRounding.AwayFromZero);

    /// <summary>Data/hora atual em UTC no formato "2026-11-04 15:30:00".</summary>
    private static string NowUtcString() =>
        DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

    /// <summary>Tira espaços e deixa minúsculo, assim "Ana@x.com " e "ana@x.com" são o mesmo e-mail.</summary>
    private static string NormalizeEmail(string email) => (email ?? "").Trim().ToLowerInvariant();

    // ------------------------------------------------------------------------
    // SENHAS: nunca guardamos a senha, só um "hash" (impressão digital) dela.
    // Formato salvo:  pbkdf2$<iterações>$<sal em base64>$<hash em base64>
    // ------------------------------------------------------------------------

    /// <summary>Gera o texto de hash para guardar no lugar da senha.</summary>
    private static string HashPassword(string password)
    {
        byte[] salt = new byte[SaltBytes];
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt); // sal aleatório novo para cada conta
        }

        byte[] hash = Pbkdf2(password, salt, HashIterations);
        return "pbkdf2$" + HashIterations + "$" + Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(hash);
    }

    /// <summary>Confere se a senha digitada gera o mesmo hash que foi guardado.</summary>
    private static bool VerifyPassword(string password, string stored)
    {
        if (string.IsNullOrEmpty(stored)) return false;

        string[] parts = stored.Split('$');
        if (parts.Length != 4 || parts[0] != "pbkdf2") return false;
        if (!int.TryParse(parts[1], out int iterations) || iterations <= 0) return false;

        byte[] salt;
        byte[] expected;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expected = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return false; // registro adulterado/corrompido
        }

        byte[] actual = Pbkdf2(password, salt, iterations);

        // Comparação em "tempo constante": não para no primeiro byte diferente,
        // o que dificultaria descobrir o hash medindo o tempo de resposta.
        int diff = actual.Length ^ expected.Length;
        for (int i = 0; i < actual.Length && i < expected.Length; i++)
            diff |= actual[i] ^ expected[i];
        return diff == 0;
    }

    /// <summary>PBKDF2 (padrão para derivar chaves a partir de senhas) com SHA-256.</summary>
    private static byte[] Pbkdf2(string password, byte[] salt, int iterations)
    {
        using (Rfc2898DeriveBytes kdf = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
        {
            return kdf.GetBytes(HashBytes);
        }
    }
}
