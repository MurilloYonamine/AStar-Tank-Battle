<?php
// Cria um array com os dados
$idade = array("Iron Man"=>57, "Thor"=> 1000, "Spider Man"=> 16);
echo "<pre>"; // Tag html para exibir o array de forma organizada
var_dump($idade); // Exibe os dados do array
echo "</pre>"; // Fecha a tag html
echo "<br>"; // Quebra a linha
// Transforma o array em um objeto JSON
$json = json_encode($idade);
echo $json; // Exibe os dados no formato json
// Em php o fechamento da tag php não é obrigatório
// Apenas quando o arquivo é somente php
