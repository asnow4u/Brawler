using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponPickUp : Interactable
{
    protected override void InputReceived(SceneObject sceneObj)
    {
        if (transform.GetChild(0).TryGetComponent(out Weapon weapon))
        {
            //Setup add sceneObject attackpoints to weapon
            if (sceneObj.AttackInputHandler != null)
            {
                foreach (GameObject attackPointObj in sceneObj.AttackInputHandler.BaseAttackCollection.AttackPointCollection.AttackPoints)
                {
                    weapon.AttackCollection.AttackPointCollection.AttackPoints.Add(attackPointObj);
                }
            }

            sceneObj.EquipmentHandler.Weapons.AddWeapon(weapon);
        }

        sceneObj.InteractionHandler.UnregisterToInputEvent(InputReceived);

        Destroy(gameObject);
    }
}
