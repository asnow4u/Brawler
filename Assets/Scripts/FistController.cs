using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


/// <summary>
/// The controller that opperates each fist independently
/// </summary>
public class FistController : NetworkBehaviour
{

    [SerializeField] private float punchTime;
    [SerializeField] private float punchDist;
    
    public float hangDistance;
    
    private Vector3 originalPos;
    private Quaternion originalRot;
    
    private bool isFistLaunching;
    private bool isFistReturning;
    private Vector3 fistDirection;
    private Vector3 startPos;
    private float currentPunchTime;

    public float returnSpeed;
    private bool ledgeGrabPossible;


    // Update is called once per frame
    void Update()
    {

        if (IsServer)
        {
            ServerUpdate();
        }
        
    }


    /// <summary>
    /// Update for the server
    /// </summary>
    private void ServerUpdate()
    {

        //Fist has collided with a ledge 
        //if (parent.GetComponent<PlayerHands>().LedgeHangingFist == gameObject)
        //{

        //    //Determin if player is close enough to fist
        //    if (Vector3.Distance(transform.position, parent.transform.position) > hangDistance)
        //    {
        //        //Bring player to the fist
        //        parent.transform.GetComponent<Rigidbody>().isKinematic = true;
        //        parent.transform.position = Vector3.MoveTowards(parent.transform.position, transform.position, returnSpeed * Time.deltaTime);
        //    }

        //    else
        //    {
        //        parent.transform.GetComponent<Character_Movement>().UpdateLedgeFist(gameObject);
        //    }
        //}

        /// <summary>
        /// Lerps back towards the player
        /// Update position and rotation
        /// </summary>
        if (isFistReturning)
        {

            //Lerp the fist back to the player
            float increment = currentPunchTime / punchTime;
            increment = Mathf.Sin(increment * Mathf.PI * 0.5f); //https://chicounity3d.wordpress.com/2014/05/23/how-to-lerp-like-a-pro/

            Vector3 parentPos = transform.parent.transform.position + originalPos;
                
            //Update position and rotation
            transform.position = Vector3.Lerp(parentPos, startPos, increment);
            transform.rotation = Quaternion.LookRotation((transform.parent.transform.position - transform.position).normalized, Vector3.up) * Quaternion.Euler(new Vector3(0, 90, 0));

            currentPunchTime -= Time.deltaTime;
        }


        /// <summary>
        /// Lerps towards click point
        /// Update position and rotation
        /// After punchtime has depleted, have the fist return back to the player
        /// </summary>
        else if (isFistLaunching)
        {

            //Lerp the fist outwards
            float increment = currentPunchTime / punchTime;
            increment = Mathf.Sin(increment * Mathf.PI * 0.5f); //https://chicounity3d.wordpress.com/2014/05/23/how-to-lerp-like-a-pro/

            transform.position = Vector3.Lerp(startPos, startPos + fistDirection * punchDist, increment);
            transform.rotation = Quaternion.LookRotation(fistDirection, Vector3.up) * Quaternion.Euler(new Vector3(0, -90, 0));

            currentPunchTime += Time.deltaTime;

            //Check if the fist needs to return back to the player
            if (currentPunchTime > punchTime)
            {
                currentPunchTime = punchTime;
                startPos = transform.position;
                isFistReturning = true;
                Debug.Log("Fist Returning");

            }
        }
    }


    /// <summary>
    /// Initiate fist attack
    /// Called from PlayerHands
    /// Request Server Movement
    /// </summary>
    /// <param name="direction"></param>
    public void StartFistAttack(Vector3 direction)
    { 
        StartFistAttackServerRpc(direction);
    }


    //Getters / Setters
    public bool LedgeGrabPossible
    {
        get { return ledgeGrabPossible; }
        set { ledgeGrabPossible = value; }
    }

    public void StopHanging()
    {
        ////TODO: See if fist is already returned
        //isFistReturning = true;
        //parent.transform.GetComponent<Rigidbody>().isKinematic = false;
        //parent.GetComponent<PlayerHands>().LedgeHangingFist = null;
    }




    #region Collisions
    private void OnTriggerEnter(Collider col)
    {
        if (IsServer)
        {
            //string layerName = LayerMask.LayerToName(collision.gameObject.layer);
            string tagName = col.gameObject.tag;

            //Collision with a player
            if (tagName == "Player")
            {

                //Determine if parent player
                if (col.transform == transform.parent)
                {

                    //Upon first trigger, set up original position and rotation
                    if (originalPos == Vector3.zero)
                    {
                        originalPos = transform.parent.transform.position - transform.position;
                        originalRot = transform.rotation;
                        FistReturnClientRpc();
                    }

                    isFistLaunching = false;
                    isFistReturning = false;

                    //Set fist to originpoint
                    transform.position = transform.parent.transform.position + originalPos;
                    transform.rotation = originalRot;

                    FistReturnClientRpc();
                }
            }
        }
    }


    private void OnCollisionEnter(Collision collision)
    {
        if (IsServer)
        {
            //string layerName = LayerMask.LayerToName(collision.gameObject.layer);
            //string tagName = collision.gameObject.tag;


            ////Collision with a player
            //if (tagName == "Player")
            //{

                    //Collision with parent
                    ////Transfer the item to the player
                    //if (characterHands.HeldItem != null && transform.childCount > 0)
                    //{
                    //    transform.GetChild(0).transform.parent = parent.transform;
                    //}
                //}

                //    //Collision with another player
                //    else
                //    {

                //        if (fistLaunching)
                //        {
                //            fistLaunching = false;
                //            fistReturning = true;

                //            //TODO determine amount of damage(0-1)
                //            float damage = 1; 

                //            Vector3 fistDir = transform.right;
                //            float angle = Vector3.Angle(transform.right, Vector3.right);

                //            //Check if player is on the ground (effects fistDir)
                //            RaycastHit hit;
                //            if (Physics.Raycast(collision.transform.position, Vector3.down, out hit, collision.transform.GetComponent<Collider>().bounds.size.y / 2 + 0.1f, ~LayerMask.NameToLayer("Ground"))){

                //                //Fist hit at a upward trajectory
                //                if (fistDir.y > 0)
                //                {
                //                    //Shot from left side
                //                    if (angle < 20)
                //                    {
                //                        fistDir = Quaternion.AngleAxis(20, Vector3.forward) * transform.right;
                //                    }

                //                    //Shot from right side
                //                    if (angle > 160)
                //                    {
                //                        fistDir = Quaternion.AngleAxis(-20, Vector3.forward) * transform.right;
                //                    }
                //                }

                //                //Fist hit at a downward trajectory
                //                if (fistDir.y <= 0)
                //                {

                //                    //Shot from left or right
                //                    if (angle < 20 || angle > 160)
                //                    {   
                //                        fistDir = Vector3.Reflect(fistDir, hit.normal);
                //                        damage *= 0.7f;
                //                    }

                //                    //Shot from above
                //                    if (angle >= 20 && angle <= 160)
                //                    {
                //                        fistDir = Vector3.Reflect(fistDir, hit.normal);
                //                        damage *= 0.5f;
                //                    }
                //                }
                //            }

                //            collision.gameObject.GetComponent<PlayerController>().DealDamage(damage, fistDir);
                //        }
                //    }
            //}

            //if (layerName == "Ground")
            //{
            //    //Normal collide with the ground
            //    if (!LedgeGrabPossible)
            //    {
            //        fistLaunching = false;
            //        fistReturning = true;
            //    }

            //    //Collision looking for hangGrab
            //    else
            //    {
            //        if (parent.GetComponent<Character_Hands>().LedgeHangingFist == null && fistLaunching)
            //        {
            //            parent.GetComponent<Character_Hands>().LedgeHangingFist = gameObject;
            //            fistLaunching = false;
            //            fistReturning = false;
            //        }
            //    }

            //}

            //if (tagName == "Item")
            //{
            //    if (characterHands.HeldItem == null)
            //    {
            //        fistLaunching = false;
            //        fistReturning = true;

            //        characterHands.HeldItem = collision.transform.gameObject;
            //        collision.gameObject.layer = LayerMask.NameToLayer("Ignore Collision");
            //        collision.gameObject.GetComponent<Rigidbody>().isKinematic = true;
            //        collision.transform.parent = transform;
            //    }
            //}
        }
    }

    #endregion


    /// <summary>
    /// Initiate the punch attack in the direction given
    /// </summary>
    /// <param name="direction"></param>
    [ServerRpc]
    private void StartFistAttackServerRpc(Vector3 direction)
    {
        isFistLaunching = true;
        startPos = transform.position;
        fistDirection = direction;
        currentPunchTime = 0f;
    }


    /// <summary>
    /// Client request to update queued fists
    /// </summary>
    [ClientRpc]
    private void FistReturnClientRpc()
    {
        ulong fistID = GetComponent<NetworkObject>().NetworkObjectId;
        transform.parent.GetComponent<PlayerHands>().FistReturned(fistID);
    }

}
