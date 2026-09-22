using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bakes jump and fall links. Each link stores requirements that agents test against their own
/// envelope during search.
///   Landing zone - from a ledge, every surface it can jump or fall to.
///   Takeoff zone - onto a ledge, every lower surface it can be jumped up to from.
/// </summary>
public static class NavJumpLinkBaker
{
    public struct Settings
    {
        public float MaxReach;
        public float MaxRise;
        public float MaxDrop;
        public float MinGap;
        public float CandidateInterval;
        public int MaxCandidatesPerLedge;
        public float CostMultiplier;
        public float FallCostMultiplier;
    }


    public static void Bake(NavGrid grid, NavLinkTable table, Settings settings, bool verbose)
    {
        for (int index = 0; index < grid.NodeCount; index++)
        {
            NavNode node = grid.Get(index);

            if (!node.IsGround || !node.IsEdge)
                continue;

            if (node.Has(NavNodeFlags.RightEdge))
                BakeLandingZone(grid, table, index, 1, settings, verbose);

            if (node.Has(NavNodeFlags.LeftEdge))
                BakeLandingZone(grid, table, index, -1, settings, verbose);
        }

        // Takeoff zones run second and skip links a landing zone already made.
        for (int index = 0; index < grid.NodeCount; index++)
        {
            NavNode node = grid.Get(index);

            if (!node.IsGround || !node.IsEdge)
                continue;

            if (node.Has(NavNodeFlags.RightEdge))
                BakeTakeoffZone(grid, table, index, 1, settings, verbose);

            if (node.Has(NavNodeFlags.LeftEdge))
                BakeTakeoffZone(grid, table, index, -1, settings, verbose);
        }
    }


    #region Zones

    /// <summary>
    /// Links a ledge to the topmost ground in each sampled column outward from it. Each downward
    /// link also gets a fall link where one is possible.
    /// </summary>
    private static void BakeLandingZone(NavGrid grid, NavLinkTable table, int ledgeNode, int directionX, Settings settings, bool verbose)
    {
        int ledgeColumn = grid.ColumnOf(ledgeNode);
        int ledgeRow = grid.RowOf(ledgeNode);

        int columnStep = ColumnStep(grid, settings);
        int minColumns = Mathf.Max(1, Mathf.CeilToInt(settings.MinGap / grid.Spacing));
        int maxColumns = Mathf.CeilToInt(settings.MaxReach / grid.Spacing);

        int topRow = ledgeRow + Mathf.CeilToInt(settings.MaxRise / grid.Spacing);
        int bottomRow = ledgeRow - Mathf.CeilToInt(settings.MaxDrop / grid.Spacing);

        int built = 0;

        for (int offset = minColumns; offset <= maxColumns && built < settings.MaxCandidatesPerLedge; offset += columnStep)
        {
            int column = ledgeColumn + directionX * offset;

            if (column < 0 || column >= grid.Columns)
                break;

            if (!TryFindTopGround(grid, column, topRow, bottomRow, out int landingNode))
                continue;

            if (!TryBuildJumpLink(grid, ledgeNode, landingNode, settings, out NavLink jump))
                continue;

            table.Add(jump);
            built++;

            if (TryBuildFallLink(grid, jump, settings, out NavLink fall))
                table.Add(fall);
        }

        if (verbose)
            Debug.Log($"NavJumpLinkBaker: landing zone from ledge ({ledgeColumn},{ledgeRow}), flags {grid.Get(ledgeNode).Flags}, " +
                      $"direction {directionX}: {built} links.");
    }

    /// <summary>
    /// Links the topmost ground in each sampled column outward from and below a ledge up onto the
    /// ledge. Starts in the column beside the ledge.
    /// </summary>
    private static void BakeTakeoffZone(NavGrid grid, NavLinkTable table, int ledgeNode, int outwardX, Settings settings, bool verbose)
    {
        int ledgeColumn = grid.ColumnOf(ledgeNode);
        int ledgeRow = grid.RowOf(ledgeNode);

        int columnStep = ColumnStep(grid, settings);
        int maxColumns = Mathf.CeilToInt(settings.MaxReach / grid.Spacing);

        int topRow = ledgeRow - 1;
        int bottomRow = ledgeRow - Mathf.CeilToInt(settings.MaxRise / grid.Spacing);

        int built = 0;

        for (int offset = 1; offset <= maxColumns && built < settings.MaxCandidatesPerLedge; offset += columnStep)
        {
            int column = ledgeColumn + outwardX * offset;

            if (column < 0 || column >= grid.Columns)
                break;

            if (!TryFindTopGround(grid, column, topRow, bottomRow, out int launchNode))
                continue;

            if (LinkExists(table, launchNode, ledgeNode))
                continue;

            if (!TryBuildJumpLink(grid, launchNode, ledgeNode, settings, out NavLink jump))
                continue;

            table.Add(jump);
            built++;
        }

        if (verbose)
            Debug.Log($"NavJumpLinkBaker: takeoff zone onto ledge ({ledgeColumn},{ledgeRow}), reaching {outwardX}: {built} links.");
    }

    private static int ColumnStep(NavGrid grid, Settings settings)
    {
        return Mathf.Max(1, Mathf.RoundToInt(settings.CandidateInterval / grid.Spacing));
    }

    /// <summary>First ground cell scanning down a column between two rows, inclusive.</summary>
    private static bool TryFindTopGround(NavGrid grid, int column, int topRow, int bottomRow, out int node)
    {
        for (int row = topRow; row >= bottomRow; row--)
        {
            if (!grid.IsGroundInBounds(column, row))
                continue;

            node = grid.Index(column, row);
            return true;
        }

        node = -1;
        return false;
    }

    #endregion


    #region Links

    /// <summary>Builds a jump link between two cells. Fails when the two are already connected by walking.</summary>
    private static bool TryBuildJumpLink(NavGrid grid, int fromNode, int toNode, Settings settings, out NavLink link)
    {
        link = default;

        float fromSurfaceY = grid.SurfaceY(fromNode);
        float toSurfaceY = grid.SurfaceY(toNode);

        float dx = grid.NodePosition(toNode).x - grid.NodePosition(fromNode).x;
        float dy = toSurfaceY - fromSurfaceY;

        if (Mathf.Abs(dy) < grid.Spacing && IsWalkable(grid, fromNode, toNode))
            return false;

        link = new NavLink
        {
            Type = NavLinkType.Jump,
            FromNode = fromNode,
            ToNode = toNode,
            Dx = dx,
            Dy = dy,
            RequiredApex = MeasureRequiredApex(grid, fromNode, toNode, fromSurfaceY, toSurfaceY, settings.MaxRise),
            Cost = Vector3.Distance(grid.NodePosition(fromNode), grid.NodePosition(toNode)) * settings.CostMultiplier,
        };

        return true;
    }

    /// <summary>Copies a downward jump as a fall link when nothing between is taller than the ledge.</summary>
    private static bool TryBuildFallLink(NavGrid grid, NavLink jump, Settings settings, out NavLink fall)
    {
        fall = default;

        if (jump.Dy >= -grid.Spacing || jump.RequiredApex > 0f)
            return false;

        fall = jump;
        fall.Type = NavLinkType.Fall;
        fall.Cost = Mathf.Sqrt(jump.Dx * jump.Dx + jump.Dy * jump.Dy) * settings.FallCostMultiplier;

        return true;
    }

    /// <summary>True when a jump link already connects the two cells.</summary>
    private static bool LinkExists(NavLinkTable table, int fromNode, int toNode)
    {
        if (!table.TryGetLinks(fromNode, out List<NavLink> links))
            return false;

        for (int i = 0; i < links.Count; i++)
        {
            if (links[i].ToNode == toNode && links[i].Type == NavLinkType.Jump)
                return true;
        }

        return false;
    }

    /// <summary>True when an unbroken run of ground on one row connects the two cells.</summary>
    private static bool IsWalkable(NavGrid grid, int fromNode, int toNode)
    {
        int row = grid.RowOf(fromNode);

        if (grid.RowOf(toNode) != row)
            return false;

        int start = Mathf.Min(grid.ColumnOf(fromNode), grid.ColumnOf(toNode));
        int end = Mathf.Max(grid.ColumnOf(fromNode), grid.ColumnOf(toNode));

        for (int column = start; column <= end; column++)
        {
            if (!grid.IsGroundInBounds(column, row))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Height above the launch surface of the tallest obstruction between the two ends, searched
    /// from the lower surface up to the maximum rise. Zero unless it is taller than both surfaces.
    /// </summary>
    private static float MeasureRequiredApex(NavGrid grid, int fromNode, int toNode, float fromSurfaceY, float toSurfaceY, float maxRise)
    {
        int start = Mathf.Min(grid.ColumnOf(fromNode), grid.ColumnOf(toNode)) + 1;
        int end = Mathf.Max(grid.ColumnOf(fromNode), grid.ColumnOf(toNode)) - 1;

        int topRow = Mathf.Clamp(Mathf.FloorToInt((fromSurfaceY + maxRise - grid.Origin.y) / grid.Spacing), 0, grid.Rows - 1);
        int bottomRow = Mathf.Clamp(Mathf.FloorToInt((Mathf.Min(fromSurfaceY, toSurfaceY) - grid.Origin.y) / grid.Spacing), 0, grid.Rows - 1);

        float highest = 0f;

        for (int column = start; column <= end; column++)
        {
            for (int row = topRow; row >= bottomRow; row--)
            {
                if (!grid.Get(column, row).IsBlocked)
                    continue;

                float top = grid.NodePosition(column, row).y + grid.Spacing * 0.5f;
                highest = Mathf.Max(highest, top - fromSurfaceY);
                break;
            }
        }

        float higherSurface = Mathf.Max(0f, toSurfaceY - fromSurfaceY);

        return highest > higherSurface ? highest : 0f;
    }

    #endregion
}
