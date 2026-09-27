<?php

$json = '[
    {
        "nome": "Goku",
        "franquia": "Dragon Ball",
        "peso": 62
    },
    {
        "nome": "Mickey Mouse",
        "franquia": "Disney",
        "peso": 10
    },
    {
        "nome": "Ryu",
        "franquia": "Street Fighter",
        "peso": 85
    }
]';

// Transforma o json em um array associativo
$personagens = json_decode($json, true);
// Exibe o array associativo
echo "<pre>";
var_dump($personagens);
echo "</pre>";
echo "<br>";
// Exibe o nome do primeiro personagem do array associativo
echo "O nome do personagem é: ".$personagens[0]['nome'];

// Transforma o json em um objeto
$personagens2 = json_decode($json);
// Exibe o objeto
echo "<pre>";
var_dump($personagens2);
echo "</pre>";
echo "<br>";
// Exibe o nome do primeiro personagem do objeto
echo "O nome do personagem é: ".$personagens2[0]->nome;

