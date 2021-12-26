using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    public float throwForce;

    private void Start()
    {

    }

    public void UseItem()
    {
        Debug.Log("UseItem");
    }

    public void ThrowItem(Vector3 dir)
    {
        //TODO: throwing is wonky
        Debug.Log("Throw Item");
        transform.parent.GetComponent<Character_Hands>().HeldItem = null;
        transform.parent = null;
        GetComponent<Collider>().enabled = true;
        Rigidbody rb = gameObject.GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.AddForce(dir * throwForce, ForceMode.Impulse);
    }
}
