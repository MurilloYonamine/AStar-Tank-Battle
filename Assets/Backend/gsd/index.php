<?php
require('controller/JogadorController.php');
$action = isset($_GET['action']) ? $_GET['action'] : 'home';
$controller = new JogadorController();
switch ($action) {
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
        // converter o password para hash bcrypt
        $data['password'] = password_hash(filter_input(INPUT_POST, 'password'), PASSWORD_DEFAULT);
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
}
