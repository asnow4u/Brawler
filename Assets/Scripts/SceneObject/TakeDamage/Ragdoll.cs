using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public struct RagdollPart
{
    public GameObject GO;

    public Collider Collider;
    public Rigidbody Rb;

    public Joint Joint;
    public Rigidbody JointConnectedRb;

    public RagdollPart(GameObject go)
    {
        GO = go;
        Collider = go.GetComponent<Collider>();
        Rb = go.GetComponent<Rigidbody>();
        Joint = go.GetComponent<Joint>();
        JointConnectedRb = Joint.connectedBody;
    }
}


public class Ragdoll
{
    private List<RagdollPart> ragdollParts = new List<RagdollPart>();    

    public Ragdoll(List<GameObject> parts)
    {
        foreach (GameObject part in parts)
        {
            ragdollParts.Add(new RagdollPart(part));
        }
    }


    public void Enable()
    {
        foreach (RagdollPart part in ragdollParts)
        {
            part.Collider.isTrigger = false;            
            part.Rb.useGravity = true;
        }
    }


    public void Disable()
    {
        foreach (RagdollPart part in ragdollParts)
        {
            part.Collider.isTrigger = true;
            part.Rb.useGravity = false;
            part.Rb.velocity = Vector3.zero;
        }
    }
}
