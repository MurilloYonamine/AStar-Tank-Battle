using UnityEngine;

public enum EnemyTankState
{
    Patrol,
    Chase,
    Attack
}

/// <summary>
/// Coordinates patrol, pursuit and attack without recalculating A* every frame.
/// </summary>
public sealed class EnemyTankStateMachine : MonoBehaviour
{
    [Header("Target and patrol")]
    [SerializeField] private GameObject target;
    [SerializeField] private Transform[] patrolWaypoints;

    [Header("Pursuit")]
    [SerializeField, Min(0.1f)] private float repathInterval = 0.75f;
    [SerializeField, Min(0.1f)] private float targetMemoryDuration = 4f;

    private EnemyTankMovement movement;
    private EnemyTankPerception perception;
    private TankGrid grid;
    private Collider targetCollider;
    private GridNode plannedTargetNode;
    private Vector3 lastKnownTargetPosition;
    private float targetMemoryRemaining;
    private float repathCooldownRemaining;
    private int nextWaypointIndex;
    private bool patrolRouteUnavailable;

    public EnemyTankState CurrentState { get; private set; } = EnemyTankState.Patrol;

    private void Awake()
    {
        movement = GetComponent<EnemyTankMovement>();
        if (movement == null)
        {
            movement = gameObject.AddComponent<EnemyTankMovement>();
        }

        perception = GetComponent<EnemyTankPerception>();
        if (perception == null)
        {
            perception = gameObject.AddComponent<EnemyTankPerception>();
        }

        grid = TankGrid.Instance;
        if (grid == null)
        {
            grid = FindFirstObjectByType<TankGrid>();
        }

        if (target == null && TryGetComponent(out AITank tank))
        {
            target = tank.enemy;
        }

        if (target != null)
        {
            targetCollider = target.GetComponent<Collider>();
        }
    }

    private void Start()
    {
        EnterPatrol();
    }

    private void Update()
    {
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
        {
            return;
        }

        targetMemoryRemaining = Mathf.Max(0f, targetMemoryRemaining - Time.deltaTime);
        repathCooldownRemaining = Mathf.Max(0f, repathCooldownRemaining - Time.deltaTime);

        bool targetVisible = target != null &&
            perception.CanSeeTarget(target.transform, targetCollider);

        if (targetVisible)
        {
            lastKnownTargetPosition = target.transform.position;
            targetMemoryRemaining = targetMemoryDuration;
        }

        switch (CurrentState)
        {
            case EnemyTankState.Patrol:
                if (targetVisible)
                {
                    EnterChase();
                    if (perception.IsWithinAttackRange(target.transform, targetCollider))
                    {
                        EnterAttack();
                    }
                }
                else if (!patrolRouteUnavailable && !movement.IsMoving)
                {
                    TryStartNextPatrolRoute();
                }
                break;

            case EnemyTankState.Chase:
                if (targetVisible && perception.IsWithinAttackRange(target.transform, targetCollider))
                {
                    EnterAttack();
                }
                else if (targetMemoryRemaining <= 0f)
                {
                    EnterPatrol();
                }
                else
                {
                    RepathToLastKnownCell();
                }
                break;

            case EnemyTankState.Attack:
                if (!targetVisible || !perception.IsWithinAttackRange(target.transform, targetCollider))
                {
                    if (targetMemoryRemaining > 0f)
                    {
                        EnterChase();
                    }
                    else
                    {
                        EnterPatrol();
                    }
                }
                break;
        }
    }

    private void EnterPatrol()
    {
        CurrentState = EnemyTankState.Patrol;
        movement.Stop();
        plannedTargetNode = null;
        patrolRouteUnavailable = false;
        nextWaypointIndex = FindNearestWaypointIndex();
        TryStartNextPatrolRoute();
    }

    private void EnterChase()
    {
        CurrentState = EnemyTankState.Chase;
        movement.Stop();
        plannedTargetNode = null;
        repathCooldownRemaining = 0f;
        RepathToLastKnownCell();
    }

    private void EnterAttack()
    {
        CurrentState = EnemyTankState.Attack;
        movement.Stop();
    }

    private void RepathToLastKnownCell()
    {
        if (grid == null || !grid.IsBuilt || repathCooldownRemaining > 0f)
        {
            return;
        }

        GridNode targetNode = grid.GetNodeFromWorldPosition(lastKnownTargetPosition);
        if (targetNode == null || targetNode == plannedTargetNode)
        {
            return;
        }

        plannedTargetNode = targetNode;
        repathCooldownRemaining = repathInterval;
        movement.SetDestination(lastKnownTargetPosition);
    }

    private int FindNearestWaypointIndex()
    {
        if (patrolWaypoints == null || patrolWaypoints.Length == 0)
        {
            return 0;
        }

        int nearestIndex = 0;
        float nearestDistance = float.PositiveInfinity;

        for (int i = 0; i < patrolWaypoints.Length; i++)
        {
            if (patrolWaypoints[i] == null)
            {
                continue;
            }

            float distance = (patrolWaypoints[i].position - transform.position).sqrMagnitude;
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestIndex = i;
            }
        }

        return nearestIndex;
    }

    private void TryStartNextPatrolRoute()
    {
        if (grid == null || !grid.IsBuilt || patrolWaypoints == null || patrolWaypoints.Length == 0)
        {
            patrolRouteUnavailable = true;
            return;
        }

        GridNode currentNode = grid.GetNodeFromWorldPosition(transform.position);

        for (int attempt = 0; attempt < patrolWaypoints.Length; attempt++)
        {
            int index = nextWaypointIndex;
            nextWaypointIndex = (nextWaypointIndex + 1) % patrolWaypoints.Length;
            Transform waypoint = patrolWaypoints[index];

            if (waypoint == null || grid.GetNodeFromWorldPosition(waypoint.position) == currentNode)
            {
                continue;
            }

            if (movement.SetDestination(waypoint.position))
            {
                return;
            }
        }

        patrolRouteUnavailable = true;
    }
}
