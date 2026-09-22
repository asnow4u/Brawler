using System.Collections.Generic;
using UnityEngine;

/// <summary>One step of a route between two ground cells.</summary>
public struct NavSegment
{
    public NavLinkType Type;
    public int FromNode;
    public int ToNode;
    public Vector3 FromPosition;
    public Vector3 ToPosition;
}


/// <summary>Ordered route from the search, with consecutive walk cells merged into single segments.</summary>
public class NavPath
{
    public readonly List<NavSegment> Segments = new List<NavSegment>();

    public bool IsValid => Segments.Count > 0;
    public int Count => Segments.Count;
    public NavSegment this[int index] => Segments[index];

    public void Clear() => Segments.Clear();

    public int CountOfType(NavLinkType type)
    {
        int count = 0;

        for (int i = 0; i < Segments.Count; i++)
        {
            if (Segments[i].Type == type)
                count++;
        }

        return count;
    }
}
