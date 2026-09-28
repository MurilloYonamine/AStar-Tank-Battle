using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rotates and moves an enemy tank through the sequence of nodes returned by A*.
/// Decision making and path recalculation intentionally remain outside this class.
/// </summary>
public sealed class EnemyTankMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TankGrid grid;

    [Header("Movement")]
    [SerializeField, Min(0.1f)] private float moveSpeed = 3f;
    [SerializeField, Min(1f)] private float rotationSpeed = 180f;
    [SerializeField, Min(0.01f)] private float nodeReachDistance = 0.2f;

    private readonly AStarPathfinder pathfinder = new();
    private readonly List<GridNode> currentPath = new();
    private int currentNodeIndex;

    public bool IsMoving => currentNodeIndex < currentPath.Count;
    public IReadOnlyList<GridNode> CurrentPath => currentPath;

    private void Awake()
    {
        ResolveGrid();
    }

    private void Update()
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
        targetPosition.y = transform.position.y;

        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime);
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) <= nodeReachDistance)
        {
            transform.position = targetPosition;
            currentNodeIndex++;
        }
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
