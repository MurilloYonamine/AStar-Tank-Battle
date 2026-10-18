<?php
session_start();

require('controller/JogadorController.php');
require('controller/PartidaController.php');

$action = isset($_GET['action']) ? $_GET['action'] : 'home';
$controller = new JogadorController();
$partida_controller = new PartidaController();

// Apenas páginas e ações web exigem sessão; as rotas da Unity validam seus próprios tokens.
$web_actions = ['home', 'listar', 'inserir', 'insert', 'editar', 'update', 'deletar', 'partidas'];
if (in_array($action, $web_actions, true) && !isset($_SESSION['web_jogador_id'])) {
    header('Location: ?action=login');
    exit;
}

switch ($action) {
    case 'login':
        $controller->login_form();
        break;
    case 'login_action':
        $controller->login_web([
            'email' => filter_input(INPUT_POST, 'email'),
            'password' => filter_input(INPUT_POST, 'password')
        ]);
        break;
    case 'sair':
        $controller->sair_web();
        break;
    case 'home':
        $controller->home();
        break;
    case 'listar':
        $controller->listar();
        break;
    case 'inserir':
        $controller->inserir_form();
        break;
    case 'insert':
        // recebe o post com os dados do jogador e envia para o controller
        $data['name'] = filter_input(INPUT_POST, 'name');
        $data['email'] = filter_input(INPUT_POST, 'email');
        $data['pontos'] = filter_input(INPUT_POST, 'pontos');
        $data['password'] = filter_input(INPUT_POST, 'password');
        $jogador = $controller->inserir($data);
        break;
    case 'editar':
        $id = $_GET['id'];
        if ($id != '') {
            $controller->editar_form($id);
        }
        break;
    case 'update':
        // recebe o post com os dados do jogador e envia para o controller
        $data['id'] = filter_input(INPUT_GET, 'id');
        $data['name'] = filter_input(INPUT_POST, 'name');
        $data['email'] = filter_input(INPUT_POST, 'email');
        $data['pontos'] = filter_input(INPUT_POST, 'pontos');
        // converter o password para hash bcrypt
        if (!empty($_POST['password'])) {
            $data['password'] = password_hash(filter_input(INPUT_POST, 'password'), PASSWORD_DEFAULT);
        }
        $jogador = $controller->atualizar($data);
        break;
    case 'deletar':
        $id = isset($_GET['id']) ? $_GET['id'] : null;
        if ($id != null) {
            $controller->deletar($id);
        }
        break;
    case 'api_listar':
        $controller->api_listar();
        break;

    case 'api_inserir':
        $controller->api_inserir($_POST);
        break;

    case 'api_atualizar':
        $controller->api_atualizar($_POST);
        break;
    case 'api_login':
        $controller->api_login($_POST);
        break;
    case 'api_check_token':
        $controller->api_check_token($_POST);
        break;
    case 'api_salvar_partida':
        $partida_controller->salvar_partida($_POST);
        break;
    case 'api_historico_partidas':
        $partida_controller->listar_historico($_POST);
        break;
    case 'api_ranking_partidas':
        $partida_controller->listar_ranking($_POST);
        break;
    case 'partidas':
        $partida_controller->pagina_partidas();
        break;
}
