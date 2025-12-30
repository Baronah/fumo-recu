// Hybrid approach - keeping your existing PathfindingGrid but with simplified A* logic
using System.Collections.Generic;
using UnityEngine;

public class PathfindingGrid
{
    private Dictionary<Vector2Int, bool> obstacleGrid = new Dictionary<Vector2Int, bool>();
    private Dictionary<Vector2Int, PathCell> pathCells = new Dictionary<Vector2Int, PathCell>();

    private float cellSize;
    private LayerMask obstacleLayer;
    private Vector2 lastUpdateCenter = Vector2.zero;
    private float lastUpdateRadius = 0f;

    // Cached collections for performance
    private List<Vector2Int> cellsToSearch = new List<Vector2Int>(256);
    private HashSet<Vector2Int> searchedCells = new HashSet<Vector2Int>(256);
    private List<Vector2> finalPath = new List<Vector2>(64);

    // Neighbor directions
    private static readonly Vector2Int[] neighborOffsets = {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
        Vector2Int.up + Vector2Int.right, Vector2Int.up + Vector2Int.left,
        Vector2Int.down + Vector2Int.right, Vector2Int.down + Vector2Int.left
    };

    public PathfindingGrid(float cellSize, LayerMask obstacleLayer)
    {
        this.cellSize = cellSize;
        this.obstacleLayer = obstacleLayer;
    }

    public Vector2Int WorldToGrid(Vector2 worldPos)
    {
        return new Vector2Int(
            Mathf.RoundToInt(worldPos.x / cellSize),
            Mathf.RoundToInt(worldPos.y / cellSize)
        );
    }

    public Vector2 GridToWorld(Vector2Int gridPos)
    {
        return new Vector2(gridPos.x * cellSize, gridPos.y * cellSize);
    }

    public void UpdateGrid(Vector2 center, float radius)
    {
        if (Vector2.Distance(center, lastUpdateCenter) < cellSize &&
            Mathf.Abs(radius - lastUpdateRadius) < cellSize * 0.5f)
            return;

        lastUpdateCenter = center;
        lastUpdateRadius = radius;

        // Clean up old data
        var keysToRemove = new List<Vector2Int>();
        float cleanupRadius = radius + cellSize * 3f;

        foreach (var kvp in obstacleGrid)
        {
            Vector2 worldPos = GridToWorld(kvp.Key);
            if (Vector2.Distance(worldPos, center) > cleanupRadius)
            {
                keysToRemove.Add(kvp.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            obstacleGrid.Remove(key);
            pathCells.Remove(key);
        }

        // Add new cells
        int gridRadius = Mathf.CeilToInt(radius / cellSize);
        Vector2Int centerGrid = WorldToGrid(center);

        for (int x = -gridRadius; x <= gridRadius; x++)
        {
            for (int y = -gridRadius; y <= gridRadius; y++)
            {
                Vector2Int gridPos = centerGrid + new Vector2Int(x, y);

                if (!obstacleGrid.ContainsKey(gridPos))
                {
                    Vector2 worldPos = GridToWorld(gridPos);
                    bool isObstacle = Physics2D.OverlapCircle(worldPos, cellSize * 0.25f, obstacleLayer) != null;
                    obstacleGrid[gridPos] = isObstacle;

                    // Initialize path cell
                    pathCells[gridPos] = new PathCell
                    {
                        position = gridPos,
                        isWall = isObstacle,
                        gCost = int.MaxValue,
                        hCost = 0,
                        fCost = int.MaxValue,
                        connection = Vector2Int.zero
                    };
                }
            }
        }
    }

    public bool IsObstacle(Vector2Int gridPos)
    {
        return obstacleGrid.TryGetValue(gridPos, out bool isObstacle) && isObstacle;
    }

    public List<Vector2> FindPath(Vector2 start, Vector2 target, bool allowDiagonal = true)
    {
        Vector2Int startGrid = WorldToGrid(start);
        Vector2Int targetGrid = WorldToGrid(target);

        // Handle obstacles at start/target
        if (IsObstacle(startGrid))
            startGrid = FindNearestFreeCell(startGrid);
        if (IsObstacle(targetGrid))
            targetGrid = FindNearestFreeCell(targetGrid);

        // Clear previous search
        cellsToSearch.Clear();
        searchedCells.Clear();
        finalPath.Clear();

        // Reset all path cells in search area
        foreach (var cell in pathCells.Values)
        {
            cell.gCost = int.MaxValue;
            cell.hCost = 0;
            cell.fCost = int.MaxValue;
            cell.connection = Vector2Int.zero;
        }

        // Initialize start cell
        if (!pathCells.ContainsKey(startGrid))
            return finalPath; // Empty path if start invalid

        var startCell = pathCells[startGrid];
        startCell.gCost = 0;
        startCell.hCost = GetDistance(startGrid, targetGrid);
        startCell.fCost = startCell.hCost;

        cellsToSearch.Add(startGrid);

        int maxIterations = 500;
        int iterations = 0;

        while (cellsToSearch.Count > 0 && iterations < maxIterations)
        {
            iterations++;

            // Find cell with lowest fCost
            Vector2Int currentCell = cellsToSearch[0];
            foreach (Vector2Int pos in cellsToSearch)
            {
                var cell = pathCells[pos];
                var currentBest = pathCells[currentCell];

                if (cell.fCost < currentBest.fCost ||
                    (cell.fCost == currentBest.fCost && cell.hCost < currentBest.hCost))
                {
                    currentCell = pos;
                }
            }

            cellsToSearch.Remove(currentCell);
            searchedCells.Add(currentCell);

            // Check if we reached the target
            if (currentCell == targetGrid)
            {
                // Reconstruct path
                var pathCell = pathCells[targetGrid];
                while (pathCell.position != startGrid)
                {
                    finalPath.Add(GridToWorld(pathCell.position));
                    pathCell = pathCells[pathCell.connection];
                }
                finalPath.Add(start); // Use actual start position
                finalPath.Reverse();

                return SmoothPath(finalPath, start, target);
            }

            // Search neighbors
            SearchCellNeighbors(currentCell, targetGrid, allowDiagonal);
        }

        return finalPath; // Return empty if no path found
    }

    private void SearchCellNeighbors(Vector2Int cellPos, Vector2Int endPos, bool allowDiagonal)
    {
        int maxNeighbors = allowDiagonal ? neighborOffsets.Length : 4;

        for (int i = 0; i < maxNeighbors; i++)
        {
            Vector2Int neighborPos = cellPos + neighborOffsets[i];

            // Check if this neighbor exists and is valid
            if (!pathCells.TryGetValue(neighborPos, out PathCell neighborCell) ||
                searchedCells.Contains(neighborPos) ||
                neighborCell.isWall)
                continue;

            // For diagonal movement, check corner cutting
            if (i >= 4) // Diagonal indices
            {
                Vector2Int horizontal = cellPos + new Vector2Int(neighborOffsets[i].x, 0);
                Vector2Int vertical = cellPos + new Vector2Int(0, neighborOffsets[i].y);

                if (IsObstacle(horizontal) || IsObstacle(vertical))
                    continue;
            }

            int gCostToNeighbor = pathCells[cellPos].gCost + GetDistance(cellPos, neighborPos);

            if (gCostToNeighbor < neighborCell.gCost)
            {
                neighborCell.connection = cellPos;
                neighborCell.gCost = gCostToNeighbor;
                neighborCell.hCost = GetDistance(neighborPos, endPos);
                neighborCell.fCost = neighborCell.gCost + neighborCell.hCost;

                if (!cellsToSearch.Contains(neighborPos))
                {
                    cellsToSearch.Add(neighborPos);
                }
            }
        }
    }

    private Vector2Int FindNearestFreeCell(Vector2Int gridPos)
    {
        for (int radius = 1; radius <= 5; radius++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    if (Mathf.Abs(x) == radius || Mathf.Abs(y) == radius)
                    {
                        Vector2Int testPos = gridPos + new Vector2Int(x, y);
                        if (pathCells.ContainsKey(testPos) && !pathCells[testPos].isWall)
                        {
                            return testPos;
                        }
                    }
                }
            }
        }
        return gridPos;
    }

    private int GetDistance(Vector2Int a, Vector2Int b)
    {
        int dstX = Mathf.Abs(a.x - b.x);
        int dstY = Mathf.Abs(a.y - b.y);

        if (dstX > dstY)
            return 14 * dstY + 10 * (dstX - dstY);
        return 14 * dstX + 10 * (dstY - dstX);
    }

    private List<Vector2> SmoothPath(List<Vector2> rawPath, Vector2 actualStart, Vector2 actualTarget)
    {
        if (rawPath.Count <= 2)
        {
            var result = new List<Vector2> { actualStart };
            if (rawPath.Count > 0)
                result.Add(actualTarget);
            return result;
        }

        var smoothedPath = new List<Vector2> { actualStart };
        int currentIndex = 0;

        while (currentIndex < rawPath.Count - 1)
        {
            int farthestReachable = currentIndex;

            for (int i = currentIndex + 2; i < rawPath.Count; i++)
            {
                Vector2 startPoint = currentIndex == 0 ? actualStart : rawPath[currentIndex];
                Vector2 direction = (rawPath[i] - startPoint).normalized;
                float distance = Vector2.Distance(startPoint, rawPath[i]);

                RaycastHit2D hit = Physics2D.Raycast(startPoint, direction, distance - 10f, obstacleLayer);
                if (hit.collider == null)
                {
                    farthestReachable = i;
                }
                else
                {
                    break;
                }
            }

            if (farthestReachable > currentIndex)
            {
                currentIndex = farthestReachable;
                smoothedPath.Add(rawPath[currentIndex]);
            }
            else
            {
                currentIndex++;
                if (currentIndex < rawPath.Count)
                    smoothedPath.Add(rawPath[currentIndex]);
            }
        }

        if (smoothedPath.Count > 0 && Vector2.Distance(smoothedPath[smoothedPath.Count - 1], actualTarget) > 10f)
        {
            smoothedPath.Add(actualTarget);
        }

        return smoothedPath;
    }
}

public class PathCell
{
    public Vector2Int position;
    public bool isWall;
    public int gCost;
    public int hCost;
    public int fCost;
    public Vector2Int connection;
}