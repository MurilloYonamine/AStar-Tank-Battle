<?php
require('model/Jogador.php');
require('model/Token.php');
class JogadorController
{
    private $model;
    private $tokens;
    public function __construct()
    {
        $this->model = new Jogador();
        $this->tokens = new Token();
    }
    public function home()
    {
        include 'views/partials/header.php';
        include 'views/home.php';
        include 'views/partials/footer.php';
    }
    public function listar()
    {
        $jogadores = $this->model->listar_jogadores(); // $jogadores é usado na view listar
        include 'views/partials/header.php'; // Inclui o cabeçalho da página
        include 'views/jogadores/listar.php';
        include 'views/partials/footer.php'; // Inclui o rodapé da página
    }
    public function inserir_form()
    {
        include 'views/partials/header.php'; // Inclui o cabeçalho da página
        include 'views/jogadores/inserir.php';
        include 'views/partials/footer.php'; // Inclui o rodapé da página
    }
    public function inserir($data)
    {
        $jogador = $this->model->inserir_jogador($data);
        // se inseriu o jogador com sucesso, retorna para a view listar, senão, retorna para a view inserir
        if ($jogador) {
            header('Location: ?action=listar');
        } else {
            header('Location: ?action=inserir');
        }
    }
    public function editar_form($id)
    {
        $jogador = $this->model->pegar_jogador($id); // $jogador é usado na view editar
        include 'views/partials/header.php'; // Inclui o cabeçalho da página
        include 'views/jogadores/editar.php';
        include 'views/partials/footer.php'; // Inclui o rodapé da página
    }
    public function atualizar($data)
    {
        $jogador = $this->model->atualizar_jogador($data);
        // se atualizou o jogador com sucesso, retorna para a view listar, senão, retorna para a view editar
        if ($jogador) {
            header('Location: ?action=listar');
        } else {
            header('Location: ?action=editar' . '&id=' . $data['id']);
        }
    }
    public function deletar($id)
    {
        $this->model->deletar_jogador($id);
        header('Location: ?action=listar');
    }

    // API
    public function api_listar()
    {
        $jogadores = $this->model->listar_placar();

        header('Content-Type: application/json');
        echo json_encode($jogadores);
    }

    public function api_inserir($data)
    {
        header('Content-Type: application/json; charset=utf-8');
        if (empty($data['name']) || !filter_var($data['email'] ?? '', FILTER_VALIDATE_EMAIL) ||
            empty($data['password']) || strlen($data['password']) < 3) {
            http_response_code(400);
            echo json_encode(['error' => 'Dados de cadastro inválidos.']);
            return;
        }
        if ($this->model->email_existe($data['email'])) {
            http_response_code(409);
            echo json_encode(['error' => 'Este e-mail já está cadastrado.']);
            return;
        }
        $data['pontos'] = 0;
        try {
            $jogador = $this->model->inserir_jogador($data);
        } catch (PDOException $exception) {
            http_response_code($exception->getCode() == '23000' ? 409 : 500);
            echo json_encode(['error' => 'Não foi possível concluir o cadastro.']);
            return;
        }
        echo json_encode([
            'success' => $jogador
        ]);
    }

    public function api_atualizar($data)
    {
        header('Content-Type: application/json; charset=utf-8');
        $_SESSION['jogador_id'] = $data['id'] ?? null;
        $_SESSION['token'] = $data['token'] ?? null;
        if (!$this->tokens->verificar_token()) {
            http_response_code(401);
            echo json_encode(['error' => 'Sessão inválida. Entre novamente.']);
            return;
        }
        // A atualização antiga de pontos não pode alterar credenciais pela API.
        $jogadorAtual = $this->model->pegar_jogador($data['id']);
        if (!$jogadorAtual || !isset($data['pontos']) ||
            filter_var($data['pontos'], FILTER_VALIDATE_INT) === false || (int)$data['pontos'] < 0) {
            http_response_code(400);
            echo json_encode(['error' => 'Pontuação inválida.']);
            return;
        }
        $data['name'] = $jogadorAtual['name'];
        $data['email'] = $jogadorAtual['email'];
        unset($data['password']);
        $jogador = $this->model->atualizar_jogador($data);

        echo json_encode([
            'success' => $jogador
        ]);
    }
    public function login_form()
    {
        if (isset($_SESSION['web_jogador_id'])) {
            header('Location: ?action=home');
            return;
        }
        include 'views/partials/header.php';
        include 'views/jogadores/login.php';
        include 'views/partials/footer.php';
    }

    public function login_web($data)
    {
        if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
            header('Location: ?action=login');
            return;
        }

        $jogador = false;
        if (filter_var($data['email'], FILTER_VALIDATE_EMAIL) && !empty($data['password'])) {
            $jogador = $this->model->autenticar($data['email'], $data['password']);
        }
        if (!$jogador) {
            $_SESSION['web_error'] = 'E-mail ou senha inválidos.';
            header('Location: ?action=login');
            return;
        }

        session_regenerate_id(true);
        $_SESSION['web_jogador_id'] = $jogador['id'];
        $_SESSION['web_jogador_nome'] = $jogador['name'];
        header('Location: ?action=home');
    }

    public function sair_web()
    {
        unset($_SESSION['web_jogador_id'], $_SESSION['web_jogador_nome']);
        session_regenerate_id(true);
        header('Location: ?action=login');
    }

    // POST email/password/token; responde com jogador sem senha após salvar o token recebido.
    public function api_login($data)
    {
        header('Content-Type: application/json; charset=utf-8');
        if (!filter_var($data['email'] ?? '', FILTER_VALIDATE_EMAIL) || empty($data['password']) ||
            !is_string($data['token'] ?? null) || !preg_match('/^[a-f0-9]{32}$/', $data['token'])) {
            http_response_code(400);
            echo json_encode(['error' => 'Informe e-mail, senha e token válido.']);
            return;
        }
        $jogador = $this->model->autenticar($data['email'], $data['password']);
        if (!$jogador) {
            http_response_code(401);
            echo json_encode(['error' => 'E-mail ou senha inválidos.']);
            return;
        }
        $dados = ['jogador_id' => $jogador['id'], 'token' => $data['token']];
        if (!$this->tokens->inserir_token($dados)) {
            http_response_code(500);
            echo json_encode(['error' => 'Erro ao salvar o token.']);
            return;
        }
        session_regenerate_id(true);
        echo json_encode($jogador);
    }

    public function api_check_token($data)
    {
        header('Content-Type: application/json; charset=utf-8');
        $_SESSION['jogador_id'] = $data['jogador_id'] ?? null;
        $_SESSION['token'] = $data['token'] ?? null;
        $valid = $this->tokens->verificar_token();
        if (!$valid) http_response_code(401);
        echo json_encode(['success' => $valid]);
    }
}
