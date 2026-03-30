using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(ISceneObject))]
[RequireComponent(typeof(IActionState))]
[RequireComponent(typeof(IStats))]
[RequireComponent(typeof(IAnimation))]
[RequireComponent(typeof(IHurtBoxHandler))]
internal class HitBoxHandler : MonoBehaviour, IHitBoxHandler
{
    private ISceneObject sceneObject;
    private IActionState actionState;
    private IStats stats;
    private IAnimation animationHandler;
    private IHurtBoxHandler hurtBoxHandler;

    //SceneObject
    [Tooltip("The root of the sceneObject mesh that will be used to find all hitboxs")]
    [SerializeField] private GameObject sceneObjectRoot;
    private HitBox[] sceneObjectHitboxs;

    //Weapon
    private List<HitBox> weaponHitboxs = new List<HitBox>();
    private List<AttackStatData.AttackStats> weaponAttackDatas = null;
    private AttackStatData.AttackStats curWeaponAttackData = null;
    private Coroutine animationEventRoutine = null;

    #region Initialize

    private void Awake()
    {
        sceneObject = GetComponent<ISceneObject>();
        if (sceneObject == null)
            Debug.LogError("HitBoxHandler sceneObject is null", gameObject);

        actionState = GetComponent<IActionState>();
        if (actionState == null)
            Debug.LogError("HitBoxHandler actionState is null", gameObject);

        stats = GetComponent<IStats>();
        if (stats == null)
            Debug.LogError("HitBoxHandler equipment is null", gameObject);

        animationHandler = GetComponent<IAnimation>();
        if (animationHandler == null)
            Debug.LogError("HitBoxHandler animationHandler is null", gameObject);

        hurtBoxHandler = GetComponent<IHurtBoxHandler>();
        if (hurtBoxHandler == null)
            Debug.LogError("HitBoxHandler hurtBoxHandler is null", gameObject);

        if (sceneObjectRoot != null)
        {
            sceneObjectHitboxs = sceneObjectRoot.GetComponentsInChildren<HitBox>(true);
            if (sceneObjectHitboxs == null || sceneObjectHitboxs.Length == 0)
                Debug.LogError("HitBoxHandler No Hitboxs found", gameObject);
        }
        else
            Debug.LogError("HitBoxHandler SceneObjectRoot not set", gameObject);

        RegisterToEvents();
    }

    private void RegisterToEvents()
    {
        actionState.ActionStateChangedEvent += OnActionStateChanged;
        stats.AttackStatsChangedEvent += OnAttackStatsChanged;
        animationHandler.AnimationStartedEvent += OnAnimationStarted;
    }

    private void OnDestroy()
    {
        UnregisterToEvents();
    }

    private void UnregisterToEvents()
    {
        actionState.ActionStateChangedEvent -= OnActionStateChanged;
        stats.AttackStatsChangedEvent -= OnAttackStatsChanged;
        animationHandler.AnimationStartedEvent -= OnAnimationStarted;
    }    

    private void OnActionStateChanged(ActionState state)
    {
        DisableAllHitBoxs();
        curWeaponAttackData = null;

        if (animationEventRoutine != null)
            StopCoroutine(animationEventRoutine);

        if (state == ActionState.HitStun)
            EnableSceneObjectHitBoxs();
    }

    private void OnAttackStatsChanged(AttackStatData data)
    {
        if (data == null || data.WeaponRootGameObject == null)
            return;
        
        weaponHitboxs = data.WeaponRootGameObject.GetComponentsInChildren<HitBox>(true).ToList();
        weaponAttackDatas = new List<AttackStatData.AttackStats>();

        if (data.UpTilt != null)
            weaponAttackDatas.Add(data.UpTilt);
        if (data.ForwardTilt != null)
            weaponAttackDatas.Add(data.ForwardTilt);
        if (data.DownTilt != null)
            weaponAttackDatas.Add(data.DownTilt);
        if (data.UpAir != null)
            weaponAttackDatas.Add(data.UpAir);
        if (data.ForwardAir != null)
            weaponAttackDatas.Add(data.ForwardAir);
        if (data.DownAir != null)
            weaponAttackDatas.Add(data.DownAir);
    }

    private void OnAnimationStarted(AnimationClip clip)
    {
        if (actionState.CurActionState == ActionState.Attacking)
        {
            foreach (var attackData in weaponAttackDatas)
            {
                if (attackData.Animation == clip)
                {
                    curWeaponAttackData = attackData;

                    if (animationEventRoutine != null)
                        StopCoroutine(animationEventRoutine);
                    animationEventRoutine = StartCoroutine(CheckForAnimationEvents());

                    break;
                }
            }
        }
    }

    private IEnumerator CheckForAnimationEvents()
    {
        while (curWeaponAttackData != null)
        {
            int frame = animationHandler.GetFrameOfCurrentAnimation();

            if (frame >= curWeaponAttackData.DisableColliderFrame)
                DisableWeaponHitboxs();
            else if (frame >= curWeaponAttackData.EnableColliderFrame)
                EnableWeaponHitboxs();

            yield return null;
        }
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
    }

    private void DisableWeaponHitboxs()
    {
        foreach (HitBox hitBox in weaponHitboxs)
        {
            hitBox.DeactivateHitBox();
            hitBox.OnCollisionEntered -= OnWeaponHit;
        }
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
        //TODO: Damage calculated based on velocity and totalMass
    }

    private void OnWeaponHit(IHurtBox hurtBox)
    {
        if (curWeaponAttackData == null)
            return;

        int frame = animationHandler.GetFrameOfCurrentAnimation();
        hurtBox.Hit(new HitData(sceneObject.UniqueID, curWeaponAttackData.Influence, curWeaponAttackData.LaunchAngle, curWeaponAttackData.GetAttackDamage(frame)));

        //Prevent being hit by sceneObject after making contact
        hurtBoxHandler.SetImmunityFrom(hurtBox.SceneObjectID);
    }

    #endregion
}
