using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(ISceneObject))]
[RequireComponent(typeof(ActionStateHandler))]
[RequireComponent(typeof(HurtBoxHandler))]
public class SOHitBoxHandler : HitBoxHandler
{
    private ISceneObject sceneObject;
    private IActionState actionState;

    private ISOHurtBoxHandler hurtBoxHandler;
    

    //SceneObject Collision
    [Header("SceneObject Collision")]
    [Tooltip("The root of the sceneObject that will be used to find all sceneObject based hitboxs")]
    [SerializeField] private GameObject sceneObjectRoot;
    [Tooltip("Momentum below which no hit registers at all." +
        "\nThis is a gate, not a floor - objects drifting together aren't colliding and shouldn't trade hits.")]
    [SerializeField] private float minHitMomentum = 600f;
    [Tooltip("Converts this sceneObject's momentum into knockback force. (How much of the velocity carries into the target)")]
    [SerializeField] private float momentumForceScale = 0.45f;
    [Tooltip("Momentum at which a collision reaches full influence (kill power).")]
    [SerializeField] private float killMomentum = 7000f;
    [Tooltip("Converts momentum into damage dealt.")]
    [SerializeField] private float damageScale = 0.0025f;
    [Tooltip("How far the launch angle bends from the incoming trajectory toward the contact geometry.")]
    [Range(0f, 1f)]
    [SerializeField] private float deflectionWeight = 0.35f;
    [Tooltip("The minimum damage that can be applied to a sceneObject hit by this sceneObject.")]
    [SerializeField] private float minSceneObjectHitDamage = 1f;
    [Tooltip("The maximum damage that can be applied to a sceneObject hit by this sceneObject.")]
    [SerializeField] private float maxSceneObjectHitDamage = 10f;
    [SerializeField] private float sceneObjectHitStunTime = 0.1f;

    protected override void Awake()
    {
        sceneObject = GetComponent<ISceneObject>();
        actionState = GetComponent<IActionState>();
        hurtBoxHandler = GetComponent<ISOHurtBoxHandler>();

        base.Awake();
    }

    protected override IHitBox[] CollectHitboxs()
    {
        if (sceneObjectRoot != null)
            return sceneObjectRoot.GetComponentsInChildren<IHitBox>(true);
        else
            Debug.LogError("HitBoxHandler SceneObjectRoot not set", gameObject);

        return null;
    }

    private void Start()
    {
        foreach (IHitBox hitbox in hitboxs)
            hitbox.SetOwner(sceneObject.UniqueID);
    }

    protected override void RegisterToEvents()
    {
        actionState.ActionStateChangedEvent += OnActionStateChanged;
        hurtBoxHandler.HitStunStateChangedEvent += OnHitStunStateChanged;
    }

    protected override void UnregisterFromEvents()
    {
        actionState.ActionStateChangedEvent -= OnActionStateChanged;
        hurtBoxHandler.HitStunStateChangedEvent -= OnHitStunStateChanged;
    }    

    private void OnActionStateChanged(ActionState state)
    {
        DisableHitboxs();
    }

    private void OnHitStunStateChanged(HitStunState state)
    {
        if (state == HitStunState.Launch || state == HitStunState.Travel)
            EnableHitBoxs();
        else
            DisableHitboxs();

        if (state == HitStunState.Null)
            ClearHitRecord();
    }    

    protected override void OnHit(IHitBox hitBox, IHurtBox hurtBox, Vector3 hitPoint)
    {
        if (actionState.CurActionState != ActionState.HitStun ||
            sceneObjectsHit.Contains(hurtBox.OwnerID) ||
            hurtBoxHandler.LastHitBy.Contains(hurtBox.OwnerID))
            return;

        Vector3 relativeVelocity = rb.linearVelocity - hurtBox.Velocity;
        relativeVelocity.z = 0;

        float momentum = rb.mass * relativeVelocity.magnitude;
        if (momentum < minHitMomentum)
            return;

        sceneObjectsHit.Add(hurtBox.OwnerID);      

        float baseForce = momentum * momentumForceScale;
        float influence = Mathf.Clamp01(momentum / killMomentum);
        float damage = Mathf.Clamp(momentum * damageScale, minSceneObjectHitDamage, maxSceneObjectHitDamage);

        float launchAngle = CalculateDeflectionAngle(relativeVelocity, hitPoint);

        HitData baseHitData = new HitData(baseForce, influence, launchAngle, damage, sceneObjectHitStunTime, hitPoint, 0);
        SceneObjectCollisionHitData collisionHitData = new SceneObjectCollisionHitData(sceneObject.UniqueID, baseForce, baseHitData);

        DeclareHit(new HitSenderData(sceneObjectHitStunTime), hurtBox, collisionHitData);
    }

    private float CalculateDeflectionAngle(Vector3 relativeVelocity, Vector3 hitPoint)
    {
        Vector3 incomingDirection = relativeVelocity.normalized;

        Vector3 contactNormal = hitPoint - rb.worldCenterOfMass;
        contactNormal.z = 0;

        //A contact sitting on the centre of mass gives no usable normal; fall back to trajectory
        if (contactNormal.sqrMagnitude < 1e-6f)
            return ToAngle(incomingDirection);

        Vector3 launchDirection = Vector3.Slerp(incomingDirection, contactNormal.normalized, deflectionWeight);
        return ToAngle(launchDirection);
    }

    private static float ToAngle(Vector3 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        if (angle < 0f)
            angle += 360f;

        return angle;
    }
}
