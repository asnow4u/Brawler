using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    public float throwForce;

    public void UseItem()
    {

    }

    public void ThrowItem(Vector3 dir)
    {
        Debug.Log("Throw Item");
        Rigidbody rb = gameObject.AddComponent<Rigidbody>();
        rb.AddForce(dir * throwForce, ForceMode.Impulse);
    }
}
