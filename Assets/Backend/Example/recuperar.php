<?php
include "visualizar.php"; 

// Chave secreta armazenada no servidor
$serverSecretKey = "123456";

// Chave recebida na requisição
$receivedKey = $_GET['chave_secreta'] ?? $_POST['chave_secreta'];

// Verifica se a chave é válida
if ($receivedKey === $serverSecretKey) {
    // Chama a função visualizar para obter os dados em formato JSON
    $json_data = visualizar();
    // Envia os dados JSON como resposta HTTP
    echo $json_data;
} else {
    // Resposta em caso de chave inválida
    echo json_encode(array("error" => "Chave secreta inválida."));
}
// Este exemplo com a chave secreta direto no código é apenas um exemplo simples
// Para uma aplicação real, a chave secreta deve ser armazenada em um arquivo
// separado, fora do diretório público do servidor web
?>
