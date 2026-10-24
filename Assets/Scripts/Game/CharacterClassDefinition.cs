using UnityEngine;

/// <summary>
/// Shared, read-only class design data, independent of models and per-tank state.
/// Only profiles marked gameplayEnabled are applied; other profiles remain visible design drafts.
/// Never store current HP, ultimate charge or cooldown on this shared asset.
/// </summary>
[CreateAssetMenu(fileName = "CharacterClass", menuName = "UmaTank/Character Class")]
public sealed class CharacterClassDefinition : ScriptableObject
{
    public enum UltimateEffect
    {
        SpeedBoost,
        Repair,
        LastStand,
        RecoveryBoost,
        EmpoweredShot
    }

    [Header("Class identity")]
    [SerializeField] private string className;
    [SerializeField, TextArea(2, 3)] private string description;

    [Header("Implementation status")]
    [Tooltip("Enable only after the class has been implemented and tested. Currently used by Suzuka.")]
    [SerializeField] private bool gameplayEnabled;

    [Header("Base attributes (applied only for enabled classes)")]
    [Tooltip("1 means the actor's existing base value. Player and AI keep their separate base speeds.")]
    [SerializeField, Min(0.1f)] private float healthMultiplier = 1f;
    [SerializeField, Min(0.1f)] private float movementSpeedMultiplier = 1f;
    [SerializeField, Min(0.1f)] private float shotDamageMultiplier = 1f;
    [Tooltip("Higher values mean a longer interval, not a higher firing rate.")]
    [SerializeField, Min(0.1f)] private float shotIntervalMultiplier = 1f;

    [Header("Passive design")]
    [SerializeField] private string passiveName;
    [SerializeField, TextArea(2, 3)] private string passiveDescription;

    [Header("Ultimate design")]
    [SerializeField] private string ultimateName;
    [SerializeField, TextArea(2, 3)] private string ultimateDescription;
    [SerializeField] private UltimateEffect ultimateEffect;
    [Tooltip("Optional HUD icon. An empty field uses the authored ULT placeholder.")]
    [SerializeField] private Texture2D ultimateIcon;
    [SerializeField, Min(0.1f)] private float ultimateDurationSeconds = 4f;
    [SerializeField, Min(1f)] private float ultimateSpeedMultiplier = 1.5f;
    [SerializeField, Range(0f, 1f)] private float ultimateHealFraction;
    [SerializeField, Range(0f, 0.95f)] private float ultimateDamageReduction;
    [SerializeField, Min(0.1f)] private float ultimateShotIntervalMultiplier = 1f;
    [SerializeField, Min(1f)] private float ultimateShotDamageMultiplier = 1f;
    [SerializeField, Min(1f)] private float ultimateExplosionRadiusMultiplier = 1f;

    [Header("Ultimate charge design")]
    [SerializeField, Range(0f, 100f)] private float chargePerSecond = 2f;
    [SerializeField, Range(0f, 100f)] private float chargePerElimination = 15f;

    public string ClassName => className;
    public bool GameplayEnabled => gameplayEnabled;
    public string Description => description;
    public float HealthMultiplier => healthMultiplier;
    public float MovementSpeedMultiplier => movementSpeedMultiplier;
    public float ShotDamageMultiplier => shotDamageMultiplier;
    public float ShotIntervalMultiplier => shotIntervalMultiplier;
    public string PassiveName => passiveName;
    public string PassiveDescription => passiveDescription;
    public string UltimateName => ultimateName;
    public string UltimateDescription => ultimateDescription;
    public UltimateEffect Effect => ultimateEffect;
    public Texture2D UltimateIcon => ultimateIcon;
    public float UltimateDurationSeconds => ultimateDurationSeconds;
    public float UltimateSpeedMultiplier => ultimateSpeedMultiplier;
    public float UltimateHealFraction => ultimateHealFraction;
    public float UltimateDamageReduction => ultimateDamageReduction;
    public float UltimateShotIntervalMultiplier => ultimateShotIntervalMultiplier;
    public float UltimateShotDamageMultiplier => ultimateShotDamageMultiplier;
    public float UltimateExplosionRadiusMultiplier => ultimateExplosionRadiusMultiplier;
    public float ChargePerSecond => chargePerSecond;
    public float ChargePerElimination => chargePerElimination;

    public string AttributeSummary =>
        $"Vida {healthMultiplier * 100f:0}%  |  Velocidade {movementSpeedMultiplier * 100f:0}%  |  " +
        $"Dano {shotDamageMultiplier * 100f:0}%  |  Intervalo de tiro {shotIntervalMultiplier * 100f:0}%";
}
