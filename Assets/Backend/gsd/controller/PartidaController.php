<?php
require('model/PartidaModel.php');

class PartidaController
{
    private $model;
    private $token;

    public function __construct()
    {
        $this->model = new PartidaModel();
        $this->token = new Token();
    }

    // Segue o exemplo de ScoreController: valida o par token/jogador antes de consultar a Model.
    private function autenticar($data)
    {
        header('Content-Type: application/json; charset=utf-8');
        if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
            http_response_code(405);
            echo json_encode(['error' => 'Use POST.']);
            return false;
        }

        $jogadorId = filter_var($data['jogador_id'] ?? null, FILTER_VALIDATE_INT);
        if ($jogadorId === false || $jogadorId <= 0 ||
            !is_string($data['token'] ?? null) || $data['token'] === '') {
            http_response_code(401);
            echo json_encode(['error' => 'Token ou jogador inválido.']);
            return false;
        }

        $_SESSION['jogador_id'] = $jogadorId;
        $_SESSION['token'] = $data['token'];
        if (!$this->token->verificar_token()) {
            http_response_code(401);
            echo json_encode(['error' => 'Token inválido ou expirado.']);
            return false;
        }
        return $jogadorId;
    }

    private function validar_data($value)
    {
        if (!is_string($value)) return false;
        $date = DateTimeImmutable::createFromFormat('!Y-m-d H:i:s', $value, new DateTimeZone('UTC'));
        if (!$date || $date->format('Y-m-d H:i:s') !== $value) return false;
        return $date;
    }

    public function salvar_partida($data)
    {
        $jogadorId = $this->autenticar($data);
        if ($jogadorId === false) return;

        $inicio = $this->validar_data($data['inicio_utc'] ?? null);
        $fim = $this->validar_data($data['fim_utc'] ?? null);
        $duracao = filter_var($data['duracao_segundos'] ?? null, FILTER_VALIDATE_FLOAT);
        $pontuacao = filter_var($data['pontuacao'] ?? null, FILTER_VALIDATE_INT);
        $eliminados = filter_var($data['inimigos_eliminados'] ?? null, FILTER_VALIDATE_INT);
        $uuid = $data['partida_uuid'] ?? null;
        $resultado = $data['resultado'] ?? null;

        if (!is_string($uuid) || strlen($uuid) !== 36 || !$inicio || !$fim ||
            $fim < $inicio || $duracao === false || $duracao === null || $duracao <= 0 ||
            $duracao > 9999999 || $duracao > $fim->getTimestamp() - $inicio->getTimestamp() + 1 ||
            !in_array($resultado, ['vitoria', 'derrota', 'empate'], true) ||
            $pontuacao === false || $pontuacao === null || $pontuacao < 0 ||
            $eliminados === false || $eliminados === null || $eliminados < 0) {
            http_response_code(400);
            echo json_encode(['error' => 'Dados da partida inválidos.']);
            return;
        }

        $record = [
            ':jogador_id' => $jogadorId,
            ':partida_uuid' => $uuid,
            ':inicio_utc' => $inicio->format('Y-m-d H:i:s'),
            ':fim_utc' => $fim->format('Y-m-d H:i:s'),
            ':duracao_segundos' => number_format($duracao, 3, '.', ''),
            ':resultado' => $resultado,
            ':pontuacao' => $pontuacao,
            ':inimigos_eliminados' => $eliminados
        ];

        try {
            $partida = $this->model->salvar_partida($record);
            http_response_code($partida['repetida'] ? 200 : 201);
            echo json_encode(['success' => true, 'partida_id' => $partida['id'],
                'repetida' => $partida['repetida']]);
        } catch (PDOException $exception) {
            http_response_code(500);
            echo json_encode(['error' => 'Não foi possível salvar a partida.']);
        }
    }

    public function listar_historico($data)
    {
        $jogadorId = $this->autenticar($data);
        if ($jogadorId === false) return;
        try {
            $partidas = $this->model->listar_historico($jogadorId);
            foreach ($partidas as &$partida) {
                $partida['id'] = (int)$partida['id'];
                $partida['jogador_id'] = (int)$partida['jogador_id'];
                $partida['duracao_segundos'] = (float)$partida['duracao_segundos'];
                $partida['pontuacao'] = (int)$partida['pontuacao'];
                $partida['inimigos_eliminados'] = (int)$partida['inimigos_eliminados'];
            }
            unset($partida);
            echo json_encode(['partidas' => $partidas], JSON_INVALID_UTF8_SUBSTITUTE);
        } catch (PDOException $exception) {
            http_response_code(500);
            echo json_encode(['error' => 'Não foi possível carregar o histórico.']);
        }
    }

    public function listar_ranking($data)
    {
        if ($this->autenticar($data) === false) return;
        try {
            $ranking = $this->model->listar_ranking();
            foreach ($ranking as &$jogador) {
                $jogador['jogador_id'] = (int)$jogador['jogador_id'];
                $jogador['pontuacao'] = (int)$jogador['pontuacao'];
            }
            unset($jogador);
            echo json_encode(['ranking' => $ranking], JSON_INVALID_UTF8_SUBSTITUTE);
        } catch (PDOException $exception) {
            http_response_code(500);
            echo json_encode(['error' => 'Não foi possível carregar o ranking.']);
        }
    }

    public function pagina_partidas()
    {
        try {
            $partidas = $this->model->listar_todas_partidas();
            $resumo = $this->model->resumo_geral();
            $melhor = $this->model->partida_extrema(true);
            $pior = $this->model->partida_extrema(false);
            $jogadores = $this->model->resumo_por_jogador();
        } catch (PDOException $exception) {
            http_response_code(500);
            $erro = 'Não foi possível carregar os dados das partidas.';
        }

        include 'views/partials/header.php';
        include 'views/partidas/listar.php';
        include 'views/partials/footer.php';
    }
}
