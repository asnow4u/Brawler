using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{

    private float numLives;
    private float numDeaths;
    
    public float totalDamage;
    public float damageMultiplier;
    public float bounceDampener;

    //CurrentEquipment

    public float stallTimer;

    Rigidbody rb;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }


    public void AddPercent(float percent)
    {
        throw new System.NotImplementedException();
    }

    public void ApplyForce(float magnitue, Vector3 direction)
    {
        throw new System.NotImplementedException();
    }

    public void RemovePercent(float percent)
    {
        throw new System.NotImplementedException();
    }

    public void ResetPercent()
    {
        throw new System.NotImplementedException();
    }

    public void ApplyForce()
    {
        throw new System.NotImplementedException();
    }



    //TODO: move this to platforms
    //private void OnCollisionEnter(Collision collision)
    //{
    //    if (collision.gameObject.CompareTag("Platforms"))
    //    {
    //        if (stallTimer > 0) 
    //        {

    //            //F = m(v/t) || t=1
    //            Vector3 bounceForce = Vector3.Reflect(-1 * collision.relativeVelocity, collision.transform.up) * rb.mass;

    //            //Debug.Log("Bounce");
    //            //Debug.Log(bounceForce);

    //            rb.AddForce(bounceForce * bounceDampener, ForceMode.Impulse);
    //        }
    //    }
    //}
}
