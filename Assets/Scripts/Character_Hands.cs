using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character_Hands : MonoBehaviour
{
    public GameObject rightFist;
    public GameObject leftFist;

    private Vector3 rightFistPos;
    private Vector3 leftFistPos;

    private bool rightOut;
    private bool leftOut;

    private enum NextFist {right, left};
    private NextFist nextFist;

    // Start is called before the first frame update
    void Start()
    {
        rightOut = false;
        leftOut = false;

        rightFistPos = rightFist.transform.position;
        leftFistPos = leftFist.transform.position;

        nextFist = NextFist.right;
    }


    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (!rightOut && nextFist == NextFist.right)
            {
                rightOut = true;
                nextFist = NextFist.left;
                rightFist.GetComponent<FistController>().LaunchFist();
            }

            else if (!leftOut && nextFist == NextFist.left)
            {
                leftOut = true;
                nextFist = NextFist.right;
                leftFist.GetComponent<FistController>().LaunchFist();
            }
        }

        //TODO: Timer between punch throws
    }


    private void OnCollisionEnter(Collision col)
    {
      if (col.gameObject == rightFist)
      {
        //TODO: reset position
      }

      if (col.gameObject == leftFist)
      {
        //TODO: reset position
      }
    }

}
