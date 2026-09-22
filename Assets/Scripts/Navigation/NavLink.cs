using System.Collections.Generic;
using UnityEngine;

/// <summary>Traversal types between ground cells. Walk links are derived during search and never stored.</summary>
public enum NavLinkType
{
    /// <summary>Horizontally adjacent ground cells on the same row.</summary>
    Walk,

    /// <summary>Authored ledge climb.</summary>
    Climb,

    /// <summary>Grounded jump from one surface to another.</summary>
    Jump,

    /// <summary>Walking off a ledge onto a lower surface.</summary>
    Fall,
}


public struct NavLink
{
    public NavLinkType Type;
    public int FromNode;
    public int ToNode;

    /// <summary>Rise from the source surface to the ledge top, in world units. Climb links.</summary>
    public float RiseHeight;

    /// <summary>Horizontal distance from launch to landing, in world units. Jump and fall links.</summary>
    public float Dx;

    /// <summary>Surface to surface height change, positive upward. Jump and fall links.</summary>
    public float Dy;

    /// <summary>
    /// Height above the launch surface the arc must clear, in world units. Zero when nothing between
    /// the two ends is taller than both surfaces. Jump and fall links.
    /// </summary>
    public float RequiredApex;

    public float Cost;
}


/// <summary>Links keyed by source node.</summary>
public class NavLinkTable
{
    private readonly Dictionary<int, List<NavLink>> linksByNode = new Dictionary<int, List<NavLink>>();
    private readonly List<NavLink> allLinks = new List<NavLink>();

    public int Count => allLinks.Count;
    public IReadOnlyList<NavLink> AllLinks => allLinks;


    public void Add(NavLink link)
    {
        if (!linksByNode.TryGetValue(link.FromNode, out List<NavLink> links))
        {
            links = new List<NavLink>();
            linksByNode[link.FromNode] = links;
        }

        links.Add(link);
        allLinks.Add(link);
    }

    public bool TryGetLinks(int fromNode, out List<NavLink> links)
    {
        return linksByNode.TryGetValue(fromNode, out links);
    }

    public void Clear()
    {
        linksByNode.Clear();
        allLinks.Clear();
    }

    public int CountOfType(NavLinkType type)
    {
        int count = 0;

        for (int i = 0; i < allLinks.Count; i++)
        {
            if (allLinks[i].Type == type)
                count++;
        }

        return count;
    }
}


/// <summary>Gizmo color for each traversal type.</summary>
public static class NavLinkColors
{
    public static Color For(NavLinkType type)
    {
        switch (type)
        {
            case NavLinkType.Climb: return Color.magenta;
            case NavLinkType.Jump: return new Color(1f, 0.55f, 0f);
            case NavLinkType.Fall: return new Color(0.35f, 0.7f, 1f);
            default: return Color.yellow;
        }
    }
}
