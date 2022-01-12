using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkDebugUI : MonoBehaviour
{

    [SerializeField] private Button startHostButton;

    [SerializeField] private Button startClientButton;

    [SerializeField] private TextMeshProUGUI numPlayers;

    private void Awake()
    {
        Cursor.visible = true;

    }

    // Start is called before the first frame update
    void Start()
    {
        startHostButton.onClick.AddListener(() =>
        {
            if (NetworkManager.Singleton.StartHost())
            {
                Debug.Log("Host Started");
            }
            else
            {
                Debug.Log("Host Didnt Start");
            }
        });

        startClientButton.onClick.AddListener(() =>
        {
            if (NetworkManager.Singleton.StartClient())
            {
                Debug.Log("Client Started");
            }
            else
            {
                Debug.Log("Client Didnt Start");
            }
        });

    }

    // Update is called once per frame
    void Update()
    {
        numPlayers.text = "Players in game: " + NetworkPlayerManager.instance.PlayersInGame;   
    }
}
