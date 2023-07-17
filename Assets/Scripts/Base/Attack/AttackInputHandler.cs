using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEditor;
using UnityEngine;

public class AttackInputHandler : MonoBehaviour, IAttack
{
    const ActionState.State attackState = ActionState.State.Attacking;

    private SceneObject sceneObj;

    private bool isFacingRightDir { get { return sceneObj.IsFacingRightDirection(); } }
    private bool isGrounded { get { return sceneObj.isGrounded; } }
    private IAnimator animator { get { return sceneObj.animator; } }
    private IActionState stateHandler { get { return sceneObj.stateHandler; } }
    private EquipmentHandler equipmentHandler { get { return sceneObj.equipmentHandler; } }
    private AttackCollection curAttackCollection { get { return sceneObj.equipmentHandler.GetCurrentWeaponAttackCollection(); } }


    public void Setup(SceneObject obj)
    {
        sceneObj = obj;

        animator.OnAnimationStartedEvent += OnAttackAnimationStarted;
        animator.OnAnimationEndedEvent += OnAttackAnimationEnded;
        animator.OnAnimationTriggerEvent += OnAttackTrigger;
    }


    #region Attack Performed Events

    private void OnAttackAnimationStarted(AnimationClip clip)
    {
        //TODO: Determine if the clip exists in the weapon

        Debug.Log("Animation " + clip.name + " Started");
    }


    private void OnAttackAnimationEnded(AnimationClip clip)
    {
        stateHandler.ResetState();

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
            attack.SetAttackDirection(sceneObj.IsFacingRightDirection());
            equipmentHandler.WeaponCollection.EnableAttackColliders(attack.attackPointTags, attack.OnHit);
        }
    }


    private void DisabledAttackColliders(AnimationClip clip)
    {
        Debug.Log("Animation " + clip.name + " Colliders Disabled");

        if (curAttackCollection.GetAttackByClip(clip, out AttackCollection.Attack attack))
        {
            equipmentHandler.WeaponCollection.DisableAttackColliders(attack.attackPointTags, attack.OnHit);
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
        if (stateHandler.ChangeState(attackState))
        {
            if (isGrounded)
            {
                animator.PlayAnimation(GetAttackFromType(AttackType.Type.upTilt));
            }

            else
            {
                animator.PlayAnimation(GetAttackFromType(AttackType.Type.upAir));
            }
        }
    }


    public void PerformDownAttack()
    {
        if (stateHandler.ChangeState(attackState))
        {
            if (isGrounded)
            {
                animator.PlayAnimation(GetAttackFromType(AttackType.Type.downTilt));                
            }

            else
            {
                animator.PlayAnimation(GetAttackFromType(AttackType.Type.downAir));
            }
        }
    }


    public void PerformRightAttack()
    {
        if (stateHandler.ChangeState(attackState))
        {
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
    }


    public void PerformLeftAttack()
    {
        if (stateHandler.ChangeState(attackState))
        {
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
    }        

    #endregion

}
