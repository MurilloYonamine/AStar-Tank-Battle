using UnityEngine;

/// <summary>
/// Replaces only the tank's visual model. Its hull, HP, input, score identity and A* stay unchanged.
/// The model prefabs belong to the group; no gameplay components are copied from the preview.
/// </summary>
public sealed class TankAppearance : MonoBehaviour
{
    [SerializeField] private Transform modelRoot;
    [SerializeField] private Vector3 muzzlePosition = new(0f, 0.1f, 1.25f);

    public string CharacterName { get; private set; }

    public void ApplyCharacter(GameObject modelPrefab, string characterName)
    {
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
            gameObject.name = "Enemy - " + characterName;
        }
    }

    private static Transform FindTurret(Transform root)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "TankTurret") return child;
        }
        return null;
    }
}
