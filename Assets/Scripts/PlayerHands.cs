using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerHands : NetworkBehaviour
{

    [SerializeField] private GameObject rightFistPrefab;
    [SerializeField] private GameObject leftFistPrefab;

    [SerializeField] private List<NetworkObject> fistQueue = new List<NetworkObject>();

    private float fistOffset = 0.2f;
    private Vector3 mouseDir;

    private GameObject ledgeHangingFist;
    private GameObject heldItem;

    public float launchForce;
    private bool canLaunch;
   

    // Start is called before the first frame update
    void Start()
    {

        //TODO: For some reason if we try to spawn the left and right fist in OnNetworkSpawn, there is a second copy of the first fist that is spawned that only appears on the client side and for no one else.
        if (IsServer)
        {
            ulong clientID = OwnerClientId;

            GameObject rightFist = Instantiate(rightFistPrefab, transform.position + new Vector3(0, 0, -fistOffset), Quaternion.identity); ;
            rightFist.GetComponent<NetworkObject>().SpawnWithOwnership(clientID, true);

            rightFist.transform.SetParent(transform);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {

            ulong clientID = OwnerClientId;

            //GameObject rightFist = Instantiate(rightFistPrefab, transform.position, Quaternion.identity);
            //rightFist.GetComponent<NetworkObject>().SpawnWithOwnership(clientID, true);
            //ulong rightFistID = rightFist.GetComponent<NetworkObject>().NetworkObjectId;

            //Debug.Log("Right Fist Spawned");

            GameObject leftFist = Instantiate(leftFistPrefab, transform.position + new Vector3(0, 0, fistOffset), Quaternion.identity);
            leftFist.GetComponent<NetworkObject>().SpawnWithOwnership(clientID, true);
            
            leftFist.transform.SetParent(transform);
        }
    }


    /// <summary>
    /// Check when a button is clicked
    /// </summary>
    void Update()
    {

        if (IsClient && IsOwner)
        {
            //Left Mouse Button
            if (Input.GetMouseButtonDown(0))
            {
                LeftMouseClick();
            }

            //Right Click Button
            if (Input.GetMouseButtonDown(1))
            {
                RightMouseClick();
            }
        }
    }


    /// <summary>
    /// Left Mouse button clicked
    /// Use item if item is being held
    /// Shoot queued fist
    /// </summary>
    private void LeftMouseClick()
    {

        //Get Mouse Pos
        mouseDir = Vector3.zero;
        float distance;
        Plane plane = new Plane(-transform.forward, 0);

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (plane.Raycast(ray, out distance))
        {
            mouseDir = (ray.GetPoint(distance) - transform.position).normalized;
        }


        ////UseItem
        //if (heldItem != null)
        //{
        //    if (heldItem.transform.IsChildOf(transform))
        //    {
        //        heldItem.GetComponent<IItemInterface>().UseItem();
        //    }
        //}

        //Attack
        //else 
        if (fistQueue.Count > 0)
        {
            fistQueue[0].transform.GetComponent<FistController>().StartFistAttack(mouseDir);
            fistQueue.Remove(fistQueue[0]);
        }
    }

    private void RightMouseClick()
    {
        ////Get Mouse Pos
        //Vector3 mouseDir = Vector3.zero;
        //float distance;
        //Plane plane = new Plane(transform.forward, 0);

        //Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        //if (plane.Raycast(ray, out distance))
        //{
        //    mouseDir = (ray.GetPoint(distance) - transform.position).normalized;
        //}

        ////UseItem
        //if (heldItem != null)
        //{
        //    if (heldItem.transform.IsChildOf(transform))
        //    {
        //        heldItem.GetComponent<Item>().ThrowItem(mouseDir);
        //    }
        //}

        ////Attack
        //else if (fistQueue.Count > 0)
        //{
        //    if (canLaunch)
        //    {
        //        //transform.GetComponent<Rigidbody>().velocity = Vector3.zero;
        //        //transform.GetComponent<Rigidbody>().AddForce(launchForce * mouseDir);
        //        //canLaunch = false;
        //        //TODO: need to reset canLaunch to true when the ground is hit
        //    }
        //}
    }


    public void FistReturned(ulong fistID)
    {
        NetworkObject fist = GetNetworkObject(fistID);

        if (!fistQueue.Contains(fist))
            fistQueue.Add(fist);
    }


    //Getter / Setter
    public GameObject HeldItem
    {
        get { return heldItem; }
        set { heldItem = value; }
    }

    public GameObject LedgeHangingFist
    {
        get { return ledgeHangingFist; }
        set { ledgeHangingFist = value; }
    }

}
