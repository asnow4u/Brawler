using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FistController : MonoBehaviour
{

    public float punchTime;
    public float punchDist;

    private bool fistLaunch;
    private bool fistReturn;

    private GameObject parent;
    private Character_Hands characterHands;

    private Vector3 dir;
    private Vector3 originalPos;
    private float currentPunchTime;

    private bool hasItem;

    // Start is called before the first frame update
    void Start()
    {
        fistLaunch = false;
        fistReturn = false;

        parent = transform.parent.gameObject;
        characterHands = parent.transform.parent.transform.GetComponent<Character_Hands>();

        hasItem = false;
    }

    // Update is called once per frame
    void Update()
    {
        DeterminCollision();

        if (fistLaunch)
        {

          currentPunchTime += Time.deltaTime;

          if (currentPunchTime > punchTime)
          {
              currentPunchTime = punchTime;
              fistLaunch = false;
              fistReturn = true;
          }

          float increment = currentPunchTime / punchTime;
          increment = Mathf.Sin(increment * Mathf.PI * 0.5f); //https://chicounity3d.wordpress.com/2014/05/23/how-to-lerp-like-a-pro/
          transform.position = Vector3.Lerp(originalPos, originalPos + dir * punchDist, increment);
          transform.LookAt(originalPos + dir * punchDist);
        }

        if (fistReturn)
        {

          currentPunchTime -= Time.deltaTime;

          if (currentPunchTime < 0f)
          {
              currentPunchTime = 0f;
              fistReturn = false;
              transform.parent = parent.transform;
              characterHands.FistReturned(gameObject);
          }

          float increment = currentPunchTime / punchTime;
          increment = Mathf.Sin(increment * Mathf.PI * 0.5f); //https://chicounity3d.wordpress.com/2014/05/23/how-to-lerp-like-a-pro/
          transform.position = Vector3.Lerp(characterHands.GetFistStartPosition(gameObject), originalPos + dir * punchDist, increment);
          transform.LookAt(characterHands.GetFistStartPosition(gameObject));
        }
    }

    private void DeterminCollision()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, 0.1f))
        {
            string layerName = LayerMask.LayerToName(hit.transform.gameObject.layer);

            //List of available collisions
            if (layerName == "Ground")
            {
                fistLaunch = false;
                fistReturn = true;
            }

            else if (layerName == "Item")
            {
                Destroy(hit.transform.gameObject.GetComponent<Rigidbody>()); //TODO want to disable instead of destroy
                hit.transform.gameObject.GetComponent<BoxCollider>().enabled = false;
                hit.transform.parent = transform;
                fistLaunch = false;
                fistReturn = true;
            }
        }
    }


    public void fistAttack(Vector3 direction){
        
        if (!hasItem)
        {
            transform.parent = null;
            dir = direction;
            originalPos = transform.position;
            currentPunchTime = 0f;
            fistLaunch = true;
        }

        else
        {
            transform.GetChild(0).GetComponent<Item>().UseItem();
        }

    }


    public void fistPowerAttack(Vector3 direction)
    {
        if (!hasItem)
        {
            //Power attack
        }

        else
        {
            Transform item = transform.GetChild(0);
            hasItem = false;
            item.transform.parent = null;
            item.GetComponent<Item>().ThrowItem(dir);
        }
    }


}
