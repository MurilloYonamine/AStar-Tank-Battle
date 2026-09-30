using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rotates and moves an enemy tank through the sequence of nodes returned by A*.
/// Decision making and path recalculation intentionally remain outside this class.
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

    private readonly AStarPathfinder pathfinder = new();
    private readonly List<GridNode> currentPath = new();
    private int currentNodeIndex;
    private Rigidbody body;
    private BoxCollider hull;

    public bool IsMoving => currentNodeIndex < currentPath.Count;
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

        FollowPath();
    }

    public bool SetDestination(Vector3 destination)
    {
        ResolveGrid();

        if (grid == null || !grid.IsBuilt)
        {
            Debug.LogWarning("EnemyTankMovement could not find a built TankGrid.", this);
            return false;
        }

        GridNode startNode = grid.GetNodeFromWorldPosition(transform.position);
        GridNode destinationNode = grid.GetNodeFromWorldPosition(destination);

        currentPath.Clear();
        currentNodeIndex = 0;

        if (!pathfinder.TryFindPath(grid, startNode, destinationNode, out List<GridNode> newPath))
        {
            grid.SetDebugPath(null);
            return false;
        }

        currentPath.AddRange(newPath);
        grid.SetDebugPath(currentPath);
        return true;
    }

    public void Stop()
    {
        currentPath.Clear();
        currentNodeIndex = 0;

        if (grid != null)
        {
            grid.SetDebugPath(null);
        }
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
