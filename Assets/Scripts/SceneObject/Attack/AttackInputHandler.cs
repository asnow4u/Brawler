using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEditor;
using UnityEngine;
using UnityEngine.Android;

public class AttackInputHandler : MonoBehaviour
{
    const ActionState ATTACKSTATE = ActionState.Attacking;

    [Header("Attacks")]
    [SerializeField] private AttackCollection curAttackCollection;

    [Header("Attack Points")]
    [SerializeField] private AttackPointCollection curAttackPointCollection;

    [Header("Base Collection (To be removed with Weapon integration)")]
    //TODO: This should be replaced when weapons are fully integrated.
    //Equipment handler will pass the correct attack collection
    [SerializeField] private AttackCollection BaseAttackCollection;


    //Attack Data
    private AttackData curAttackData;

    //SceneObject
    private SceneObject sceneObj => GetComponent<SceneObject>();

    #region Initialize

    public void Setup()
    {
        //TODO: Remove this with the implementation of equipmenthandler
        //Equipment handler should handle updating the current weapon
        OnWeaponChanged(null);

        curAttackPointCollection = new AttackPointCollection(gameObject);

        SetUpEvents();
    }


    #endregion

    #region Events

    private void SetUpEvents()
    {

        //sceneObj.EquipmentHandler.Weapons.WeaponChangedEvent += OnWeaponChanged;
        sceneObj.AnimationStateHandler.OnAnimationUpdateEvent += OnAnimationUpdated;
    }

    private void OnWeaponChanged(Weapon weapon)
    {
        //TODO: Remove this with the implementation of equipmenthandler
        //Should always get the attackCollection even for base
        if (weapon == null)
            curAttackCollection = BaseAttackCollection;

        else
            curAttackCollection = weapon.AttackCollection;
    }


    #region Attack Performed Events

    private void OnAnimationUpdated(string animationState, AnimationTrigger.Type triggerType)
    {
        if (curAttackCollection.TryGetAttackByAnimation(animationState, out AttackData attackData))
        {
            switch(triggerType)
            {
                case AnimationTrigger.Type.Start:
                    OnAttackAnimationStarted(attackData);
                    break;

                case AnimationTrigger.Type.EnableCollider:
                    SetUpAttackPoints(attackData);
                    break;

                case AnimationTrigger.Type.DisableCollider: 
                    ResetAttackPoints(attackData);
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


    private void SetUpAttackPoints(AttackData attackData)
    {
        curAttackPointCollection.SetupAttackPointsForAttack(attackData);        
    }


    private void ResetAttackPoints(AttackData attackData)
    {
        curAttackPointCollection.ResetAttackPoints(attackData);        
    }


    private void OnAttackAnimationEnded(AttackData attackData)
    {
        if (curAttackData != null && curAttackData.AttackAnimation.name == attackData.AttackAnimation.name)
        {
            ResetAttackPoints(attackData);

            curAttackData = null;
        }
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
        if (curAttackData == null && curAttackCollection.GetAttackByType(attackType, out AttackData attack))
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
