using System;
using UnityEngine;

/// <summary>
/// Stores one tank's HP and creates a damaging explosion when it is destroyed.
/// </summary>
public sealed class TankHealth : MonoBehaviour
{
    [SerializeField, Min(1)] private int maxHealth = 100;
    [SerializeField] private GameObject deathExplosion;
    [SerializeField] private bool deactivateOnDeath;

    public int MaxHealth => maxHealth;
    public int CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }

    public event Action<TankHealth, GameObject> Died;

    private void Awake()
    {
        RestoreFullHealth();
    }

    public void RestoreFullHealth()
    {
        CurrentHealth = maxHealth;
        IsDead = false;
    }

    public void ApplyDamage(int amount, GameObject source)
    {
        if (IsDead || amount <= 0)
        {
            return;
        }

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        if (CurrentHealth > 0)
        {
            return;
        }

        IsDead = true;
        SpawnDeathExplosion(source);
        Died?.Invoke(this, source);
        if (deactivateOnDeath)
        {
            gameObject.SetActive(false);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void SpawnDeathExplosion(GameObject source)
    {
        if (deathExplosion == null)
        {
            return;
        }

        Collider hull = GetComponent<Collider>();
        Vector3 position = hull != null && hull.enabled
            ? hull.bounds.center
            : transform.position + Vector3.up;
        GameObject effect = Instantiate(deathExplosion, position, Quaternion.identity);

        if (effect.TryGetComponent(out ExplosionDamage damageArea))
        {
            damageArea.SetSource(source);
        }
    }
}
