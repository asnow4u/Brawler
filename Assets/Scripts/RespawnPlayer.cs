//
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class RespawnPlayer : NetworkBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Transform respawnPoint;


    // Start is called before the first frame update
    void Start()    
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        //if (IsServer)
        //{
            if (other.gameObject.tag == "Player")
            {
                other.transform.position = respawnPoint.transform.position;
                //player.transform.position = respawnPoint.transform.position;
                Physics.SyncTransforms();
                Rigidbody rb = other.GetComponent<Rigidbody>();
                rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
                Debug.Log("That's the player, uh huh!");
            }

            if (other.CompareTag("Item"))
            {
                Destroy(other.gameObject);
            }
        //}
    }

}
