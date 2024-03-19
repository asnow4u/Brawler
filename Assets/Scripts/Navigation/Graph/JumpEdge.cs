using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpEdge : Edge
{
    public float JumpYVelocity;
    public float JumpXVelocity;
    public float JumpTime;
    public float JumpDistance;


    public JumpEdge(EdgeType type, Node startNode, Node endNode, float jumpYVelocity, float jumpXVelocity, float jumpTime, float jumpDistance) : base(type, startNode, endNode)
    {
        JumpYVelocity = jumpYVelocity;
        JumpXVelocity = jumpXVelocity;
        JumpTime = jumpTime;
        JumpDistance = jumpDistance;
    }
}
