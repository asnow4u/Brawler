using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformLedge : MonoBehaviour
{

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PlayerFist"))
        {
            other.gameObject.GetComponent<FistController>().LedgeGrabPossible = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("PlayerFist"))
        {
            other.gameObject.GetComponent<FistController>().LedgeGrabPossible = false;
        }
    }
}
