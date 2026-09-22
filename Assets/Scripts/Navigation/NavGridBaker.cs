using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds a NavGrid from the environment colliders in the scene.
///   1. Occupancy - marks cells overlapping the environment as blocked.
///   2. Enclosure - optionally marks open cells unreachable from a seed point as blocked.
///   3. Classify  - derives Ground, Edge and Wall flags and clearance from neighbors.
/// </summary>
public static class NavGridBaker
{
    public struct BakeResult
    {
        public int BlockedCount;
        public int EnclosedCount;
        public int GroundCount;
        public int EdgeCount;
        public float ElapsedMilliseconds;
    }


    public static BakeResult Bake(NavGrid grid, LayerMask environmentMask, float zHalfExtent, bool fillEnclosedRegions, Vector3 enclosureSeed, float maxClearance)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        BakeResult result = new BakeResult();

        MarkOccupancy(grid, environmentMask, zHalfExtent, ref result);

        if (fillEnclosedRegions)
            MarkEnclosedRegions(grid, enclosureSeed, ref result);

        Classify(grid, maxClearance, ref result);

        stopwatch.Stop();
        result.ElapsedMilliseconds = (float)stopwatch.Elapsed.TotalMilliseconds;

        return result;
    }


    #region Pass 1 - Occupancy

    /// <summary>Marks every cell that overlaps the environment as blocked.</summary>
    private static void MarkOccupancy(NavGrid grid, LayerMask environmentMask, float zHalfExtent, ref BakeResult result)
    {
        Vector3 halfExtents = new Vector3(grid.Spacing * 0.5f, grid.Spacing * 0.5f, zHalfExtent);

        for (int row = 0; row < grid.Rows; row++)
        {
            for (int column = 0; column < grid.Columns; column++)
            {
                Vector3 position = grid.NodePosition(column, row);

                if (!Physics.CheckBox(position, halfExtents, Quaternion.identity, environmentMask, QueryTriggerInteraction.Ignore))
                    continue;

                grid.AddFlag(grid.Index(column, row), NavNodeFlags.Blocked);
                result.BlockedCount++;
            }
        }
    }

    #endregion


    #region Pass 2 - Enclosure

    /// <summary>Marks open cells that cannot be reached from the seed as blocked.</summary>
    private static void MarkEnclosedRegions(NavGrid grid, Vector3 enclosureSeed, ref BakeResult result)
    {
        bool[] reachable = new bool[grid.NodeCount];
        Queue<int> frontier = new Queue<int>();

        if (!grid.TryGetCoordinates(enclosureSeed, out int seedColumn, out int seedRow))
        {
            Debug.LogWarning($"NavGridBaker: enclosure seed {enclosureSeed} is outside the grid bounds. Skipping enclosure fill.");
            return;
        }

        if (grid.Get(seedColumn, seedRow).IsBlocked)
        {
            Debug.LogWarning($"NavGridBaker: enclosure seed {enclosureSeed} sits inside environment geometry. Move it into open playable space. Skipping enclosure fill.");
            return;
        }

        TryVisit(grid, reachable, frontier, seedColumn, seedRow);

        while (frontier.Count > 0)
        {
            int index = frontier.Dequeue();
            int column = grid.ColumnOf(index);
            int row = grid.RowOf(index);

            TryVisit(grid, reachable, frontier, column - 1, row);
            TryVisit(grid, reachable, frontier, column + 1, row);
            TryVisit(grid, reachable, frontier, column, row - 1);
            TryVisit(grid, reachable, frontier, column, row + 1);
        }

        for (int index = 0; index < grid.NodeCount; index++)
        {
            if (reachable[index] || grid.Get(index).IsBlocked)
                continue;

            grid.AddFlag(index, NavNodeFlags.Blocked);
            result.EnclosedCount++;
            result.BlockedCount++;
        }
    }

    private static void TryVisit(NavGrid grid, bool[] reachable, Queue<int> frontier, int column, int row)
    {
        if (!grid.InBounds(column, row))
            return;

        int index = grid.Index(column, row);

        if (reachable[index] || grid.Get(index).IsBlocked)
            return;

        reachable[index] = true;
        frontier.Enqueue(index);
    }

    #endregion


    #region Pass 3 - Classification

    /// <summary>Flags walls and ground, measures clearance, then marks edges.</summary>
    private static void Classify(NavGrid grid, float maxClearance, ref BakeResult result)
    {
        int maxClearanceCells = Mathf.Max(1, Mathf.CeilToInt(maxClearance / grid.Spacing));

        for (int row = 0; row < grid.Rows; row++)
        {
            for (int column = 0; column < grid.Columns; column++)
            {
                int index = grid.Index(column, row);

                if (grid.Get(index).IsBlocked)
                    continue;

                if (grid.IsBlockedOrOutside(column - 1, row))
                    grid.AddFlag(index, NavNodeFlags.WallLeft);

                if (grid.IsBlockedOrOutside(column + 1, row))
                    grid.AddFlag(index, NavNodeFlags.WallRight);

                // Ground: an open cell standing on a blocked cell.
                if (!grid.InBounds(column, row - 1) || !grid.Get(column, row - 1).IsBlocked)
                    continue;

                grid.AddFlag(index, NavNodeFlags.Ground);
                grid.SetClearance(index, MeasureClearance(grid, column, row, maxClearanceCells));
                result.GroundCount++;
            }
        }

        MarkEdges(grid, ref result);
    }

    /// <summary>Open space above a cell in world units, up to maxClearanceCells.</summary>
    private static float MeasureClearance(NavGrid grid, int column, int row, int maxClearanceCells)
    {
        int openCells = 0;

        for (int offset = 0; offset < maxClearanceCells; offset++)
        {
            if (grid.IsBlockedOrOutside(column, row + offset))
                break;

            openCells++;
        }

        return openCells * grid.Spacing;
    }

    /// <summary>
    /// Flags ground cells with no ground continuing to the left or right. Ground on the same row or
    /// one row up counts as continuing.
    /// </summary>
    private static void MarkEdges(NavGrid grid, ref BakeResult result)
    {
        for (int index = 0; index < grid.NodeCount; index++)
        {
            if (!grid.Get(index).IsGround)
                continue;

            int column = grid.ColumnOf(index);
            int row = grid.RowOf(index);

            bool leftContinues = grid.IsGroundInBounds(column - 1, row) || grid.IsGroundInBounds(column - 1, row + 1);
            bool rightContinues = grid.IsGroundInBounds(column + 1, row) || grid.IsGroundInBounds(column + 1, row + 1);

            if (!leftContinues)
            {
                grid.AddFlag(index, NavNodeFlags.LeftEdge);
                result.EdgeCount++;
            }

            if (!rightContinues)
            {
                grid.AddFlag(index, NavNodeFlags.RightEdge);
                result.EdgeCount++;
            }
        }
    }

    #endregion
}
