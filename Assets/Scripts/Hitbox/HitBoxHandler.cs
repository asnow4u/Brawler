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
    private IAnimationEvent animationEventHandler;
    private IHurtBoxHandler hurtBoxHandler;
    private IAttack attackHandler;

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
    [SerializeField] private float sceneObjectHitStunTime = 0.1f;

    private HitBox[] sceneObjectHitboxs;
    private MovementStatData movementStatData;

    //Weapon
    private List<HitBox> weaponHitboxs = new List<HitBox>();
    private ParticleSystem weaponSwingEffect;
    private Dictionary<AttackState, AttackStats> weaponAttackDatas = null;

    private HashSet<Guid> sceneObjectsHit = new HashSet<Guid>();

    #region Initialize

    private void Awake()
    {
        sceneObject = GetComponent<ISceneObject>();
        if (sceneObject == null)
            Debug.LogError("HitBoxHandler No ISceneObject found", gameObject);

        animationEventHandler = GetComponentInChildren<IAnimationEvent>();
        if (animationEventHandler == null)
            Debug.LogError("HitBoxHandler No IAnimationEvent found", gameObject);

        actionState = GetComponent<IActionState>();
        statHandler = GetComponent<IStats>();
        animationHandler = GetComponent<IAnimation>();
        hurtBoxHandler = GetComponent<IHurtBoxHandler>();        
        attackHandler = GetComponent<IAttack>();

        rb = GetComponent<Rigidbody>();

        if (sceneObjectRoot != null)
        {
            sceneObjectHitboxs = sceneObjectRoot.GetComponentsInChildren<HitBox>(true);
            if (sceneObjectHitboxs == null || sceneObjectHitboxs.Length == 0)
                Debug.LogError("HitBoxHandler No SceneObject Hitboxs found", gameObject);
            else
            {
                foreach (HitBox hitbox in sceneObjectHitboxs)
                    hitbox.SetOwner(sceneObject.UniqueID);
            }
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
        animationEventHandler.OnAnimationEventFiredEvent += OnAnimationEventFired;
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
        animationEventHandler.OnAnimationEventFiredEvent -= OnAnimationEventFired;
        hurtBoxHandler.HitStunStateChangedEvent -= OnHitStunStateChanged;
    }    

    private void OnActionStateChanged(ActionState state)
    {
        DisableAllHitBoxs();

        if (weaponSwingEffect != null)
            weaponSwingEffect.Stop();
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
        foreach (HitBox hitbox in weaponHitboxs)
            hitbox.SetOwner(sceneObject.UniqueID);

        weaponSwingEffect = data.SwingEffect;
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
            case AnimationEventState.AttackStarted:
                if (weaponSwingEffect != null)
                    weaponSwingEffect.Play();
                break;

            case AnimationEventState.EnableHitbox:
                EnableWeaponHitboxs();
                break;

            case AnimationEventState.DisableHitbox:
                DisableWeaponHitboxs();
                break;

            case AnimationEventState.AttackEnded:
                if (weaponSwingEffect != null)
                    weaponSwingEffect.Stop();
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

    private void OnSceneObjectHit(IHurtBox hurtBox, Vector3 hitPoint)
    {
        if (actionState.CurActionState != ActionState.HitStun ||
            sceneObjectsHit.Contains(hurtBox.OwnerID) ||
            hurtBoxHandler.LastHitBy.Contains(hurtBox.OwnerID))
            return;

        sceneObjectsHit.Add(hurtBox.OwnerID);

        animationHandler.PauseAnimation(sceneObjectHitStunTime);

        float t = Mathf.Clamp01(rb.linearVelocity.x / movementStatData.MaxAerialXVelocity);
        float launchAngle = Mathf.Lerp(minSceneObjectHitLaunchAngle, maxSceneObjectHitLaunchAngle, t);
        if (rb.linearVelocity.x < 0)
            launchAngle = 180 - launchAngle;

        float speed = rb.linearVelocity.magnitude;
        float damage = rb.mass * speed * speed;
        damage = Mathf.Clamp(damage, minSceneObjectHitDamage, maxSceneObjectHitDamage);

        Debug.Log("SceneObject HitData", gameObject);
        hurtBox.Hit(new HitData(sceneObject.UniqueID, 0f, launchAngle, damage, sceneObjectHitStunTime, hitPoint));
    }

    private void OnWeaponHit(IHurtBox hurtBox, Vector3 hitPoint)
    {
        if (attackHandler == null ||
            attackHandler.CurAttackState == AttackState.Null || 
            sceneObjectsHit.Contains(hurtBox.OwnerID))
            return;

        sceneObjectsHit.Add(hurtBox.OwnerID);

        AttackStats curAttackStats = weaponAttackDatas[attackHandler.CurAttackState];

        animationHandler.PauseAnimation(curAttackStats.HitStunTime);

        float animationDelta = animationHandler.GetCurrentAnimationDelta();

        float launchAngle = curAttackStats.LaunchAngle;
        if (!sceneObject.IsFacingRightDirection)
            launchAngle = 180 - launchAngle;

        Debug.Log(new HitData(sceneObject.UniqueID, curAttackStats.Influence, launchAngle, curAttackStats.GetAttackDamage(animationDelta), curAttackStats.HitStunTime, hitPoint));
        hurtBox.Hit(new HitData(sceneObject.UniqueID, curAttackStats.Influence, launchAngle, curAttackStats.GetAttackDamage(animationDelta), curAttackStats.HitStunTime, hitPoint));
    }

    #endregion
}
