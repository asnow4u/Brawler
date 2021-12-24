using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character_Hands : MonoBehaviour
{
    public GameObject rightFist;
    public GameObject leftFist;
    public GameObject rightFistHolder;
    public GameObject leftFistHolder;
    
    private bool rightOut;
    private bool leftOut;

    private enum NextFist {right, left};
    private NextFist nextFist;

    // Start is called before the first frame update
    void Start()
    {
        rightOut = false;
        leftOut = false;

        nextFist = NextFist.right;
    }


    // Update is called once per frame
    void Update()
    {
        //Need to determine if item is being held and have all buttons relate to that instead of doing fistAttack and powerFistAttack

        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = Vector3.zero;
            float distance;
            Plane plane = new Plane(transform.forward, 0);
            
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (plane.Raycast(ray, out distance))
            {
                mousePos = ray.GetPoint(distance).normalized;
            }

            if (!rightOut && nextFist == NextFist.right)
            {
                rightOut = true;
                nextFist = NextFist.left;
                rightFist.GetComponent<FistController>().fistAttack(mousePos);
            }

            else if (!leftOut && nextFist == NextFist.left)
            {
                leftOut = true;
                nextFist = NextFist.right;
                leftFist.GetComponent<FistController>().fistAttack(mousePos);
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            if (!rightOut && nextFist == NextFist.right)
            {
                rightOut = true;
                nextFist = NextFist.left;
                rightFist.GetComponent<FistController>().fistPowerAttack(Vector3.right);
            }

            else if (!leftOut && nextFist == NextFist.left)
            {
                leftOut = true;
                nextFist = NextFist.right;
                leftFist.GetComponent<FistController>().fistPowerAttack(Vector3.right);
            }
        }

        //TODO: Timer between punch throws
    }

    public void FistReturned(GameObject go)
    {
        if (go == rightFist)
        {
            rightOut = false;
        }

        if (go == leftFist)
        {
            leftOut = false;
        }
    }

    public GameObject GetFistHoldingParent(GameObject go)
    {
        if (go == rightFist)
        {
            return rightFistHolder;
        }

        if (go == leftFist)
        {
            return leftFistHolder;
        }

        return null;
    }

    public Vector3 GetFistStartPosition(GameObject go)
    {
        if (go == rightFist)
        {
            return rightFistHolder.transform.position;
        }

        if (go == leftFist)
        {
            return leftFistHolder.transform.position;
        }

        Debug.LogError(go + " does not match one of the fists");
        return Vector3.zero;
    }


}
