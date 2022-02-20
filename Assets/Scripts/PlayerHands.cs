using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerHands : NetworkBehaviour
{

    [SerializeField] private GameObject rightFistPrefab;
    [SerializeField] private GameObject leftFistPrefab;

    [SerializeField] private List<GameObject> fistQueue = new List<GameObject>();

    private float fistOffset = 0.2f;
    private Vector3 mouseDir;

    private GameObject ledgeHangingFist;
    private GameObject heldItem;

    public float launchForce;
    private bool canLaunch;

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


    // Start is called before the first frame update
    void Start()
    {
        if (isLocalPlayer)
        {
            Debug.Log("Start Player Hand (Client)");
            CmdSpawnFist();
        }
    }


    /// <summary>
    /// Check when a button is clicked
    /// </summary>
    void Update()
    {

        if (isLocalPlayer)
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
            CmdFistAttack(fistQueue[0], mouseDir);
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


    public void FistReturned(GameObject fist)
    {
        Debug.Log("Fist added to queue");
        if (!fistQueue.Contains(fist))
            fistQueue.Add(fist);
    }


    [Command(requiresAuthority = false)]
    private void CmdSpawnFist()
    {
        Debug.Log(netId);
        Debug.Log("Spawn Right Fist (Server)");
        GameObject rightFist = Instantiate(rightFistPrefab, transform.position + new Vector3(0, 0, rightFistPrefab.transform.position.z), Quaternion.identity);
        NetworkServer.Spawn(rightFist);
        rightFist.GetComponent<FistController>().InitFist(transform, netId);

        Debug.Log("Spawn Right Fist (Server)");
        GameObject leftFist = Instantiate(leftFistPrefab, transform.position + new Vector3(0, 0, leftFistPrefab.transform.position.z), Quaternion.identity); ;
        NetworkServer.Spawn(leftFist);
        leftFist.GetComponent<FistController>().InitFist(transform, netId);
    }

    [Command]
    private void CmdFistAttack(GameObject fist, Vector3 dir)
    {
        Debug.Log("Fist Attack Setup (Server)");
        fist.transform.GetComponent<FistController>().StartFistAttack(dir);
    }
}
