<?php
include_once 'DB.php';

class PartidaModel
{
    private $conn;
    private $table_name = 'partidas';

    public function __construct()
    {
        $database = new DB();
        $this->conn = $database->getConnection();
    }

    // Diferente de ScoreModel, cada partida nova e inserida; repetir seu UUID nao sobrescreve dados.
    public function salvar_partida($data)
    {
        $query = 'INSERT INTO ' . $this->table_name . ' '
            . '(jogador_id, partida_uuid, inicio_utc, fim_utc, duracao_segundos, '
            . 'resultado, pontuacao, inimigos_eliminados) VALUES '
            . '(:jogador_id, :partida_uuid, :inicio_utc, :fim_utc, :duracao_segundos, '
            . ':resultado, :pontuacao, :inimigos_eliminados) '
            . 'ON DUPLICATE KEY UPDATE id = LAST_INSERT_ID(id)';
        $stmt = $this->conn->prepare($query);
        $stmt->execute($data);
        return [
            'id' => (int)$this->conn->lastInsertId(),
            'repetida' => $stmt->rowCount() === 0
        ];
    }

    public function listar_historico($jogadorId)
    {
        $query = 'SELECT p.id, p.jogador_id, j.name AS jogador_nome, p.inicio_utc, '
            . 'p.fim_utc, p.duracao_segundos, p.resultado, p.pontuacao, p.inimigos_eliminados '
            . 'FROM partidas p JOIN jogadores j ON j.id = p.jogador_id '
            . 'WHERE p.jogador_id = :jogador_id ORDER BY p.inicio_utc DESC, p.id DESC';
        $stmt = $this->conn->prepare($query);
        $stmt->execute([':jogador_id' => $jogadorId]);
        return $stmt->fetchAll(PDO::FETCH_ASSOC);
    }

    // Melhor pontuacao de cada jogador apenas entre vitorias; desempate por ID do jogador.
    public function listar_ranking()
    {
        $query = "SELECT j.id AS jogador_id, j.name AS jogador_nome, "
            . "MAX(p.pontuacao) AS pontuacao FROM partidas p "
            . "JOIN jogadores j ON j.id = p.jogador_id "
            . "WHERE p.resultado = 'vitoria' "
            . "GROUP BY j.id, j.name ORDER BY pontuacao DESC, j.id ASC LIMIT 10";
        $stmt = $this->conn->prepare($query);
        $stmt->execute();
        return $stmt->fetchAll(PDO::FETCH_ASSOC);
    }

    // Consultas da página web: todas as partidas entram nos totais e na média de duração.
    public function listar_todas_partidas()
    {
        $query = 'SELECT p.id, j.name AS jogador_nome, p.inicio_utc, p.fim_utc, '
            . 'p.duracao_segundos, p.resultado, p.pontuacao, p.inimigos_eliminados '
            . 'FROM partidas p JOIN jogadores j ON j.id = p.jogador_id '
            . 'ORDER BY p.inicio_utc DESC, p.id DESC';
        $stmt = $this->conn->prepare($query);
        $stmt->execute();
        return $stmt->fetchAll(PDO::FETCH_ASSOC);
    }

    public function resumo_geral()
    {
        $query = "SELECT COUNT(*) AS total, "
            . "COALESCE(SUM(resultado = 'vitoria'), 0) AS vitorias, "
            . "COALESCE(SUM(resultado = 'derrota'), 0) AS derrotas, "
            . "COALESCE(SUM(resultado = 'empate'), 0) AS empates, "
            . "AVG(duracao_segundos) AS duracao_media FROM partidas";
        $stmt = $this->conn->prepare($query);
        $stmt->execute();
        return $stmt->fetch(PDO::FETCH_ASSOC);
    }

    // Desempenho por pontuação entre todas as partidas, inclusive derrotas e empates.
    public function partida_extrema($melhor)
    {
        if ($melhor) {
            $ordem = 'p.pontuacao DESC, p.duracao_segundos ASC, p.id ASC';
        } else {
            $ordem = 'p.pontuacao ASC, p.duracao_segundos DESC, p.id ASC';
        }

        $query = 'SELECT p.id, j.name AS jogador_nome, p.pontuacao, p.resultado, '
            . 'p.duracao_segundos FROM partidas p '
            . 'JOIN jogadores j ON j.id = p.jogador_id ORDER BY ' . $ordem . ' LIMIT 1';
        $stmt = $this->conn->prepare($query);
        $stmt->execute();
        return $stmt->fetch(PDO::FETCH_ASSOC);
    }

    public function resumo_por_jogador()
    {
        $query = "SELECT j.id, j.name AS jogador_nome, COUNT(p.id) AS total, "
            . "AVG(p.duracao_segundos) AS duracao_media, "
            . "MAX(CASE WHEN p.resultado = 'vitoria' THEN p.pontuacao END) AS melhor_vitoria "
            . "FROM jogadores j LEFT JOIN partidas p ON p.jogador_id = j.id "
            . "GROUP BY j.id, j.name ORDER BY total DESC, j.id ASC";
        $stmt = $this->conn->prepare($query);
        $stmt->execute();
        return $stmt->fetchAll(PDO::FETCH_ASSOC);
    }
}
