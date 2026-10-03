<?php
require('model/Jogador.php');
class JogadorController
{
    private $model;
    public function __construct()
    {
        $this->model = new Jogador();
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
        $jogadores = $this->model->listar_jogadores();

        header('Content-Type: application/json');
        echo json_encode($jogadores);
    }

    public function api_inserir($data)
    {
        $jogador = $this->model->inserir_jogador($data);

        header('Content-Type: application/json');
        echo json_encode([
            'success' => $jogador
        ]);
    }

    public function api_atualizar($data)
    {
        $jogador = $this->model->atualizar_jogador($data);

        header('Content-Type: application/json');
        echo json_encode([
            'success' => $jogador
        ]);
    }
}
