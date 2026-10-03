using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stores one tank's HP and creates a damaging explosion when it is destroyed.
/// </summary>
public sealed class TankHealth : MonoBehaviour
{
    [SerializeField, Min(1)] private int maxHealth = 100;
    [SerializeField] private GameObject deathExplosion;
    [SerializeField] private GameObject deathSmoke;
    [SerializeField] private bool deactivateOnDeath;

    public int MaxHealth => maxHealth;
    public int CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }
    public bool DeactivatesOnDeath => deactivateOnDeath;
    public float DeathAnimationDuration { get; private set; }
    private Coroutine deathRoutine;
    private readonly List<Behaviour> stoppedBehaviours = new();
    private readonly List<Collider> stoppedColliders = new();

    // Only surviving hits notify AI; lethal hits follow the existing death flow.
    public event Action<TankHealth, GameObject> Damaged;
    public event Action<TankHealth, GameObject> Died;

    private void Awake()
    {
        RestoreFullHealth();
    }

    public void RestoreFullHealth()
    {
        StopDeathRoutine();
        foreach (Behaviour behaviour in stoppedBehaviours)
            if (behaviour != null) behaviour.enabled = true;
        foreach (Collider collider in stoppedColliders)
            if (collider != null) collider.enabled = true;
        stoppedBehaviours.Clear();
        stoppedColliders.Clear();
        CurrentHealth = maxHealth;
        IsDead = false;
        DeathAnimationDuration = 0f;
        GetComponent<TankAppearance>()?.CharacterAnimation?.PlayIdle();
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
            GetComponent<TankAppearance>()?.CharacterAnimation?.PlayHit();
            Damaged?.Invoke(this, source);
            return;
        }

        IsDead = true;
        TankCharacterAnimation animation = GetComponent<TankAppearance>()?.CharacterAnimation;
        DeathAnimationDuration = animation != null ? animation.PlayDeath() : 0f;
        SpawnDeathSmoke();
        SpawnDeathExplosion(source);
        StopGameplay();
        Died?.Invoke(this, source);
        // Score/death events are immediate; only cosmetic disappearance is delayed.
        if (!IsDead || !gameObject.activeInHierarchy) return;
        if (DeathAnimationDuration > 0f)
            deathRoutine = StartCoroutine(FinishDeath());
        else DeactivateDeadTank();
    }

    private void StopGameplay()
    {
        foreach (Behaviour behaviour in GetComponents<Behaviour>())
        {
            if (behaviour.enabled && (behaviour is Drive || behaviour is AITank ||
                behaviour is EnemyTankMovement || behaviour is EnemyTankStateMachine))
            {
                stoppedBehaviours.Add(behaviour);
                behaviour.enabled = false;
            }
        }
        foreach (Collider collider in GetComponentsInChildren<Collider>(true))
        {
            if (!collider.enabled) continue;
            stoppedColliders.Add(collider);
            collider.enabled = false;
        }
        if (TryGetComponent(out Rigidbody body) && !body.isKinematic)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
    }

    private IEnumerator FinishDeath()
    {
        yield return new WaitForSecondsRealtime(DeathAnimationDuration + 0.1f);
        deathRoutine = null;
        if (IsDead) DeactivateDeadTank();
    }

    private void DeactivateDeadTank()
    {
        if (deactivateOnDeath)
        {
            gameObject.SetActive(false);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDisable() => StopDeathRoutine();

    private void StopDeathRoutine()
    {
        if (deathRoutine != null) StopCoroutine(deathRoutine);
        deathRoutine = null;
    }

    private Vector3 GetDeathEffectPosition()
    {
        Collider hull = GetComponent<Collider>();
        return hull != null && hull.enabled
            ? hull.bounds.center
            : transform.position + Vector3.up;
    }

    private void SpawnDeathSmoke()
    {
        if (deathSmoke == null) return;
        // Independent cosmetic burst: it survives pooled tank deactivation and runs while paused.
        Instantiate(deathSmoke, GetDeathEffectPosition(), Quaternion.identity);
    }

    private void SpawnDeathExplosion(GameObject source)
    {
        if (deathExplosion == null)
        {
            return;
        }

        GameObject effect = Instantiate(deathExplosion, GetDeathEffectPosition(), Quaternion.identity);

        if (effect.TryGetComponent(out ExplosionDamage damageArea))
        {
            damageArea.SetSource(source);
        }
    }
}
