using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents one logical cell of the navigation grid.
/// This is a plain C# class because grid cells do not need GameObjects.
/// </summary>
public sealed class GridNode
{
    private readonly List<GridNode> neighbours = new(4);

    public int X { get; }
    public int Z { get; }
    public Vector3 WorldPosition { get; }
    public bool IsWalkable { get; private set; }
    public int MovementCost { get; }
    public IReadOnlyList<GridNode> Neighbours => neighbours;

    // Values used by A*. They are reset before every search.
    public int GCost { get; set; }
    public int HCost { get; set; }
    public int FCost => GCost + HCost;
    public GridNode Parent { get; set; }

    public GridNode(int x, int z, Vector3 worldPosition, bool isWalkable, int movementCost = 10)
    {
        X = x;
        Z = z;
        WorldPosition = worldPosition;
        IsWalkable = isWalkable;
        MovementCost = Mathf.Max(1, movementCost);
        ResetPathData();
    }

    public void SetWalkable(bool isWalkable)
    {
        IsWalkable = isWalkable;
    }

    public void AddNeighbour(GridNode neighbour)
    {
        if (neighbour != null && neighbour != this && !neighbours.Contains(neighbour))
        {
            neighbours.Add(neighbour);
        }
    }

    public void ResetPathData()
    {
        GCost = int.MaxValue;
        HCost = 0;
        Parent = null;
    }
}
