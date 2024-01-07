using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainNode
{
    public Vector3 Pos;
    public bool InsideTerrain;

    public TerrainCollisionNode UpCollision;
    public TerrainCollisionNode DownCollision;
    public TerrainCollisionNode RightCollision;
    public TerrainCollisionNode LeftCollision;


    public TerrainNode(Vector3 pos)
    {
        this.Pos = pos;

        UpCollision = null;
        DownCollision = null;
        RightCollision = null;
        LeftCollision = null;        
    }


    /// <summary>
    /// Check each direction for collisions
    /// Create TerrainCollisionNodes for each collision, null for misses
    /// </summary>
    /// <param name="castDist"></param>
    public void PerformRaycastCheck(float castDist)
    {
        UpCollision = new TerrainCollisionNode();
        if (!UpCollision.AttemptRaycast(Pos, Vector3.up, castDist))        
            UpCollision = null;

        DownCollision = new TerrainCollisionNode();
        if (!DownCollision.AttemptRaycast(Pos, Vector3.down, castDist))
            DownCollision = null;

        RightCollision = new TerrainCollisionNode();
        if (!RightCollision.AttemptRaycast(Pos, Vector3.right, castDist))
            RightCollision = null;

        LeftCollision = new TerrainCollisionNode();    
        if (!LeftCollision.AttemptRaycast(Pos, Vector3.left, castDist))
            LeftCollision = null;
    }

}
