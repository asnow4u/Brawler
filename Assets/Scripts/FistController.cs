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
    private Vector3 parentPositionDif;

    private Vector3 dir;
    private Vector3 originalPos;
    private float currentPunchTime;

    // Start is called before the first frame update
    void Start()
    {
        fistLaunch = false;
        fistReturn = false;

        parent = transform.parent.gameObject;
        characterHands = parent.transform.GetComponent<Character_Hands>();
        parentPositionDif = parent.transform.position - transform.position;

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

                //Transfer the item to the player
                if (characterHands.HeldItem != null && transform.childCount > 0)
                {
                    transform.GetChild(0).transform.parent = parent.transform;
                }
            }

            float increment = currentPunchTime / punchTime;
            increment = Mathf.Sin(increment * Mathf.PI * 0.5f); //https://chicounity3d.wordpress.com/2014/05/23/how-to-lerp-like-a-pro/
            transform.position = Vector3.Lerp(parent.transform.position + parentPositionDif, originalPos + dir * punchDist, increment);
            transform.LookAt(parent.transform.position + parentPositionDif);
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
                if (characterHands.HeldItem == null)
                {
                    fistLaunch = false;
                    fistReturn = true;
                    characterHands.HeldItem = hit.transform.gameObject;

                    hit.transform.gameObject.GetComponent<Rigidbody>().isKinematic = true;
                    hit.transform.gameObject.GetComponent<Collider>().enabled = false;
                    hit.transform.parent = transform;
                }
            }
        }
    }

    public void FistAttack(Vector3 direction){
        transform.parent = null;
        dir = direction;
        originalPos = transform.position;
        currentPunchTime = 0f;
        fistLaunch = true;
    }
}
