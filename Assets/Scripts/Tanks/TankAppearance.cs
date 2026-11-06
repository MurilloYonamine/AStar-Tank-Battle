using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// Replaces only the tank's visual model. Its hull, HP, input, score identity and A* stay unchanged.
/// The group maintains the prefab setup; original character/art rights remain with their owners.
/// No gameplay components are copied from the preview. Rebinding uses the TankTurret marker and
/// a muzzle offset instead of retaining destroyed model transforms or a previous character's Animator.
/// </summary>
public sealed class TankAppearance : MonoBehaviour
{
    [SerializeField] private Transform modelRoot;
    [SerializeField] private Vector3 muzzlePosition = new(0f, 0.1f, 1.25f);

    public string CharacterName { get; private set; }
    public TankCharacterAnimation CharacterAnimation { get; private set; }
    // Visual replacement only stores the profile; the battle explicitly applies supported stats.
    public CharacterClassDefinition ClassDefinition { get; private set; }

    /// <summary>
    /// Replaces cosmetic children and rebinds the weapon without replacing the actor's physics,
    /// event subscriptions or account identity. The battle applies the stored class separately.
    /// </summary>
    public void ApplyCharacter(GameObject modelPrefab, string characterName,
        CharacterClassDefinition classDefinition = null)
    {
        ClassDefinition = classDefinition;
        if (modelRoot != null)
        {
            modelRoot.gameObject.SetActive(false);
            Destroy(modelRoot.gameObject);
        }

        modelRoot = Instantiate(modelPrefab, transform).transform;
        modelRoot.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        modelRoot.localScale = Vector3.one;
        modelRoot.name = "Character Model";
        CharacterName = characterName;
        CharacterAnimation = modelRoot.GetComponentInChildren<TankCharacterAnimation>(true);
        CharacterAnimation?.PlayIdle();
        foreach (Animator animator in modelRoot.GetComponentsInChildren<Animator>(true))
        {
            // Tank movement is owned by Drive/A*, never by cosmetic animation root motion.
            animator.applyRootMotion = false;
        }

        Transform turret = FindTurret(modelRoot);
        if (turret == null)
        {
            Debug.LogError($"Character {characterName} needs a TankTurret transform.", this);
            return;
        }

        Transform muzzle = new GameObject("Bullet Spawn").transform;
        muzzle.SetParent(turret, false);
        muzzle.localPosition = muzzlePosition;

        if (TryGetComponent(out Drive drive))
        {
            drive.SetWeaponTransforms(turret, muzzle);
            gameObject.name = "Player - " + characterName;
        }
        else if (TryGetComponent(out AITank aiTank))
        {
            aiTank.SetWeaponTransforms(turret, muzzle);
            // The previous model is destroyed; never keep its Animator (or another tank's).
            aiTank.anim = modelRoot.GetComponentInChildren<Animator>(true);
            gameObject.name = "Enemy - " + characterName;
        }
    }

    private int applyVersion; // [NOVO] versão da última aplicação: respostas atrasadas são descartadas

    /// <summary>Versão Addressables do ApplyCharacter: instancia o modelo de forma assíncrona. concluido(true) = tudo configurado.</summary>
    public void ApplyCharacterAsync(AssetReferenceGameObject modelRef, string characterName,
        CharacterClassDefinition classDefinition = null, Action<bool> concluido = null) {
        int version = ++applyVersion;                       // nova aplicação invalida as anteriores
        ClassDefinition = classDefinition;                  // a classe vale já (o PrepareRound lê isso)
        CharacterName = characterName;                      // nome vale já
        ReleaseModel();                                     // libera o modelo anterior (ReleaseInstance; Destroy só se não for Addressable)

        if (modelRef == null || !modelRef.RuntimeKeyIsValid())   // referência vazia
        {
            Debug.LogError($"[Addressables] ModelRef vazio para {characterName}.", this); // avisa
            concluido?.Invoke(false);                       // avisa quem chamou, sem travar a partida
            return;
        }

        modelRef.InstantiateAsync(transform).Completed += handle => // cria o modelo como filho deste tanque
        {
            if (handle.Status != AsyncOperationStatus.Succeeded)    // falha de carregamento
            {
                Debug.LogError($"[Addressables] Falha ao carregar {characterName}: {handle.OperationException}");
                concluido?.Invoke(false);                   // informa a falha
                return;
            }
            if (version != applyVersion)                    // chegou atrasado (houve nova chamada ou Release)
            {
                Addressables.ReleaseInstance(handle.Result);// descarta a instância
                return;                                     // a chamada mais nova tem o próprio callback
            }
            modelRoot = handle.Result.transform;            // o novo modelo vira o modelRoot
            concluido?.Invoke(ConfigureModel(characterName)); // pós-processamento e aviso de conclusão
        };
    }

    /// <summary>Libera o modelo (chamado ao voltar ao menu, rejogar ou destruir).</summary>
    public void ReleaseCharacter() {
        applyVersion++;                                     // cancela aplicações pendentes
        ReleaseModel();                                     // libera o modelo atual
    }

    private void ReleaseModel() {
        CharacterAnimation = null;                          // o animador antigo deixa de existir
        if (modelRoot == null) return;                      // nada a liberar
        GameObject old = modelRoot.gameObject;              // guarda o objeto
        modelRoot = null;                                   // zera a referência
        old.SetActive(false);                               // some na hora
        if (!Addressables.ReleaseInstance(old)) Destroy(old); // Addressables libera; senão Destroy comum (modelo embutido antigo)
    }

    private void OnDestroy() => ReleaseCharacter();         // [NOVO] garante a liberação ao destruir o tanque

    // Mesmo pós-processamento do ApplyCharacter (animator, turret, muzzle, nome); devolve false se faltar o TankTurret.
    private bool ConfigureModel(string characterName) {
        modelRoot.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity); // zera posição/rotação locais
        modelRoot.localScale = Vector3.one;                 // escala padrão
        modelRoot.name = "Character Model";                 // nome padrão
        CharacterAnimation = modelRoot.GetComponentInChildren<TankCharacterAnimation>(true); // animação do personagem
        CharacterAnimation?.PlayIdle();                     // começa parado
        foreach (Animator animator in modelRoot.GetComponentsInChildren<Animator>(true))
            animator.applyRootMotion = false;               // movimento é do Drive/A*

        Transform turret = FindTurret(modelRoot);           // acha a torre
        if (turret == null) {
            Debug.LogError($"Character {characterName} needs a TankTurret transform.", this);
            return false;
        }
        Transform muzzle = new GameObject("Bullet Spawn").transform; // ponto de saída do tiro
        muzzle.SetParent(turret, false);                    // filho da torre
        muzzle.localPosition = muzzlePosition;              // offset configurado

        if (TryGetComponent(out Drive drive))               // é o jogador
        {
            drive.SetWeaponTransforms(turret, muzzle);
            gameObject.name = "Player - " + characterName;
        } else if (TryGetComponent(out AITank aiTank))        // é inimigo
          {
            aiTank.SetWeaponTransforms(turret, muzzle);
            aiTank.anim = modelRoot.GetComponentInChildren<Animator>(true);
            gameObject.name = "Enemy - " + characterName;
        }
        return true;
    }

    private static Transform FindTurret(Transform root)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "TankTurret") return child;
        }
        return null;
    }

    public static Transform FindCharacterRoot(Transform root)
    {
        Animator animator = root.GetComponentInChildren<Animator>(true);
        if (animator != null && animator.transform != root) return animator.transform;
        // Unanimated models still need a character-only result preview, without their tank.
        foreach (Transform child in root)
            if (child.GetComponentInChildren<SkinnedMeshRenderer>(true) != null) return child;
        return null;
    }
}
