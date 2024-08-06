using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEditor;
using UnityEngine;
using UnityEngine.Android;

public class AttackInputHandler : MonoBehaviour
{
    const ActionState ATTACKSTATE = ActionState.Attacking;

    [Header("Collection")]
    [SerializeField] private AttackCollection curAttackCollection;

    [Header("Attack Points")]
    [SerializeField] private AttackPointCollection curAttackPointCollection;

    [Header("Base Collection (To be removed with Weapon integration)")]
    //TODO: This should be replaced when weapons are fully integrated.
    //Equipment handler will pass the correct attack collection
    [SerializeField] private AttackCollection BaseAttackCollection;

    //Attack Data
    private AttackData curAttackData;
    private Action bufferedAttackAction = null;

    //SceneObject
    private SceneObject sceneObj => GetComponent<SceneObject>();

    //Events
    public event Action<AttackType> AttackStateChangedEvent;

    #region Getters

    public AttackCollection CurAttackCollection => curAttackCollection;

    #endregion


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
            switch (triggerType)
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

    private void PlayAttackAnimation(AttackType attackType)
    {
        if (curAttackData == null && curAttackCollection.TryGetAttackByType(attackType, out AttackData attack))
        {
            sceneObj.AnimationStateHandler.PlayAnimation(new AnimationStateData(attack.AttackAnimation.name, ATTACKSTATE, attack.GetAttackTriggers()));
        }
    }


    /// <summary>
    /// Play the animation for a buffered attack
    /// </summary>
    public void ExecuteBufferedAttack()
    {
        if (bufferedAttackAction != null)
        {
            bufferedAttackAction.Invoke();
            bufferedAttackAction = null;
        }
    }


    /// <summary>
    /// Buffer attack if currently jumping or landing
    /// Buffered attack will attempt to exacute when transition state ends
    /// </summary>
    /// <param name="attackType"></param>
    private void BufferAttack(Action bufferedAttackAction)
    {
        if (sceneObj.AnimationStateHandler.CurActionState == ActionState.MoveTransition)
        {
            this.bufferedAttackAction = bufferedAttackAction;
        }
    }


    /// <summary>
    /// Try to perform a grounded / Air Up attack
    /// </summary>
    public void PerformUpAttack()
    {
        if (sceneObj.AnimationStateHandler.IsStatePossible(ATTACKSTATE))
        {
            //TODO: What attack would happen when sliding
            if (sceneObj.GroundedState == GroundedState.Airborn)
                PlayAttackAnimation(AttackType.UpAir);

            else
                PlayAttackAnimation(AttackType.UpTilt);
        }

        else
        {
            BufferAttack(this.PerformUpAttack);
        }
    }


    /// <summary>
    /// Try to perform a grounded / Air Down attack
    /// </summary>
    public void PerformDownAttack()
    {
        if (sceneObj.AnimationStateHandler.IsStatePossible(ATTACKSTATE))
        {
            if (sceneObj.GroundedState == GroundedState.Airborn)
                PlayAttackAnimation(AttackType.DownAir);

            else
                PlayAttackAnimation(AttackType.DownTilt);
        }

        else
        {
            BufferAttack(this.PerformDownAttack);
        }
    }


    /// <summary>
    /// Try to perform a grounded / Air Forward attack <\br>
    /// Turn around if facing the wrong direction
    /// </summary>
    public void PerformRightAttack()
    {
        if (sceneObj.AnimationStateHandler.IsStatePossible(ATTACKSTATE))
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

        else
        {
            BufferAttack(this.PerformRightAttack);            
        }
    }


    /// <summary>
    /// Try to perform a grounded / Air Forward attack <\br>
    /// Turn around if facing the wrong direction
    /// </summary>
    public void PerformLeftAttack()
    {
        if (sceneObj.AnimationStateHandler.IsStatePossible(ATTACKSTATE))
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

        else
        {
            BufferAttack(this.PerformLeftAttack);           
        }
    }

    #endregion

}
