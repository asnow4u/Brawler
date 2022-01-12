using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkPlayerManager : MonoBehaviour
{
    public static NetworkPlayerManager instance;


    private NetworkVariable<int> playerInGame = new NetworkVariable<int>();

    public int PlayersInGame
    {
        get { return playerInGame.Value; }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        } else
        {
            Destroy(this);
        }

    }

    // Start is called before the first frame update
    void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += (id) =>
        {
            if (NetworkManager.Singleton.IsServer)
            {
                Debug.Log(id + " joined");
                playerInGame.Value++;
            }
        };

        NetworkManager.Singleton.OnClientDisconnectCallback += (id) =>
        {
            if (NetworkManager.Singleton.IsServer)
            {
                Debug.Log(id + " left");
                playerInGame.Value--;
            }
        };


    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
