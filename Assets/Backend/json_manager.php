<?php
$file = null; // variável que recebeo json
$path = "data.json"; // caminho do arquivo json

function abrir_arquivo(){    
    global $file, $path; // palavra-chave global para acessar variáveis globais
    // Verifica se o arquivo existe
    if(file_exists($path)){
        // Abre o arquivo para leitura e escrita
        $file = fopen($path, "r+");
    }    
    else{
        // Cria o arquivo se ele não existir
        $file = fopen($path, "w+");
    }
    // Retorna o arquivo
    return $file;
}

function fechar_arquivo(){
    global $file;
    // Fecha o arquivo
    fclose($file);
}
