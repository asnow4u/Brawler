using System;
using UnityEngine;

internal class EquipmentHandler : StatHandler
{    
    //Componenets
    IInteractionInput interactionInput = null;
    private WeaponHandler weaponHandler = null;

    [SerializeField] private float pickupRange = 1f;    


    #region Initialize

    protected override void Awake()
    {
        interactionInput = GetComponent<IInteractionInput>();
        weaponHandler = GetComponentInChildren<WeaponHandler>();
        
        base.Awake();
    }

    protected override void RegisterToEvents()
    {
        if (interactionInput != null)
            interactionInput.InteractionPerformedEvent += OnInteractionPerformed;

        if (weaponHandler != null)
            weaponHandler.OnWeaponEquippedEvent += OnWeaponChanged;
    }

    protected override void Start()
    {
        base.Start();

        if (weaponHandler != null)
            weaponHandler.Initialize();        
    }

    protected override void UnregisterFromEvents()
    {
        if (interactionInput != null)
            interactionInput.InteractionPerformedEvent -= OnInteractionPerformed;

        if (weaponHandler != null)
            weaponHandler.OnWeaponEquippedEvent -= OnWeaponChanged;
    }

    #endregion

    private void OnInteractionPerformed()
    {
        if (actionState.CurActionState == ActionState.Idle ||
            actionState.CurActionState == ActionState.Moving)
        {
            Bounds bounds = sceneObject.Bounds;
            Collider[] interactableColliders = Physics.OverlapBox(bounds.center, new Vector3(bounds.extents.x + pickupRange / 2, bounds.extents.y, bounds.extents.z), Quaternion.identity, LayerMask.GetMask("Item"));

            if (interactableColliders == null || interactableColliders.Length > 0)
                return;

            if (interactableColliders[0].gameObject.TryGetComponent(out IItem item))
            {
                item.DisableInteraction();

                if (item is IWeapon weapon)
                    weaponHandler.HandleWeaponPickup(weapon);
            }
        }
    }

    private void OnWeaponChanged(IWeapon weapon)
    {
        if (weapon == null || weapon.WeaponData == null)
            return;

        UpdateRBMass();
        UpdateAnimationStats(weapon.WeaponData.MovementCollection, weapon.WeaponData.AttackCollection);
        UpdateMovementStats(weapon.WeaponData.MovementCollection);
        UpdateAttackStats(weapon.WeaponData.AttackCollection);
    }

    private void UpdateRBMass()
    {
        float accumulatedMass = 0f;

        if (weaponHandler.EquippedWeapon != null)
            accumulatedMass += weaponHandler.EquippedWeapon.Mass;

        rb.mass = Mathf.Clamp(baseSceneObjectData.MinMass + accumulatedMass, baseSceneObjectData.MinMass, baseSceneObjectData.MaxMass);
    }
}
