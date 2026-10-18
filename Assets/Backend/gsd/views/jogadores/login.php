<h2>Faça o login para acessar o sistema</h2>
<?php if (isset($_SESSION['web_error'])): ?>
    <div class="alert alert-danger" role="alert">
        <?= htmlspecialchars($_SESSION['web_error'], ENT_QUOTES, 'UTF-8') ?>
    </div>
    <?php unset($_SESSION['web_error']); ?>
<?php endif; ?>
<hr>
<form action="?action=login_action" method="post">
    <label class="badge badge-secondary" for="email">E-mail</label>
    <input class="form-control" type="email" name="email" id="email" required><br>
    <label class="badge badge-secondary" for="password">Senha</label>
    <input class="form-control" type="password" name="password" id="password" required><br>
    <input type="submit" class="btn btn-success" value="Acessar">
</form>
