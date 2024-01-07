using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.InputSystem.HID;

public class TerrainCollisionNode
{
    public float SlopeGradiant;

    private RaycastHit Hit;

    #region Getters

    public GameObject CollisionObj => Hit.collider.gameObject;
    public Vector3 CollisionPoint => Hit.point;

    #endregion

    public TerrainCollisionNode()
    {

    }


    public bool AttemptRaycast(Vector3 origin, Vector3 dir, float dist)
    {
        if (Physics.Raycast(origin, dir, out Hit, dist, LayerMask.GetMask("Environment")))
        {
            //Hit.normal
            SlopeGradiant = Vector3.Angle(Vector3.right, Hit.normal);

            if (SlopeGradiant > 90)
                SlopeGradiant = 180 - SlopeGradiant;

            Debug.Log(SlopeGradiant);

            return true;
        }

        return false;
    }
}
