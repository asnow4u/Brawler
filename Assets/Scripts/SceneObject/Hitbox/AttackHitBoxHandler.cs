using System;
using System.Collections.Generic;
using UnityEngine;
using static AttackStatData;

[RequireComponent(typeof(ISceneObject))]
[RequireComponent(typeof(ActionStateHandler))]
[RequireComponent(typeof(StatHandler))]
public class AttackHitBoxHandler : HitBoxHandler, IAttackHitBoxHandler
{
    private ISceneObject sceneObject;
    private IActionState actionState;
    private IStats statHandler;
    private IAnimationEvent animationEventHandler;

    private AttackStats curAttackStats;
    private ParticleSystem weaponSwingEffect;    


    protected override void Awake()
    {
        sceneObject = GetComponent<ISceneObject>();
        if (sceneObject == null)
            Debug.LogError("HitBoxHandler No ISceneObject found", gameObject);

        animationEventHandler = GetComponentInChildren<IAnimationEvent>();
        if (animationEventHandler == null)
            Debug.LogError("HitBoxHandler No IAnimationEvent found", gameObject);

        actionState = GetComponent<IActionState>();
        statHandler = GetComponent<IStats>();

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
        statHandler.AttackStatsChangedEvent += OnAttackStatsChanged;
        animationEventHandler.OnAnimationEventFiredEvent += OnAnimationEventFired;
    }

    protected override void UnregisterFromEvents()
    {
        base.UnregisterFromEvents();

        actionState.ActionStateChangedEvent -= OnActionStateChanged;
        statHandler.AttackStatsChangedEvent -= OnAttackStatsChanged;
        animationEventHandler.OnAnimationEventFiredEvent -= OnAnimationEventFired;
    }


    private void OnActionStateChanged(ActionState state)
    {
        DisableHitboxs();
        ClearHitRecord();

        if (weaponSwingEffect != null)
            weaponSwingEffect.Stop();
    }

    private void OnAttackStatsChanged(AttackStatData data)
    {
        DisableHitboxs();
        ClearHitRecord();

        if (weaponSwingEffect != null)
            weaponSwingEffect.Stop();

        if (data == null || data.WeaponRootGameObject == null)
        {
            hitboxs = null;
            weaponSwingEffect = null;
            return;
        }
        
        hitboxs = data.WeaponRootGameObject.GetComponentsInChildren<IHitBox>(true);
        foreach (IHitBox hitbox in hitboxs)
            hitbox.SetOwner(sceneObject.UniqueID);

        weaponSwingEffect = data.SwingEffect;       
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
                EnableHitBoxs();
                break;

            case AnimationEventState.DisableHitbox:
                DisableHitboxs();
                ClearHitRecord();
                break;

            case AnimationEventState.AttackEnded:
                if (weaponSwingEffect != null)
                    weaponSwingEffect.Stop();
                break;
        }
    }

    public void SetCurrentAttackStat(AttackStats attackStats)
    {
        curAttackStats = attackStats;
    }

    protected override void OnHit(IHitBox hitBox, IHurtBox hurtBox, Vector3 hitPoint)
    {
        if (curAttackStats == null ||
            sceneObjectsHit.Contains(hurtBox.OwnerID))
            return;

        sceneObjectsHit.Add(hurtBox.OwnerID);

        float launchAngle = curAttackStats.LaunchAngle;
        if (!sceneObject.IsFacingRightDirection)
            launchAngle = 180 - launchAngle;

        HitData baseHitData = new HitData(curAttackStats.Influence, launchAngle, curAttackStats.Damage, curAttackStats.HitStunTime, hitPoint, curAttackStats.Type);
        SceneObjectHitData sceneObjectHitData = new SceneObjectHitData(sceneObject.UniqueID, baseHitData);

        DeclareHit(new HitBoxConnectedData(curAttackStats.HitPauseTime), hurtBox, sceneObjectHitData);
    }
}