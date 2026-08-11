using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ISceneObject))]
[RequireComponent(typeof(ActionStateHandler))]
public class AttackHitBoxHandler : HitBoxHandler, IAttackHitBoxHandler
{
    private ISceneObject sceneObject;
    private IActionState actionState;
    private IAnimationEvent animationEventHandler;

    private HitData curHitData;
    private AttackHitSenderData curAttackSenderData;

    protected override void Awake()
    {
        sceneObject = GetComponent<ISceneObject>();
        if (sceneObject == null)
            Debug.LogError("HitBoxHandler No ISceneObject found", gameObject);

        animationEventHandler = GetComponentInChildren<IAnimationEvent>();
        if (animationEventHandler == null)
            Debug.LogError("HitBoxHandler No IAnimationEvent found", gameObject);

        actionState = GetComponent<IActionState>();

        base.Awake();
    }

    protected override IHitBox[] CollectHitboxs()
    {
        return null;
    }

    protected override void RegisterToEvents()
    {
        base.RegisterToEvents();

        actionState.ActionStateChangedEvent += OnActionStateChanged;
        animationEventHandler.OnAnimationEventFiredEvent += OnAnimationEventFired;
    }

    protected override void UnregisterFromEvents()
    {
        base.UnregisterFromEvents();

        actionState.ActionStateChangedEvent -= OnActionStateChanged;
        animationEventHandler.OnAnimationEventFiredEvent -= OnAnimationEventFired;
    }


    private void OnActionStateChanged(ActionState state)
    {
        DisableHitboxs();
        ClearHitRecord();
    }    

    private void OnAnimationEventFired(AnimationEventState eventState)
    {
        switch (eventState)
        {
            case AnimationEventState.EnableHitbox:
                EnableHitBoxs();
                break;

            case AnimationEventState.DisableHitbox:
                DisableHitboxs();
                ClearHitRecord();
                break;
        }
    }

    public void SetWeaponHitBoxs(GameObject weapon)
    {
        DisableHitboxs();
        ClearHitRecord();

        if (weapon == null)
        {
            hitboxs = null;
            return;
        }

        hitboxs = weapon.GetComponentsInChildren<IHitBox>(true);
        foreach (IHitBox hitbox in hitboxs)
            hitbox.SetOwner(sceneObject.UniqueID);
    }

    public void SetAttackHitData(HitData hitData, AttackHitSenderData attackHitBoxConnectedData)
    {
        curHitData = hitData;
        curAttackSenderData = attackHitBoxConnectedData;
    }

    protected override void OnHit(IHitBox hitBox, IHurtBox hurtBox, Vector3 hitPoint)
    {
        if (curHitData == null ||
            curAttackSenderData == null ||
            sceneObjectsHit.Contains(hurtBox.OwnerID))
            return;

        sceneObjectsHit.Add(hurtBox.OwnerID);

        curHitData.HitPoint = hitPoint;

        SceneObjectHitData sceneObjectHitData = new SceneObjectHitData(sceneObject.UniqueID, curHitData);
        
        // Reverse launch angle if the scene object is facing left.
        if (!sceneObject.IsFacingRightDirection)
            sceneObjectHitData.LaunchAngle = 180 - sceneObjectHitData.LaunchAngle;


        DeclareHit(curAttackSenderData, hurtBox, sceneObjectHitData);
    }
}