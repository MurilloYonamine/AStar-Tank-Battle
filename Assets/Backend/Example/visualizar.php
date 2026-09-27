<?php
include "json_manager.php"; // inclui o arquivo json_manager para poder usar os métodos dele

// Abre o arquivo somente no modo leitura, faz a leitura dos dados, fecha o arquivo e dá um echo nos dados
function visualizar()
{
    $json = abrir_arquivo(); // abre o arquivo
    if ($json) { // se o arquivo foi aberto
        // verifica se possui dados no arquivo
        if (filesize($GLOBALS['path']) > 0) {
            $data = fread($json, filesize($GLOBALS['path'])); // lê o arquivo            
            // percorre os dados e exibe na tabela            
            return $data; // retorna o JSON diretamente
        }
    }
    fechar_arquivo(); // fecha o arquivo
    exit(); // encerra o script
}
