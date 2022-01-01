using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class testCube : Item, IItemInterface
{
    // Start is called before the first frame update
    new void Start()
    {  
        base.Start();
    }

    // Update is called once per frame
    new void Update()
    {
        base.Update();
    }


    public void UseItem()
    {
        Debug.Log("ACTIVATED");
        Debug.Log(gameObject);
    }
}
