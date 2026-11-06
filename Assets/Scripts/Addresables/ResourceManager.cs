using System.Collections; 
using System.Collections.Generic; 
using UnityEngine; 
using UnityEngine.AddressableAssets; 
using UnityEngine.ResourceManagement.AsyncOperations; 

public class ResourceManager : MonoBehaviour{ 
    public AssetReference assetReference; // referência ao prefab Addressable 
    public List<GameObject> prefabs = new List<GameObject>(); // lista das instâncias já criadas 
    AsyncOperationHandle<GameObject> handle; // handle da operação de instanciar; fica válido enquanto carrega e enquanto a instância existe
    bool cancelarAoTerminar; // marca que o jogador saiu (Desativador) antes do carregamento terminar

    [Header("A* (opcional)")] // título do grupo no Inspector
    public TankGrid tankGrid; // referência opcional ao grid do A*
    public bool reconstruirGrid; // se ligado, chama BuildGrid() depois de carregar/descarregar

    [Header("Debug")] // título do grupo no Inspector
    public bool logar; // se ligado, escreve "carregou/descarregou" no Console
    public Color corAtivador = new Color(0f, 1f, 0f, 0.35f); // cor do gizmo da zona Ativador (verde)
    public Color corDesativador = new Color(1f, 0f, 0f, 0.35f); // cor do gizmo da zona Desativador (vermelho)

    public bool EmCarregamento => handle.IsValid() && !handle.IsDone; // true enquanto a instanciação ainda não terminou

    public void LoadAsset() // chamado pelo cubo Ativador
    { // abre o método
        if (assetReference == null || !assetReference.RuntimeKeyIsValid()) return; // sai se não há prefab Addressable atribuído
        if (handle.IsValid()) // se já existe operação em andamento OU já concluída
        { // abre o if
            cancelarAoTerminar = false; // se o jogador voltou durante o carregamento, cancela o pedido de descarregar
            return; // não carrega de novo (resolve o carregamento duplicado)
        } // fecha o if
        if (prefabs.Count > 0) return; // segurança extra: já existem instâncias na lista
        handle = assetReference.InstantiateAsync(transform.position, Quaternion.identity); // pede a instância na posição do SpawnPoint
        handle.Completed += h => // callback executado quando a operação termina
        { // abre o callback
            if (h.Status != AsyncOperationStatus.Succeeded) // se a operação falhou
            { // abre o if
                Debug.LogWarning($"[{name}] falha ao carregar o Addressable."); // avisa o erro no Console
                handle = default; // limpa o handle para permitir nova tentativa
                return; // encerra o callback
            } // fecha o if
            if (cancelarAoTerminar) // se o jogador já saiu enquanto carregava
            { // abre o if
                cancelarAoTerminar = false; // zera a marca
                Addressables.ReleaseInstance(h.Result); // libera a instância recém-criada (destrói e solta a referência)
                handle = default; // limpa o handle
                Registrar("descarregou (cancelado durante o carregamento)"); // log opcional
                ReconstruirGrid(); // atualiza o A* se configurado
                return; // encerra o callback
            } // fecha o if
            prefabs.Add(h.Result); // guarda a instância na lista, como no exemplo do professor
            Registrar("carregou"); // log opcional
            ReconstruirGrid(); // atualiza o A* se configurado
        }; // fecha o callback
    } // fecha LoadAsset

    public void UnloadAsset() // chamado pelo cubo Desativador
    { // abre o método
        if (EmCarregamento) // se ainda está carregando
        { // abre o if
            cancelarAoTerminar = true; // libera o handle assim que a instância existir (soltar no meio da operação é instável)
            return; // o callback do LoadAsset termina o serviço
        } // fecha o if
        if (prefabs.Count == 0) return; // nada para descarregar
        foreach (GameObject prefab in prefabs) // percorre as instâncias criadas
        { // abre o foreach
            if (prefab != null) Addressables.ReleaseInstance(prefab); // libera cada instância (destrói e solta a referência)
        } // fecha o foreach
        prefabs.Clear(); // esvazia a lista
        handle = default; // zera o handle para permitir carregar de novo depois
        Registrar("descarregou"); // log opcional
        ReconstruirGrid(); // atualiza o A* se configurado
    } // fecha UnloadAsset

    void ReconstruirGrid() // dispara a reconstrução do grid de forma segura
    { // abre o método
        if (reconstruirGrid && tankGrid != null) StartCoroutine(ReconstruirDepoisDeUmFrame()); // só roda se ligado e com TankGrid atribuído
    } // fecha ReconstruirGrid

    IEnumerator ReconstruirDepoisDeUmFrame() // espera 1 frame: o Destroy só vale no fim do frame
    { // abre a coroutine
        yield return null; // espera o próximo frame para os colisores antigos sumirem
        tankGrid.BuildGrid(); // refaz o grid do A* lendo a obstacleMask (camadas 6 e 8)
    } // fecha a coroutine

    void Registrar(string acao) // escreve o log opcional
    { // abre o método
        if (logar) Debug.Log($"[{name}] {acao}: {(assetReference != null ? assetReference.RuntimeKey.ToString() : "sem referência")}"); // log só se ligado
    } // fecha Registrar

    void OnDrawGizmos() // desenha no Editor (Scene view), mesmo com o MeshRenderer desligado
    { // abre o método
        foreach (LoadAsset a in GetComponentsInChildren<LoadAsset>()) DesenharZona(a.gameObject, corAtivador); // desenha cada Ativador
        foreach (UnloadAsset d in GetComponentsInChildren<UnloadAsset>()) DesenharZona(d.gameObject, corDesativador); // desenha cada Desativador
        Gizmos.color = Color.yellow; // cor do marcador do ponto de spawn
        Gizmos.DrawWireSphere(transform.position, 0.5f); // esfera amarela onde o prefab nasce
    } // fecha OnDrawGizmos

    void DesenharZona(GameObject zona, Color cor) // desenha o BoxCollider de um cubo
    { // abre o método
        BoxCollider box = zona.GetComponent<BoxCollider>(); // pega o colisor do cubo
        if (box == null) return; // sai se não houver BoxCollider
        Gizmos.matrix = zona.transform.localToWorldMatrix; // aplica posição, rotação e escala do cubo
        Gizmos.color = cor; // define a cor de preenchimento
        Gizmos.DrawCube(box.center, box.size); // desenha o volume preenchido
        Gizmos.color = new Color(cor.r, cor.g, cor.b, 1f); // mesma cor, opaca, para a borda
        Gizmos.DrawWireCube(box.center, box.size); // desenha a borda do volume
        Gizmos.matrix = Matrix4x4.identity; // restaura a matriz padrão
    } // fecha DesenharZona
    void Awake() // roda quando o objeto é criado ao dar Play
    { // abre o método
        if (logar) Debug.Log($"[{name}] ResourceManager ativo, Logar ligado.", this); // confirma que a caixa Logar chegou ligada ao jogo
    } // fecha o Awake

    [ContextMenu("Testar log")] // cria um item no menu "..." do componente no Inspector
    void TestarLog() // método chamado pelo item do menu
    { // abre o método
        Debug.Log($"[{name}] teste de log OK (logar = {logar})", this); // imprime sempre, mesmo com Logar desligado
    } // fecha o TestarLog

} // fecha a classe