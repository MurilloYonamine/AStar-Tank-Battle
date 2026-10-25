using UnityEngine;

/// <summary>
/// Aims and fires the enemy weapon; state choice and hull navigation live in separate components.
/// A shot requires Attack, current perception/range, a reachable low ballistic arc, aim alignment,
/// a clear trajectory and an elapsed cooldown. Animation is cosmetic, never the fire-rate clock.
/// </summary>
public class AITank : MonoBehaviour
{
    public GameObject bulletPrefab;
    public GameObject bulletSpawn;
    public GameObject enemy;
    public Transform cannon;
    public Animator anim;

    [Min(1f)] public float rotationSpeed = 120f;
    [Min(0.1f)] public float fireInterval = 2.2f;
    [SerializeField, Min(0.1f)] private float projectileSpeed = 15f;

    private const float AimToleranceDegrees = 2f;
    private static readonly int AttackState = Animator.StringToHash("Base Layer.Soco");
    private EnemyTankPerception perception;
    private EnemyTankStateMachine stateMachine;
    private Collider targetCollider;
    private float fireCooldownRemaining;
    private Quaternion restingCannonRotation;
    private TankHealth health;
    public float ShotInterval => fireInterval * (health != null ? health.ShotIntervalMultiplier : 1f);
    public int WeaponGeneration { get; private set; }
    private void Awake() => health = GetComponent<TankHealth>();

    private void Start()
    {
        perception = GetComponent<EnemyTankPerception>();
        stateMachine = GetComponent<EnemyTankStateMachine>();

        if (perception == null)
        {
            perception = gameObject.AddComponent<EnemyTankPerception>();
        }

        if (stateMachine == null)
        {
            stateMachine = gameObject.AddComponent<EnemyTankStateMachine>();
        }

        if (enemy != null)
        {
            targetCollider = enemy.GetComponent<Collider>();
        }

        if (cannon != null)
        {
            restingCannonRotation = cannon.localRotation;
        }
        fireCooldownRemaining = ShotInterval;
    }

    private void Update()
    {
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
        {
            return;
        }

        if (cannon == null)
        {
            return;
        }

        fireCooldownRemaining = Mathf.Max(0f, fireCooldownRemaining - Time.deltaTime);

        if (stateMachine.CurrentState != EnemyTankState.Attack ||
            enemy == null || bulletSpawn == null || bulletPrefab == null ||
            !perception.CanSeeTarget(enemy.transform, targetCollider))
        {
            ReturnCannonToRest();
            return;
        }

        if (TryAimAtTarget() &&
            perception.IsWithinAttackRange(enemy.transform, targetCollider) &&
            fireCooldownRemaining <= 0f)
        {
            // The player may be visible while a branch or wall still intersects the ballistic arc.
            if (!HasClearShotFrom(transform.position))
            {
                stateMachine.NotifyBlockedShot();
                fireCooldownRemaining = Mathf.Min(0.25f, fireInterval);
                return;
            }
            CreateBullet();
            fireCooldownRemaining = Mathf.Max(0.1f, ShotInterval);
            // Cosmetic animation is optional and must never prevent a shot or its cooldown.
            TankCharacterAnimation characterAnimation = GetComponent<TankAppearance>()?.CharacterAnimation;
            if (characterAnimation != null) characterAnimation.PlayAttack();
            else if (anim != null && anim.isActiveAndEnabled && anim.isInitialized &&
                anim.runtimeAnimatorController != null && anim.HasState(0, AttackState))
            {
                anim.Play(AttackState, 0, 0f);
            }
        }
    }

    private void ReturnCannonToRest()
    {
        cannon.localRotation = Quaternion.RotateTowards(
            cannon.localRotation,
            restingCannonRotation,
            rotationSpeed * Time.deltaTime);
    }

    public void ResetForNewRound()
    {
        WeaponGeneration++;
        fireCooldownRemaining = ShotInterval;
        if (cannon != null)
        {
            cannon.localRotation = restingCannonRotation;
        }
    }

    public void SetWeaponTransforms(Transform turret, Transform muzzle)
    {
        cannon = turret;
        bulletSpawn = muzzle.gameObject;
        restingCannonRotation = cannon.localRotation;
        ResetForNewRound();
    }

    private bool TryAimAtTarget()
    {
        bool canFire = TryGetLaunchDirection(bulletSpawn.transform.position, out Vector3 launchDirection, out _);
        if (launchDirection.sqrMagnitude < 0.0001f) return false;
        Quaternion targetRotation = Quaternion.LookRotation(launchDirection);
        cannon.rotation = Quaternion.RotateTowards(
            cannon.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        return canFire && Quaternion.Angle(cannon.rotation, targetRotation) <= AimToleranceDegrees;
    }

    /// <summary>
    /// Solves a fixed-speed, gravity-only low arc toward the target collider center.
    /// With horizontal distance d, height difference h, speed v and downward gravity g:
    /// D = v^4 - g(g*d^2 + 2*h*v^2); theta = atan2(v^2 - sqrt(D), g*d).
    /// Negative D, non-downward gravity or negligible distance reject the shot instead of
    /// creating an invalid direction; flight time is d / (v*cos(theta)).
    /// </summary>
    private bool TryGetLaunchDirection(Vector3 origin, out Vector3 launchDirection, out float flightTime)
    {
        Vector3 targetPosition = targetCollider != null && targetCollider.enabled
            ? targetCollider.bounds.center
            : enemy.transform.position;
        Vector3 toTarget = targetPosition - origin;
        Vector3 horizontal = Vector3.ProjectOnPlane(toTarget, Vector3.up);
        float distance = horizontal.magnitude;

        launchDirection = horizontal.normalized;
        flightTime = 0f;
        if (distance < 0.01f)
        {
            return false;
        }

        float gravity = -Physics.gravity.y;
        float speedSquared = projectileSpeed * projectileSpeed;
        float discriminant = speedSquared * speedSquared -
            gravity * (gravity * distance * distance + 2f * toTarget.y * speedSquared);
        bool canFire = gravity > 0f && discriminant >= 0f;

        if (canFire)
        {
            float pitch = Mathf.Atan2(speedSquared - Mathf.Sqrt(discriminant), gravity * distance);
            launchDirection =
                horizontal.normalized * Mathf.Cos(pitch) + Vector3.up * Mathf.Sin(pitch);
            flightTime = distance / (projectileSpeed * Mathf.Cos(pitch));
        }
        return canFire;
    }

    /// <summary>
    /// Predicts the muzzle pose for a candidate hull position and sphere-casts the sampled arc.
    /// A visible player is not enough if a branch intersects the projectile trajectory.
    /// </summary>
    public bool HasClearShotFrom(Vector3 position)
    {
        if (enemy == null || cannon == null || bulletSpawn == null || bulletPrefab == null) return false;
        Vector3 launch = Vector3.ProjectOnPlane(enemy.transform.position - position, Vector3.up).normalized;
        if (launch.sqrMagnitude < 0.0001f) return false;
        Vector3 pivot = position + (cannon.position - transform.position);
        Vector3 muzzleOffset = Quaternion.Inverse(cannon.rotation) * (bulletSpawn.transform.position - cannon.position);
        Vector3 origin = pivot;
        float duration = 0f;
        // Pitch also raises the muzzle: predict the aimed pose, not the current resting barrel.
        for (int i = 0; i < 4; i++)
        {
            origin = pivot + Quaternion.LookRotation(launch) * muzzleOffset;
            if (!TryGetLaunchDirection(origin, out launch, out duration)) return false;
        }
        float radius = 0.1f;
        if (bulletPrefab.TryGetComponent(out CapsuleCollider capsule))
            radius = Mathf.Max(radius, capsule.radius * Mathf.Max(
                Mathf.Abs(capsule.transform.lossyScale.x), Mathf.Abs(capsule.transform.lossyScale.z)));
        // Allow for the node arrival tolerance and discrete physics steps near foliage edges.
        return ProjectileObstacleCheck.IsTrajectoryClear(origin, launch * projectileSpeed, duration, radius + 0.2f);
    }

    // Pooling reuses this weapon: an old projectile must not reposition a newly spawned life.
    public void NotifyBlockedShot(int generation)
    {
        if (generation == WeaponGeneration && isActiveAndEnabled && stateMachine != null)
            stateMachine.NotifyBlockedShot();
    }

    private void CreateBullet()
    {
        Vector3 direction = cannon.forward;
        GameObject shell = Instantiate(
            bulletPrefab,
            bulletSpawn.transform.position,
            Quaternion.LookRotation(direction));

        if (shell.TryGetComponent(out AIShell projectile))
        {
            projectile.SetOwner(gameObject);
            projectile.SetCombatMultipliers(health != null ? health.ShotDamageMultiplier : 1f,
                health != null ? health.ExplosionRadiusMultiplier : 1f);
        }

        if (shell.TryGetComponent(out Rigidbody body))
        {
            body.linearVelocity = projectileSpeed * direction;
        }
        BattleAudioController.PlayShot(true);
        CombatEffects.PlayMuzzle(bulletSpawn.transform);
    }
}
