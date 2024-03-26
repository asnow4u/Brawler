using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpEdge : Edge
{
    private float initialVelocity;
    private float jumpXInfluence;
    private float jumpYInfluence;


    //Getters
    public float InitialVelocity => initialVelocity;
    public float JumpXInfluence => jumpXInfluence;
    public float JumpYInfluence => jumpYInfluence;



    public JumpEdge(GraphNode startNode, GraphNode endNode, float initialVelocity, float jumpXInfluence, float jumpYInfluence) : base(startNode, endNode)
    {
        edgeType = EdgeType.Jump;
    }
}
