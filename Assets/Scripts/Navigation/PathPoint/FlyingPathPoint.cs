using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyingPathPoint : PathPoint
{
    public FlyingPathPoint(Node graphNode, Vector3 pos) : base(graphNode, TraversalType.Fly, pos)
    { }
}
