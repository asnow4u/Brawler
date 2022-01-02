using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
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

    void Update()
    {
        if (stallTimer > 0)
        {
            stallTimer -= Time.deltaTime;
        }    
    }

    public void DealDamage(float damage, Vector3 dir)
    {
        totalDamage += damage;

        //Linar based function to determine amount of force
        float force = damageMultiplier * totalDamage * rb.mass;

        RaycastHit hit;

        //if (Physics.Raycast(transform.position, Vector3.down, out hit, transform.GetComponent<Collider>().bounds.size.y / 2 + 0.1f, ~LayerMask.NameToLayer("Ground")))
        //{
        //    dir = Vector3.Reflect(dir, hit.normal);
        //    Debug.Log(dir);
        //}

        //Debug.Log(force);
        rb.AddForce(force * dir, ForceMode.Impulse);

        //Stall
        stallTimer = Mathf.Abs(force / Physics.gravity.y); //v1 = v0 + at => v1 / a = t || v0 is 0
    }


    public bool IsStalled()
    {
        if (stallTimer > 0)
        {
            return true;
        }

        return false;
    }


    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Platforms"))
        {
            if (stallTimer > 0) 
            {

                //F = m(v/t) || t=1
                Vector3 bounceForce = Vector3.Reflect(-1 * collision.relativeVelocity, collision.transform.up) * rb.mass;
                
                //Debug.Log("Bounce");
                //Debug.Log(bounceForce);

                rb.AddForce(bounceForce * bounceDampener, ForceMode.Impulse);
            }
        }
    }
}
