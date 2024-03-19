using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEditor;
using UnityEngine;

public class AttackInputHandler : MonoBehaviour
{
    const ActionState.State ATTACKSTATE = ActionState.State.Attacking;

    //Attack Data
    private AttackData curAttackData;
    public AttackCollection BaseAttackCollection;
    public AttackCollection CurAttackCollection; 

    //SceneObject
    private SceneObject sceneObj => GetComponent<SceneObject>();
    


    public void Setup()
    {
        //sceneObj.EquipmentHandler.Weapons.WeaponChangedEvent += OnWeaponChanged;
        sceneObj.AnimationHandler.OnAnimationUpdateEvent += OnAttackAnimationUpdated;

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
        if (CurAttackCollection.TryGetAttackByAnimationClipName(animationState, out AttackData attackData))
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
            sceneObj.StateHandler.ResetState();
        }
    }


    public void OnAttackConnected(IDamage hitTarget)
    {
        if (sceneObj.AnimationHandler.TryGetCurrentFrameOfAnimation(curAttackData.AttackAnimation.name, out float curFrame))
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
            damageDebug += "Launch Vector: " + xLaunch * (sceneObj.IsFacingRightDirection() ? 1 : -1) + ", " + yLaunch + "\n";
            Debug.Log(damageDebug);


            hitTarget.AddDamage(damage);
            hitTarget.ApplyForceBasedOnDamage(knockBack, influence, new Vector2(xLaunch * (sceneObj.IsFacingRightDirection() ? 1 : -1), yLaunch));
        }
    }

    #endregion

    #endregion

    #region Perform Attack

    private void PlayAttackAnimation(AttackType attackType)
    {
        if (curAttackData == null && CurAttackCollection.GetAttackByType(attackType, out AttackData attack))
        {
            sceneObj.AnimationHandler.PlayAnimation(attack.AttackAnimation.name, attack.GetAttackTriggers());
        }
    }

                
    public void PerformUpAttack()
    {
        if (sceneObj.StateHandler.ChangeState(ATTACKSTATE))
        {
            if (sceneObj.MovementInputHandler.IsGrounded)
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
        if (sceneObj.StateHandler.ChangeState(ATTACKSTATE))
        {
            if (sceneObj.MovementInputHandler.IsGrounded)
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
        if (sceneObj.StateHandler.ChangeState(ATTACKSTATE))
        {
            if (sceneObj.MovementInputHandler.IsGrounded)
            {
                if (!sceneObj.IsFacingRightDirection())
                {
                    sceneObj.TurnAround();
                }

                PlayAttackAnimation(AttackType.ForwardTilt);
            }

            else
            {
                if (!sceneObj.IsFacingRightDirection())
                    PlayAttackAnimation(AttackType.BackAir);

                else
                    PlayAttackAnimation(AttackType.ForwardAir);
            }
        }
    }


    public void PerformLeftAttack()
    {
        if (sceneObj.StateHandler.ChangeState(ATTACKSTATE))
        {
            if (sceneObj.MovementInputHandler.IsGrounded)
            {
                if (sceneObj.IsFacingRightDirection())
                {
                    sceneObj.TurnAround();
                }

                PlayAttackAnimation(AttackType.ForwardTilt);
            }

            else
            {
                if (sceneObj.IsFacingRightDirection())
                    PlayAttackAnimation(AttackType.BackAir);

                else
                    PlayAttackAnimation(AttackType.ForwardAir);
            }
        }
    }        

    #endregion

}
