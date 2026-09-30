using UnityEngine;

public class AITank : MonoBehaviour
{
    public GameObject bulletPrefab;
    public GameObject bulletSpawn;
    public GameObject enemy;
    public Transform cannon;

    [Min(1f)] public float rotationSpeed = 120f;
    [Min(0.1f)] public float fireInterval = 1.5f;
    [SerializeField, Min(0.1f)] private float projectileSpeed = 15f;

    private const float AimToleranceDegrees = 2f;
    private EnemyTankPerception perception;
    private EnemyTankStateMachine stateMachine;
    private Collider targetCollider;
    private float fireCooldownRemaining;
    private Quaternion restingCannonRotation;

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
            CreateBullet();
            fireCooldownRemaining = Mathf.Max(0.1f, fireInterval);
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
        fireCooldownRemaining = 0f;
        if (cannon != null)
        {
            cannon.localRotation = restingCannonRotation;
        }
    }

    private bool TryAimAtTarget()
    {
        Vector3 targetPosition = targetCollider != null && targetCollider.enabled
            ? targetCollider.bounds.center
            : enemy.transform.position;
        Vector3 toTarget = targetPosition - bulletSpawn.transform.position;
        Vector3 horizontal = Vector3.ProjectOnPlane(toTarget, Vector3.up);
        float distance = horizontal.magnitude;

        if (distance < 0.01f)
        {
            return false;
        }

        float gravity = -Physics.gravity.y;
        float speedSquared = projectileSpeed * projectileSpeed;
        float discriminant = speedSquared * speedSquared -
            gravity * (gravity * distance * distance + 2f * toTarget.y * speedSquared);
        bool canFire = gravity > 0f && discriminant >= 0f;
        Vector3 launchDirection = horizontal.normalized;

        if (canFire)
        {
            float pitch = Mathf.Atan2(speedSquared - Mathf.Sqrt(discriminant), gravity * distance);
            launchDirection =
                horizontal.normalized * Mathf.Cos(pitch) + Vector3.up * Mathf.Sin(pitch);
        }

        Quaternion targetRotation = Quaternion.LookRotation(launchDirection);

        cannon.rotation = Quaternion.RotateTowards(
            cannon.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        return canFire && Quaternion.Angle(cannon.rotation, targetRotation) <= AimToleranceDegrees;
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
        }

        if (shell.TryGetComponent(out Rigidbody body))
        {
            body.linearVelocity = projectileSpeed * direction;
        }
    }
}
