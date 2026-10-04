using System.Collections.Generic;
using UnityEngine;

public enum EnemyTankState
{
    Patrol,
    Chase,
    Attack,
    Reposition
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
    [SerializeField, Min(0.1f)] private float targetMemoryDuration = 12f;

    [Header("Blocked shot recovery")]
    [SerializeField, Min(0.2f)] private float repositionRetryInterval = 1.5f;
    [SerializeField, Min(1f)] private float repositionDuration = 6f;
    [SerializeField, Range(1, 4)] private int firingPositionSearchRadius = 2;

    private EnemyTankMovement movement;
    private EnemyTankPerception perception;
    private TankHealth health;
    private AITank attack;
    private readonly AStarPathfinder pathfinder = new();
    private readonly Dictionary<GridNode, float> blockedFiringCells = new();
    private TankGrid grid;
    private Collider targetCollider;
    private GridNode plannedTargetNode;
    private Vector3 trackedTargetPosition;
    private float targetMemoryRemaining;
    private float repathCooldownRemaining;
    private int nextWaypointIndex;
    private bool isRepositioning;
    private bool hasFiringPosition;
    private float repositionRemaining;
    private float repositionCooldownRemaining;

    public EnemyTankState CurrentState { get; private set; } = EnemyTankState.Patrol;
    public bool IsRepositioning => isRepositioning;

    private void Awake()
    {
        attack = GetComponent<AITank>();
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

    private void OnEnable()
    {
        health = GetComponent<TankHealth>();
        if (health != null) health.Damaged += OnDamaged;
    }

    private void OnDisable()
    {
        if (health != null) health.Damaged -= OnDamaged;
    }

    private void OnDamaged(TankHealth damagedTank, GameObject source)
    {
        if (damagedTank.IsDead || target == null || !target.activeInHierarchy || source == null ||
            !source.transform.IsChildOf(target.transform) ||
            (PauseManager.Instance != null && PauseManager.Instance.IsPaused))
        {
            return;
        }

        // A hit reveals the attacker, but does not permit firing through cover or outside the FOV.
        trackedTargetPosition = target.transform.position;
        targetMemoryRemaining = targetMemoryDuration;
        if (isRepositioning) return;
        if (perception.CanSeeTarget(target.transform, targetCollider) &&
            perception.IsWithinAttackRange(target.transform, targetCollider))
        {
            EnterAttack();
        }
        else if (CurrentState != EnemyTankState.Chase)
        {
            EnterChase();
        }
        else
        {
            // Repeated hits refresh memory without clearing the movement's blocked-path recovery.
            RepathToTrackedCell();
        }
    }

    public void ResetForNewRound()
    {
        blockedFiringCells.Clear();
        repositionCooldownRemaining = 0f;
        targetMemoryRemaining = 0f;
        repathCooldownRemaining = 0f;
        trackedTargetPosition = Vector3.zero;
        plannedTargetNode = null;
        EnterPatrol();
    }

    public void NotifyBlockedShot()
    {
        if (!isActiveAndEnabled || health == null || health.IsDead || target == null ||
            !target.activeInHierarchy || grid == null || !grid.IsBuilt || attack == null ||
            isRepositioning || repositionCooldownRemaining > 0f ||
            (PauseManager.Instance != null && PauseManager.Instance.IsPaused)) return;

        trackedTargetPosition = target.transform.position;
        targetMemoryRemaining = targetMemoryDuration;
        repositionCooldownRemaining = repositionRetryInterval;
        GridNode start = grid.GetNearestWalkableNode(transform.position);
        if (start == null) return;
        blockedFiringCells[start] = Time.time + repositionDuration;
        BoxCollider hull = GetComponent<BoxCollider>();
        GridNode best = null;
        float bestScore = float.PositiveInfinity;
        for (int x = -firingPositionSearchRadius; x <= firingPositionSearchRadius; x++)
        {
            for (int z = -firingPositionSearchRadius; z <= firingPositionSearchRadius; z++)
            {
                int distance = Mathf.Abs(x) + Mathf.Abs(z);
                if (distance == 0 || distance > firingPositionSearchRadius) continue;
                GridNode candidate = grid.GetNode(start.X + x, start.Z + z);
                if (candidate == null || !candidate.IsWalkable ||
                    blockedFiringCells.TryGetValue(candidate, out float expiry) && expiry > Time.time) continue;
                Vector3 position = candidate.WorldPosition;
                position.y = transform.position.y;
                if (hull == null || !TankCollisionGuard.CanOccupy(hull, position, transform.rotation, 1 << 6) ||
                    !perception.IsWithinAttackRangeFrom(position, target.transform, targetCollider) ||
                    !perception.HasLineOfSightFrom(position, target.transform, targetCollider) ||
                    !attack.HasClearShotFrom(position) ||
                    !pathfinder.TryFindPath(grid, start, candidate, out List<GridNode> route)) continue;
                float score = route.Count + (position - target.transform.position).sqrMagnitude * 0.001f;
                if (score >= bestScore) continue;
                bestScore = score;
                best = candidate;
            }
        }

        if (best == null)
        {
            // Ordinary pursuit always targets the player's cell, never a speculative firing cell.
            repathCooldownRemaining = 0f;
            EnterChase();
            return;
        }

        // A distinct tactical state keeps firing reposition separate from player-cell pursuit.
        CurrentState = EnemyTankState.Reposition;
        movement.Stop();
        plannedTargetNode = null;
        isRepositioning = true;
        hasFiringPosition = true;
        repositionRemaining = repositionDuration;
        Vector3 destination = best.WorldPosition;
        destination.y = transform.position.y;
        movement.SetDestination(destination);
    }

    private void Update()
    {
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
        {
            return;
        }

        targetMemoryRemaining = Mathf.Max(0f, targetMemoryRemaining - Time.deltaTime);
        repathCooldownRemaining = Mathf.Max(0f, repathCooldownRemaining - Time.deltaTime);
        repositionCooldownRemaining = Mathf.Max(0f, repositionCooldownRemaining - Time.deltaTime);
        repositionRemaining = Mathf.Max(0f, repositionRemaining - Time.deltaTime);

        bool targetVisible = target != null &&
            perception.CanSeeTarget(target.transform, targetCollider);

        if (targetVisible)
        {
            trackedTargetPosition = target.transform.position;
            targetMemoryRemaining = targetMemoryDuration;
        }
        else if (target != null && targetMemoryRemaining > 0f &&
                 CurrentState != EnemyTankState.Patrol)
        {
            // Keep pursuing the target's grid cell briefly while a wall hides it.
            trackedTargetPosition = target.transform.position;
        }

        switch (CurrentState)
        {
            case EnemyTankState.Patrol:
                if (targetVisible)
                {
                    if (perception.IsWithinAttackRange(target.transform, targetCollider))
                    {
                        EnterAttack();
                    }
                    else
                    {
                        EnterChase();
                    }
                }
                else if (!movement.HasDestination && repathCooldownRemaining <= 0f)
                {
                    TryStartNextPatrolRoute();
                }
                break;

            case EnemyTankState.Reposition:
                if (targetMemoryRemaining <= 0f)
                {
                    EnterPatrol();
                }
                else if (!movement.HasDestination && hasFiringPosition && !attack.HasClearShotFrom(transform.position))
                {
                    isRepositioning = false;
                    repositionCooldownRemaining = 0f;
                    NotifyBlockedShot();
                }
                else if (!movement.HasDestination && hasFiringPosition && !targetVisible && repositionRemaining > 0f)
                {
                    // Arriving sideways must not send the tank back toward the obstructed firing spot.
                    movement.FaceTarget(target.transform.position);
                }
                else if (!movement.HasDestination || repositionRemaining <= 0f)
                {
                    isRepositioning = false;
                    plannedTargetNode = null;
                    repathCooldownRemaining = 0f;
                    if (targetVisible && perception.IsWithinAttackRange(target.transform, targetCollider))
                    {
                        EnterAttack();
                    }
                    else EnterChase();
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
                    RepathToTrackedCell();
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
        isRepositioning = false;
        CurrentState = EnemyTankState.Patrol;
        movement.Stop();
        plannedTargetNode = null;
        nextWaypointIndex = FindNearestWaypointIndex();
        TryStartNextPatrolRoute();
    }

    private void EnterChase()
    {
        isRepositioning = false;
        bool wasPatrolling = CurrentState == EnemyTankState.Patrol;
        CurrentState = EnemyTankState.Chase;
        movement.Stop();
        plannedTargetNode = null;

        if (wasPatrolling)
        {
            repathCooldownRemaining = 0f;
        }

        RepathToTrackedCell();
    }

    private void EnterAttack()
    {
        isRepositioning = false;
        CurrentState = EnemyTankState.Attack;
        movement.Stop();
    }

    private void RepathToTrackedCell()
    {
        if (grid == null || !grid.IsBuilt || repathCooldownRemaining > 0f)
        {
            return;
        }

        GridNode targetNode = grid.GetNodeFromWorldPosition(trackedTargetPosition);
        if (targetNode == null || targetNode == plannedTargetNode && movement.HasDestination)
        {
            return;
        }

        plannedTargetNode = targetNode;
        repathCooldownRemaining = repathInterval;
        if (!movement.SetDestination(trackedTargetPosition))
        {
            plannedTargetNode = null;
        }
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
        // Retry unreachable patrol routes after a delay: another tank may move out of the way.
        repathCooldownRemaining = repathInterval;
        if (grid == null || !grid.IsBuilt || patrolWaypoints == null || patrolWaypoints.Length == 0)
        {
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

    }
}
