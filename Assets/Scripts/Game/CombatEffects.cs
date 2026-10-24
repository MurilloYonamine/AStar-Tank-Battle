using UnityEngine;

/// <summary>
/// Optional cosmetic feedback from Free Quick Effects (URP package supplied by the group).
/// Source: Assets/GabrielAguiarProductions/FreeQuickEffectsVol1. Never applies damage.
/// </summary>
public sealed class CombatEffects : MonoBehaviour
{
    [SerializeField] private GameObject muzzlePrefab;
    [SerializeField] private GameObject impactPrefab;
    [Tooltip("Project-owned URP material; original imported materials stay unchanged.")]
    [SerializeField] private Material particleMaterial;
    [SerializeField, Min(.01f)] private float muzzleScale = .65f;
    [SerializeField, Min(.01f)] private float impactScale = .55f;
    private static CombatEffects instance;

    private void Awake() => instance = this;
    private void OnDestroy() { if (instance == this) instance = null; }

    public static void PlayMuzzle(Transform muzzle)
    {
        if (instance == null || muzzle == null) return;
        instance.Play(instance.muzzlePrefab, muzzle.position, muzzle.rotation, instance.muzzleScale, .65f);
    }

    public static void PlayImpact(Vector3 position)
    {
        if (instance != null) instance.Play(instance.impactPrefab, position, Quaternion.identity, instance.impactScale, 1.2f);
    }

    private void Play(GameObject prefab, Vector3 position, Quaternion rotation, float scale, float duration)
    {
        if (prefab == null || PauseManager.Instance != null && PauseManager.Instance.IsPaused) return;
        GameObject effect = Instantiate(prefab, position, rotation * prefab.transform.localRotation);
        effect.transform.localScale = prefab.transform.localScale * scale;
        foreach (Collider collider in effect.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        foreach (ParticleSystemRenderer renderer in effect.GetComponentsInChildren<ParticleSystemRenderer>(true))
        {
            if (particleMaterial == null) continue;
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++) materials[i] = particleMaterial;
            renderer.sharedMaterials = materials;
        }
        effect.AddComponent<CombatParticleBurst>().Initialize(duration);
    }

    public static void ClearBursts()
    {
        foreach (CombatParticleBurst burst in FindObjectsByType<CombatParticleBurst>(FindObjectsSortMode.None))
        {
            burst.gameObject.SetActive(false);
            Destroy(burst.gameObject);
        }
    }
}
