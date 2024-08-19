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
    //NOTE: This tracks what attack is currently happening. This prevents multiple attacks from overwriting one another before an attack animation starts
    [SerializeField] private AttackType curAttackState; 
    //NOTE: This tracks the current attack data being used
    private AttackData curAttackData;
    //NOTE: This tracks any buffered attack
    private Action bufferedAttackAction = null;

    //SceneObject
    private SceneObject sceneObj => GetComponent<SceneObject>();

    //Events
    public event Action<AttackType> AttackStateChangedEvent;


    #region Getters

    public AttackCollection CurAttackCollection => curAttackCollection;

    #endregion


    public void Initialize()
    {
        //TODO: Remove this with the implementation of equipmenthandler
        //Equipment handler should handle updating the current weapon
        OnWeaponChanged(null);

        curAttackPointCollection = new AttackPointCollection(gameObject);

        SetUpEvents();
    }


    private void Update()
    {
        CheckForAnimationTriggers();
    }


    #region Events

    private void SetUpEvents()
    {
        //Animation Events
        sceneObj.AnimationHandler.AnimationStartedEvent += OnAnimationStarted;
        sceneObj.AnimationHandler.AnimationEndedEvent += OnAnimationEnded;
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


    #endregion


    #region Attack State

    private void SetCurrentAttackState(AttackType attackType)
    {
        if (attackType != AttackType.Null)
        {
            //Change state
            if (sceneObj.ActionStateHandler.TryChangeState(ATTACKSTATE))
            {
                //Check not currently attacking
                //Check that attack exists
                if (curAttackState == AttackType.Null &&
                    curAttackCollection.TryGetAttackByType(attackType, out AttackData attack))
                {
                    Debug.Log("ATTACK: CurAttackState Set To: " + attackType);
                    curAttackState = attackType;
                    AttackStateChangedEvent?.Invoke(attackType);
                }
            }
        }

        else
        {
            curAttackState = AttackType.Null;
            curAttackData = null;
            AttackStateChangedEvent?.Invoke(AttackType.Null);
        }
    }

    #endregion


    #region Perform Attack

    /// <summary>
    /// Play the animation for a buffered attack
    /// </summary>
    public void ExecuteBufferedAttack()
    {
        //TODO: ReImplement
        //if (bufferedAttackAction != null)
        //{
        //    bufferedAttackAction.Invoke();
        //    bufferedAttackAction = null;
        //}
    }


    /// <summary>
    /// Buffer attack if currently jumping or landing
    /// Buffered attack will attempt to exacute when transition state ends
    /// </summary>
    /// <param name="attackType"></param>
    private void BufferAttack(Action bufferedAttackAction)
    {
        //TODO: ReImplement
        //if (sceneObj.ActionStateHandler.CurActionState == ActionState.MoveTransition)
        //{
        //    this.bufferedAttackAction = bufferedAttackAction;
        //}
    }


    /// <summary>
    /// Try to perform a grounded / Air Up attack
    /// </summary>
    public void PerformUpAttack()
    {        
        //TODO: What attack would happen when sliding
        if (sceneObj.GroundedState == GroundedState.Airborn)
            SetCurrentAttackState(AttackType.UpAir);
        else
            SetCurrentAttackState(AttackType.UpTilt);              
    }


    /// <summary>
    /// Try to perform a grounded / Air Down attack
    /// </summary>
    public void PerformDownAttack()
    {
        if (sceneObj.GroundedState == GroundedState.Airborn)
            SetCurrentAttackState(AttackType.DownAir);
        else
            SetCurrentAttackState(AttackType.DownTilt); 
    }


    /// <summary>
    /// Try to perform a grounded / Air Forward attack <\br>
    /// Turn around if facing the wrong direction
    /// </summary>
    public void PerformRightAttack()
    {
        if (sceneObj.GroundedState == GroundedState.Airborn)
        {
            if (!sceneObj.IsFacingRightDirection())
                sceneObj.TurnAround();

            SetCurrentAttackState(AttackType.ForwardAir);
        }

        else
        {
            if (!sceneObj.IsFacingRightDirection())
            {
                //Check not sliding
                if (sceneObj.GroundedState == GroundedState.Grounded)
                    sceneObj.TurnAround();
            }

            SetCurrentAttackState(AttackType.ForwardTilt);
        }                
    }


    /// <summary>
    /// Try to perform a grounded / Air Forward attack <\br>
    /// Turn around if facing the wrong direction
    /// </summary>
    public void PerformLeftAttack()
    { 
        if (sceneObj.GroundedState == GroundedState.Airborn)
        {
            if (sceneObj.IsFacingRightDirection())
                sceneObj.TurnAround();

            SetCurrentAttackState(AttackType.ForwardAir);
        }

        else
        {
            if (sceneObj.IsFacingRightDirection())
            {
                if (sceneObj.GroundedState == GroundedState.Grounded)
                    sceneObj.TurnAround();
            }

            SetCurrentAttackState(AttackType.ForwardTilt);
        }        
    }

    #endregion


    #region Animation

    /// <summary>
    /// Check for attack animation
    /// </summary>
    /// <param name="clip"></param>
    private void OnAnimationStarted(AnimationClip clip)
    {
        if (curAttackCollection.TryGetAttackByAnimation(clip.name, out AttackData attackData))
        {
            curAttackData = attackData;

            foreach (AnimationTrigger trigger in attackData.GetAttackTriggers())
                trigger.Reset();
        }
    }


    /// <summary>
    /// Check if attack animation ended
    /// </summary>
    /// <param name="clip"></param>
    /// <exception cref="NotImplementedException"></exception>
    private void OnAnimationEnded(AnimationClip clip)
    {
        if (curAttackCollection.TryGetAttackByAnimation(clip.name, out AttackData attackData))
        {
            if (curAttackData != null)
            {
                SetCurrentAttackState(AttackType.Null);
                curAttackPointCollection.ResetAttackPoints(attackData);
            }
        }
    }


    /// <summary>
    /// Check for animation triggers that need to fire
    /// </summary>
    private void CheckForAnimationTriggers()
    {
        if (sceneObj.ActionStateHandler.CurActionState == ActionState.Attacking)
        {
            int curAnimationFrame = sceneObj.AnimationHandler.GetFrameOfCurrentAnimation();
            
            foreach (AnimationTrigger trigger in curAttackData.GetAttackTriggers())
            {
                if (!trigger.WasTriggered && curAnimationFrame >= trigger.TriggerFrame)
                {
                    ExecuteTrigger(trigger);
                }
            }
        }
    }


    /// <summary>
    /// Execute animation trigger
    /// </summary>
    /// <param name="trigger"></param>
    private void ExecuteTrigger(AnimationTrigger trigger)
    {
        trigger.WasTriggered = true;

        switch (trigger.TriggerType) 
        {
            case AnimationTrigger.Type.EnableCollider:
                curAttackPointCollection.SetupAttackPointsForAttack(curAttackData);
                break;

            case AnimationTrigger.Type.DisableCollider:
                curAttackPointCollection.ResetAttackPoints(curAttackData);
                break;
        }
    }

    #endregion
}
