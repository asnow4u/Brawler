using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


public class Item : NetworkBehaviour
{
    public float throwForce;

    private Rigidbody rb;

    private bool isThrown;

    protected void Start()
    {
        //rb = gameObject.GetComponent<Rigidbody>();
        //isThrown = false;
    }

    protected void Update()
    {
        //When this object is thrown determin if the object is far enough away to switch to a layer that is collidable
        //if (isThrown && gameObject.layer == LayerMask.NameToLayer("Ignore Collision"))
        //{
        //    if (Vector3.Distance(transform.position, transform.parent.transform.position) > 1f)
        //    {
        //        gameObject.layer = LayerMask.NameToLayer("Default");
        //        transform.parent.GetComponent<PlayerHands>().HeldItem = null;
        //        transform.parent = null;
        //    }
        //}
    }


    /// <summary>
    /// The current item being held will be thrown in the direction of "dir"
    /// Reset the ridgidbody
    /// Apply force
    /// Set bool, so we know the item has been thrown
    /// </summary>
    /// <param name="dir"></param>
    public void ThrowItem(Vector3 dir)
    {
        //rb.isKinematic = false;
        //rb.velocity = Vector3.zero;
        //rb.AddForce(dir * throwForce, ForceMode.Impulse);

        //isThrown = true;
    }

}


public interface IItemInterface
{
    //void UseItem();
}
