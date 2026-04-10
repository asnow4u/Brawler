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
[RequireComponent(typeof(Rigidbody))]
public class HitBoxHandler : MonoBehaviour, IHitBoxHandler
{
    private ISceneObject sceneObject;
    private IActionState actionState;
    private IStats statHandler;
    private IAnimation animationHandler;
    private IHurtBoxHandler hurtBoxHandler;

    //Components
    private Rigidbody rb;

    //SceneObject Collision
    [Header("SceneObject Collision")]
    [Tooltip("The root of the sceneObject that will be used to find all sceneObject based hitboxs")]
    [SerializeField] private GameObject sceneObjectRoot;
    [Tooltip("The minimum launch angle that can be applied to a sceneObject hit by this sceneObject." +
        "\nThis is used when this sceneObject is moving slowly, poping the collided sceneObject more up")]
    [SerializeField] private float minSceneObjectHitLaunchAngle = 70f;
    [Tooltip("The maximum launch angle that can be applied to a sceneObject hit by this sceneObject." +
        "\nThis is used when this sceneObject is moving quickly, pushing the collided sceneObject more horizontally")]
    [SerializeField] private float maxSceneObjectHitLaunchAngle = 30f;
    [Tooltip("The minimum damage that can be applied to a sceneObject hit by this sceneObject." +
        "\nThis is used when this sceneObject is moving slowly and/or has low mass, dealing less damage")]
    [SerializeField] private float minSceneObjectHitDamage = 2f;
    [Tooltip("The maximum damage that can be applied to a sceneObject hit by this sceneObject." +
        "\nThis is used when this sceneObject is moving quickly and/or has high mass, dealing more damage")]
    [SerializeField] private float maxSceneObjectHitDamage = 20f;

    private HitBox[] sceneObjectHitboxs;
    private MovementStatData movementStatData;

    //Weapon Collision
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

        rb = GetComponent<Rigidbody>();

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
        statHandler.MovementStatsChangedEvent += OnMovementStatsChanged;
        statHandler.AttackStatsChangedEvent += OnAttackStatsChanged;
        animationHandler.AnimationEventFiredEvent += OnAnimationEventFired;
        hurtBoxHandler.HitStunStateChangedEvent += OnHitStunStateChanged;
    }

    private void OnDestroy()
    {
        UnregisterToEvents();
    }

    private void UnregisterToEvents()
    {
        actionState.ActionStateChangedEvent -= OnActionStateChanged;
        statHandler.MovementStatsChangedEvent -= OnMovementStatsChanged;
        statHandler.AttackStatsChangedEvent -= OnAttackStatsChanged;
        animationHandler.AnimationEventFiredEvent -= OnAnimationEventFired;
        hurtBoxHandler.HitStunStateChangedEvent -= OnHitStunStateChanged;
    }    

    private void OnActionStateChanged(ActionState state)
    {
        DisableAllHitBoxs();
    }

    private void OnMovementStatsChanged(MovementStatData data)
    {
        movementStatData = data;
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

    private void OnAnimationEventFired(AnimationEventState eventState)
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

    private void OnHitStunStateChanged(HitStunState state)
    {
        if (state == HitStunState.Launch || state == HitStunState.Travel)
            EnableSceneObjectHitBoxs();
        else
            DisableSceneObjectHitBoxs();
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
        if (actionState.CurActionState != ActionState.HitStun ||
            sceneObjectsHit.Contains(hurtBox.SceneObjectID) ||
            hurtBoxHandler.LastHitBy.Contains(hurtBox.SceneObjectID))
            return;

        sceneObjectsHit.Add(hurtBox.SceneObjectID);

        float t = Mathf.Clamp01(rb.linearVelocity.x / movementStatData.MaxAerialXVelocity);
        float launchAngle = Mathf.Lerp(minSceneObjectHitLaunchAngle, maxSceneObjectHitLaunchAngle, t);
        if (rb.linearVelocity.x < 0)
            launchAngle = 180 - launchAngle;

        float speed = rb.linearVelocity.magnitude;
        float damage = rb.mass * speed * speed;
        damage = Mathf.Clamp(damage, minSceneObjectHitDamage, maxSceneObjectHitDamage);

        Debug.Log(damage);

        hurtBox.Hit(new HitData(sceneObject.UniqueID, 0f, launchAngle, damage));
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
