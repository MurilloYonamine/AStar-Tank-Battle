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
        Definition.Effect == CharacterClassDefinition.UltimateEffect.SpeedBoost;
    public bool IsActive => RemainingDuration > 0f;
    public bool IsReady => IsEnabled && !IsActive && Charge >= 100f;
    public float SpeedMultiplier => IsActive ? Mathf.Max(1f, Definition.UltimateSpeedMultiplier) : 1f;

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

    public void Tick(float combatSeconds)
    {
        if (!IsEnabled || combatSeconds <= 0f) return;
        if (IsActive)
        {
            RemainingDuration = Mathf.Max(0f, RemainingDuration - combatSeconds);
            return; // No charge is gained during the effect, including its final frame.
        }
        Charge = Mathf.Clamp(Charge + Definition.ChargePerSecond * combatSeconds, 0f, 100f);
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
}
