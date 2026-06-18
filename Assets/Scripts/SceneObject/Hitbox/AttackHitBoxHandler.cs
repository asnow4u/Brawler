using System.Collections.Generic;
using UnityEngine;
using static AttackStatData;

[RequireComponent(typeof(ISceneObject))]
[RequireComponent(typeof(ActionStateHandler))]
[RequireComponent(typeof(StatHandler))]
[RequireComponent(typeof(AnimationHandler))]
[RequireComponent(typeof(AttackHandler))]
public class AttackHitBoxHandler : HitBoxHandler
{
    private ISceneObject sceneObject;
    private IActionState actionState;
    private IStats statHandler;
    private IAnimation animationHandler;
    private IAnimationEvent animationEventHandler;
    private IAttack attackHandler;

    private ParticleSystem weaponSwingEffect;
    private Dictionary<AttackState, AttackStats> weaponAttackDatas = null;    

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
        animationHandler = GetComponent<IAnimation>();
        attackHandler = GetComponent<IAttack>();

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

        if (weaponSwingEffect != null)
            weaponSwingEffect.Stop();
    }

    private void OnAttackStatsChanged(AttackStatData data)
    {
        if (data == null || data.WeaponRootGameObject == null)
            return;
        
        hitboxs = data.WeaponRootGameObject.GetComponentsInChildren<IHitBox>(true);
        foreach (IHitBox hitbox in hitboxs)
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
                EnableHitBoxs();
                break;

            case AnimationEventState.DisableHitbox:
                DisableHitboxs();
                break;

            case AnimationEventState.AttackEnded:
                if (weaponSwingEffect != null)
                    weaponSwingEffect.Stop();
                break;
        }
    }

    protected override void OnHit(IHitBox hitBox, IHurtBox hurtBox, Vector3 hitPoint)
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

        HitData hitData = new HitData(curAttackStats.Influence, launchAngle, curAttackStats.Damage, curAttackStats.HitStunTime, hitPoint, curAttackStats.Type);
        hurtBox.Hit(new SceneObjectHitData(sceneObject.UniqueID, hitData));
    }
}
