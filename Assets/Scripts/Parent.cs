using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Parent : NetworkBehaviour
{
    [SyncVar]
    public Transform parentObj;

    // Update is called once per frame
    void FixedUpdate()
    {
        
        if (parentObj != null)
        {
            transform.position = parentObj.transform.position + new Vector3(0, 2, 0);
            GetComponent<Rigidbody>().isKinematic = true;
        }
    }
}
