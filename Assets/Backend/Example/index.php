<?php include "visualizar.php";?>
<!DOCTYPE html>
<html lang="pt-br">

<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Cadastramento de Pontos via WEB - Exemplo</title>
</head>

<body>
    <h1>Cadastramento de Pontos via WEB</h1>
    <hr><br>
    <!-- Formulário para adicionar os dados -->
    <form action="adicionar.php" method="POST">
        <label for="apelido">Apelido: </label>
        <input type="text" name="apelido" id="apelido" required>
        
        <label for="pontos">Pontos: </label>
        <input type="number" name="pontos" id="pontos" required>
        
        <input type="submit" value="Enviar">
    </form>
    <br>
    <hr>
    <br>
    <!-- Exibe os dados em uma tabela com o apelido, a pontuação e data de inserção no sistema -->
    <table border="1">
        <thead>
            <tr>
                <th>Apelido</th>
                <th>Pontos</th>
                <th>Data</th>
            </tr>
        </thead>
        <tbody>
            <?php $data = json_decode(visualizar());?>
            <!-- Ordena os dados pelo campo pontos de forma decrescente -->
            <?php usort($data, function($a, $b) { return $b->pontos - $a->pontos; }); ?>
            <?php foreach ($data as $key => $value): ?>
                <tr>
                    <td><?= $value->apelido ?></td>
                    <td><?= $value->pontos ?></td>
                    <td><?= $value->data ?></td>
                </tr>                
            <?php endforeach; ?>            
        </tbody>
        <!-- Exibe o total de jogadores cadastrados -->
        <tfoot>
            <tr>
                <td colspan="3">Total de jogadores: <?= count($data) ?></td>
            </tr>
        </tfoot>
    </table>
</body>

</html>