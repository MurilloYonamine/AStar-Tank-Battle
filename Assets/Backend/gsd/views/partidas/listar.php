<?php
function nome_jogador($valor)
{
    return htmlspecialchars($valor, ENT_QUOTES, 'UTF-8');
}

function segundos($valor)
{
    return number_format((float)$valor, 3, ',', '.') . ' s';
}

function horario_utc($valor)
{
    return nome_jogador(substr($valor, 0, 19)) . ' UTC';
}
?>
<h1>Histórico e análise das partidas</h1>

<?php if (isset($erro)): ?>
    <div class="alert alert-danger"><?= nome_jogador($erro) ?></div>
<?php elseif ((int)$resumo['total'] === 0): ?>
    <div class="alert alert-info">Ainda não há partidas registradas.</div>
<?php else: ?>
    <h2>Visão geral</h2>
    <div class="table-responsive mb-4">
        <table class="table table-striped table-hover">
            <thead><tr><th>Partidas</th><th>Vitórias</th><th>Derrotas</th><th>Empates</th><th>Duração média (sem pausas)</th></tr></thead>
            <tbody><tr>
                <td><?= (int)$resumo['total'] ?></td>
                <td><?= (int)$resumo['vitorias'] ?></td>
                <td><?= (int)$resumo['derrotas'] ?></td>
                <td><?= (int)$resumo['empates'] ?></td>
                <td><?= segundos($resumo['duracao_media']) ?></td>
            </tr></tbody>
        </table>
    </div>

    <h2>Melhor e pior tempo de conclusão (somente vitórias)</h2>
    <p>Menor tempo é melhor. Derrotas e empates aparecem no histórico e na média geral, mas não entram nesta comparação nem no ranking.</p>
    <?php if (!$melhor || !$pior): ?>
        <div class="alert alert-info">Ainda não há vitórias para comparar os tempos de conclusão.</div>
    <?php else: ?>
    <div class="table-responsive mb-4">
        <table class="table table-striped table-hover">
            <thead><tr><th>Desempenho</th><th>Jogador</th><th>Resultado</th><th>Tempo de conclusão (sem pausas)</th></tr></thead>
            <tbody>
                <tr><td>Melhor</td><td><?= nome_jogador($melhor['jogador_nome']) ?></td><td><?= nome_jogador($melhor['resultado']) ?></td><td><?= segundos($melhor['duracao_segundos']) ?></td></tr>
                <tr><td>Pior</td><td><?= nome_jogador($pior['jogador_nome']) ?></td><td><?= nome_jogador($pior['resultado']) ?></td><td><?= segundos($pior['duracao_segundos']) ?></td></tr>
            </tbody>
        </table>
    </div>
    <?php endif; ?>

    <h2>Resumo por jogador</h2>
    <div class="table-responsive mb-4">
        <table class="table table-striped table-hover">
            <thead><tr><th>Jogador</th><th>Partidas</th><th>Duração média (todas)</th><th>Menor tempo em vitória</th></tr></thead>
            <tbody>
                <?php foreach ($jogadores as $jogador): ?>
                    <tr>
                        <td><?= nome_jogador($jogador['jogador_nome']) ?></td>
                        <td><?= (int)$jogador['total'] ?></td>
                        <td><?= $jogador['duracao_media'] === null ? '—' : segundos($jogador['duracao_media']) ?></td>
                        <td><?= $jogador['melhor_vitoria'] === null ? 'Sem vitórias' : segundos($jogador['melhor_vitoria']) ?></td>
                    </tr>
                <?php endforeach; ?>
            </tbody>
        </table>
    </div>

    <h2>Todas as partidas</h2>
    <div class="table-responsive">
        <table class="table table-striped table-hover">
            <thead><tr><th>ID</th><th>Jogador</th><th>Início (UTC)</th><th>Término (UTC)</th><th>Duração (sem pausas)</th><th>Resultado</th><th>Eliminações</th></tr></thead>
            <tbody>
                <?php foreach ($partidas as $partida): ?>
                    <tr>
                        <td><?= (int)$partida['id'] ?></td>
                        <td><?= nome_jogador($partida['jogador_nome']) ?></td>
                        <td><?= horario_utc($partida['inicio_utc']) ?></td>
                        <td><?= horario_utc($partida['fim_utc']) ?></td>
                        <td><?= segundos($partida['duracao_segundos']) ?></td>
                        <td><?= nome_jogador($partida['resultado']) ?></td>
                        <td><?= (int)$partida['inimigos_eliminados'] ?></td>
                    </tr>
                <?php endforeach; ?>
            </tbody>
        </table>
    </div>
<?php endif; ?>
