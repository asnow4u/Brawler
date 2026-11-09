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

    private readonly HashSet<Guid> ignoredThisAttack = new HashSet<Guid>();

    [Tooltip("How far the weapon hitboxes stretch past the weapon tip, in weapon lengths. 0 keeps the base hitboxes.")]
    [Min(0f)]
    [SerializeField] private float reachStretch = 0f;

    private Transform weaponRoot;

    public IReadOnlyList<IHitBox> HitBoxs => hitboxs ?? Array.Empty<IHitBox>();
    public event Action HitBoxsChangedEvent;

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
        ignoredThisAttack.Clear();
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
        ignoredThisAttack.Clear();

        if (hitboxs != null)
        {
            foreach (IHitBox hitbox in hitboxs)
                hitbox.ResetStretch();
        }

        if (weapon == null)
        {
            hitboxs = null;
            weaponRoot = null;
            HitBoxsChangedEvent?.Invoke();
            return;
        }

        weaponRoot = weapon.transform;
        hitboxs = weapon.GetComponentsInChildren<IHitBox>(true);
        foreach (IHitBox hitbox in hitboxs)
            hitbox.SetOwner(sceneObject.UniqueID);

        ApplyReachStretch();
        HitBoxsChangedEvent?.Invoke();
    }

    /// <summary>Lengthens each weapon hitbox outward along the weapon's Y axis by reachStretch weapon lengths. Weapon length is the tip-to-tip span of all base hitboxes.</summary>
    private void ApplyReachStretch()
    {
        if (hitboxs == null || hitboxs.Length == 0 || weaponRoot == null)
            return;

        float spanStart = float.MaxValue;
        float spanEnd = float.MinValue;
        bool hasSpan = false;
        foreach (IHitBox hitbox in hitboxs)
        {
            if (hitbox.TryGetBaseSpan(weaponRoot, out float min, out float max))
            {
                spanStart = Mathf.Min(spanStart, min);
                spanEnd = Mathf.Max(spanEnd, max);
                hasSpan = true;
            }
        }

        if (!hasSpan)
            return;

        float extension = (spanEnd - spanStart) * reachStretch;
        foreach (IHitBox hitbox in hitboxs)
            hitbox.ApplyStretch(weaponRoot, extension);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying)
            ApplyReachStretch();
    }
#endif

    public void SetAttackHitData(HitData hitData, AttackHitSenderData attackHitBoxConnectedData)
    {
        curHitData = hitData;
        curAttackSenderData = attackHitBoxConnectedData;
    }

    public void IgnoreForCurrentAttack(Guid sceneObjectID)
    {
        ignoredThisAttack.Add(sceneObjectID);
    }

    protected override void OnHit(IHitBox hitBox, IHurtBox hurtBox, Vector3 hitPoint)
    {
        if (curHitData == null ||
            curAttackSenderData == null ||
            sceneObjectsHit.Contains(hurtBox.OwnerID) ||
            ignoredThisAttack.Contains(hurtBox.OwnerID))
            return;

        sceneObjectsHit.Add(hurtBox.OwnerID);

        curHitData.HitPoint = hitPoint;

        SceneObjectHitData sceneObjectHitData = new SceneObjectHitData(sceneObject.UniqueID, curHitData);
        sceneObjectHitData.SourcePosition = sceneObject.Bounds.center;
        
        // Reverse launch angle if the scene object is facing left.
        if (!sceneObject.IsFacingRightDirection)
            sceneObjectHitData.LaunchAngle = 180 - sceneObjectHitData.LaunchAngle;


        DeclareHit(curAttackSenderData, hurtBox, sceneObjectHitData);
    }
}