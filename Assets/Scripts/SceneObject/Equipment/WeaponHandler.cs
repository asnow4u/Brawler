using System;
using System.Collections.Generic;
using UnityEngine;

internal class WeaponHandler : MonoBehaviour
{
    [SerializeField] private Transform grabPoint;

    [SerializeField] private IWeapon equippedWeapon;
    [SerializeField] private IWeapon secondaryWeapon;

    private SceneObjectType ownerType;

    public IWeapon EquippedWeapon => equippedWeapon;

    public bool HasSecondryWeapon => secondaryWeapon != null;

    public event Action<IWeapon> OnWeaponEquippedEvent;
    public event Action<IWeapon> OnEquippedEnhancementsChangedEvent;


    private void Awake()
    {
        if (grabPoint == null)
            Debug.LogError("Weapon GrabPoint is Null", gameObject);
    }

    private void OnDestroy()
    {
        if (equippedWeapon != null)
            equippedWeapon.EnhancementsChangedEvent -= OnEquippedEnhancementsChanged;
    }

    public void Initialize(SceneObjectType ownerType)
    {
        this.ownerType = ownerType;
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
        if (equippedWeapon != null)
            equippedWeapon.EnhancementsChangedEvent -= OnEquippedEnhancementsChanged;

        if (weapon == secondaryWeapon)
        {
            AddWeaponToInventory(equippedWeapon);            
            UnsubscribeFromRuntimeDataChanges();
        }
        
        equippedWeapon = weapon;
        equippedWeapon.gameObject.SetActive(true);
        equippedWeapon.EnhancementsChangedEvent += OnEquippedEnhancementsChanged;

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
        //UnsubscribeFromRuntimeDataChanges();
    }
    
    public void SwapEquippedWeapon()
    {
        if (secondaryWeapon != null)
            EquipWeapon(secondaryWeapon);
    }

    /// <summary>Returns the weapon's attack collection for the owner type. Logs an error when none exists.</summary>
    public AttackDataCollection GetAttackCollection(IWeapon weapon)
    {
        if (weapon == null || weapon.WeaponData == null) return null;

        AttackDataCollection attackData = SelectAttackCollection(weapon);
        if (attackData == null)
            Debug.LogError($"{weapon.gameObject.name} has no attack collection for owner type {ownerType}", weapon.gameObject);

        return attackData;
    }

    /// <summary>Returns the weapon's attack collection for the owner type, or null.</summary>
    private AttackDataCollection SelectAttackCollection(IWeapon weapon)
    {
        if (weapon == null || weapon.WeaponData == null) return null;

        switch (ownerType)
        {
            case SceneObjectType.Player: return weapon.WeaponData.PlayerAttackCollection;
            case SceneObjectType.Enemy:  return weapon.WeaponData.EnemyAttackCollection;
            default:                     return null;
        }
    }

    public AttackStatData ParseWeaponAttackData(IWeapon weapon)
    {
        AttackDataCollection attackData = SelectAttackCollection(weapon);
        if (attackData == null) return null;

        AttackStatData data = new AttackStatData();
        data.WeaponRootGameObject = weapon.gameObject;
        data.SwingEffect = weapon.AttackEffect;

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

    public EnhancementStatData ParseWeaponEnhancementData(IWeapon weapon)
    {
        EnhancementStatData data = new EnhancementStatData();

        if (weapon == null || weapon.Enhancements == null)
            return data;

        foreach (EnhancementData enhancement in weapon.Enhancements)
        {
            if (!enhancement.IsValid())
            {
                Debug.LogWarning($"{weapon.gameObject.name} has a invalid enhancement.", weapon.gameObject);
                continue;
            }

            data.Enhancements.Add(new EnhancementStatData.EnhancementStats(enhancement));
        }

        return data;
    }

    private void OnEquippedEnhancementsChanged()
    {
        OnEquippedEnhancementsChangedEvent?.Invoke(equippedWeapon);
    }


    #region Runtime Weapon Data Change

    private void SubscribeToRuntimeDataChanges()
    {
        #if UNITY_EDITOR
        if (equippedWeapon != null && equippedWeapon.WeaponData != null)
        {
            equippedWeapon.WeaponData.OnChangedEvent += OnWeaponDataChanged;

            SubscribeToAttackCollection(SelectAttackCollection(equippedWeapon));
        }
        #endif
    }

    private void UnsubscribeFromRuntimeDataChanges()
    {
        #if UNITY_EDITOR
        if (equippedWeapon != null && equippedWeapon.WeaponData != null)
        {
            equippedWeapon.WeaponData.OnChangedEvent -= OnWeaponDataChanged;

            UnsubscribeFromAttackCollection(SelectAttackCollection(equippedWeapon));
        }
        #endif
    }


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
        UnsubscribeFromAttackData(SelectAttackCollection(equippedWeapon));
        SubscribeToAttackData(SelectAttackCollection(equippedWeapon));
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

