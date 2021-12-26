using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character_Hands : MonoBehaviour
{
    public GameObject rightFist;
    public GameObject leftFist;
    private List<GameObject> fistQueue = new List<GameObject>();

    public float launchForce;
    private bool canLaunch;

    private GameObject heldItem;

    // Start is called before the first frame update
    void Start()
    {
        fistQueue.Add(rightFist);
        fistQueue.Add(leftFist);
        canLaunch = true;
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

        //TODO: Timer between punch throws
    }


    /// <summary>
    /// Left Mouse button clicked
    /// Use item if item is being held
    /// Shoot queued fist
    /// </summary>
    private void LeftMouseClick()
    {
        //TODO: Get the correct mouse pos
        //Get Mouse Pos
        Vector3 mousePos = Vector3.zero;
        float distance;
        Plane plane = new Plane(transform.forward, 0);

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (plane.Raycast(ray, out distance))
        {
            mousePos = ray.GetPoint(distance).normalized;
        }

        //UseItem
        if (heldItem != null)
        {
            if (heldItem.transform.IsChildOf(transform))
            {
                heldItem.GetComponent<Item>().UseItem();
            }
        }

        //Attack
        else if (fistQueue.Count > 0)
        {
            fistQueue[0].transform.GetComponent<FistController>().FistAttack(mousePos);
            fistQueue.Remove(fistQueue[0]);
        }
    }

    private void RightMouseClick()
    {
        //Get Mouse Pos
        Vector3 mousePos = Vector3.zero;
        float distance;
        Plane plane = new Plane(transform.forward, 0);

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (plane.Raycast(ray, out distance))
        {
            mousePos = ray.GetPoint(distance).normalized;
        }

        //UseItem
        if (heldItem != null)
        {
            if (heldItem.transform.IsChildOf(transform))
            {
                heldItem.GetComponent<Item>().ThrowItem(mousePos);
            }
        }

        //Attack
        else if (fistQueue.Count > 0)
        {
            if (canLaunch)
            {
                transform.GetComponent<Rigidbody>().AddForce(launchForce * mousePos);
                canLaunch = false;
                //TODO: need to reset canLaunch to true when the ground is hit
            }
        }
    }

    public void FistReturned(GameObject go)
    {
        fistQueue.Add(go);
    }

    public GameObject HeldItem
    {
        get { return heldItem; }
        set { heldItem = value; }
    }

}
