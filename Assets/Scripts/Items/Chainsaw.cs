using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chainsaw : Item, IItemInterface
{
    // Start is called before the first frame update
    void Start()
    {
        base.Start();
    }

    // Update is called once per frame
    void Update()
    {
        base.Update();
    }


    public void UseItem()
    {
        Debug.Log("ACTIVATED");
        Debug.Log(gameObject);
    }
}
