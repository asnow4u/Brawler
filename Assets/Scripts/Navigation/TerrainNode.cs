using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TerrainNodeType { Air, Surface, Wall, Ceiling, SurfaceWall, CeilngWall, WallLedge, SurfaceLedge, Inside }

public class TerrainNode
{
    public TerrainNodeType Type;
    public Vector3 Pos;

    public TerrainCollisionNode UpCollision;
    public TerrainCollisionNode DownCollision;
    public TerrainCollisionNode RightCollision;
    public TerrainCollisionNode LeftCollision;


    public TerrainNode(Vector3 pos)
    {
        this.Pos = pos;

        ResetRaycasts();
    }


    public void ResetRaycasts()
    {
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

        DetermineType();
    }

    private void DetermineType()
    {
        if (UpCollision == null
            && DownCollision == null
            && RightCollision == null
            && LeftCollision == null)
        {
            Type = TerrainNodeType.Air;
            return;
        }

        if (UpCollision != null
            && (DownCollision == null && RightCollision == null && LeftCollision == null))
        {
            Type = TerrainNodeType.Ceiling;
            return;
        }

        if (DownCollision != null
            && (UpCollision == null && RightCollision == null && LeftCollision == null))
        {
            Type = TerrainNodeType.Surface;
            return;
        }

        if ((RightCollision != null || LeftCollision != null)
            && (UpCollision == null && DownCollision == null))
        {
            Type = TerrainNodeType.Wall;
            return;
        }

        if ((RightCollision != null || LeftCollision != null)
            && DownCollision != null
            && UpCollision == null)
        {
            Type = TerrainNodeType.SurfaceWall;
            return;
        }

        if ((RightCollision != null || LeftCollision != null)
            && UpCollision != null
            && DownCollision == null)
        {
            Type = TerrainNodeType.CeilngWall;
            return;
        }
    }
}
