using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEditor;
using UnityEngine;

public class AttackInputHandler : MonoBehaviour
{
    const ActionState.State attackState = ActionState.State.Attacking;

    private SceneObject sceneObj;

    private AttackData curAttackData;

    public AttackCollection BaseAttackCollection;
    public AttackCollection CurAttackCollection;

    private bool isFacingRightDir { get { return sceneObj.IsFacingRightDirection(); } }
    private bool isGrounded { get { return sceneObj.IsGrounded; } }
    private IAnimator animator { get { return sceneObj.Animator; } }
    private IActionState stateHandler { get { return sceneObj.StateHandler; } }
    private IWeaponCollection weaponCollection { get { return sceneObj.EquipmentHandler.Weapons; } }    


    public void Setup(SceneObject obj)
    {
        sceneObj = obj;

        weaponCollection.WeaponChangedEvent += OnWeaponChanged;
        animator.OnAnimationUpdateEvent += OnAttackAnimationUpdated;

        OnWeaponChanged(null);
    }

    #region Events

    private void OnWeaponChanged(Weapon weapon)
    {
        if (weapon == null)
            CurAttackCollection = BaseAttackCollection;

        else
            CurAttackCollection = weapon.AttackCollection;
    }

    #region Attack Performed Events

    private void OnAttackAnimationUpdated(string animationState, AnimationTrigger.Type triggerType)
    {
        if (CurAttackCollection.GetAttackByAnimationClipName(animationState, out AttackData attackData))
        {
            switch(triggerType)
            {
                case AnimationTrigger.Type.Start:
                    OnAttackAnimationStarted(attackData);
                    break;

                case AnimationTrigger.Type.EnableCollider:
                    EnabledAttackColliders(attackData);
                    break;

                case AnimationTrigger.Type.DisableCollider: 
                    DisabledAttackColliders(attackData);
                    break;

                case AnimationTrigger.Type.End:
                    OnAttackAnimationEnded(attackData);
                    break;
            }
        }
    }


    private void OnAttackAnimationStarted(AttackData attackData)
    {
        curAttackData = attackData;
    }


    private void EnabledAttackColliders(AttackData attackData)
    {
        CurAttackCollection.EnableAttackColliders(attackData.ColliderType, OnAttackConnected);        
    }


    private void DisabledAttackColliders(AttackData attackData)
    {
        CurAttackCollection.DisableAttackColliders(attackData.ColliderType, OnAttackConnected);        
    }


    private void OnAttackAnimationEnded(AttackData attackData)
    {
        if (curAttackData != null && curAttackData.AttackAnimation.name == attackData.AttackAnimation.name)
        {
            DisabledAttackColliders(attackData);

            curAttackData = null;
            stateHandler.ResetState();
        }
    }


    public void OnAttackConnected(IDamage hitTarget)
    {
        if (animator.TryGetCurrentFrameOfAnimation(curAttackData.AttackAnimation.name, out float curFrame))
        {            
            float damage = curAttackData.GetAttackDamage(curFrame);            
            float influence = curAttackData.GetAttackInflucence();
            float knockBack = curAttackData.GetAttackKnockBack(curFrame);

            float launchAngle = curAttackData.GetAttackLaunchAngle(curFrame);
            float xLaunch = Mathf.Cos(launchAngle * Mathf.Deg2Rad);
            float yLaunch = Mathf.Sin(launchAngle * Mathf.Deg2Rad);

            string damageDebug = "Damage: " + damage + "\n";
            damageDebug += "Knockback Force: " + knockBack + "\n";
            damageDebug += "Launch Angle: " + launchAngle + "\n";
            damageDebug += "Launch Vector: " + xLaunch * (isFacingRightDir ? 1 : -1) + ", " + yLaunch + "\n";
            Debug.Log(damageDebug);


            hitTarget.AddDamage(damage);
            hitTarget.ApplyForceBasedOnDamage(knockBack, influence, new Vector2(xLaunch * (isFacingRightDir ? 1 : -1), yLaunch));
        }
    }

    #endregion

    #endregion

    #region Perform Attack

    private void PlayAttackAnimation(AttackType attackType)
    {
        if (curAttackData == null && CurAttackCollection.GetAttackByType(attackType, out AttackData attack))
        {
            animator.PlayAnimation(attack.AttackAnimation.name, attack.GetAttackTriggers());
        }
    }

                
    public void PerformUpAttack()
    {
        if (stateHandler.ChangeState(attackState))
        {
            if (isGrounded)
            {
                PlayAttackAnimation(AttackType.UpTilt);
            }

            else
            {
               PlayAttackAnimation(AttackType.UpAir);
            }
        }
    }


    public void PerformDownAttack()
    {
        if (stateHandler.ChangeState(attackState))
        {
            if (isGrounded)
            {
                PlayAttackAnimation(AttackType.DownTilt);                
            }

            else
            {
                PlayAttackAnimation(AttackType.DownAir);
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

                PlayAttackAnimation(AttackType.ForwardTilt);
            }

            else
            {
                if (!isFacingRightDir)
                    PlayAttackAnimation(AttackType.BackAir);

                else
                    PlayAttackAnimation(AttackType.ForwardAir);
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

                PlayAttackAnimation(AttackType.ForwardTilt);
            }

            else
            {
                if (isFacingRightDir)
                    PlayAttackAnimation(AttackType.BackAir);

                else
                    PlayAttackAnimation(AttackType.ForwardAir);
            }
        }
    }        

    #endregion

}
