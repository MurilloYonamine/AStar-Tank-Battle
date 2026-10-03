<!-- Formulário para inserir jogador com nome, email, senha e confirmação de senha -->
<h2>Inserir Novo Jogador</h2>
<hr>
<form action="?action=insert" method="post">
    <label class="badge badge-secondary" for="name">Nome</label>
    <input class="form-control" type="text" name="name" id="name" required><br>
    <label class="badge badge-secondary" for="email">E-mail</label>
    <input class="form-control" type="email" name="email" id="email" required><br>
        <label class="badge badge-secondary" for="pontos">Pontos</label>
    <input class="form-control" type="number" name="pontos" id="pontos" required><br>
    <label class="badge badge-secondary" for="password">Senha</label>
    <input class="form-control" type="password" name="password" id="password" required><br>
    <label class="badge badge-secondary" for="password_confirmation">Confirmação de senha</label>
    <input class="form-control" type="password" name="password_confirmation"
        id="password_confirmation" required><br>
    <input type="submit" class="btn btn-success" value="Inserir">
</form>