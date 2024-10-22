using System;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

public enum AttackType { Null, UpTilt, DownTilt, ForwardTilt, UpAir, DownAir, ForwardAir, Dash };

public class AttackInputHandler : MonoBehaviour
{
    const ActionState ATTACKSTATE = ActionState.Attacking;

    [Header("Collection")]
    //TODO: This should be replaced when weapons are fully integrated.
    //Equipment handler will pass the correct attack collection
    [SerializeField] private AttackCollection BaseAttackCollection;
    private AttackCollection curAttackCollection = null;

    [Header("Attack Points")]
    [SerializeField] private AttackPointCollection curAttackPointCollection;

    //Attack Data
    //NOTE: This tracks what attack is currently happening. This prevents multiple attacks from overwriting one another before an attack animation starts
    [SerializeField] private AttackType curAttackState; 
    //NOTE: This tracks the current attack data being used
    private AttackData curAttackData;
    //NOTE: This tracks any buffered attack
    private Action bufferedAttackAction = null;

    //SceneObject
    private SceneObject sceneObject;

    //Events
    public event Action<AttackType> AttackStateChangedEvent;


    #region Getters

    public AttackCollection CurAttackCollection => curAttackCollection;

    #endregion


    #region Initialize

    public void Setup()
    {
        sceneObject = GetComponent<SceneObject>();
        curAttackPointCollection = new AttackPointCollection(gameObject);

        SetUpEvents();
    }


    public void Initialize()
    {
        //TODO: Remove this with the implementation of equipmenthandler
        //Equipment handler should handle updating the current weapon
        OnWeaponChanged(null);
    }

    #endregion


    private void Update()
    {
        CheckForAnimationTriggers();
    }


    #region Events

    private void SetUpEvents()
    {
        //Animation Events
        sceneObject.AnimationHandler.AnimationStartedEvent += OnAnimationStarted;
        sceneObject.AnimationHandler.AnimationEndedEvent += OnAnimationEnded;
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

    /// <summary>
    /// Attempt to set the current attack state <br></br>
    /// This will initiate the animation of the attackType
    /// </summary>
    /// <param name="attackType"></param>
    /// <returns></returns>
    private bool TrySetCurrentAttackState(AttackType attackType)
    {
        if (curAttackCollection != null)
        {
            if (attackType != AttackType.Null)
            {
                //Check not currently attacking and
                //Check that attack exists
                if (curAttackState == AttackType.Null &&
                    curAttackCollection.TryGetAttackByType(attackType, out AttackData attack))
                {
                    //Change state
                    if (sceneObject.ActionStateHandler.TryChangeState(ATTACKSTATE))
                    {
                        curAttackState = attackType;
                        AttackStateChangedEvent?.Invoke(attackType);
                        return true;
                    }
                }
            }

            else
            {
                curAttackState = AttackType.Null;
                curAttackData = null;
                AttackStateChangedEvent?.Invoke(AttackType.Null);
                return true;
            }
        }

        return false;
    }    

    #endregion


    #region Perform Attack

    /// <summary>
    /// Determine if the attack needs to be buffered due to other states
    /// </summary>
    /// <returns></returns>
    private bool AttackToBeBuffered()
    {
        //Cant attack while jumping from ground
        if (sceneObject.ActionStateHandler.CurActionState == ActionState.Moving && sceneObject.MovementInputHandler.CurMoveState == MovementType.Jump)
        {
            //TODO: Buffer attack
            return true;
        }

        return false;
    }


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
        if (!AttackToBeBuffered())
        {
            if (sceneObject.CurGroundedState == GroundedState.Airborn)
                TrySetCurrentAttackState(AttackType.UpAir);
            else
            {
                if (sceneObject.MovementInputHandler.HorizontalInfluence != 0)
                    TrySetCurrentAttackState(AttackType.Dash);
                else
                    TrySetCurrentAttackState(AttackType.UpTilt);              
            }
        }
    }


    /// <summary>
    /// Try to perform a grounded / Air Down attack
    /// </summary>
    public void PerformDownAttack()
    {
        if (!AttackToBeBuffered())
        {
            if (sceneObject.CurGroundedState == GroundedState.Airborn)
                TrySetCurrentAttackState(AttackType.DownAir);
            else
            {
                if (sceneObject.MovementInputHandler.HorizontalInfluence != 0)
                    TrySetCurrentAttackState(AttackType.Dash);
                else
                    TrySetCurrentAttackState(AttackType.DownTilt);
            }
        }
    }


    /// <summary>
    /// Try to perform a grounded / Air Forward attack <\br>
    /// Turn around if facing the wrong direction
    /// </summary>
    public void PerformRightAttack()
    {
        if (!AttackToBeBuffered())
        {
            //Air Attack
            if (sceneObject.CurGroundedState == GroundedState.Airborn)
            {
                if (TrySetCurrentAttackState(AttackType.ForwardAir) && !sceneObject.IsFacingRightDirection())
                    sceneObject.TurnAround();
            }

            //Grounded Attack
            else
            {
                //Dash Attack
                if (sceneObject.MovementInputHandler.HorizontalInfluence != 0)
                    TrySetCurrentAttackState(AttackType.Dash);                

                //Tilt Attacl
                else
                {
                    if (TrySetCurrentAttackState(AttackType.ForwardTilt))
                    {
                        if (!sceneObject.IsFacingRightDirection())
                            sceneObject.TurnAround();                    
                    }                
                }
            }
        }
    }


    /// <summary>
    /// Try to perform a grounded / Air Forward attack <\br>
    /// Turn around if facing the wrong direction
    /// </summary>
    public void PerformLeftAttack()
    {
        if (!AttackToBeBuffered())
        {
            //Air Attack
            if (sceneObject.CurGroundedState == GroundedState.Airborn)
            {
                if (TrySetCurrentAttackState(AttackType.ForwardAir) && sceneObject.IsFacingRightDirection())
                    sceneObject.TurnAround();
            }

            else
            {
                //Dash Attack
                if (sceneObject.MovementInputHandler.HorizontalInfluence != 0)
                    TrySetCurrentAttackState(AttackType.Dash);
                
                //Tilt Attack
                else
                {
                    if (TrySetCurrentAttackState(AttackType.ForwardTilt))
                    {
                        if (sceneObject.IsFacingRightDirection())
                            sceneObject.TurnAround();
                    }
                }
            }
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
        if (curAttackCollection != null && 
            curAttackCollection.TryGetAttackByAnimation(clip, out AttackData attackData))
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
        if (curAttackCollection != null &&
            curAttackCollection.TryGetAttackByAnimation(clip, out AttackData attackData))
        {
            if (curAttackData != null)
            {
                TrySetCurrentAttackState(AttackType.Null);
                curAttackPointCollection.ResetAttackPoints(attackData);
            }
        }
    }


    /// <summary>
    /// Check for animation triggers that need to fire
    /// </summary>
    private void CheckForAnimationTriggers()
    {
        if (sceneObject.ActionStateHandler.CurActionState == ActionState.Attacking && curAttackData != null)
        {
            int curAnimationFrame = sceneObject.AnimationHandler.GetFrameOfCurrentAnimation();

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
