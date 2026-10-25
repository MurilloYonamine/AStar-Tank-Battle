using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stores one tank's HP and creates a damaging explosion when it is destroyed.
/// Class limits/passives and temporary skill modifiers are composed per actor; shared assets
/// are never runtime health storage. Death is reported once immediately, while cosmetic death
/// may finish before the pooled actor deactivates. Damaged reports surviving hits to enemy AI.
/// </summary>
public sealed class TankHealth : MonoBehaviour
{
    [SerializeField, Min(1)] private int maxHealth = 100;
    [SerializeField] private GameObject deathExplosion;
    [SerializeField] private GameObject deathSmoke;
    [SerializeField] private bool deactivateOnDeath;

    private float classHealthMultiplier = 1f;
    private CharacterClassDefinition classDefinition;
    private TankUltimateState uniqueSkill;
    private float healingRemainder;
    public float ShotDamageMultiplier => (classDefinition != null ? classDefinition.ShotDamageMultiplier : 1f) *
        (uniqueSkill != null ? uniqueSkill.ShotDamageMultiplier : 1f);
    public float ExplosionRadiusMultiplier => uniqueSkill != null ? uniqueSkill.ExplosionRadiusMultiplier : 1f;
    public float ShotIntervalMultiplier
    {
        get
        {
            float interval = classDefinition != null ? classDefinition.ShotIntervalMultiplier : 1f;
            if (classDefinition != null && CurrentHealth > 0 &&
                classDefinition.LowHealthThreshold > 0f &&
                CurrentHealth <= Mathf.FloorToInt(MaxHealth * classDefinition.LowHealthThreshold + .0001f))
                interval *= classDefinition.LowHealthShotIntervalMultiplier;
            return interval * (uniqueSkill != null ? uniqueSkill.ShotIntervalMultiplier : 1f);
        }
    }

    /// <summary>Per-actor references only. Never mutates the shared profile or enables enemy skills.</summary>
    public void ConfigureClass(CharacterClassDefinition definition)
    {
        classDefinition = definition != null && definition.GameplayEnabled ? definition : null;
        uniqueSkill = null;
        healingRemainder = 0f;
        SetClassHealthMultiplier(classDefinition != null ? classDefinition.HealthMultiplier : 1f);
    }
    public void SetUniqueSkillState(TankUltimateState state) => uniqueSkill = state;
    public void ConsumeEmpoweredShot() => uniqueSkill?.ConsumeEmpoweredShot();
    public int MaxHealth => Mathf.Max(1, Mathf.RoundToInt(maxHealth * classHealthMultiplier));
    /// <summary>Changes the class limit, never the serialized base HP or a tank's death state.</summary>
    public void SetClassHealthMultiplier(float value)
    {
        classHealthMultiplier = Mathf.Max(0.1f, value);
        CurrentHealth = Mathf.Min(CurrentHealth, MaxHealth);
    }
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

    /// <summary>
    /// Resets a new attempt/pooled life, including stopped components, death animation and
    /// fractional healing. Only this explicit lifecycle reset can revive the actor.
    /// </summary>
    public void RestoreFullHealth()
    {
        StopDeathRoutine();
        foreach (Behaviour behaviour in stoppedBehaviours)
            if (behaviour != null) behaviour.enabled = true;
        foreach (Collider collider in stoppedColliders)
            if (collider != null) collider.enabled = true;
        stoppedBehaviours.Clear();
        stoppedColliders.Clear();
        CurrentHealth = MaxHealth;
        healingRemainder = 0f;
        uniqueSkill = null;
        IsDead = false;
        DeathAnimationDuration = 0f;
        GetComponent<TankAppearance>()?.CharacterAnimation?.PlayIdle();
    }

    /// <summary>
    /// Applies ceil(incoming * (1 - skill reduction)), with at least one damage for a valid hit.
    /// Source is the firing actor, not the projectile, so elimination attribution survives impact.
    /// IsDead is set before events/explosions can reenter and report a second death.
    /// </summary>
    public void ApplyDamage(int amount, GameObject source)
    {
        if (IsDead || amount <= 0)
        {
            return;
        }

        float reduction = uniqueSkill != null ? Mathf.Clamp(uniqueSkill.DamageReduction, 0f, .95f) : 0f;
        int effectiveDamage = Mathf.Max(1, Mathf.CeilToInt(amount * (1f - reduction)));
        CurrentHealth = Mathf.Max(0, CurrentHealth - effectiveDamage);
        if (CurrentHealth > 0)
        {
            GetComponent<TankAppearance>()?.CharacterAnimation?.PlayHit();
            Damaged?.Invoke(this, source);
            return;
        }

        IsDead = true;
        BattleAudioController.PlayDeath();
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

    /// <summary>Accumulates sub-HP ticks, clamps at maximum HP and never resurrects a dead actor.</summary>
    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount)) return;
        if (CurrentHealth >= MaxHealth) { healingRemainder = 0f; return; }
        healingRemainder += amount;
        int recovered = Mathf.FloorToInt(healingRemainder + .0001f);
        healingRemainder = Mathf.Max(0f, healingRemainder - recovered);
        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + recovered);
        if (CurrentHealth == MaxHealth) healingRemainder = 0f;
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
