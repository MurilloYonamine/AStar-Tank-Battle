<!DOCTYPE html>
<html lang="pt-BR">

<head>
    <title>Gerenciamento de Usuários</title>
    <meta charset="UTF-8">
    <link rel="stylesheet" href="css/bootstrap.min.css">
    <link rel="stylesheet" href="//cdn.datatables.net/1.10.19/css/jquery.dataTables.min.css">
    <link rel="stylesheet" href="css/estilo.css">
</head>

<body>
    <header>
        <div class="container">
            <a href="?action=home"><img src="img/logo.png" title="Logo" alt="Logo"></a>
            <div id="menu">
                <?php if (isset($_SESSION['web_jogador_id'])): ?>
                    <a href="?action=listar">Jogadores</a>
                    <a href="?action=partidas">Histórico de partidas</a>
                    <a href="?action=sair">Sair</a>
                <?php endif; ?>
            </div>
        </div>
    </header>
    <div id="conteudo" class="container">
