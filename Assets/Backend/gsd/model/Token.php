<?php

class Token
{
    private $conn;
    public $id;
    public $jogador_id;
    public $token;
    public $updated_at;
    public $expiration_at;
    private $table_name = "tokens";

    public function __construct()
    {
        $database = new DB();
        $this->conn = $database->getConnection();
    }

    // metodo para inserir um token contendo (jogador_id, token, updated_at, expiration_at)
    // verifica se já existe o jogador_id no banco
    // se existir, atualiza o token e a data de expiração
    // se não existir, insere o jogador_id, token e data de expiração
    function inserir_token($data)
    {
        // recebe os dados do jogador
        $this->jogador_id = $data['jogador_id'];
        $this->token = $data['token'];
        $this->updated_at = date('Y-m-d H:i:s');
        $this->expiration_at = date('Y-m-d H:i:s', strtotime('+1 day'));        

        $query = "SELECT * FROM " . $this->table_name . " WHERE jogador_id=:jogador_id";

        $stmt = $this->conn->prepare($query);
        // trata os dados para evitar sql injection
        $this->jogador_id = htmlspecialchars(strip_tags($this->jogador_id));

        $stmt->bindParam(":jogador_id", $this->jogador_id);

        if ($stmt->execute()) {
            $num = $stmt->rowCount();
            if ($num > 0) {
                $query = "UPDATE " . $this->table_name . " SET token=:token, updated_at=:updated_at, expiration_at=:expiration_at WHERE jogador_id=:jogador_id";
                $stmt = $this->conn->prepare($query);
                // trata os dados para evitar sql injection
                $this->token = htmlspecialchars(strip_tags($this->token));
                $this->updated_at = htmlspecialchars(strip_tags($this->updated_at));
                $this->expiration_at = htmlspecialchars(strip_tags($this->expiration_at));
                $this->jogador_id = htmlspecialchars(strip_tags($this->jogador_id));
                // associa os valores recebidos aos parâmetros da query
                $stmt->bindParam(":token", $this->token);
                $stmt->bindParam(":updated_at", $this->updated_at);
                $stmt->bindParam(":expiration_at", $this->expiration_at);
                $stmt->bindParam(":jogador_id", $this->jogador_id);

                if ($stmt->execute()) {
                    return true;
                }
            } else {
                $query = "INSERT INTO " . $this->table_name . " SET jogador_id=:jogador_id, token=:token, updated_at=:updated_at, expiration_at=:expiration_at";
                $stmt = $this->conn->prepare($query);
                // trata os dados para evitar sql injection
                $this->jogador_id = htmlspecialchars(strip_tags($this->jogador_id));
                $this->token = htmlspecialchars(strip_tags($this->token));
                $this->updated_at = htmlspecialchars(strip_tags($this->updated_at));
                $this->expiration_at = htmlspecialchars(strip_tags($this->expiration_at));
                // associa os valores recebidos aos parâmetros da query
                $stmt->bindParam(":jogador_id", $this->jogador_id);
                $stmt->bindParam(":token", $this->token);
                $stmt->bindParam(":updated_at", $this->updated_at);
                $stmt->bindParam(":expiration_at", $this->expiration_at);

                if ($stmt->execute()) {
                    return true;
                }
            }
        }
        return false;
    }

    // método para verificar se o token existe e se está válido
    function verificar_token(){
        // utiliza a session para verificar se há token setado
        $token = isset($_SESSION['token']) ? $_SESSION['token'] : null;
        $jogador_id = isset($_SESSION['jogador_id']) ? $_SESSION['jogador_id'] : null;
        // se não houver token, retorna false
        if($token == null || $jogador_id == null){
            return false;
        }
        // se houver token, verifica se ele existe no banco através do jogador_id da session
        $query = "SELECT * FROM " . $this->table_name . " WHERE jogador_id=:jogador_id";
        $stmt = $this->conn->prepare($query);
        // trata os dados para evitar sql injection
        $jogador_id = htmlspecialchars($jogador_id);
        // associa os valores recebidos aos parâmetros da query
        $stmt->bindParam(":jogador_id", $jogador_id);
        // executa a query
        $stmt->execute();        
        $row = $stmt->fetch(PDO::FETCH_ASSOC);                
        if(!$row){
            // se não houver token no banco, retorna false
            return false;
        }
        // se o token do banco for igual ao token da session, verifica se o token está válido
        if($row['token'] == $token){
            // se o token estiver válido, retorna true
            if(strtotime($row['expiration_at']) > strtotime(date('Y-m-d H:i:s'))){
                return true;
            } else {
                // se o token estiver inválido, retorna false
                return false;
            }
        } else {
            // se o token não for igual ao token da session, retorna false
            return false;
        }
    }
}
