using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character_Hands : MonoBehaviour
{
    public GameObject rightFist;
    public GameObject leftFist;
    public List<GameObject> fistQueue = new List<GameObject>();

    public float launchForce;
    private bool canLaunch;

    private GameObject ledgeHangingFist;

    private Vector3 mouseDir;

    private GameObject heldItem;

    // Start is called before the first frame update
    void Start()
    {
        fistQueue.Add(rightFist);
        fistQueue.Add(leftFist);
        canLaunch = true;
        ledgeHangingFist = null;
        heldItem = null;
    }


    /// <summary>
    /// Check when a button is clicked
    /// </summary>
    void Update()
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


        //UseItem
        if (heldItem != null)
        {
            if (heldItem.transform.IsChildOf(transform))
            {
                heldItem.GetComponent<IItemInterface>().UseItem();
            }
        }

        //Attack
        else if (fistQueue.Count > 0)
        {
            fistQueue[0].transform.GetComponent<FistController>().FistAttack(mouseDir);
            fistQueue.Remove(fistQueue[0]);
        }
    }

    private void RightMouseClick()
    {
        //Get Mouse Pos
        Vector3 mouseDir = Vector3.zero;
        float distance;
        Plane plane = new Plane(transform.forward, 0);

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (plane.Raycast(ray, out distance))
        {
            mouseDir = (ray.GetPoint(distance) - transform.position).normalized;
        }

        //UseItem
        if (heldItem != null)
        {
            if (heldItem.transform.IsChildOf(transform))
            {
                heldItem.GetComponent<Item>().ThrowItem(mouseDir);
            }
        }

        //Attack
        else if (fistQueue.Count > 0)
        {
            if (canLaunch)
            {
                //transform.GetComponent<Rigidbody>().velocity = Vector3.zero;
                //transform.GetComponent<Rigidbody>().AddForce(launchForce * mouseDir);
                //canLaunch = false;
                //TODO: need to reset canLaunch to true when the ground is hit
            }
        }
    }

    public void FistReturned(GameObject go)
    {
        if (!fistQueue.Contains(go))
            fistQueue.Add(go);
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
