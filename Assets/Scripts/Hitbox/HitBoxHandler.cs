using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static AttackStatData;

[RequireComponent(typeof(ISceneObject))]
[RequireComponent(typeof(ActionStateHandler))]
[RequireComponent(typeof(StatHandler))]
[RequireComponent(typeof(HurtBoxHandler))]
[RequireComponent(typeof(AnimationHandler))]
public class HitBoxHandler : MonoBehaviour, IHitBoxHandler
{
    private ISceneObject sceneObject;
    private IActionState actionState;
    private IStats statHandler;
    private IAnimation animationHandler;
    private IHurtBoxHandler hurtBoxHandler;

    //SceneObject
    [Tooltip("The root of the sceneObject that will be used to find all sceneObject based hitboxs")]
    [SerializeField] private GameObject sceneObjectRoot;
    private HitBox[] sceneObjectHitboxs;

    //Weapon
    private List<HitBox> weaponHitboxs = new List<HitBox>();
    private Dictionary<AttackState, AttackStats> weaponAttackDatas = null;

    private HashSet<Guid> sceneObjectsHit = new HashSet<Guid>();

    #region Initialize

    private void Awake()
    {
        sceneObject = GetComponent<ISceneObject>();
        if (sceneObject == null)
            Debug.LogError("HitBoxHandler No ISceneObject found", gameObject);

        actionState = GetComponent<IActionState>();
        statHandler = GetComponent<IStats>();        
        animationHandler = GetComponent<IAnimation>();
        hurtBoxHandler = GetComponent<IHurtBoxHandler>();        

        if (sceneObjectRoot != null)
        {
            sceneObjectHitboxs = sceneObjectRoot.GetComponentsInChildren<HitBox>(true);
            if (sceneObjectHitboxs == null || sceneObjectHitboxs.Length == 0)
                Debug.LogError("HitBoxHandler No SceneObject Hitboxs found", gameObject);
        }
        else
            Debug.LogError("HitBoxHandler SceneObjectRoot not set", gameObject);

        RegisterToEvents();
    }

    private void RegisterToEvents()
    {
        actionState.ActionStateChangedEvent += OnActionStateChanged;
        statHandler.AttackStatsChangedEvent += OnAttackStatsChanged;
        animationHandler.AnimationEventFiredEvent += OnAnimationEvent;
        hurtBoxHandler.OnHitEvent += OnBeingHit;
    }

    private void OnDestroy()
    {
        UnregisterToEvents();
    }

    private void UnregisterToEvents()
    {
        actionState.ActionStateChangedEvent -= OnActionStateChanged;
        statHandler.AttackStatsChangedEvent -= OnAttackStatsChanged;
        animationHandler.AnimationEventFiredEvent -= OnAnimationEvent;
        hurtBoxHandler.OnHitEvent -= OnBeingHit;
    }    

    private void OnActionStateChanged(ActionState state)
    {
        DisableAllHitBoxs();
    }

    private void OnAttackStatsChanged(AttackStatData data)
    {
        if (data == null || data.WeaponRootGameObject == null)
            return;
        
        weaponHitboxs = data.WeaponRootGameObject.GetComponentsInChildren<HitBox>(true).ToList();
        weaponAttackDatas = new Dictionary<AttackState, AttackStats>();

        if (data.UpTilt != null)
            weaponAttackDatas.Add(AttackState.UpTilt, data.UpTilt);
        if (data.ForwardTilt != null)
            weaponAttackDatas.Add(AttackState.ForwardTilt, data.ForwardTilt);
        if (data.DownTilt != null)
            weaponAttackDatas.Add(AttackState.DownTilt, data.DownTilt);
        if (data.UpAir != null)
            weaponAttackDatas.Add(AttackState.UpAir, data.UpAir);
        if (data.ForwardAir != null)
            weaponAttackDatas.Add(AttackState.ForwardAir, data.ForwardAir);
        if (data.DownAir != null)
            weaponAttackDatas.Add(AttackState.DownAir, data.DownAir);
    }

    private void OnAnimationEvent(AnimationEventState eventState)
    {
        switch (eventState)
        {
            case AnimationEventState.EnableHitbox:
                EnableWeaponHitboxs();
                break;

            case AnimationEventState.DisableHitbox:
                DisableWeaponHitboxs();
                break;
        }
    }

    private void OnBeingHit(HitData hitData)
    {
        sceneObjectsHit.Add(hitData.SceneObjectID);
        EnableSceneObjectHitBoxs();
    }

    #endregion


    #region Hitboxs

    private void EnableSceneObjectHitBoxs()
    {
        foreach (HitBox hitBox in sceneObjectHitboxs)
        {
            hitBox.ActivateHitBox();
            hitBox.OnCollisionEntered += OnSceneObjectHit;
        }
    }

    private void EnableWeaponHitboxs()
    {
        foreach (HitBox hitBox in weaponHitboxs)
        {
            hitBox.ActivateHitBox();
            hitBox.OnCollisionEntered += OnWeaponHit;
        }
    }

    private void DisableSceneObjectHitBoxs()
    {
        foreach (HitBox hitbox in sceneObjectHitboxs)
        {
            hitbox.DeactivateHitBox();
            hitbox.OnCollisionEntered -= OnSceneObjectHit;
        }

        sceneObjectsHit.Clear();
    }

    private void DisableWeaponHitboxs()
    {
        foreach (HitBox hitBox in weaponHitboxs)
        {
            hitBox.DeactivateHitBox();
            hitBox.OnCollisionEntered -= OnWeaponHit;
        }

        sceneObjectsHit.Clear();
    }

    private void DisableAllHitBoxs()
    {
        DisableSceneObjectHitBoxs();
        DisableWeaponHitboxs();
    }

    #endregion


    #region Hurtbox Handling

    private void OnSceneObjectHit(IHurtBox hurtBox)
    {
        if (actionState.CurActionState == ActionState.HitStun ||
            sceneObjectsHit.Contains(hurtBox.SceneObjectID))
            return;

        //TODO: Damage calculated based on velocity and totalMass
        hurtBox.Hit(new HitData(sceneObject.UniqueID, 1, 40, 5));

        sceneObjectsHit.Add(hurtBox.SceneObjectID);
    }

    private void OnWeaponHit(IHurtBox hurtBox)
    {
        if (actionState.CurAttackState == AttackState.Null || 
            sceneObjectsHit.Contains(hurtBox.SceneObjectID))
            return;

        sceneObjectsHit.Add(hurtBox.SceneObjectID);

        AttackStats curAttackStats = weaponAttackDatas[actionState.CurAttackState];
        int curAnimationFrame = animationHandler.GetFrameOfCurrentAnimation();

        float launchAngle = curAttackStats.LaunchAngle;
        if (!sceneObject.IsFacingRightDirection)
            launchAngle = 180 - launchAngle;

        hurtBox.Hit(new HitData(sceneObject.UniqueID, curAttackStats.Influence, launchAngle, curAttackStats.GetAttackDamage(curAnimationFrame)));
    }

    #endregion
}
