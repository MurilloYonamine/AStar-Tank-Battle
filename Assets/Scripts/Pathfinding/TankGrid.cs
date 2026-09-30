using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds the logical grid used by enemy tanks and marks blocked cells with
/// Physics.CheckBox. The grid uses only four orthogonal neighbours.
/// </summary>
[DefaultExecutionOrder(-1000)]
public sealed class TankGrid : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField, Min(1)] private int width = 20;
    [SerializeField, Min(1)] private int depth = 20;
    [SerializeField, Min(0.1f)] private float cellSize = 5f;
    [SerializeField] private Vector3 gridCenter = Vector3.zero;

    [Header("Obstacle detection")]
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField, Min(0.1f)] private float obstacleCheckHeight = 3f;
    [SerializeField, Range(0.1f, 1f)] private float obstacleCheckScale = 0.9f;

    [Header("Debug")]
    [SerializeField] private bool drawGrid = true;
    [SerializeField] private bool drawOnlyWhenSelected = true;
    [SerializeField] private Color walkableColor = new(0.2f, 0.8f, 0.3f, 0.18f);
    [SerializeField] private Color blockedColor = new(0.9f, 0.15f, 0.15f, 0.45f);
    [SerializeField] private Color pathColor = new(1f, 0.85f, 0.1f, 0.75f);

    private GridNode[,] nodes;
    private readonly List<GridNode> debugPath = new();

    public static TankGrid Instance { get; private set; }
    public int Width => width;
    public int Depth => depth;
    public float CellSize => cellSize;
    public bool IsBuilt => nodes != null;

    private Vector3 Origin => transform.position + gridCenter -
                              new Vector3(width * cellSize, 0f, depth * cellSize) * 0.5f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("There is more than one TankGrid in the scene. The newest one will be ignored.", this);
            return;
        }

        Instance = this;
        BuildGrid();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    [ContextMenu("Rebuild Grid")]
    public void BuildGrid()
    {
        nodes = new GridNode[width, depth];
        debugPath.Clear();

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                Vector3 worldPosition = GetCellCenter(x, z);
                bool isWalkable = !IsCellBlocked(worldPosition);
                nodes[x, z] = new GridNode(x, z, worldPosition, isWalkable);
            }
        }

        ConnectOrthogonalNeighbours();
    }

    public GridNode GetNode(int x, int z)
    {
        if (nodes == null || x < 0 || x >= width || z < 0 || z >= depth)
        {
            return null;
        }

        return nodes[x, z];
    }

    public GridNode GetNodeFromWorldPosition(Vector3 worldPosition)
    {
        if (nodes == null)
        {
            BuildGrid();
        }

        Vector3 local = worldPosition - Origin;
        int x = Mathf.Clamp(Mathf.FloorToInt(local.x / cellSize), 0, width - 1);
        int z = Mathf.Clamp(Mathf.FloorToInt(local.z / cellSize), 0, depth - 1);
        return nodes[x, z];
    }

    public IEnumerable<GridNode> GetAllNodes()
    {
        if (nodes == null)
        {
            yield break;
        }

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                yield return nodes[x, z];
            }
        }
    }

    public void SetDebugPath(IReadOnlyList<GridNode> path)
    {
        debugPath.Clear();

        if (path == null)
        {
            return;
        }

        for (int i = 0; i < path.Count; i++)
        {
            debugPath.Add(path[i]);
        }
    }

    private Vector3 GetCellCenter(int x, int z)
    {
        return Origin + new Vector3((x + 0.5f) * cellSize, 0f, (z + 0.5f) * cellSize);
    }

    private bool IsCellBlocked(Vector3 worldPosition)
    {
        Vector3 halfExtents = new(
            cellSize * obstacleCheckScale * 0.5f,
            obstacleCheckHeight * 0.5f,
            cellSize * obstacleCheckScale * 0.5f);

        Vector3 checkCenter = worldPosition + Vector3.up * (obstacleCheckHeight * 0.5f);

        return Physics.CheckBox(
            checkCenter,
            halfExtents,
            Quaternion.identity,
            obstacleMask,
            QueryTriggerInteraction.Ignore);
    }

    private void ConnectOrthogonalNeighbours()
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                GridNode node = nodes[x, z];
                TryAddNeighbour(node, x - 1, z);
                TryAddNeighbour(node, x + 1, z);
                TryAddNeighbour(node, x, z - 1);
                TryAddNeighbour(node, x, z + 1);
            }
        }
    }

    private void TryAddNeighbour(GridNode node, int x, int z)
    {
        GridNode neighbour = GetNode(x, z);

        if (neighbour != null)
        {
            node.AddNeighbour(neighbour);
        }
    }

    private void OnDrawGizmos()
    {
        if (drawGrid && !drawOnlyWhenSelected)
        {
            DrawGridGizmos();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (drawGrid && drawOnlyWhenSelected)
        {
            DrawGridGizmos();
        }
    }

    private void DrawGridGizmos()
    {
        Vector3 cubeSize = new(cellSize * 0.92f, 0.12f, cellSize * 0.92f);

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                GridNode node = nodes != null ? nodes[x, z] : null;
                Vector3 position = node != null ? node.WorldPosition : GetCellCenter(x, z);
                bool isWalkable = node == null || node.IsWalkable;

                Gizmos.color = isWalkable ? walkableColor : blockedColor;
                Gizmos.DrawCube(position + Vector3.up * 0.06f, cubeSize);
            }
        }

        Gizmos.color = pathColor;

        for (int i = 0; i < debugPath.Count; i++)
        {
            Vector3 position = debugPath[i].WorldPosition + Vector3.up * 0.2f;
            Gizmos.DrawCube(position, cubeSize * 0.65f);

            if (i > 0)
            {
                Gizmos.DrawLine(debugPath[i - 1].WorldPosition + Vector3.up * 0.2f, position);
            }
        }
    }
}
