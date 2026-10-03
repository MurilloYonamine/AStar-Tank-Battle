using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rotates and moves an enemy tank through the sequence of nodes returned by A*.
/// The state machine chooses destinations; this component retries paths when physical travel is blocked.
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
public sealed class EnemyTankMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TankGrid grid;

    [Header("Movement")]
    [SerializeField, Min(0.1f)] private float moveSpeed = 3f;
    [SerializeField, Min(1f)] private float rotationSpeed = 180f;
    [SerializeField, Min(0.01f)] private float nodeReachDistance = 0.2f;
    [SerializeField, Min(0f)] private float collisionSkin = 0.05f;
    [SerializeField] private LayerMask obstacleMask = 1 << 6;
    [Header("Blocked path recovery")]
    [SerializeField, Min(0.2f)] private float blockedTimeout = 0.8f;
    [SerializeField, Min(0.2f)] private float retryInterval = 1.2f;
    [SerializeField, Min(0.5f)] private float avoidanceDuration = 4f;

    private readonly AStarPathfinder pathfinder = new();
    private readonly List<GridNode> currentPath = new();
    private readonly HashSet<GridNode> temporaryObstacles = new();
    private readonly Dictionary<GridNode, float> avoidedCells = new();
    private readonly List<GridNode> expiredCells = new();
    private int currentNodeIndex;
    private Rigidbody body;
    private BoxCollider hull;
    private Vector3 destination;
    private Vector3 progressPosition;
    private float blockedTime;
    private float retryRemaining;
    private int failedAttempts;

    public bool IsMoving => currentNodeIndex < currentPath.Count;
    public bool HasDestination { get; private set; }
    public IReadOnlyList<GridNode> CurrentPath => currentPath;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        hull = GetComponent<BoxCollider>();
        body.isKinematic = true;
        body.useGravity = false;
        ResolveGrid();
    }

    private void FixedUpdate()
    {
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
        {
            return;
        }

        retryRemaining = Mathf.Max(0f, retryRemaining - Time.fixedDeltaTime);
        if (HasDestination && !IsMoving && retryRemaining <= 0f)
        {
            RecalculatePath();
        }

        if (IsMoving)
        {
            // Measure actual travel instead of trusting MovePosition: physics can reject a pose.
            if ((body.position - progressPosition).sqrMagnitude >= 0.0025f)
            {
                progressPosition = body.position;
                blockedTime = 0f;
            }
            else
            {
                blockedTime += Time.fixedDeltaTime;
            }

            if (blockedTime >= blockedTimeout && retryRemaining <= 0f)
            {
                GridNode blockedNode = currentPath[currentNodeIndex];
                GridNode currentNode = grid.GetNodeFromWorldPosition(body.position);
                if (blockedNode != currentNode)
                {
                    avoidedCells[blockedNode] = Time.time + avoidanceDuration;
                }

                RecalculatePath();
            }
        }

        FollowPath();
    }

    public bool SetDestination(Vector3 destination)
    {
        this.destination = destination;
        HasDestination = true;
        failedAttempts = 0;
        return RecalculatePath();
    }

    private bool RecalculatePath()
    {
        ResolveGrid();

        if (grid == null || !grid.IsBuilt)
        {
            HasDestination = false;
            Debug.LogWarning("EnemyTankMovement could not find a built TankGrid.", this);
            return false;
        }

        GridNode startNode = grid.GetNearestWalkableNode(transform.position);
        GridNode destinationNode = grid.GetNearestWalkableNode(destination);

        currentPath.Clear();
        currentNodeIndex = 0;
        blockedTime = 0f;
        progressPosition = body.position;
        // Different retry times let one tank yield instead of both retrying in lockstep.
        retryRemaining = retryInterval + Mathf.Abs(GetInstanceID() % 7) * 0.08f;
        CollectTemporaryObstacles(startNode, destinationNode);

        if (!pathfinder.TryFindPath(grid, startNode, destinationNode, out List<GridNode> newPath,
                temporaryObstacles))
        {
            failedAttempts++;
            if (failedAttempts >= 3)
            {
                HasDestination = false;
            }
            grid.SetDebugPath(null);
            return false;
        }

        failedAttempts = 0;
        if (newPath.Count > 0 &&
            Vector3.ProjectOnPlane(startNode.WorldPosition - body.position, Vector3.up).sqrMagnitude >
            nodeReachDistance * nodeReachDistance)
        {
            // Return to the current cell's center before turning into the next orthogonal edge.
            newPath.Insert(0, startNode);
        }
        currentPath.AddRange(newPath);
        HasDestination = currentPath.Count > 0;
        grid.SetDebugPath(currentPath);
        return true;
    }

    public void Stop()
    {
        HasDestination = false;
        blockedTime = 0f;
        retryRemaining = 0f;
        avoidedCells.Clear();
        currentPath.Clear();
        currentNodeIndex = 0;

        if (grid != null)
        {
            grid.SetDebugPath(null);
        }
    }

    private void CollectTemporaryObstacles(GridNode startNode, GridNode destinationNode)
    {
        temporaryObstacles.Clear();
        expiredCells.Clear();
        foreach (KeyValuePair<GridNode, float> avoided in avoidedCells)
        {
            if (avoided.Value <= Time.time)
            {
                expiredCells.Add(avoided.Key);
            }
            else
            {
                temporaryObstacles.Add(avoided.Key);
            }
        }

        foreach (GridNode expired in expiredCells)
        {
            avoidedCells.Remove(expired);
        }

        // Snapshot moving hulls only when A* runs. Never mark shared grid nodes permanently blocked.
        foreach (TankHealth tank in FindObjectsByType<TankHealth>(FindObjectsSortMode.None))
        {
            if (tank.gameObject == gameObject || tank.IsDead ||
                !tank.TryGetComponent(out Collider otherHull))
            {
                continue;
            }

            Bounds occupied = otherHull.bounds;
            Vector3 scaledSize = Vector3.Scale(hull.size, hull.transform.lossyScale);
            float clearance = new Vector2(scaledSize.x, scaledSize.z).magnitude * 0.5f + collisionSkin;
            occupied.Expand(new Vector3(clearance * 2f, 0f, clearance * 2f));
            foreach (GridNode node in grid.GetAllNodes())
            {
                Vector3 point = node.WorldPosition;
                if (point.x >= occupied.min.x && point.x <= occupied.max.x &&
                    point.z >= occupied.min.z && point.z <= occupied.max.z)
                {
                    temporaryObstacles.Add(node);
                }
            }
        }

        temporaryObstacles.Remove(startNode);
        // Chase still targets the player's cell, as required by the assignment.
        temporaryObstacles.Remove(destinationNode);
    }

    private void FollowPath()
    {
        if (!IsMoving)
        {
            return;
        }

        GridNode targetNode = currentPath[currentNodeIndex];
        Vector3 targetPosition = targetNode.WorldPosition;
        targetPosition.y = body.position.y;

        Vector3 direction = targetPosition - body.position;
        direction.y = 0f;

        float distance = direction.magnitude;
        if (distance <= nodeReachDistance)
        {
            currentNodeIndex++;
            if (!IsMoving)
            {
                HasDestination = false;
            }
            return;
        }

        Quaternion nextRotation = body.rotation;
        if (distance > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            nextRotation = Quaternion.RotateTowards(
                body.rotation,
                targetRotation,
                rotationSpeed * Time.fixedDeltaTime);
            if (TankCollisionGuard.CanOccupy(hull, body.position, nextRotation, obstacleMask))
            {
                body.MoveRotation(nextRotation);
            }
            else
            {
                nextRotation = body.rotation;
            }
        }

        float step = Mathf.Min(moveSpeed * Time.fixedDeltaTime, distance);
        Vector3 travelDirection = direction / distance;
        float allowedStep = step;

        // A* chooses the route; a sweep keeps the tank's physical hull out of walls and other tanks.
        foreach (RaycastHit hit in body.SweepTestAll(
                     travelDirection, step + collisionSkin, QueryTriggerInteraction.Ignore))
        {
            if (TankCollisionGuard.IsBlocking(hit.collider, transform, obstacleMask))
            {
                allowedStep = Mathf.Min(allowedStep, Mathf.Max(0f, hit.distance - collisionSkin));
            }
        }

        Vector3 nextPosition = body.position + travelDirection * allowedStep;
        if (allowedStep > 0f &&
            TankCollisionGuard.CanOccupy(hull, nextPosition, nextRotation, obstacleMask))
        {
            body.MovePosition(nextPosition);
        }
        else
        {
            nextPosition = body.position;
        }

        TankCollisionGuard.ReservePose(hull, nextPosition, nextRotation);
    }

    private void ResolveGrid()
    {
        if (grid == null)
        {
            grid = TankGrid.Instance;
        }

        if (grid == null)
        {
            grid = FindFirstObjectByType<TankGrid>();
        }
    }
}
