using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpPathPoint : PathPoint
{
    public float InitialVelocity;
    public float JumpXInfluence;
    public float JumpYInfluence;

    public JumpPathPoint(PathPoint point, float initialVelocity, float jumpXInfluence, float jumpYInfluence) : base(point.GraphNode, TraversalType.Jump, point.Pos)
    {
        this.InitialVelocity = initialVelocity;
        this.JumpXInfluence = jumpXInfluence;
        this.JumpYInfluence = jumpYInfluence;
    }
}
