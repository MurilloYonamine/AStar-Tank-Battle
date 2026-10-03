// Autor: Murillo Gomes Yonamine | Professor Eduardo
// Data: 27/09/2026

/*
PlayerData.cs) devem ter o [System.Serializable], ou não poderão ser
convertidos em JSON para envio dos dados depois.
*/

[System.Serializable]
public class PlayerData
{
    public int id;
    public string name;
    public string email;
    public string password;
    public int pontos;
    public string created_at;
    public string updated_at;

    public PlayerData()
    {
    }

    public PlayerData(string name, int pontos)
    {
        this.name = name;
        this.pontos = pontos;
    }

    public PlayerData(string name, string email, string password, int pontos)
    {
        this.name = name;
        this.email = email;
        this.password = password;
        this.pontos = pontos;
    }

    public PlayerData(int id, string name, string email, string password, int pontos, string created_at, string updated_at)
    {
        this.id = id;
        this.name = name;
        this.email = email;
        this.password = password;
        this.pontos = pontos;
        this.created_at = created_at;
        this.updated_at = updated_at;
    }
}

[System.Serializable]
public class PlayerRootObject
{
    public PlayerData[] players;
}