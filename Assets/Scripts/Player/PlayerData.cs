// Autor: Murillo Gomes Yonamine
// Data: 27/09/2026

/*
PlayerData.cs) devem ter o [System.Serializable], ou não poderão ser
convertidos em JSON para envio dos dados depois.
*/

[System.Serializable]
public class PlayerData
{
    public string apelido;
    public int pontos;
    public string data;

    public PlayerData(string playerName, int score, string date)
    {
        this.apelido = playerName;
        this.pontos = score;
        this.data = date;
    }
}

[System.Serializable]
public class PlayerRootObject
{
    public PlayerData[] players;
}