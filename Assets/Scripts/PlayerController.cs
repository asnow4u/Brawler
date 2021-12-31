using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{

    private float numLives;
    private float numDeaths;
    
    public float totalDamage;
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
        float force = 2 * damage;

        rb.AddForce(force * dir, ForceMode.Impulse);


        //Stall
        stallTimer = force / Physics.gravity.y; //v1 = v0 + at => v1 / a = t || v0 is 0
    }


    public bool IsStalled()
    {
        if (stallTimer > 0)
        {
            return true;
        }

        return false;
    }
}
