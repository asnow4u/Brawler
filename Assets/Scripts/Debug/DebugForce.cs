using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugForce : MonoBehaviour
{
    public bool AdmitForce = false;

    public Vector3 direction;
    public float power;

    private void Update()
    {
        if (AdmitForce)
        {
            AdmitForce = false;
            GetComponent<Rigidbody>().AddForce(direction.normalized * power, ForceMode.Impulse);
        }
    }
}
