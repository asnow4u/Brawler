using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ragdoll
{
    public List<GameObject> Parts;    

    public Ragdoll(List<GameObject> parts)
    {
        Parts = parts;        
    }


    public void Enable()
    {
        foreach (GameObject part in Parts)
        {
            if (part.TryGetComponent(out Collider collider))
            {
                collider.isTrigger = false;
            }
        }
    }


    public void Disable()
    {
        foreach (GameObject part in Parts)
        {
            if (part.TryGetComponent(out Collider collider))
            {
                collider.isTrigger = true;
            }
        }
    }
}
