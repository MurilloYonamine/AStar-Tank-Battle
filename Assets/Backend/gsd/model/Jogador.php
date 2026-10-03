<?php
include_once 'DB.php';
class Jogador
{
    private $conn;
    private $table_name = "jogadores";
    public $id;
    public $nome;
    public $email;
    private $password;
    private $pontos;
    public $created_at;
    public $updated_at;
    public function __construct()
    {
        $database = new DB();
        $this->conn = $database->getConnection();
    }
    function listar_jogadores()
    {
        $query = "SELECT * FROM " . $this->table_name;
        $stmt = $this->conn->prepare($query);
        $stmt->execute();
        return $stmt->fetchAll(PDO::FETCH_ASSOC);
    }
    function inserir_jogador($data)
    {
        // recebe os dados do jogador via post e trata-os
        $this->nome = $data['name'];
        $this->email = $data['email'];
        $this->password = $data['password'];
        $this->pontos = $data['pontos'];
        $this->created_at = date('Y-m-d H:i:s');
        $this->updated_at = date('Y-m-d H:i:s');
        $query = "INSERT INTO " . $this->table_name . " SET name=:name, 
        email=:email, 
        password=:password, 
        pontos=:pontos,
        created_at=:created_at, 
        updated_at=:updated_at";

        $stmt = $this->conn->prepare($query);

        // trata os dados para evitar sql injection
        $this->nome = htmlspecialchars(strip_tags($this->nome));
        $this->email = htmlspecialchars(strip_tags($this->email));
        $this->password = htmlspecialchars(strip_tags($this->password));
        $this->pontos = htmlspecialchars(strip_tags($this->pontos));
        $this->created_at = htmlspecialchars(strip_tags($this->created_at));
        $this->updated_at = htmlspecialchars(strip_tags($this->updated_at));
        $stmt->bindParam(":name", $this->nome);
        $stmt->bindParam(":email", $this->email);
        $stmt->bindParam(":password", $this->password);
        $stmt->bindParam(":pontos", $this->pontos);
        $stmt->bindParam(":created_at", $this->created_at);
        $stmt->bindParam(":updated_at", $this->updated_at);
        if ($stmt->execute()) {
            return true;
        }
        return false;
    }
    function pegar_jogador($id)
    {
        // retorna o id, name e email do jogador
        $query = "SELECT id, name, email, pontos FROM " . $this->table_name . " WHERE id=:id";
        //$query = "SELECT * FROM " . $this->table_name . " WHERE id=:id";
        $stmt = $this->conn->prepare($query);
        $stmt->bindParam(":id", $id);
        $stmt->execute();
        return $stmt->fetch(PDO::FETCH_ASSOC);
    }
    // metodo para atualizar um jogador contendo (name, email, password, updated_at)
    function atualizar_jogador($data)
    {
        $this->id = $data['id'];
        $this->nome = $data['name'];
        $this->email = $data['email'];
        $this->pontos = $data['pontos'];
        $this->updated_at = date('Y-m-d H:i:s');

        $this->id = htmlspecialchars(strip_tags($this->id));
        $this->nome = htmlspecialchars(strip_tags($this->nome));
        $this->email = htmlspecialchars(strip_tags($this->email));
        $this->pontos = htmlspecialchars(strip_tags($this->pontos));

        if (isset($data['password'])) {
            $this->password = $data['password'];
            $this->password = htmlspecialchars(strip_tags($this->password));

            $query = "UPDATE " . $this->table_name . " SET 
            name=:name, 
            email=:email, 
            password=:password, 
            pontos=:pontos,
            updated_at=:updated_at 
            WHERE id=:id";
        } else {
            $query = "UPDATE " . $this->table_name . " SET 
            name=:name, 
            email=:email, 
            pontos=:pontos,
            updated_at=:updated_at 
            WHERE id=:id";
        }

        $stmt = $this->conn->prepare($query);

        $stmt->bindParam(":id", $this->id);
        $stmt->bindParam(":name", $this->nome);
        $stmt->bindParam(":email", $this->email);
        $stmt->bindParam(":pontos", $this->pontos);

        if (isset($data['password'])) {
            $stmt->bindParam(":password", $this->password);
        }

        $stmt->bindParam(":updated_at", $this->updated_at);

        if ($stmt->execute()) {
            return true;
        }

        return false;
    }
    // metodo para deletar um jogador contendo (id)
    function deletar_jogador($id)
    {
        // recebe os dados do jogador via post e trata-os
        $this->id = $id;
        $query = "DELETE FROM " . $this->table_name . " WHERE id=:id";
        $stmt = $this->conn->prepare($query);
        // trata os dados para evitar sql injection
        $this->id = htmlspecialchars(strip_tags($this->id));
        $stmt->bindParam(":id", $this->id);
        if ($stmt->execute()) {
            return true;
        }
        return false;
    }
}
