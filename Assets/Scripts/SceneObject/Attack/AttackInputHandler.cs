using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEditor;
using UnityEngine;

public class AttackInputHandler : MonoBehaviour, IAttack
{
    const ActionState.State attackState = ActionState.State.Attacking;

    private SceneObject sceneObj;

    private string curAttackAnimationState;

    private bool isFacingRightDir { get { return sceneObj.IsFacingRightDirection(); } }
    private bool isGrounded { get { return sceneObj.isGrounded; } }
    private IAnimator animator { get { return sceneObj.animator; } }
    private IActionState stateHandler { get { return sceneObj.stateHandler; } }
    private IWeaponCollection weaponCollection { get { return sceneObj.equipmentHandler.Weapons; } }
    private Weapon curWeapon { get { return weaponCollection.GetCurWeapon(); } }
    private AttackCollection curAttackCollection { get { return curWeapon.AttackCollection; } }


    public void Setup(SceneObject obj)
    {
        sceneObj = obj;

        animator.OnAnimationUpdateEvent += OnAttackAnimationUpdated;
    }


    #region Attack Performed Events

    private void OnAttackAnimationUpdated(string animationState, AnimationTrigger.Type triggerType)
    {
        if (GetAttackTypeFromAnimationState(animationState, out AttackType.Type type))
        {
            switch(triggerType)
            {
                case AnimationTrigger.Type.Start:
                    OnAttackAnimationStarted(animationState);
                    break;

                case AnimationTrigger.Type.EnableCollider:
                    EnabledAttackColliders(type);
                    break;

                case AnimationTrigger.Type.DisableCollider: 
                    DisabledAttackColliders(type);
                    break;

                case AnimationTrigger.Type.End:
                    OnAttackAnimationEnded(animationState);
                    break;
            }
        }
    }


    private bool GetAttackTypeFromAnimationState(string animationState, out AttackType.Type type)
    {
        string str = animationState;
        str = str.Replace(gameObject.name, "");
        str = str.Replace(curWeapon.weaponType.ToString(), "");

        if (Enum.TryParse(str, out type))
        {
            return true;
        }

        return false;
    }


    private void OnAttackAnimationStarted(string animationState)
    {
        curAttackAnimationState = animationState;        
    }


    private void EnabledAttackColliders(AttackType.Type attackType)
    {
        Debug.Log("ATTACK: Enable Collider " + attackType.ToString());
        if (curAttackCollection.GetAttackByType(attackType, out AttackCollection.Attack attack))
        {
            attack.SetAttackDirection(sceneObj.IsFacingRightDirection());
            weaponCollection.EnableAttackColliders(attack.attackPointTags, attack.OnHit);
        }
    }


    private void DisabledAttackColliders(AttackType.Type attackType)
    {
        Debug.Log("ATTACK: Disable Collider " + attackType.ToString());
        if (curAttackCollection.GetAttackByType(attackType, out AttackCollection.Attack attack))
        {
            weaponCollection.DisableAttackColliders(attack.attackPointTags, attack.OnHit);
        }
    }


    private void OnAttackAnimationEnded(string animationState)
    {
        if (curAttackAnimationState == animationState)
        {
            if (GetAttackTypeFromAnimationState(animationState, out AttackType.Type attackType))
            {
                DisabledAttackColliders(attackType);
            }

            curAttackAnimationState = null;
            stateHandler.ResetState();
        }
    }   

    #endregion

    #region Perform Attack

    private void PlayAttackAnimation(AttackType.Type attackType)
    {
        if (curAttackCollection.GetAttackByType(attackType, out AttackCollection.Attack attack))
        {
            string name = gameObject.name;
            string weaponName = curWeapon.weaponType.ToString();
            string attackName = attack.type.ToString();

            animator.PlayAnimation(name + weaponName + attackName, attack.triggers.ToArray());
        }
    }

                
    public void PerformUpAttack()
    {
        if (stateHandler.ChangeState(attackState))
        {
            if (isGrounded)
            {
                PlayAttackAnimation(AttackType.Type.UpTilt);
            }

            else
            {
               PlayAttackAnimation(AttackType.Type.UpAir);
            }
        }
    }


    public void PerformDownAttack()
    {
        if (stateHandler.ChangeState(attackState))
        {
            if (isGrounded)
            {
                PlayAttackAnimation(AttackType.Type.DownTilt);                
            }

            else
            {
                PlayAttackAnimation(AttackType.Type.DownAir);
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

                PlayAttackAnimation(AttackType.Type.ForwardTilt);
            }

            else
            {
                if (!isFacingRightDir)
                    PlayAttackAnimation(AttackType.Type.BackAir);

                else
                    PlayAttackAnimation(AttackType.Type.ForwardAir);
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

                PlayAttackAnimation(AttackType.Type.ForwardTilt);
            }

            else
            {
                if (isFacingRightDir)
                    PlayAttackAnimation(AttackType.Type.BackAir);

                else
                    PlayAttackAnimation(AttackType.Type.ForwardAir);
            }
        }
    }        

    #endregion

}
