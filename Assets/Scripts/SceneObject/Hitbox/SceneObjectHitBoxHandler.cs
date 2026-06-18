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
public class SceneObjectHitBoxHandler : HitBoxHandler
{
    private ISceneObject sceneObject;
    private IActionState actionState;
    private IStats statHandler;
    private IAnimation animationHandler;
    private IHurtBoxHandler hurtBoxHandler;
    

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

    private MovementStatData movementStatData;

    protected override void Awake()
    {
        sceneObject = GetComponent<ISceneObject>();
        if (sceneObject == null)
            Debug.LogError("HitBoxHandler No ISceneObject found", gameObject);

        actionState = GetComponent<IActionState>();
        statHandler = GetComponent<IStats>();
        animationHandler = GetComponent<IAnimation>();
        hurtBoxHandler = GetComponent<IHurtBoxHandler>();

        base.Awake();
    }

    protected override IHitBox[] CollectHitboxs()
    {
        IHitBox[] startingHitBoxes = null;

        if (sceneObjectRoot != null)
        {
            startingHitBoxes = sceneObjectRoot.GetComponentsInChildren<IHitBox>(true);
            foreach (IHitBox hitbox in startingHitBoxes)
                hitbox.SetOwner(sceneObject.UniqueID);
        }
        else
            Debug.LogError("HitBoxHandler SceneObjectRoot not set", gameObject);

        return startingHitBoxes;
    }

    protected override void RegisterToEvents()
    {
        actionState.ActionStateChangedEvent += OnActionStateChanged;
        statHandler.MovementStatsChangedEvent += OnMovementStatsChanged;
        hurtBoxHandler.HitStunStateChangedEvent += OnHitStunStateChanged;
    }

    protected override void UnregisterFromEvents()
    {
        actionState.ActionStateChangedEvent -= OnActionStateChanged;
        statHandler.MovementStatsChangedEvent -= OnMovementStatsChanged;        
        hurtBoxHandler.HitStunStateChangedEvent -= OnHitStunStateChanged;
    }    

    private void OnActionStateChanged(ActionState state)
    {
        DisableHitboxs();
    }

    private void OnMovementStatsChanged(MovementStatData data)
    {
        movementStatData = data;
    }    

    private void OnHitStunStateChanged(HitStunState state)
    {
        if (state == HitStunState.Launch || state == HitStunState.Travel)
            EnableHitBoxs();
        else
            DisableHitboxs();
    }    

    protected override void OnHit(IHitBox hitBox, IHurtBox hurtBox, Vector3 hitPoint)
    {
        if (actionState.CurActionState != ActionState.HitStun ||
            sceneObjectsHit.Contains(hurtBox.OwnerID) ||
            hurtBoxHandler.LastHitBy.Contains(hurtBox.OwnerID))
            return;

        Debug.Log("Hit From SceneObject:");

        sceneObjectsHit.Add(hurtBox.OwnerID);

        animationHandler.PauseAnimation(sceneObjectHitStunTime);

        float t = Mathf.Clamp01(rb.linearVelocity.x / movementStatData.MaxAerialXVelocity);
        float launchAngle = Mathf.Lerp(minSceneObjectHitLaunchAngle, maxSceneObjectHitLaunchAngle, t);
        if (rb.linearVelocity.x < 0)
            launchAngle = 180 - launchAngle;

        float speed = rb.linearVelocity.magnitude;
        float damage = rb.mass * speed * speed;
        damage = Mathf.Clamp(damage, minSceneObjectHitDamage, maxSceneObjectHitDamage);

        hurtBox.Hit(new HitData(sceneObject.UniqueID, 0, 0f, launchAngle, damage, sceneObjectHitStunTime, hitPoint));
    }
}
