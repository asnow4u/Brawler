using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;


public class MovementInputHandler : MonoBehaviour
{
    const ActionState.State MOVESTATE = ActionState.State.Moving;

    //Movement State Data
    private MovementType curMoveState = MovementType.Move;
    private string curMoveAnimationState;

    //Movement Data
    public bool IsGrounded = true;    
    public float gravityScaler = 1;
    public MovementCollection BaseMovementCollection;
    public MovementCollection CurMovementCollection;

    //Infulence
    private float horizontalInfluence;
    private float verticalInfluence;
    private int numJumpsPerformed;
   
    //SceneObject
    private SceneObject sceneObj => GetComponent<SceneObject>();
    private Rigidbody rb => GetComponent<Rigidbody>();
    

    //Events
    public Action<MovementCollection> MovementCollectionChanged;


    #region Initialize

    public void Setup()
    {
        //NOTE: Uses ApplyGravity instead
        rb.useGravity = false;

        //sceneObj.EquipmentHandler.Weapons.WeaponChangedEvent += OnWeaponChanged;
        sceneObj.AnimationHandler.OnAnimationUpdateEvent += OnMovementAnimationUpdated;

        OnWeaponChanged(null);        
    }

    #endregion

    #region Events

    private void OnWeaponChanged(Weapon weapon)
    {
        if (weapon == null)        
            CurMovementCollection = BaseMovementCollection;       

        else
            CurMovementCollection = weapon.MovementCollection;

        MovementCollectionChanged?.Invoke(CurMovementCollection);
    }

    private void OnMovementAnimationUpdated(string animationState, AnimationTrigger.Type triggerType)
    {
        if (CurMovementCollection.TryGetMovementFromAnimationClip(animationState, out MovementData move))
        {
            switch (triggerType)
            {
                case AnimationTrigger.Type.Start:
                    OnMovementAnimationStarted(animationState, move.Type);
                    break;

                case AnimationTrigger.Type.End:
                    OnMovementAnimationEnded(animationState, move.Type);
                    break;
            }
        }
    }

    private void OnMovementAnimationStarted(string animationState, MovementType type)
    {
        curMoveAnimationState = animationState;        
    }

    private void OnMovementAnimationEnded(string animationState, MovementType type) 
    {        
        if (curMoveAnimationState == animationState)
        {
            curMoveAnimationState = null;

            switch(type)
            {
                case MovementType.Move:
                    break;

                case MovementType.Jump:
                case MovementType.AirJump:
                    //curMoveState = MovementType.Type.Fall;                    
                    break;

                case MovementType.Fall:
                    curMoveState = MovementType.Land;
                    break;

                case MovementType.Roll:
                case MovementType.Land:
                    curMoveState = MovementType.Move;
                    sceneObj.StateHandler.ResetState();
                    break;
            }                                                    
        }
    }

    #endregion


    private void ChangeMoveState(MovementType moveType)
    {
        curMoveState = moveType;
    }


    /// <summary>
    /// Horizontal movement based on input value
    /// Input value ranges between (-1, 1) 
    /// Input Value determines how much of the curCollection moveSpeed should be applied
    /// </summary>
    /// <param name="moveInfluence"></param>
    public void PerformMovement(Vector2 moveInfluence)
    {       
        //Check that moveData exists
        if (CurMovementCollection.TryGetMovementByType(MovementType.Move, out MovementData movement))
        {
            if (sceneObj.StateHandler.ChangeState(MOVESTATE))
            {
                horizontalInfluence = Mathf.Clamp(moveInfluence.x, -1, 1);
                verticalInfluence = Mathf.Clamp(moveInfluence.y, -1, 1);                 
            }
        }                           
    }


    #region Jump

    /// <summary>
    /// Vertical jump movement based on input value
    /// Input value ranges between (0, 1) 
    /// </summary>
    /// <param name="jumpInfluence"></param>
    public void PerformJump(float jumpInfluence)
    {
        jumpInfluence = Mathf.Clamp01(jumpInfluence);

        if (IsGrounded)
        {
            if (CurMovementCollection.TryGetMovementByType(MovementType.Jump, out MovementData jump))
            {
                if (sceneObj.StateHandler.ChangeState(MOVESTATE))
                {
                    JumpAction((JumpData)jump, jumpInfluence);
                }
            }
        }
        
        else
        {
            if (CurMovementCollection.TryGetMovementByType(MovementType.AirJump, out MovementData airJump))
            {
                if (sceneObj.StateHandler.ChangeState(MOVESTATE))
                {
                    AirJumpAction((AirJumpData)airJump, jumpInfluence);
                }
            }
        }        
    }


    private void JumpAction(JumpData jumpData, float jumpInfluence)
    {
        ChangeMoveState(jumpData.Type);
        
        rb.velocity = new Vector3(rb.velocity.x, jumpData.JumpVelocity * jumpInfluence, rb.velocity.z);
        numJumpsPerformed++;

        PlayMoveAnimation(jumpData.Type);        
    }


    private void AirJumpAction(AirJumpData airJumpData, float jumpInfluence)
    {
        if (numJumpsPerformed < airJumpData.JumpsAvailable)
        {
            ChangeMoveState(airJumpData.Type);
         
            rb.velocity = new Vector3(rb.velocity.x, airJumpData.AirJumpVelocity * jumpInfluence, rb.velocity.z);
            numJumpsPerformed++;

            CheckTurnAround();

            PlayMoveAnimation(airJumpData.Type);
        }
    }


    //TODO: Will work out later
    private void PerformLand()
    {
        Debug.Log("Land: \nPos: " + transform.position.x + "\nVelocity: " + rb.velocity.x + "\nTime: " + Time.time);

        curMoveState = MovementType.Move;
        numJumpsPerformed = 0;

        sceneObj.StateHandler.ResetState();

        //if (curMovementCollection.GetMovementByType(MovementType.Type.Land, out MovementCollection.Movement movement))
        //{
        //    ChangeMoveState(MovementType.Type.Land);
        //    PlayMoveAnimation(movement.type);
        //}
    }

    #endregion


    private void CheckTurnAround()
    {
        if ((sceneObj.IsFacingRightDirection() && horizontalInfluence < 0) 
            || (!sceneObj.IsFacingRightDirection() && horizontalInfluence > 0))
        {
            sceneObj.TurnAround();
            rb.velocity = new Vector3(rb.velocity.x * -1, rb.velocity.y, rb.velocity.z);
        }
    }


    private void FixedUpdate()
    {
        //IsGrounded
        CheckGroundedStatus();

        //Gravity Scaler
        ApplyGravity();

        //Check for landing
        if (IsGrounded && rb.velocity.y < 0 && (curMoveState == MovementType.Jump || curMoveState == MovementType.AirJump))
        {
            PerformLand();
        }


        //Check Action State and Movement data
        if (sceneObj.StateHandler.ChangeState(MOVESTATE) && CurMovementCollection.ContainsMovementType(MovementType.Move))
        {
            if (IsGrounded && (curMoveState == MovementType.Move || curMoveState == MovementType.Land))
            {
                UpdateGroundMovement();
            }

            else
            {
                UpdateAirMovement();
            }
        }
    }


    /// <summary>
    /// Check to see if grounded
    /// </summary>
    private void CheckGroundedStatus()
    {
        //TODO: Store bounds
        Bounds bounds = GetComponent<Collider>().bounds;
        
        if (Physics.Raycast(bounds.center, Vector3.down, out RaycastHit hit, bounds.size.y, ~LayerMask.NameToLayer("Environment")))
        {
            if (bounds.min.y <= hit.point.y + 0.001f)
                IsGrounded = true;

            else
                IsGrounded = false;
        }                                
    }


    /// <summary>
    /// Mimic rb.UseGravity but allows the gravity to be scaled
    /// </summary>
    private void ApplyGravity()
    {
        if (rb.velocity.y < 0)
            rb.AddForce(Physics.gravity * rb.mass * gravityScaler);
        else
            rb.AddForce(Physics.gravity * rb.mass);
    }


    /// <summary>
    /// Update velocity while on the ground
    /// Based on current movement collection and horizontal influence try to speed up, slow down or stop
    /// </summary>
    private void UpdateGroundMovement()
    {
        //Turn around
        CheckTurnAround();

        //Apply Movement based on influence
        if (horizontalInfluence != 0)
        {
            //Animation
            PlayMoveAnimation(MovementType.Move);

            //Cap Velocity based on horizontal influence
            float targetVelocity = CurMovementCollection.GetMaxXVelocity() * horizontalInfluence;

            //Update velocity based on horizontal influence
            rb.velocity += Vector3.right * horizontalInfluence * CurMovementCollection.GetXAcceleration() * Time.fixedDeltaTime;

            //Cant exceed target velocity
            if ((horizontalInfluence > 0 && rb.velocity.x > targetVelocity) ||
                (horizontalInfluence < 0 && rb.velocity.x < targetVelocity))
            {
                rb.velocity = new Vector3(targetVelocity, rb.velocity.y);
            }
        }

        //Stopping
        else if (rb.velocity.x != 0)
        {
            //Stop pos movement
            if (rb.velocity.x > 0f)
            {
                rb.velocity -= Vector3.right * CurMovementCollection.GetXDeceleration() * Time.fixedDeltaTime;

                if (rb.velocity.x < 0f)
                {
                    rb.velocity = new Vector3(0, rb.velocity.y, rb.velocity.z);
                    sceneObj.StateHandler.ResetState();
                }
            }

            //Stop neg movement
            else
            {
                rb.velocity += Vector3.right * CurMovementCollection.GetXDeceleration() * Time.fixedDeltaTime;

                if (rb.velocity.x > 0f)
                {
                    rb.velocity = new Vector3(0, rb.velocity.y, rb.velocity.z);
                    sceneObj.StateHandler.ResetState();
                }
            }
        }

        sceneObj.AnimationHandler.SetFloatPerameter("Velocity", Mathf.Abs(rb.velocity.x) / CurMovementCollection.GetMaxXVelocity());             
    }


    /// <summary>
    /// Update velocity while in the air
    /// </summary>
    private void UpdateAirMovement()
    {
        //Apply Movement based on influence
        if (horizontalInfluence != 0)
        {
            //Cap Velocity based on horizontal influence
            float targetVelocity = CurMovementCollection.GetMaxXVelocity() * horizontalInfluence;

            //Update velocity based on horizontal influence
            rb.velocity += Vector3.right * horizontalInfluence * CurMovementCollection.GetXAcceleration() * Time.fixedDeltaTime;

            //Cant exceed target velocity
            if ((horizontalInfluence > 0 && rb.velocity.x > targetVelocity) ||
                (horizontalInfluence < 0 && rb.velocity.x < targetVelocity))
            {
                rb.velocity = new Vector3(targetVelocity, rb.velocity.y);
            }
        }


        //Vertical Movement
        //if (verticalInfluence < 0f)
        //{
            //rb.velocity += transform.up * verticalInfluence * curMoveData.FastFallVelocity * Time.fixedDeltaTime;
        //}
    }


    //TODO: Look at how animations are played. 
    //Move blendtree makes this not work so great...
    //This is the only place that uses the GetCurWeapon(), would like to remove
    /// <summary>
    /// Play move animation based on moveType
    /// </summary>
    /// <param name="moveType"></param>
    private void PlayMoveAnimation(MovementType moveType)
    {
        if (CurMovementCollection.TryGetMovementByType(moveType, out MovementData move))
        {
            string animationName = move.Animation.name;

            //Need to call name for blend tree
            if (move.Type == moveType)
            {
                string userName = gameObject.name;
                string clipName = moveType.ToString();
                string weaponName;

                //TODO: Need to fix with EquipmentHandler
                //if (sceneObj.EquipmentHandler.Weapons.GetCurWeapon() != null)
                //    weaponName = sceneObj.EquipmentHandler.Weapons.GetCurWeapon().name;
                //else
                    weaponName = "Base";

                animationName = userName + weaponName + clipName;
            }           

            sceneObj.AnimationHandler.PlayAnimation(animationName);            
        }
    }
}
