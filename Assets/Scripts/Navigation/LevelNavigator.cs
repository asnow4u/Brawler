using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A* over ground cells. Walk neighbors come from same-row adjacency; all other links come from the
/// link table, filtered by the agent's profile. Search state is stamped per query rather than cleared.
/// </summary>
public class LevelNavigator
{
    /// <summary>Clearance required above a link's RequiredApex for a jump over it.</summary>
    public const float JumpApexMargin = 0.35f;

    /// <summary>How far straight down to look for ground under a position, in world units.</summary>
    public float SnapDrop = 3f;

    /// <summary>How far in any direction to look for ground when nothing is directly below, in world units.</summary>
    public float SnapRadius = 4f;

    private readonly NavGrid grid;
    private readonly NavLinkTable links;

    private readonly float[] gScore;
    private readonly int[] cameFrom;
    private readonly NavLinkType[] cameFromType;
    private readonly int[] openStamp;
    private readonly int[] closedStamp;
    private int currentStamp;

    private readonly MinHeap open = new MinHeap(256);


    public LevelNavigator(NavGrid grid, NavLinkTable links)
    {
        this.grid = grid;
        this.links = links;

        int count = grid.NodeCount;

        gScore = new float[count];
        cameFrom = new int[count];
        cameFromType = new NavLinkType[count];
        openStamp = new int[count];
        closedStamp = new int[count];
    }


    #region Search

    /// <summary>
    /// Fills path with a route from start to destination. False when either end cannot be resolved,
    /// both resolve to the same cell, or no route exists for this agent.
    /// </summary>
    public bool TryFindPath(Vector3 start, Vector3 destination, NavAgentProfile profile, NavPath path)
    {
        path.Clear();

        if (!TryResolveNode(start, out int startNode) || !TryResolveNode(destination, out int goalNode))
            return false;

        if (startNode == goalNode)
            return false;

        currentStamp++;
        open.Clear();

        gScore[startNode] = 0f;
        cameFrom[startNode] = -1;
        openStamp[startNode] = currentStamp;
        open.Push(startNode, Heuristic(startNode, goalNode));

        while (open.Count > 0)
        {
            int current = open.Pop();

            if (closedStamp[current] == currentStamp)
                continue;

            closedStamp[current] = currentStamp;

            if (current == goalNode)
            {
                BuildPath(startNode, goalNode, path);
                return true;
            }

            ExpandWalk(current, goalNode, profile);
            ExpandLinks(current, goalNode, profile);
        }

        return false;
    }

    private void ExpandWalk(int current, int goalNode, NavAgentProfile profile)
    {
        if (!profile.CanWalk)
            return;

        int column = grid.ColumnOf(current);
        int row = grid.RowOf(current);

        TryRelaxWalk(current, column - 1, row, goalNode, profile);
        TryRelaxWalk(current, column + 1, row, goalNode, profile);
    }

    private void TryRelaxWalk(int current, int column, int row, int goalNode, NavAgentProfile profile)
    {
        if (!grid.IsGroundInBounds(column, row))
            return;

        Relax(current, grid.Index(column, row), NavLinkType.Walk, grid.Spacing, goalNode, profile);
    }

    private void ExpandLinks(int current, int goalNode, NavAgentProfile profile)
    {
        if (!links.TryGetLinks(current, out List<NavLink> nodeLinks))
            return;

        for (int i = 0; i < nodeLinks.Count; i++)
        {
            NavLink link = nodeLinks[i];

            if (!CanTraverse(link, profile))
                continue;

            Relax(current, link.ToNode, link.Type, link.Cost, goalNode, profile);
        }
    }

    private void Relax(int current, int neighbor, NavLinkType type, float cost, int goalNode, NavAgentProfile profile)
    {
        if (closedStamp[neighbor] == currentStamp)
            return;

        if (!FitsAgent(neighbor, profile))
            return;

        float tentative = gScore[current] + cost;

        if (openStamp[neighbor] == currentStamp && tentative >= gScore[neighbor])
            return;

        gScore[neighbor] = tentative;
        cameFrom[neighbor] = current;
        cameFromType[neighbor] = type;
        openStamp[neighbor] = currentStamp;

        open.Push(neighbor, tentative + Heuristic(neighbor, goalNode));
    }

    private float Heuristic(int node, int goalNode)
    {
        return Vector3.Distance(grid.NodePosition(node), grid.NodePosition(goalNode));
    }

    #endregion


    #region Resolving Positions

    /// <summary>
    /// Resolves a position to the ground cell beneath it, probing straight down from slightly above
    /// the position. Falls back to the nearest ground in any direction.
    /// </summary>
    private bool TryResolveNode(Vector3 position, out int node)
    {
        float lift = grid.Spacing * 2f;
        Vector3 probe = position + Vector3.up * lift;

        if (grid.TryGetGroundBelow(probe, SnapDrop + lift, out node))
            return true;

        return grid.TryGetNearestGround(position, SnapRadius, out node);
    }

    #endregion


    #region Agent Constraints

    /// <summary>Whether the agent fits vertically in a cell.</summary>
    private bool FitsAgent(int node, NavAgentProfile profile)
    {
        return grid.Get(node).Clearance >= profile.Height;
    }

    private bool CanTraverse(NavLink link, NavAgentProfile profile)
    {
        switch (link.Type)
        {
            case NavLinkType.Climb:
                return profile.CanClimb && link.RiseHeight <= profile.ClimbLimit;

            case NavLinkType.Jump:
                return CanTraverseJump(link, profile);

            case NavLinkType.Fall:
                return profile.CanFall && profile.Airborne.CanFallTo(link.Dx, link.Dy);

            default:
                return true;
        }
    }

    /// <summary>Whether the agent can reach the landing and clear any obstruction between.</summary>
    private bool CanTraverseJump(NavLink link, NavAgentProfile profile)
    {
        if (!profile.CanJump)
            return false;

        if (!profile.Airborne.CanJumpTo(link.Dx, link.Dy))
            return false;

        return link.RequiredApex <= 0f || profile.Airborne.ApexHeight >= link.RequiredApex + JumpApexMargin;
    }

    #endregion


    #region Path Construction

    /// <summary>Builds the path back from the goal, merging consecutive walk steps into one segment.</summary>
    private void BuildPath(int startNode, int goalNode, NavPath path)
    {
        int node = goalNode;

        while (node != startNode)
        {
            int previous = cameFrom[node];

            if (previous < 0)
                break;

            NavLinkType type = cameFromType[node];

            if (type == NavLinkType.Walk && TryExtendLastWalk(path, node, previous))
            {
                node = previous;
                continue;
            }

            path.Segments.Add(new NavSegment
            {
                Type = type,
                FromNode = previous,
                ToNode = node,
                FromPosition = grid.NodePosition(previous),
                ToPosition = grid.NodePosition(node),
            });

            node = previous;
        }

        path.Segments.Reverse();
    }

    /// <summary>Moves the start of the last walk segment back to previous, if it begins at node.</summary>
    private bool TryExtendLastWalk(NavPath path, int node, int previous)
    {
        if (path.Count == 0)
            return false;

        int lastIndex = path.Count - 1;
        NavSegment last = path.Segments[lastIndex];

        if (last.Type != NavLinkType.Walk || last.FromNode != node)
            return false;

        last.FromNode = previous;
        last.FromPosition = grid.NodePosition(previous);
        path.Segments[lastIndex] = last;

        return true;
    }

    #endregion


    #region Min Heap

    /// <summary>Binary min heap of node indices keyed by estimated total cost.</summary>
    private class MinHeap
    {
        private int[] items;
        private float[] priorities;
        private int count;

        public int Count => count;

        public MinHeap(int capacity)
        {
            items = new int[capacity];
            priorities = new float[capacity];
        }

        public void Clear() => count = 0;

        public void Push(int item, float priority)
        {
            if (count == items.Length)
                Grow();

            items[count] = item;
            priorities[count] = priority;

            SiftUp(count);
            count++;
        }

        public int Pop()
        {
            int result = items[0];
            count--;

            items[0] = items[count];
            priorities[0] = priorities[count];

            SiftDown(0);

            return result;
        }

        private void Grow()
        {
            System.Array.Resize(ref items, items.Length * 2);
            System.Array.Resize(ref priorities, priorities.Length * 2);
        }

        private void SiftUp(int index)
        {
            while (index > 0)
            {
                int parent = (index - 1) / 2;

                if (priorities[parent] <= priorities[index])
                    break;

                Swap(parent, index);
                index = parent;
            }
        }

        private void SiftDown(int index)
        {
            while (true)
            {
                int left = index * 2 + 1;
                int right = left + 1;
                int smallest = index;

                if (left < count && priorities[left] < priorities[smallest])
                    smallest = left;

                if (right < count && priorities[right] < priorities[smallest])
                    smallest = right;

                if (smallest == index)
                    return;

                Swap(smallest, index);
                index = smallest;
            }
        }

        private void Swap(int a, int b)
        {
            (items[a], items[b]) = (items[b], items[a]);
            (priorities[a], priorities[b]) = (priorities[b], priorities[a]);
        }
    }

    #endregion
}
