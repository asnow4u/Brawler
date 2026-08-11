using System;
using UnityEngine;

[RequireComponent(typeof(ActionStateHandler))]
[RequireComponent(typeof(StatHandler))]
[DefaultExecutionOrder(1)] // NOTE: Ensure this script executes after StatHandler.
public class EquipmentHandler : MonoBehaviour, IEquipment, IAttackCancel
{    
    //Componenets
    ActionStateHandler actionState;
    private StatHandler statHandler;
    IEquipmentInput equipmentInput;
    private IInputBuffer inputBuffer;
    private WeaponHandler weaponHandler; 

    // Optional Components
    private IAttackHitBoxHandler attackHitBoxHandler;

    private bool swapAllowed => weaponHandler != null &&
                                weaponHandler.HasSecondryWeapon &&
                                actionState.CurActionState < ActionState.Attacking;
    
    private bool swapCancelAllowed => weaponHandler != null &&
                                    weaponHandler.HasSecondryWeapon;

    
    private float hitCancelExecuteTime = -1f;
    private bool IsHitCancelPending => hitCancelExecuteTime > 0f;

    public event Action<BufferedInput> PerformedAttackCancel;


    #region Initialize

    private void Awake()
    {
        actionState = GetComponent<ActionStateHandler>();
        statHandler = GetComponent<StatHandler>();
        equipmentInput = GetComponent<IEquipmentInput>();
        inputBuffer = GetComponent<IInputBuffer>();
        weaponHandler = GetComponentInChildren<WeaponHandler>();

        // Optional Components
        attackHitBoxHandler = GetComponent<IAttackHitBoxHandler>();

        RegisterToEvents();
    }
    
    private void RegisterToEvents()
    {
        if (weaponHandler != null)
            weaponHandler.OnWeaponEquippedEvent += OnWeaponChanged;

        if (attackHitBoxHandler != null)
            attackHitBoxHandler.OnHitConnected += OnHitConnected;
    }
    
    private void Start()
    {
        if (weaponHandler != null)
            weaponHandler.Initialize();        
    }

    private void OnDestroy()
    {
        UnregisterFromEvents();
    }

    private void UnregisterFromEvents()
    {
        if (weaponHandler != null)
            weaponHandler.OnWeaponEquippedEvent -= OnWeaponChanged;

        if (attackHitBoxHandler != null)
            attackHitBoxHandler.OnHitConnected -= OnHitConnected;
    }

    #endregion


    #region Buffered Actions

    private void FixedUpdate()
    {
        UpdateOnHitCancelWindow();
        TryConsumeBufferedSwap();
    }

    private void TryConsumeBufferedSwap()
    {
        if (inputBuffer == null || !swapAllowed)
            return;

        if (!inputBuffer.TryConsume(BufferedInput.SwapWeapon))
            return;

        weaponHandler.SwapEquippedWeapon();
    }

    private void OnHitConnected(HitSenderData hitSenderData)
    {
        if (hitSenderData is not AttackHitSenderData attackSenderData)
            return;

        hitCancelExecuteTime = Time.time + attackSenderData.hitPauseTime;
    }

    private void UpdateOnHitCancelWindow()
    {
        if (!IsHitCancelPending)
            return;

        //Still frozen - whatever is held stays held until the pause releases
        if (Time.time < hitCancelExecuteTime)
            return;

        TrySwapCancel();
        hitCancelExecuteTime = -1f;
    }

    private void TrySwapCancel()
    {
        if (inputBuffer == null || !swapCancelAllowed)
            return;

        if (!inputBuffer.TryGetNewestLive(InputSets.AttackHitCancelInputs, out BufferedInput newest) ||
            newest != BufferedInput.SwapWeapon)
            return;

        if (!inputBuffer.TryConsume(BufferedInput.SwapWeapon))
            return;

        weaponHandler.SwapEquippedWeapon();

        hitCancelExecuteTime = -1f;
        PerformedAttackCancel?.Invoke(BufferedInput.SwapWeapon);
    }

    #endregion

    private void OnWeaponChanged(IWeapon weapon)
    {
        if (weapon == null || weapon.WeaponData == null)
            return;

        statHandler.UpdateAccumulatedMass(CalculateAccumulatedMass());
        statHandler.UpdateAnimationStats(weapon.WeaponData.MovementCollection, weapon.WeaponData.AttackCollection);
        statHandler.UpdateMovementStats(weapon.WeaponData.MovementCollection);
        statHandler.UpdateAttackStats(weaponHandler.ParseWeaponAttackData(weapon));        
    }

    private float CalculateAccumulatedMass()
    {
        float accumulatedMass = 0f;

        if (weaponHandler.EquippedWeapon != null)
            accumulatedMass += weaponHandler.EquippedWeapon.Mass;

        return accumulatedMass;
    }    
}
