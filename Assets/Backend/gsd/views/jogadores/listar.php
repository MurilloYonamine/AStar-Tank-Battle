<a class="btn btn-success" href="?action=inserir">Inserir novo jogador</a><br><br>
<table class="table table-hover table-striped" id="jogador">
    <thead>
        <tr>
            <th>ID</th>
            <th>Nome</th>
            <th>Email</th>
            <th>Pontos</th>
            <th>Criado em</th>
            <th>Atualizado em</th>
            <th>Editar</th>
            <th>Deletar</th>
        </tr>
    </thead>
    <tbody>
        <?php foreach ($jogadores as $jogador): ?>
            <tr>
                <td><?php echo $jogador['id']; ?></td>
                <td><?php echo $jogador['name']; ?></td>
                <td><?php echo $jogador['email']; ?></td>
                <td><?php echo $jogador['pontos']; ?></td>
                <td><?php echo date('d/m/Y H:i:s', strtotime($jogador['created_at'])); ?></td>
                <td><?php echo date('d/m/Y H:i:s', strtotime($jogador['updated_at'])); ?></td>
                <td><a href="?action=editar&id=<?php echo $jogador['id']; ?>">[Editar]</a></td>
                <td><a href="?action=deletar&id=<?php echo $jogador['id']; ?>">[Deletar]</a></td>
            </tr>
        <?php endforeach; ?>
    </tbody>
</table>