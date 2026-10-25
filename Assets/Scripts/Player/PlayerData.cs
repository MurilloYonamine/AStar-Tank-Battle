// Autor: Murillo Gomes Yonamine | Professor Eduardo
// Data: 27/09/2026

/*
PlayerData.cs) devem ter o [System.Serializable], ou não poderão ser
convertidos em JSON para envio dos dados depois.
*/

/// <summary>
/// Serializable account DTO matching the existing PHP field names, not per-match history.
/// Authenticated login responses create it without a password; the legacy password/points
/// fields remain for original registration/API compatibility. Times/outcomes live in partidas.
/// </summary>
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

/// <summary>Legacy wrapper for consumers that explicitly provide a JSON object with a players array.</summary>
[System.Serializable]
public class PlayerRootObject
{
    public PlayerData[] players;
}
