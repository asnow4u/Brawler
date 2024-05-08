using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEditor;
using UnityEngine;
using UnityEngine.Android;

public class AttackInputHandler : MonoBehaviour
{
    const ActionState ATTACKSTATE = ActionState.Attacking;

    //Attack Data
    private AttackData curAttackData;
    public AttackCollection BaseAttackCollection;
    public AttackCollection CurAttackCollection; 

    //SceneObject
    private SceneObject sceneObj => GetComponent<SceneObject>();


    public void Setup()
    {
        //sceneObj.EquipmentHandler.Weapons.WeaponChangedEvent += OnWeaponChanged;
        sceneObj.AnimationStateHandler.OnAnimationUpdateEvent += OnAnimationUpdated;

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

    private void OnAnimationUpdated(string animationState, AnimationTrigger.Type triggerType)
    {
        if (CurAttackCollection.TryGetAttackByAnimation(animationState, out AttackData attackData))
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
        }
    }


    public void OnAttackConnected(IDamage hitTarget)
    {
        //TODO: have handler keep track of what animation is cur playing
        //if (sceneObj.AnimationHandler.TryGetCurrentFrameOfAnimation(curAttackData.AttackAnimation.name, out float curFrame))
        //{            
        //    float damage = curAttackData.GetAttackDamage(curFrame);            
        //    float influence = curAttackData.GetAttackInflucence();
        //    float knockBack = curAttackData.GetAttackKnockBack(curFrame);

        //    float launchAngle = curAttackData.GetAttackLaunchAngle(curFrame);
        //    float xLaunch = Mathf.Cos(launchAngle * Mathf.Deg2Rad);
        //    float yLaunch = Mathf.Sin(launchAngle * Mathf.Deg2Rad);

        //    string damageDebug = "Damage: " + damage + "\n";
        //    damageDebug += "Knockback Force: " + knockBack + "\n";
        //    damageDebug += "Launch Angle: " + launchAngle + "\n";
        //    damageDebug += "Launch Vector: " + xLaunch * (sceneObj.IsFacingRightDirection() ? 1 : -1) + ", " + yLaunch + "\n";
        //    Debug.Log(damageDebug);


        //    hitTarget.AddDamage(damage);
        //    hitTarget.ApplyForceBasedOnDamage(knockBack, influence, new Vector2(xLaunch * (sceneObj.IsFacingRightDirection() ? 1 : -1), yLaunch));
        //}
    }

    #endregion

    #endregion

    #region Perform Attack

    private bool ChangeToAttackState()
    {
        //TODO: Buffer attack till after jump/landing is done

        if (sceneObj.MovementInputHandler.CurMoveState != MovementType.Jump &&
            sceneObj.MovementInputHandler.CurMoveState != MovementType.Landing) 
        {
            return sceneObj.AnimationStateHandler.IsStatePossible(ATTACKSTATE);
        }

        return false;
    }

    private void PlayAttackAnimation(AttackType attackType)
    {
        if (curAttackData == null && CurAttackCollection.GetAttackByType(attackType, out AttackData attack))
        {
            sceneObj.AnimationStateHandler.PlayAnimation(new AnimationStateData(attack.AttackAnimation.name, ATTACKSTATE, attack.GetAttackTriggers()));
        }
    }

                
    public void PerformUpAttack()
    {
        if (ChangeToAttackState())
        {
            //TODO: What attack would happen when sliding
            if (sceneObj.GroundedState == GroundedState.Airborn)
            {
                PlayAttackAnimation(AttackType.UpAir);
            }

            else
            {
                PlayAttackAnimation(AttackType.UpTilt);
            }
        }
    }


    public void PerformDownAttack()
    {
        if (ChangeToAttackState())
        {
            if (sceneObj.GroundedState == GroundedState.Airborn)
            {
                PlayAttackAnimation(AttackType.DownAir);
            }

            else
            {
                PlayAttackAnimation(AttackType.DownTilt);                
            }
        }
    }


    public void PerformRightAttack()
    {
        if (ChangeToAttackState())
        {
            if (sceneObj.GroundedState == GroundedState.Airborn)
            {
                if (!sceneObj.IsFacingRightDirection())
                    sceneObj.TurnAround();

                PlayAttackAnimation(AttackType.ForwardAir);                
            }

            else
            {
                if (!sceneObj.IsFacingRightDirection())
                {
                    //Check not sliding
                    if (sceneObj.GroundedState == GroundedState.Grounded)
                        sceneObj.TurnAround();  
                }

                PlayAttackAnimation(AttackType.ForwardTilt);
            }
        }
    }


    public void PerformLeftAttack()
    {
        if (ChangeToAttackState())
        {
            if (sceneObj.GroundedState == GroundedState.Airborn)
            {
                    if (sceneObj.IsFacingRightDirection())
                        sceneObj.TurnAround();

                    PlayAttackAnimation(AttackType.ForwardAir);                
            }

            else
            {
                if (sceneObj.IsFacingRightDirection())
                {
                    if (sceneObj.GroundedState == GroundedState.Grounded)                    
                        sceneObj.TurnAround();
                }

                PlayAttackAnimation(AttackType.ForwardTilt);
            }
        }
    }        

    #endregion

}
