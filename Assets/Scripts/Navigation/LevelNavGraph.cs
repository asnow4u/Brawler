using UnityEngine;

/// <summary>
/// Owns the nav grid, links and navigator for a level section. The grid starts at this transform
/// and extends up and right by levelSize.
/// </summary>
public class LevelNavGraph : MonoBehaviour
{
    public enum NodeDrawMode { GroundAndEdges, AllOpen, Everything }

    [Header("Bounds")]
    [Tooltip("Level size in world units, extending up and right from this transform.")]
    [SerializeField] private Vector2 levelSize = new Vector2(132f, 60f);
    [Tooltip("World units per cell.")]
    [SerializeField] private float nodeSpacing = 0.25f;

    [Header("Baking")]
    [SerializeField] private LayerMask environmentMask;
    [Tooltip("Half depth of the occupancy test on Z.")]
    [SerializeField] private float zHalfExtent = 0.4f;
    [Tooltip("Mark open cells unreachable from the enclosure seed as blocked.")]
    [SerializeField] private bool fillEnclosedRegions = false;
    [Tooltip("Point in open playable space the enclosure fill starts from.")]
    [SerializeField] private Transform enclosureSeed;
    [Tooltip("Maximum clearance measured above a ground cell, in world units.")]
    [SerializeField] private float maxClearance = 8f;
    [SerializeField] private bool bakeOnStart = true;

    [Header("Climb Links")]
    [Tooltip("Distance past the ledge lip where a climb ends, in world units.")]
    [SerializeField] private float climbStandOffset = 0.5f;
    [Tooltip("Distance to search for ground when resolving link endpoints and positions, in world units.")]
    [SerializeField] private float linkSnapRadius = 4f;
    [Tooltip("Cost multiplier for climb links.")]
    [SerializeField] private float climbCostMultiplier = 1.5f;
    [Tooltip("Log each climb edge as it is linked or rejected.")]
    [SerializeField] private bool logClimbLinkBaking = false;

    [Header("Jump and Fall Links")]
    [Tooltip("Maximum horizontal span of a jump or fall link, in world units.")]
    [SerializeField] private float maxJumpReach = 12f;
    [Tooltip("Maximum rise of a jump link, in world units.")]
    [SerializeField] private float maxJumpRise = 5f;
    [Tooltip("Maximum drop of a jump or fall link, in world units.")]
    [SerializeField] private float maxJumpDrop = 14f;
    [Tooltip("Minimum gap for a link from a ledge, in world units.")]
    [SerializeField] private float minJumpGap = 0.75f;
    [Tooltip("Spacing between sampled link candidates, in world units.")]
    [SerializeField] private float jumpCandidateInterval = 1f;
    [Tooltip("Maximum candidates per ledge per direction.")]
    [SerializeField] private int maxJumpCandidatesPerLedge = 24;
    [Tooltip("Cost multiplier for jump links.")]
    [SerializeField] private float jumpCostMultiplier = 1.8f;
    [Tooltip("Cost multiplier for fall links.")]
    [SerializeField] private float fallCostMultiplier = 1.2f;
    [Tooltip("Log each ledge's landing and takeoff zones.")]
    [SerializeField] private bool logJumpLinkBaking = false;

    [Header("Gizmos")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private NodeDrawMode drawMode = NodeDrawMode.GroundAndEdges;
    [Tooltip("Maximum cells drawn.")]
    [SerializeField] private int maxGizmoNodes = 20000;
    [SerializeField] private bool drawBounds = true;
    [Tooltip("Draw every baked link.")]
    [SerializeField] private bool drawLinks = true;

    private NavGrid grid;
    private readonly NavLinkTable links = new NavLinkTable();
    private LevelNavigator navigator;

    public NavGrid Grid => grid;
    public NavLinkTable Links => links;
    public LevelNavigator Navigator => navigator;
    public bool IsBaked => grid != null;


    #region Initialize

    private void Reset()
    {
        environmentMask = LayerMask.GetMask("Environment");
    }

    private void Start()
    {
        if (bakeOnStart)
            Bake();
    }

    #endregion


    #region Bake

    /// <summary>Builds the grid, the links and the navigator.</summary>
    [ContextMenu("Bake")]
    public void Bake()
    {
        int columns = Mathf.Max(1, Mathf.CeilToInt(levelSize.x / nodeSpacing));
        int rows = Mathf.Max(1, Mathf.CeilToInt(levelSize.y / nodeSpacing));

        grid = new NavGrid(transform.position, nodeSpacing, columns, rows);

        Vector3 seedPosition = enclosureSeed != null ? enclosureSeed.position : transform.position;

        NavGridBaker.BakeResult result = NavGridBaker.Bake(grid, environmentMask, zHalfExtent, fillEnclosedRegions, seedPosition, maxClearance);

        Debug.Log($"{name}: nav grid baked. {columns}x{rows} = {grid.NodeCount} cells in {result.ElapsedMilliseconds:F0}ms. " +
                  $"Blocked {result.BlockedCount} (enclosed {result.EnclosedCount}), ground {result.GroundCount}, edges {result.EdgeCount}.", this);

        if (result.GroundCount == 0)
            Debug.LogWarning($"{name}: nav grid has no ground cells. Check the environment mask, the level bounds, and whether the enclosure fill sealed the playable space.", this);

        BakeLinks();

        navigator = new LevelNavigator(grid, links) { SnapRadius = linkSnapRadius };
    }

    private void BakeLinks()
    {
        links.Clear();

        NavClimbLinkBaker.Bake(grid, links, climbStandOffset, linkSnapRadius, climbCostMultiplier, logClimbLinkBaking);

        NavJumpLinkBaker.Settings jumpSettings = new NavJumpLinkBaker.Settings
        {
            MaxReach = maxJumpReach,
            MaxRise = maxJumpRise,
            MaxDrop = maxJumpDrop,
            MinGap = minJumpGap,
            CandidateInterval = jumpCandidateInterval,
            MaxCandidatesPerLedge = maxJumpCandidatesPerLedge,
            CostMultiplier = jumpCostMultiplier,
            FallCostMultiplier = fallCostMultiplier,
        };

        NavJumpLinkBaker.Bake(grid, links, jumpSettings, logJumpLinkBaking);

        Debug.Log($"{name}: {links.CountOfType(NavLinkType.Climb)} climb links, {links.CountOfType(NavLinkType.Jump)} jump links, " +
                  $"{links.CountOfType(NavLinkType.Fall)} fall links baked.", this);
    }

    #endregion


    #region Gizmos

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
            return;

        if (drawBounds)
        {
            Gizmos.color = Color.white;
            Vector3 size = new Vector3(levelSize.x, levelSize.y, 0f);
            Gizmos.DrawWireCube(transform.position + size * 0.5f, size);
        }

        if (grid == null)
            return;

        DrawNodes();
        DrawLinks();
    }

    private void DrawNodes()
    {
        float cubeSize = grid.Spacing * 0.6f;
        int drawn = 0;

        for (int index = 0; index < grid.NodeCount; index++)
        {
            NavNode node = grid.Get(index);

            if (!ShouldDraw(node))
                continue;

            if (drawn++ >= maxGizmoNodes)
                break;

            Gizmos.color = ColorFor(node);
            Gizmos.DrawCube(grid.NodePosition(index), Vector3.one * cubeSize);
        }
    }

    private void DrawLinks()
    {
        if (!drawLinks)
            return;

        for (int i = 0; i < links.AllLinks.Count; i++)
        {
            NavLink link = links.AllLinks[i];
            Vector3 from = grid.NodePosition(link.FromNode);
            Vector3 to = grid.NodePosition(link.ToNode);

            Gizmos.color = NavLinkColors.For(link.Type);
            Gizmos.DrawLine(from, to);

            if (link.Type == NavLinkType.Climb)
                Gizmos.DrawWireSphere(to, grid.Spacing * 0.8f);
        }
    }

    private bool ShouldDraw(NavNode node)
    {
        switch (drawMode)
        {
            case NodeDrawMode.GroundAndEdges:
                return node.IsGround;

            case NodeDrawMode.AllOpen:
                return !node.IsBlocked;

            default:
                return true;
        }
    }

    private Color ColorFor(NavNode node)
    {
        if (node.IsBlocked)
            return new Color(0.15f, 0.15f, 0.15f);

        if (node.IsEdge)
            return Color.white;

        if (node.IsGround)
            return Color.green;

        if (node.IsWall)
            return Color.red;

        return new Color(0f, 0.6f, 0.8f, 0.35f);
    }

    #endregion
}
