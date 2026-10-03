<!-- Formulário para inserir jogador com nome, email, senha e confirmação de senha -->
<h2>Editar Jogador</h2>
<hr>
<form action="?action=update&id=<?= $jogador['id']; ?>" method="post">
    <label class="badge badge-secondary" for="name">Nome</label>
    <input class="form-control" type="text" name="name" id="name" value="<?= $jogador['name']; ?>"
        required><br>

    <label class="badge badge-secondary" for="email">E-mail</label>
    <input class="form-control" type="email" name="email" value="<?= $jogador['email']; ?>" id="email" required><br>

    <label class="badge badge-secondary" for="pontos">Pontos</label>
    <input class="form-control" type="number" name="pontos" value="<?= $jogador['pontos']; ?>" id="pontos" required><br>

    <label class="badge badge-secondary" for="password">Senha</label>
    <input class="form-control" type="password" name="password" id="password"><br>

    <label class="badge badge-secondary" for="password_confirmation">Confirmação de senha</label>
    <input class="form-control" type="password" name="password_confirmation"
        id="password_confirmation"><br>
        
    <input type="submit" class="btn btn-success" value="Salvar">
</form>