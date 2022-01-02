using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FistController : MonoBehaviour
{

    public float punchTime;
    public float punchDist;

    private bool fistLaunching;
    private bool fistReturning;

    private GameObject parent;
    private Character_Hands characterHands;
    private Vector3 parentPositionDif;

    private Vector3 dir;
    private Vector3 originalPos;
    private float currentPunchTime;

    // Start is called before the first frame update
    void Start()
    {
        fistLaunching = false;
        fistReturning = false;

        parent = transform.parent.gameObject;
        characterHands = parent.transform.GetComponent<Character_Hands>();
        parentPositionDif = parent.transform.position - transform.position;

    }

    // Update is called once per frame
    void Update()
    {

        if (fistLaunching)
        {

            currentPunchTime += Time.deltaTime;

            if (currentPunchTime > punchTime)
            {
                currentPunchTime = punchTime;
                fistLaunching = false;
                fistReturning = true;
            }

            float increment = currentPunchTime / punchTime;
            increment = Mathf.Sin(increment * Mathf.PI * 0.5f); //https://chicounity3d.wordpress.com/2014/05/23/how-to-lerp-like-a-pro/
            transform.position = Vector3.Lerp(originalPos, originalPos + dir * punchDist, increment);
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(new Vector3(0, -90, 0));

        }

        if (fistReturning)
        {

            currentPunchTime -= Time.deltaTime;

            //if (currentPunchTime < 0f)
            //{
            //    currentPunchTime = 0f;
            //    fistReturn = false;
            //    transform.parent = parent.transform;
            //    characterHands.FistReturned(gameObject);

            //    //Transfer the item to the player
            //    if (characterHands.HeldItem != null && transform.childCount > 0)
            //    {
            //        transform.GetChild(0).transform.parent = parent.transform;
            //    }
            //}

            float increment = currentPunchTime / punchTime;
            increment = Mathf.Sin(increment * Mathf.PI * 0.5f); //https://chicounity3d.wordpress.com/2014/05/23/how-to-lerp-like-a-pro/
            transform.position = Vector3.Lerp(parent.transform.position + parentPositionDif, originalPos + dir * punchDist, increment);
            transform.rotation = Quaternion.LookRotation((transform.position - parent.transform.position).normalized, Vector3.up) * Quaternion.Euler(new Vector3(0, 90, 0));
        }
    }
        

    public void FistAttack(Vector3 direction){
        transform.parent = null;
        dir = direction;
        originalPos = transform.position;
        currentPunchTime = 0f;
        fistLaunching = true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        string layerName = LayerMask.LayerToName(collision.gameObject.layer);
        string tagName = collision.gameObject.tag;

        //List of available collisions
        
        //Collision with a player
        if (tagName == "Player")
        {
            //Determine if parent player
            if (collision.gameObject == parent)
            {

                //Return the fist to its location
                currentPunchTime = 0f;
                fistReturning = false;
                transform.position = parent.transform.position + parentPositionDif;
                transform.parent = parent.transform;
                characterHands.FistReturned(gameObject);

                //Transfer the item to the player
                if (characterHands.HeldItem != null && transform.childCount > 0)
                {
                    transform.GetChild(0).transform.parent = parent.transform;
                }
            }

            //Collision with another player
            else
            {

                if (fistLaunching)
                {
                    fistLaunching = false;
                    fistReturning = true;

                    //TODO determine amount of damage(0-1)
                    float damage = 1; 

                    Vector3 fistDir = transform.right;
                    float angle = Vector3.Angle(transform.right, Vector3.right);

                    //Check if player is on the ground (effects fistDir)
                    RaycastHit hit;
                    if (Physics.Raycast(collision.transform.position, Vector3.down, out hit, collision.transform.GetComponent<Collider>().bounds.size.y / 2 + 0.1f, ~LayerMask.NameToLayer("Ground"))){

                        //Fist hit at a upward trajectory
                        if (fistDir.y > 0)
                        {
                            //Shot from left side
                            if (angle < 20)
                            {
                                fistDir = Quaternion.AngleAxis(20, Vector3.forward) * transform.right;
                            }

                            //Shot from right side
                            if (angle > 160)
                            {
                                fistDir = Quaternion.AngleAxis(-20, Vector3.forward) * transform.right;
                            }
                        }

                        //Fist hit at a downward trajectory
                        if (fistDir.y <= 0)
                        {

                            //Shot from left or right
                            if (angle < 20 || angle > 160)
                            {   
                                fistDir = Vector3.Reflect(fistDir, hit.normal);
                                damage *= 0.7f;
                            }

                            //Shot from above
                            if (angle >= 20 && angle <= 160)
                            {
                                fistDir = Vector3.Reflect(fistDir, hit.normal);
                                damage *= 0.5f;
                            }
                        }
                    }

                    collision.gameObject.GetComponent<PlayerController>().DealDamage(damage, fistDir);
                }
            }
        }

        if (layerName == "Ground")
        {
            fistLaunching = false;
            fistReturning = true;
        }

        if (tagName == "Item")
        {
            if (characterHands.HeldItem == null)
            {
                fistLaunching = false;
                fistReturning = true;

                characterHands.HeldItem = collision.transform.gameObject;
                collision.gameObject.layer = LayerMask.NameToLayer("Ignore Collision");
                collision.gameObject.GetComponent<Rigidbody>().isKinematic = true;
                collision.transform.parent = transform;
            }
        }
    }
}
