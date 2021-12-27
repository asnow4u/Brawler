using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    public float throwForce;

    private Rigidbody rb;

    private bool isThrown;

    private void Start()
    {
        rb = gameObject.GetComponent<Rigidbody>();
        isThrown = false;
    }

    private void Update()
    {
        //When this object is thrown determin if the object is far enough away to switch to a layer that is collidable
        if (isThrown && gameObject.layer == LayerMask.NameToLayer("Ignore Collision"))
        {
            if (Vector3.Distance(transform.position, transform.parent.transform.position) > 1f)
            {
                gameObject.layer = LayerMask.NameToLayer("Default");
                transform.parent.GetComponent<Character_Hands>().HeldItem = null;
                transform.parent = null;
            }
        }
    }

    
    //Note: This will need to call some singleton to determine the effect of the item
    public void UseItem()
    {
        Debug.Log("UseItem");
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
        rb.isKinematic = false;
        rb.velocity = Vector3.zero;
        rb.AddForce(dir * throwForce, ForceMode.Impulse);

        isThrown = true;
    }

}
