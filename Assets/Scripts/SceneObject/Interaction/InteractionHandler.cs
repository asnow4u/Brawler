using Game.Interactable;
using Game.SceneObjects;
using Game.SceneObjects.Equipment;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InteractionHandler : SceneObjectHandler
{
    private List<IInteractable> availableInteractables = new List<IInteractable>();        

    public override void RegisterToEvents()
    {
        
    }

    public override void UnregisterToEvents()
    {
        
    }

    public override void Setup()
    { }

    /// <summary>
    /// Update what available interactables are within interacting range.
    /// </summary>
    public void CheckForInteractables()
    {
        availableInteractables.Clear();

        //TODO: Refine by making public feilds to be set in inspector
        Collider[] colliders = Physics.OverlapBox(transform.position, new Vector3(2.5f, 3f, 1f), Quaternion.identity, LayerMask.GetMask("Interactable"), QueryTriggerInteraction.Collide);
        
        foreach (Collider collider in colliders)
        {
            collider.TryGetComponent(out IInteractable interactable);
                availableInteractables.Add(interactable);                
        }
    }

    /// <summary>
    /// Interact with the closest interactable.
    /// </summary>
    public void InitiateInteraction()
    {
        // TODO: Check which interactable is closest
        IInteractable interactable = availableInteractables.FirstOrDefault();
        if (interactable != null && interactable is Weapon weapon)
        {
            sceneObject.EquipmentHandler.WeaponHandler.AddWeapon(weapon);
        }
    }
}
