using UnityEngine;

/// <summary>
/// Per-player charge and effect lifetime. The battle supplies combat-only time and kill events;
/// this object never reads input, advances in menus, or writes to a shared class asset.
/// </summary>
public sealed class TankUltimateState
{
    public CharacterClassDefinition Definition { get; private set; }
    public float Charge { get; private set; }
    public float RemainingDuration { get; private set; }
    public bool IsEnabled => Definition != null && Definition.GameplayEnabled &&
        (uint)Definition.Effect <= (uint)CharacterClassDefinition.UltimateEffect.EmpoweredShot;
    public bool IsActive => RemainingDuration > 0f;
    public bool IsReady => IsEnabled && !IsActive && Charge >= 100f;
    public float SpeedMultiplier => IsActive ? Mathf.Max(1f, Definition.UltimateSpeedMultiplier) : 1f;
    public float ShotIntervalMultiplier => IsActive ? Definition.UltimateShotIntervalMultiplier : 1f;
    public float ShotDamageMultiplier => IsActive ? Definition.UltimateShotDamageMultiplier : 1f;
    public float ExplosionRadiusMultiplier => IsActive ? Definition.UltimateExplosionRadiusMultiplier : 1f;
    public float DamageReduction => IsActive ? Definition.UltimateDamageReduction : 0f;
    public Color FeedbackColor => Definition == null ? Color.cyan : Definition.Effect switch
    {
        CharacterClassDefinition.UltimateEffect.Repair => new Color(.35f, 1f, .55f, .55f),
        CharacterClassDefinition.UltimateEffect.LastStand => new Color(1f, .75f, .25f, .55f),
        CharacterClassDefinition.UltimateEffect.RecoveryBoost => new Color(1f, .55f, .8f, .55f),
        CharacterClassDefinition.UltimateEffect.EmpoweredShot => new Color(.8f, .55f, 1f, .55f),
        _ => new Color(.65f, .9f, 1f, .55f)
    };
    public float ActivationHealFraction => IsActive &&
        Definition.Effect == CharacterClassDefinition.UltimateEffect.RecoveryBoost
            ? Definition.UltimateHealFraction : 0f;

    public void Configure(CharacterClassDefinition definition)
    {
        Definition = definition;
        Reset();
    }

    public void Reset()
    {
        Charge = 0f;
        RemainingDuration = 0f;
    }

    /// <summary>Returns only this tick's gradual healing, as a fraction of maximum HP.</summary>
    public float Tick(float combatSeconds)
    {
        if (!IsEnabled || combatSeconds <= 0f) return 0f;
        if (IsActive)
        {
            float activeSeconds = Mathf.Min(combatSeconds, RemainingDuration);
            float healing = Definition.Effect == CharacterClassDefinition.UltimateEffect.Repair
                ? Definition.UltimateHealFraction * activeSeconds / Mathf.Max(.1f, Definition.UltimateDurationSeconds)
                : 0f;
            RemainingDuration = Mathf.Max(0f, RemainingDuration - combatSeconds);
            return healing; // No charge is gained during the effect, including its final frame.
        }
        Charge = Mathf.Clamp(Charge + Definition.ChargePerSecond * combatSeconds, 0f, 100f);
        return 0f;
    }

    public void AddElimination()
    {
        if (IsEnabled && !IsActive)
            Charge = Mathf.Clamp(Charge + Definition.ChargePerElimination, 0f, 100f);
    }

    public bool TryActivate()
    {
        if (!IsReady) return false;
        Charge = 0f;
        RemainingDuration = Mathf.Max(0.1f, Definition.UltimateDurationSeconds);
        return true;
    }

    /// <summary>The fired projectile keeps a snapshot; a miss or cover hit still consumes the shot.</summary>
    public bool ConsumeEmpoweredShot()
    {
        if (!IsActive || Definition.Effect != CharacterClassDefinition.UltimateEffect.EmpoweredShot) return false;
        RemainingDuration = 0f;
        return true;
    }
}
