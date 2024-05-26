using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIFactory : MonoBehaviour
{
    public static UIFactory Instance;
    
    public GameObject DamageBubble;


    private void Start()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this);
    }


    public void SpawnDamageBubble(Vector3 pos, float damageValue)
    {
        GameObject go = Instantiate(DamageBubble);
        go.transform.position = pos;

        if (go.TryGetComponent(out DamageBubble damageBubble))
        {
            damageBubble.Initialize(damageValue);
        }
    }
}
