<?php
include "json_manager.php"; // inclui o arquivo json_manager para poder usar os métodos dele
// Configura o horário padrão para Ameroca/São Paulo
date_default_timezone_set("America/Sao_Paulo");
// Verifica se os dados foram enviados pelo programa
if(isset($_POST["apelido"], $_POST["pontos"]) && 
   !empty($_POST["apelido"]) && !empty($_POST["pontos"])) {
    // Pega os dados enviados por POST
    $apelido = $_POST["apelido"];        
    $pontos = $_POST["pontos"];
    // Cria um array com os dados
    $dados = array("apelido" => $apelido, "pontos" => $pontos, "data" => date("d/m/Y H:i:s"));
    
    $json = abrir_arquivo(); // aqui abre o arquivo
    
    if($json) {      
        fseek($json, 0, SEEK_END); // procura o final dos dados no arquivo    
        
        if(ftell($json) > 0) { // se o json não está vazio          
            fseek($json, -1, SEEK_END); // move o ponteiro até o final e move um byte para trás
            fwrite($json, ',', 1); // adiciona uma vírgula      
            fwrite($json, json_encode($dados).']'); // adiciona os novos dados
        } else { // se estiver vazio
            fwrite($json, json_encode(array($dados))); // escreve os dados de dados
        }
    }
    
    fechar_arquivo(); // fecha o arquivo para leitura e gravação, desbloqueando-o
    header("Location: index.php"); // redireciona para a página inicial
    exit(); // encerra o script
}
?>

