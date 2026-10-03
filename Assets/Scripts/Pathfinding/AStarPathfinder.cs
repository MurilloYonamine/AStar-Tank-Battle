using System.Collections.Generic;

/// <summary>
/// A* implementation adapted from the group's AStar-Pathfinding-3D project.
/// It uses g, h and f costs, an open list, a closed set and parent references.
/// Reference: AStar-Pathfinding-3D/Assets/Scripts/Pathfinding/AStar.cs,
/// lines 30-106 (Run: open/closed lists, costs and predecessors), 108-117 (path reconstruction).
/// These ideas were adapted to reuse the group's class exercise, not a ready-made pathfinding package.
/// Adaptations: logical cells, Manhattan heuristic for four neighbours, forward node order,
/// optional temporary hull obstacles and optional start node for centering within a cell.
/// </summary>
public sealed class AStarPathfinder
{
    private readonly List<GridNode> openNodes = new();
    private readonly HashSet<GridNode> closedNodes = new();

    public bool TryFindPath(
        TankGrid grid,
        GridNode startNode,
        GridNode destinationNode,
        out List<GridNode> path,
        ISet<GridNode> temporaryObstacles = null,
        bool includeStartNode = false)
    {
        path = new List<GridNode>();

        if (grid == null ||
            startNode == null ||
            destinationNode == null ||
            !startNode.IsWalkable ||
            !destinationNode.IsWalkable)
        {
            return false;
        }

        ResetSearchData(grid);
        openNodes.Clear();
        closedNodes.Clear();

        startNode.GCost = 0;
        startNode.HCost = CalculateManhattanDistance(startNode, destinationNode);
        openNodes.Add(startNode);

        while (openNodes.Count > 0)
        {
            GridNode currentNode = GetLowestCostNode();

            if (currentNode == destinationNode)
            {
                path = ReconstructPath(startNode, destinationNode, includeStartNode);
                return true;
            }

            openNodes.Remove(currentNode);
            closedNodes.Add(currentNode);

            foreach (GridNode neighbour in currentNode.Neighbours)
            {
                if (!neighbour.IsWalkable || closedNodes.Contains(neighbour) ||
                    temporaryObstacles != null && temporaryObstacles.Contains(neighbour))
                {
                    continue;
                }

                int tentativeGCost = currentNode.GCost + neighbour.MovementCost;

                if (tentativeGCost >= neighbour.GCost)
                {
                    continue;
                }

                neighbour.Parent = currentNode;
                neighbour.GCost = tentativeGCost;
                neighbour.HCost = CalculateManhattanDistance(neighbour, destinationNode);

                if (!openNodes.Contains(neighbour))
                {
                    openNodes.Add(neighbour);
                }
            }
        }

        return false;
    }

    private static void ResetSearchData(TankGrid grid)
    {
        foreach (GridNode node in grid.GetAllNodes())
        {
            node.ResetPathData();
        }
    }

    private GridNode GetLowestCostNode()
    {
        GridNode bestNode = openNodes[0];

        for (int i = 1; i < openNodes.Count; i++)
        {
            GridNode candidate = openNodes[i];

            if (candidate.FCost < bestNode.FCost ||
                candidate.FCost == bestNode.FCost && candidate.HCost < bestNode.HCost)
            {
                bestNode = candidate;
            }
        }

        return bestNode;
    }

    private static int CalculateManhattanDistance(GridNode a, GridNode b)
    {
        return (System.Math.Abs(a.X - b.X) + System.Math.Abs(a.Z - b.Z)) * 10;
    }

    private static List<GridNode> ReconstructPath(GridNode startNode, GridNode destinationNode, bool includeStartNode)
    {
        List<GridNode> path = new();
        GridNode currentNode = destinationNode;

        while (currentNode != null && currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.Parent;
        }

        if (currentNode != startNode)
        {
            path.Clear();
            return path;
        }

        if (includeStartNode) path.Add(startNode);
        path.Reverse();
        return path;
    }
}
