using UnityEngine;

/// <summary>
/// Classification flags for a single nav grid cell.
/// </summary>
[System.Flags]
public enum NavNodeFlags
{
    None = 0,

    /// <summary>Environment occupies this cell, or the cell is sealed inside the environment.</summary>
    Blocked = 1 << 0,

    /// <summary>Open cell with a blocked cell directly below. The only cells paths are built from.</summary>
    Ground = 1 << 1,

    /// <summary>Ground cell with no ground continuing to the left. Jump and fall link source.</summary>
    LeftEdge = 1 << 2,

    /// <summary>Ground cell with no ground continuing to the right. Jump and fall link source.</summary>
    RightEdge = 1 << 3,

    /// <summary>Open cell with blocked environment immediately to its left.</summary>
    WallLeft = 1 << 4,

    /// <summary>Open cell with blocked environment immediately to its right.</summary>
    WallRight = 1 << 5,
}


public struct NavNode
{
    public NavNodeFlags Flags;

    /// <summary>
    /// Open vertical space above this cell in world units. Only meaningful on Ground cells.
    /// Stored in world units so any creature size can be tested against one bake.
    /// </summary>
    public float Clearance;

    public bool Has(NavNodeFlags flag) => (Flags & flag) != 0;
    public bool IsBlocked => (Flags & NavNodeFlags.Blocked) != 0;
    public bool IsGround => (Flags & NavNodeFlags.Ground) != 0;
    public bool IsEdge => (Flags & (NavNodeFlags.LeftEdge | NavNodeFlags.RightEdge)) != 0;
    public bool IsWall => (Flags & (NavNodeFlags.WallLeft | NavNodeFlags.WallRight)) != 0;
}


/// <summary>
/// Uniform grid covering a level section. Flat struct array with index math rather than a
/// dictionary of node objects.
///
/// Cells are centered inside their footprint: cell (0,0) sits at Origin + (spacing/2, spacing/2),
/// so adjacent cells tile the level exactly with no gaps or overlap.
///
/// Solid geometry rounds UP to cell boundaries: a cell is blocked if anything overlaps it at all.
/// </summary>
public class NavGrid
{
    public readonly Vector3 Origin;
    public readonly float Spacing;
    public readonly int Columns;
    public readonly int Rows;

    private readonly NavNode[] nodes;

    public int NodeCount => nodes.Length;


    public NavGrid(Vector3 origin, float spacing, int columns, int rows)
    {
        Origin = origin;
        Spacing = spacing;
        Columns = Mathf.Max(1, columns);
        Rows = Mathf.Max(1, rows);

        nodes = new NavNode[Columns * Rows];
    }


    #region Index Math

    public int Index(int column, int row) => row * Columns + column;
    public int ColumnOf(int index) => index % Columns;
    public int RowOf(int index) => index / Columns;

    public bool InBounds(int column, int row)
    {
        return column >= 0 && column < Columns && row >= 0 && row < Rows;
    }

    public Vector3 NodePosition(int column, int row)
    {
        return Origin + new Vector3((column + 0.5f) * Spacing, (row + 0.5f) * Spacing, 0f);
    }

    public Vector3 NodePosition(int index) => NodePosition(ColumnOf(index), RowOf(index));

    /// <summary>
    /// World height of the surface a ground cell stands on: the bottom of the cell, which is the
    /// top of the blocked cell beneath it.
    /// </summary>
    public float SurfaceY(int index) => NodePosition(index).y - Spacing * 0.5f;

    /// <summary>
    /// Convert a world position to grid coordinates. Returns false if the position falls outside
    /// the baked bounds.
    /// </summary>
    public bool TryGetCoordinates(Vector3 worldPosition, out int column, out int row)
    {
        column = Mathf.FloorToInt((worldPosition.x - Origin.x) / Spacing);
        row = Mathf.FloorToInt((worldPosition.y - Origin.y) / Spacing);

        return InBounds(column, row);
    }

    #endregion


    #region Access

    public NavNode Get(int index) => nodes[index];
    public NavNode Get(int column, int row) => nodes[Index(column, row)];

    public void AddFlag(int index, NavNodeFlags flag) => nodes[index].Flags |= flag;
    public void SetClearance(int index, float clearance) => nodes[index].Clearance = clearance;

    /// <summary>
    /// Flag test that treats out of bounds as blocked, so classification never falls off the edge
    /// of the grid into a false open cell.
    /// </summary>
    public bool IsBlockedOrOutside(int column, int row)
    {
        if (!InBounds(column, row))
            return true;

        return nodes[Index(column, row)].IsBlocked;
    }

    public bool IsGroundInBounds(int column, int row)
    {
        return InBounds(column, row) && nodes[Index(column, row)].IsGround;
    }

    #endregion


    #region Queries

    /// <summary>
    /// Find the first ground cell at or below a world position, scanning straight down in that
    /// position's own column only.
    /// </summary>
    public bool TryGetGroundBelow(Vector3 worldPosition, float maxDrop, out int index)
    {
        index = -1;

        int column = Mathf.FloorToInt((worldPosition.x - Origin.x) / Spacing);
        int startRow = Mathf.Min(Mathf.FloorToInt((worldPosition.y - Origin.y) / Spacing), Rows - 1);
        int lowestRow = Mathf.Max(0, startRow - Mathf.CeilToInt(maxDrop / Spacing));

        for (int row = startRow; row >= lowestRow; row--)
        {
            if (!IsGroundInBounds(column, row))
                continue;

            index = Index(column, row);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Find the nearest ground cell to a world position in any direction.
    /// </summary>
    public bool TryGetNearestGround(Vector3 worldPosition, float searchRadius, out int index)
    {
        index = -1;

        int startColumn = Mathf.FloorToInt((worldPosition.x - Origin.x) / Spacing);
        int startRow = Mathf.FloorToInt((worldPosition.y - Origin.y) / Spacing);
        int radius = Mathf.Max(1, Mathf.CeilToInt(searchRadius / Spacing));

        float bestSqrDistance = float.PositiveInfinity;

        for (int columnOffset = -radius; columnOffset <= radius; columnOffset++)
        {
            int column = startColumn + columnOffset;

            if (column < 0 || column >= Columns)
                continue;

            for (int rowOffset = 0; rowOffset <= radius; rowOffset++)
            {
                if (TryConsiderGround(column, startRow - rowOffset, worldPosition, ref bestSqrDistance, ref index))
                    break;

                if (rowOffset != 0 && TryConsiderGround(column, startRow + rowOffset, worldPosition, ref bestSqrDistance, ref index))
                    break;
            }
        }

        return index >= 0;
    }

    private bool TryConsiderGround(int column, int row, Vector3 worldPosition, ref float bestSqrDistance, ref int bestIndex)
    {
        if (!IsGroundInBounds(column, row))
            return false;

        float sqrDistance = (NodePosition(column, row) - worldPosition).sqrMagnitude;

        if (sqrDistance < bestSqrDistance)
        {
            bestSqrDistance = sqrDistance;
            bestIndex = Index(column, row);
        }

        //NOTE: Stop scanning this column. Anything further away in the same column loses by definition.
        return true;
    }

    #endregion
}
