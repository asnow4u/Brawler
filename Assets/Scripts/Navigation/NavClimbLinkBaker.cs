using UnityEngine;

/// <summary>
/// Bakes climb links from ClimbableEdge components. Geometry comes from the edge's parent
/// platform collider.
/// </summary>
public static class NavClimbLinkBaker
{
    public static int Bake(NavGrid grid, NavLinkTable table, float standOffset, float snapDrop, float costMultiplier, bool verbose)
    {
        ClimbableEdge[] edges = Object.FindObjectsByType<ClimbableEdge>(FindObjectsSortMode.None);
        int built = 0;

        if (verbose)
            Debug.Log($"NavClimbLinkBaker: found {edges.Length} ClimbableEdge components.");

        for (int i = 0; i < edges.Length; i++)
        {
            if (TryBuildLink(grid, edges[i], standOffset, snapDrop, costMultiplier, out NavLink link, out string reason))
            {
                table.Add(link);
                built++;

                if (verbose)
                    Debug.Log($"NavClimbLinkBaker: '{GetPath(edges[i])}' linked. Rise {link.RiseHeight:F2}.", edges[i]);
            }
            else if (verbose)
            {
                Debug.LogWarning($"NavClimbLinkBaker: '{GetPath(edges[i])}' rejected. {reason}", edges[i]);
            }

            WarnOnTriggerMisalignment(edges[i], verbose);
        }

        return built;
    }


    private static string GetPath(ClimbableEdge edge)
    {
        Transform parent = edge.transform.parent;
        return parent != null ? $"{parent.name}/{edge.name}" : edge.name;
    }


    /// <summary>Links the ground beside a climbable corner to the ground just past its lip.</summary>
    private static bool TryBuildLink(NavGrid grid, ClimbableEdge edge, float standOffset, float snapDrop, float costMultiplier, out NavLink link, out string reason)
    {
        link = default;
        reason = string.Empty;

        Transform parent = edge.transform.parent;

        if (parent == null)
        {
            reason = "Edge has no parent platform to take geometry from.";
            return false;
        }

        Collider platform = parent.GetComponent<Collider>();

        if (platform == null)
        {
            reason = $"Parent '{parent.name}' has no collider.";
            return false;
        }

        Bounds platformBounds = platform.bounds;

        // Side of the platform the edge marks, and the direction the character walks to climb it.
        bool isRight = platformBounds.center.x < edge.transform.position.x;
        int directionX = isRight ? -1 : 1;

        float ledgeTop = platformBounds.max.y;
        float cornerX = isRight ? platformBounds.max.x : platformBounds.min.x;

        string info = $"platform y [{platformBounds.min.y:F2} .. {platformBounds.max.y:F2}], x [{platformBounds.min.x:F2} .. {platformBounds.max.x:F2}], isRight {isRight}";

        // Approach cell: ground beside the platform.
        Vector3 approachProbe = new Vector3(cornerX - directionX * grid.Spacing, ledgeTop, 0f);

        if (!grid.TryGetGroundBelow(approachProbe, snapDrop, out int fromNode))
        {
            reason = $"No ground within {snapDrop} below the approach probe {approachProbe}. {info}";
            return false;
        }

        // Destination cell: ground just past the lip.
        Vector3 standProbe = new Vector3(cornerX + directionX * standOffset, ledgeTop + grid.Spacing, 0f);

        if (!grid.TryGetGroundBelow(standProbe, snapDrop, out int toNode))
        {
            reason = $"No ground within {snapDrop} below the stand probe {standProbe}. {info}";
            return false;
        }

        if (fromNode == toNode)
        {
            reason = $"Both probes resolved to the same cell at {grid.NodePosition(fromNode)}. {info}";
            return false;
        }

        float fromSurfaceY = grid.SurfaceY(fromNode);
        float riseHeight = ledgeTop - fromSurfaceY;

        if (riseHeight <= 0f)
        {
            reason = $"Rise height {riseHeight:F2} is not positive. Approach surface y {fromSurfaceY:F2}. {info}";
            return false;
        }

        if (grid.NodePosition(toNode).y <= grid.NodePosition(fromNode).y)
        {
            reason = $"Destination cell is not above the approach cell. From {grid.NodePosition(fromNode)}, to {grid.NodePosition(toNode)}. {info}";
            return false;
        }

        float distance = Vector3.Distance(grid.NodePosition(fromNode), grid.NodePosition(toNode));

        link = new NavLink
        {
            Type = NavLinkType.Climb,
            FromNode = fromNode,
            ToNode = toNode,
            RiseHeight = riseHeight,
            Cost = distance * costMultiplier,
        };

        return true;
    }


    /// <summary>Warns when an edge's trigger top does not match its platform's surface.</summary>
    private static void WarnOnTriggerMisalignment(ClimbableEdge edge, bool verbose)
    {
        if (!verbose)
            return;

        Collider trigger = edge.GetComponent<Collider>();
        Transform parent = edge.transform.parent;

        if (trigger == null || parent == null)
            return;

        Collider platform = parent.GetComponent<Collider>();

        if (platform == null)
            return;

        float difference = trigger.bounds.max.y - platform.bounds.max.y;

        if (Mathf.Abs(difference) < 0.05f)
            return;

        Debug.LogWarning($"NavClimbLinkBaker: '{GetPath(edge)}' trigger top is {difference:F2} from the platform surface " +
                         $"(trigger {trigger.bounds.max.y:F2}, platform {platform.bounds.max.y:F2}). " +
                         $"InputMovementHandler measures the climb against the trigger top, so the in game climb will not match the graph.", edge);
    }
}
