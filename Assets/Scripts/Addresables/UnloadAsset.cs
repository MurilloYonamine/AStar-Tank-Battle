using UnityEngine; 

public class UnloadAsset : MonoBehaviour // fica no cubo filho "Desativador"
{ // abre a classe
    ResourceManager manager; // referência ao ResourceManager do pai (SpawnPoint)

    void Start() // roda uma vez ao iniciar
    { // abre o método
        manager = GetComponentInParent<ResourceManager>(); // procura o ResourceManager no objeto pai
        if (manager == null) Debug.LogWarning($"[{name}] sem ResourceManager no pai."); // avisa se esqueceu de configurar
    } // fecha Start

    void OnTriggerEnter(Collider other) // chamado quando algo entra no trigger
    { // abre o método
        if (manager == null) return; // sem manager não há o que fazer
        if (!other.CompareTag("Player")) return; // ignora tudo que não for o tanque do jogador
        if (manager.prefabs.Count > 0 || manager.EmCarregamento) manager.UnloadAsset(); // descarrega se há instância ou carregamento pendente
    } // fecha OnTriggerEnter
} // fecha a classe