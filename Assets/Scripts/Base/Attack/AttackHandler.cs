using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEditor;
using UnityEngine;

public class AttackHandler : MonoBehaviour, IAttack
{
    private SceneObject sceneObj;

    private bool isFacingRightDir { get { return sceneObj.IsFacingRightDirection(); } }
    private bool isGrounded { get { return sceneObj.isGrounded; } }
    private IAnimator animator { get { return sceneObj.animator; } }

    private AttackCollection curAttackCollection; //TODO: Later fetched from equipment handler to grab weapon


    public void Setup(SceneObject obj, EquipmentHandler equipmentHandler)
    {
        sceneObj = obj;
        equipmentHandler.RegisterToWeaponChange(SetWeapon);

        animator.OnAnimationStartedEvent += OnAttackAnimationStarted;
        animator.OnAnimationEndedEvent += OnAttackAnimationEnded;
        animator.OnAnimationTriggerEvent += OnAttackTrigger;
    }


    public void SetWeapon(Weapon weapon)
    {
        curAttackCollection = weapon.attackCollection;


        //AttackAnimationEventListener listener = GetComponentInChildren<AttackAnimationEventListener>();
        //listener.AttackAnimationStarted += OnAttackAnimationStarted;
        //listener.AttackAnimationEnded += OnAttackAnimationEnded;
        //listener.AttackAnimationCollidersEnabled += OnAttackCollidersEnabled;
        //listener.AttackAnimationCollidersDisabled += OnAttackCollidersDisabled;
    }


    #region Attack Performed Events

    private void OnAttackAnimationStarted(AnimationClip clip)
    {
        //TODO: Determine if the clip exists in the weapon


        Debug.Log("Animation " + clip.name + " Started");
    }


    private void OnAttackAnimationEnded(AnimationClip clip)
    {
        //TODO: Determine if the clip exists in the weapon

        Debug.Log("Animation " + clip.name + " ended");
    }


    private void OnAttackTrigger(AnimationClip clip, string trigger)
    {
        switch (trigger)
        {
            case "EnableColliders":
                EnabledAttackColliders(clip);
                break;

            case "DisableColliders":
                DisabledAttackColliders(clip);
                break;
        }
    }


    private void EnabledAttackColliders(AnimationClip clip)
    {
        Debug.Log("Animation " + clip.name + " Colliders Enabled");
        if (curAttackCollection.GetAttackByClip(clip, out AttackCollection.Attack attack))
        {
            attack.EnableColliders(isFacingRightDir);
        }
    }


    private void DisabledAttackColliders(AnimationClip clip)
    {
        Debug.Log("Animation " + clip.name + " Colliders Disabled");

        if (curAttackCollection.GetAttackByClip(clip, out AttackCollection.Attack attack))
        {
            attack.DisableColliders();
        }
    }


    #endregion

    #region Perform Attack

    private string GetAttackFromType(AttackType.Type attackType)
    {
        if (curAttackCollection.GetAttackByType(attackType, out AttackCollection.Attack attack))
        {
            return attack.animationClip.name;            
        }

        return null;
    }

                
    public void PerformUpAttack()
    {
        //TODO: Check state
        if (isGrounded)
        {
            animator.PlayAnimation(GetAttackFromType(AttackType.Type.upTilt));
        }

        else
        {
            animator.PlayAnimation(GetAttackFromType(AttackType.Type.upAir));
        }
    }


    public void PerformDownAttack()
    {
        //TODO: Check state
        if (isGrounded)
        {
            animator.PlayAnimation(GetAttackFromType(AttackType.Type.downTilt));
        }

        else
        {
            animator.PlayAnimation(GetAttackFromType(AttackType.Type.downAir));
        }
    }


    public void PerformRightAttack()
    {
        //TODO: Check state
        if (isGrounded)
        {
            if (!isFacingRightDir)
            {
                sceneObj.TurnAround();
            }
                    
            animator.PlayAnimation(GetAttackFromType(AttackType.Type.forwardTilt));               
        }

        else
        {
            if (!isFacingRightDir)
                animator.PlayAnimation(GetAttackFromType(AttackType.Type.backAir));

            else
                animator.PlayAnimation(GetAttackFromType(AttackType.Type.forwardAir));
        }
    }


    public void PerformLeftAttack()
    {
        //TODO: Check state
        if (isGrounded)
        {
            if (isFacingRightDir)
            {
                sceneObj.TurnAround();
            }

            animator.PlayAnimation(GetAttackFromType(AttackType.Type.forwardTilt));
        }

        else
        {
            if (isFacingRightDir)
                animator.PlayAnimation(GetAttackFromType(AttackType.Type.backAir));

            else
                animator.PlayAnimation(GetAttackFromType(AttackType.Type.forwardAir));
        }
    }        

    #endregion
    
}
