using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class LevelPortal : Interactable
{
    public LevelType Destination;

    protected override void InputReceived(SceneObject sceneObj)
    {
        //TODO: Go through GameManager to load the scene nessisary

        switch (Destination) 
        {
            case LevelType.Forest:
                break;

            case LevelType.Desert:
                break;
        }
    }
}
