using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TraversalType { Move, Jump, Land }

[RequireComponent(typeof(SphereCollider))]
public class Waypoint : MonoBehaviour
{
    [Serializable]
    public class TraversalPoint
    {
        public Waypoint Destination;
        public TraversalType TraversalType;

        public TraversalPoint(Waypoint destination, TraversalType type)
        {
            this.Destination = destination;
            this.TraversalType = type;
        }
    }

    [Serializable]
    public class JumpTraversalPoint : TraversalPoint
    {
        public Vector2 JumpVelocity;

        public JumpTraversalPoint(Waypoint destination, TraversalType type, Vector2 jumpVelocity) : base (destination, type)
        {
            this.JumpVelocity = jumpVelocity;
        }
    }


    public int Column;
    public int Row;

    public List<TraversalPoint> TraversalPoints = new List<TraversalPoint>();   
}
