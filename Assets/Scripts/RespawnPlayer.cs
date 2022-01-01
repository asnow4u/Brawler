//
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RespawnPlayer : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Transform respawnPoint;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            player.transform.position = respawnPoint.transform.position;
            Physics.SyncTransforms();
            Rigidbody rb = other.GetComponent<Rigidbody>();
            rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        }

        if (other.CompareTag("Item"))
        {
            Destroy(other.gameObject);
        }

    }

}
