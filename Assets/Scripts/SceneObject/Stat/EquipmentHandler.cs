using System;
using UnityEngine;

internal class EquipmentHandler : StatHandler
{    
    //Componenets
    IInteractionInput interactionInput = null;
    IEquipmentInput equipmentInput = null;
    private WeaponHandler weaponHandler = null;

    [SerializeField] private float pickupRange = 1f;    


    #region Initialize

    protected override void Awake()
    {
        interactionInput = GetComponent<IInteractionInput>();
        equipmentInput = GetComponent<IEquipmentInput>();
        weaponHandler = GetComponentInChildren<WeaponHandler>();        

        base.Awake();
    }

    protected override void RegisterToEvents()
    {
        if (interactionInput != null)
            interactionInput.InteractionPerformedEvent += OnInteractionPerformed;

        if (equipmentInput != null)
            equipmentInput.ToggleEquippedWeaponEvent += OnToggleEquippedWeapon;

        if (weaponHandler != null)
            weaponHandler.OnWeaponEquippedEvent += OnWeaponChanged;        

        base.RegisterToEvents();
    }

    protected override void Start()
    {
        if (weaponHandler != null)
            weaponHandler.Initialize();        

        base.Start();
    }

    protected override void UnregisterFromEvents()
    {
        if (interactionInput != null)
            interactionInput.InteractionPerformedEvent -= OnInteractionPerformed;

        if (equipmentInput != null)
            equipmentInput.ToggleEquippedWeaponEvent -= OnToggleEquippedWeapon;

        if (weaponHandler != null)
            weaponHandler.OnWeaponEquippedEvent -= OnWeaponChanged;

        base.UnregisterFromEvents();
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

    private void OnToggleEquippedWeapon()
    {
        weaponHandler.SwapEquippedWeapon();
    }

    private void OnWeaponChanged(IWeapon weapon)
    {
        if (weapon == null || weapon.WeaponData == null)
            return;

        UpdateRBMass();
        UpdateAnimationStats(weapon.WeaponData.MovementCollection, weapon.WeaponData.AttackCollection);
        UpdateMovementStats(weapon.WeaponData.MovementCollection);
        UpdateAttackStats(ParseWeaponAttackData(weapon));
    }

    private void UpdateRBMass()
    {
        float accumulatedMass = 0f;

        if (weaponHandler.EquippedWeapon != null)
            accumulatedMass += weaponHandler.EquippedWeapon.Mass;

        rb.mass = Mathf.Clamp(baseSceneObjectData.MinMass + accumulatedMass, baseSceneObjectData.MinMass, baseSceneObjectData.MaxMass);
    }

    protected virtual AttackStatData ParseWeaponAttackData(IWeapon weapon)
    {
        if (weapon == null || weapon.WeaponData == null) return null;

        AttackStatData data = new AttackStatData();
        data.WeaponRootGameObject = weapon.gameObject;
        data.SwingEffect = weapon.SwingEffect;

        AttackDataCollection attackData = weapon.WeaponData.AttackCollection;

        if (attackData.UpTiltData != null)
            data.UpTilt = new AttackStatData.AttackStats(0, attackData.UpTiltData);

        if (attackData.DownTiltData != null)
            data.DownTilt = new AttackStatData.AttackStats(1, attackData.DownTiltData);

        if (attackData.ForwardTiltData != null)
            data.ForwardTilt = new AttackStatData.AttackStats(2, attackData.ForwardTiltData);

        if (attackData.UpAirData != null)
            data.UpAir = new AttackStatData.AttackStats(3, attackData.UpAirData);

        if (attackData.DownAirData != null)
            data.DownAir = new AttackStatData.AttackStats(4, attackData.DownAirData);

        if (attackData.ForwardAirData != null)
            data.ForwardAir = new AttackStatData.AttackStats(5, attackData.ForwardAirData);

        return data;
    }
}
