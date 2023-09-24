using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickUpable : Interactable
{
    protected override void InputReceived(SceneObject sceneObj)
    {
        base.InputReceived(sceneObj);

        if (transform.GetChild(0).TryGetComponent(out Weapon weapon))
        {
            sceneObj.equipmentHandler.Weapons.AddWeapon(weapon);
        }

        sceneObj.interactionHandler.UnregisterToInputEvent(InputReceived);

        Destroy(gameObject);
    }
}
