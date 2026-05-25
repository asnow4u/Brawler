using System;
using System.Collections.Generic;
using UnityEngine;

internal class WeaponHandler : MonoBehaviour
{
    [SerializeField] private Transform grabPoint;

    [SerializeField] private IWeapon equippedWeapon;
    [SerializeField] private IWeapon secondaryWeapon;

    public IWeapon EquippedWeapon => equippedWeapon;

    public event Action<IWeapon> OnWeaponEquippedEvent;


    private void Awake()
    {
        if (grabPoint == null)
            Debug.LogError("Weapon GrabPoint is Null", gameObject);
    }

    public void Initialize()
    {
        SetupStartingWeapons();        
    }

    private void SetupStartingWeapons()
    {
        IWeapon[] startingWeapons = GetComponentsInChildren<IWeapon>();

        if (startingWeapons == null || startingWeapons.Length == 0)
            return;

        if (startingWeapons.Length == 1)
            EquipWeapon(startingWeapons[0]);

        else if (startingWeapons.Length == 2)
        {
            AddWeaponToInventory(startingWeapons[1]);
            EquipWeapon(startingWeapons[0]);
        }

        else if (startingWeapons.Length > 2)
        {
            Debug.LogError("Too many starting weapons found in Weapon Inventory", gameObject);
            return;
        }
    }

    public void HandleWeaponPickup(IWeapon weapon)
    {        
        if (equippedWeapon != null && secondaryWeapon != null)
            DropEquippedWeapon();
        
        EquipWeapon(weapon);
    }
    
    private void EquipWeapon(IWeapon weapon)
    {
        if (weapon == secondaryWeapon)
        {
            AddWeaponToInventory(equippedWeapon);            
            UnsubscribeFromRuntimeDataChanges();
        }
        
        equippedWeapon = weapon;
        equippedWeapon.gameObject.SetActive(true);

        Quaternion rotationOffset = grabPoint.rotation * Quaternion.Inverse(weapon.GripPoint.rotation);
        equippedWeapon.transform.rotation = rotationOffset * equippedWeapon.transform.rotation;

        Vector3 positionOffset = grabPoint.position - weapon.GripPoint.position;
        equippedWeapon.transform.position += positionOffset;

        equippedWeapon.transform.SetParent(grabPoint.transform, true);

        OnWeaponEquippedEvent?.Invoke(equippedWeapon);

        SubscribeToRuntimeDataChanges();
    }

  

    private void AddWeaponToInventory(IWeapon weapon)
    {
        secondaryWeapon = weapon;
        secondaryWeapon.gameObject.SetActive(false);
        secondaryWeapon.transform.parent = transform;
    }

    private void DropEquippedWeapon()
    {
        throw new NotImplementedException();
    }
    
    public void SwapEquippedWeapon()
    {
        if (secondaryWeapon != null)
            EquipWeapon(secondaryWeapon);
    }


    #region Runtime Weapon Data Change

    private void SubscribeToRuntimeDataChanges()
    {
        #if UNITY_EDITOR
        if (equippedWeapon != null && equippedWeapon.WeaponData != null)
        {
            equippedWeapon.WeaponData.OnChangedEvent += OnWeaponDataChanged;

            SubscribeToMovementCollection(equippedWeapon.WeaponData.MovementCollection);
            SubscribeToAttackCollection(equippedWeapon.WeaponData.AttackCollection);
        }
        #endif
    }

    private void UnsubscribeFromRuntimeDataChanges()
    {
        #if UNITY_EDITOR
        if (equippedWeapon != null && equippedWeapon.WeaponData != null)
        {
            equippedWeapon.WeaponData.OnChangedEvent -= OnWeaponDataChanged;

            UnsubscribeFromMovementCollection(equippedWeapon.WeaponData.MovementCollection);
            UnsubscribeFromAttackCollection(equippedWeapon.WeaponData.AttackCollection);
        }
        #endif
    }


    #region Movement Data

    private void SubscribeToMovementCollection(MovementDataCollection movementData)
    {
        #if UNITY_EDITOR
        if (movementData == null) return;

        movementData.OnChangedEvent += OnMovementCollectionChanged;
        SubscribeToMovementData(movementData);
        #endif
    }

    private void UnsubscribeFromMovementCollection(MovementDataCollection movementData)
    {
        #if UNITY_EDITOR
        if (movementData == null) return;

        movementData.OnChangedEvent -= OnMovementCollectionChanged;
        UnsubscribeFromMovementData(movementData);
        #endif       
    }

    private void SubscribeToMovementData(MovementDataCollection movementData)
    {
        #if UNITY_EDITOR
        if (movementData == null) return;

        if (movementData.MoveData != null)       movementData.MoveData.OnChangedEvent       += OnWeaponDataChanged;
        if (movementData.AirMoveData != null)    movementData.AirMoveData.OnChangedEvent    += OnWeaponDataChanged;
        if (movementData.JumpData != null)       movementData.JumpData.OnChangedEvent       += OnWeaponDataChanged;
        if (movementData.AirJumpData != null)    movementData.AirJumpData.OnChangedEvent    += OnWeaponDataChanged;
        if (movementData.WallJumpData != null)   movementData.WallJumpData.OnChangedEvent   += OnWeaponDataChanged;
        if (movementData.ClimbMoveData != null)  movementData.ClimbMoveData.OnChangedEvent  += OnWeaponDataChanged;
        if (movementData.LedgeClimbData != null) movementData.LedgeClimbData.OnChangedEvent += OnWeaponDataChanged;
        if (movementData.WallLeanData != null)   movementData.WallLeanData.OnChangedEvent   += OnWeaponDataChanged;
        if (movementData.WallSlideData != null)  movementData.WallSlideData.OnChangedEvent  += OnWeaponDataChanged;
        #endif
    }

    private void UnsubscribeFromMovementData(MovementDataCollection movementData)
    {
        #if UNITY_EDITOR
        if (movementData == null) return;

        if (movementData.MoveData != null)       movementData.MoveData.OnChangedEvent       -= OnWeaponDataChanged;
        if (movementData.AirMoveData != null)    movementData.AirMoveData.OnChangedEvent    -= OnWeaponDataChanged;
        if (movementData.JumpData != null)       movementData.JumpData.OnChangedEvent       -= OnWeaponDataChanged;
        if (movementData.AirJumpData != null)    movementData.AirJumpData.OnChangedEvent    -= OnWeaponDataChanged;
        if (movementData.WallJumpData != null)   movementData.WallJumpData.OnChangedEvent   -= OnWeaponDataChanged;
        if (movementData.ClimbMoveData != null)  movementData.ClimbMoveData.OnChangedEvent  -= OnWeaponDataChanged;
        if (movementData.LedgeClimbData != null) movementData.LedgeClimbData.OnChangedEvent -= OnWeaponDataChanged;
        if (movementData.WallLeanData != null)   movementData.WallLeanData.OnChangedEvent   -= OnWeaponDataChanged;
        if (movementData.WallSlideData != null)  movementData.WallSlideData.OnChangedEvent  -= OnWeaponDataChanged;
        #endif
    }

    private void OnMovementCollectionChanged()
    {
        #if UNITY_EDITOR
        UnsubscribeFromMovementData(equippedWeapon.WeaponData.MovementCollection);
        SubscribeToMovementData(equippedWeapon.WeaponData.MovementCollection);
        #endif

        OnWeaponDataChanged();
    }

    #endregion


    #region Attack Data

    private void SubscribeToAttackCollection(AttackDataCollection attackData)
    {
        #if UNITY_EDITOR
        if (attackData == null) return;

        attackData.OnChangedEvent += OnAttackCollectionChanged;
        SubscribeToAttackData(attackData);
        #endif
    }

    private void UnsubscribeFromAttackCollection(AttackDataCollection attackData)
    {
        #if UNITY_EDITOR
        if (attackData == null) return;

        attackData.OnChangedEvent -= OnAttackCollectionChanged;
        UnsubscribeFromAttackData(attackData);
        #endif        
    }

     private void SubscribeToAttackData(AttackDataCollection attackData)
    {
        #if UNITY_EDITOR
        if (attackData == null) return;

        if (attackData.UpTiltData != null)      attackData.UpTiltData.OnChangedEvent        += OnWeaponDataChanged;
        if (attackData.UpAirData != null)       attackData.UpAirData.OnChangedEvent         += OnWeaponDataChanged;
        if (attackData.DownTiltData != null)    attackData.DownTiltData.OnChangedEvent      += OnWeaponDataChanged;
        if (attackData.DownAirData != null)     attackData.DownAirData.OnChangedEvent       += OnWeaponDataChanged;
        if (attackData.ForwardTiltData != null) attackData.ForwardTiltData.OnChangedEvent   += OnWeaponDataChanged;
        if (attackData.ForwardAirData != null)  attackData.ForwardAirData.OnChangedEvent    += OnWeaponDataChanged;
        #endif
    }

    private void UnsubscribeFromAttackData(AttackDataCollection attackData)
    {
        #if UNITY_EDITOR
        if (attackData == null) return;

        if (attackData.UpTiltData != null)      attackData.UpTiltData.OnChangedEvent        -= OnWeaponDataChanged;
        if (attackData.UpAirData != null)       attackData.UpAirData.OnChangedEvent         -= OnWeaponDataChanged;
        if (attackData.DownTiltData != null)    attackData.DownTiltData.OnChangedEvent      -= OnWeaponDataChanged;
        if (attackData.DownAirData != null)     attackData.DownAirData.OnChangedEvent       -= OnWeaponDataChanged;
        if (attackData.ForwardTiltData != null) attackData.ForwardTiltData.OnChangedEvent   -= OnWeaponDataChanged;
        if (attackData.ForwardAirData != null)  attackData.ForwardAirData.OnChangedEvent    -= OnWeaponDataChanged;
        #endif
    }

    private void OnAttackCollectionChanged()
    {
        #if UNITY_EDITOR
        UnsubscribeFromAttackData(equippedWeapon.WeaponData.AttackCollection);
        SubscribeToAttackData(equippedWeapon.WeaponData.AttackCollection);
        #endif

        OnWeaponDataChanged();
    }

    #endregion


    private void OnWeaponDataChanged()
    {
        if (equippedWeapon != null)
            OnWeaponEquippedEvent?.Invoke(equippedWeapon);
    }

    #endregion
}

