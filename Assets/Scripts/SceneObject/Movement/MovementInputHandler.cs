using System;
using System.Collections.Generic;
using UnityEngine;

public enum GroundedState { Airborn, Grounded, Sliding }

public class MovementInputHandler : MonoBehaviour
{
    const ActionState.State MOVESTATE = ActionState.State.Moving;

    //Movement State Data
    [Header("State")]
    [SerializeField] private MovementType curMoveState = MovementType.Move;
    [SerializeField] private GroundedState curGroundedState;
    [SerializeField] private string curMoveAnimationState;

    //Slope Properties
    [Header("Slope")]
    [SerializeField] private float maxSlopeAngle;
    [SerializeField] private float slidingMaxVelocity;
    [SerializeField] private float slidingAcceleration;

    //Movement Collection
    [Header("Collection")]
    public MovementCollection BaseMovementCollection;
    public MovementCollection CurMovementCollection;

    //Infulence
    private float horizontalInfluence;
    private float verticalInfluence;
    private int numJumpsPerformed;
   
    //SceneObject
    private SceneObject sceneObj => GetComponent<SceneObject>();
    private Rigidbody rb => GetComponent<Rigidbody>();
    private Collider collider => GetComponent<Collider>();


    //Getters
    public GroundedState GroundedState => curGroundedState;
    public float MaxSlopeAngle => maxSlopeAngle;


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

            switch (type)
            {
                case MovementType.Jump:
                case MovementType.AirJump:
                    Debug.Log("Jump Animation Ended");
                    curMoveState = MovementType.FreeFall;
                    sceneObj.StateHandler.ResetState();
                    break;

                case MovementType.Landing:
                    Debug.Log("Landing Animation Ended");
                    curMoveState = MovementType.Move;
                    sceneObj.StateHandler.ResetState();
                    break;
            }
        }
    }

    #endregion


    private void CheckTurnAround()
    {
        if (sceneObj.IsFacingRightDirection() && horizontalInfluence < 0)
        {
            sceneObj.TurnAround();

            if (rb.velocity.x > 0)
            {
                switch (curGroundedState)
                {
                    case GroundedState.Grounded:
                    case GroundedState.Sliding:
                        if (TryGetSlopeAngle(out Vector3 slope))
                        {
                            rb.velocity = slope * rb.velocity.magnitude;
                        }
                        break;

                    case GroundedState.Airborn:
                        rb.velocity = new Vector3(rb.velocity.x * -1, rb.velocity.y, rb.velocity.z);
                        break;
                }
            }
        }

        else if (!sceneObj.IsFacingRightDirection() && horizontalInfluence > 0)
        {
            sceneObj.TurnAround();

            switch (curGroundedState)
            {
                case GroundedState.Grounded:
                case GroundedState.Sliding:
                    if (TryGetSlopeAngle(out Vector3 slope))
                    {
                        rb.velocity = slope * rb.velocity.magnitude;
                    }
                    break;

                case GroundedState.Airborn:
                    rb.velocity = new Vector3(rb.velocity.x * -1, rb.velocity.y, rb.velocity.z);
                    break;
            }
        }
    }


    /// <summary>
    /// Attempt to get the current slope of the environment under the sceneObject
    /// </summary>
    /// <param name="slopeAngle"></param>
    /// <returns></returns>
    private bool TryGetSlopeAngle(out Vector3 slopeAngle)
    {
        if (Physics.SphereCast(collider.bounds.center, collider.bounds.extents.x, Vector3.down, out RaycastHit hit, collider.bounds.extents.y + 0.001f, ~LayerMask.NameToLayer("Environment")))
        {
            slopeAngle = Vector3.Cross(hit.normal, transform.forward).normalized;
            Debug.DrawRay(hit.point, slopeAngle, Color.green);
            return true;
        }


        slopeAngle = Vector3.zero;
        return false;
    }



    #region Perform Movement

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
            horizontalInfluence = Mathf.Clamp(moveInfluence.x, -1, 1);
            verticalInfluence = Mathf.Clamp(moveInfluence.y, -1, 1);                             
        }                           
    }


    //TODO: Add timer which prevents influence from affectting player. Cancled if landing
    //Not jumping up a slope. More dedicated jump

    /// <summary>
    /// Jump action based on input value <br/>
    /// Input value ranges between (0, 1) 
    /// </summary>
    /// <param name="jumpInfluence"></param>
    public void PerformJump(float jumpInfluence)
    {
        if (sceneObj.StateHandler.ChangeState(MOVESTATE))
        {            
            jumpInfluence = Mathf.Clamp01(jumpInfluence);

            switch (curGroundedState)
            {
                case GroundedState.Grounded:
                    if (CurMovementCollection.TryGetMovementByType(MovementType.Jump, out MovementData jump))
                    {                        
                        VerticalJumpAction((JumpData)jump, jumpInfluence);
                    }
                    break;

                case GroundedState.Sliding:
                    if (CurMovementCollection.TryGetMovementByType(MovementType.Jump, out MovementData slideJump))
                    {
                        SlidingJumpAction((JumpData)slideJump, jumpInfluence);
                    }
                    break;

                case GroundedState.Airborn:
                    if (CurMovementCollection.TryGetMovementByType(MovementType.AirJump, out MovementData airJump))
                    {
                        if (numJumpsPerformed < ((AirJumpData)airJump).JumpsAvailable)
                        {
                            AirJumpAction((AirJumpData)airJump, jumpInfluence);
                        }
                    }
                    break;
            }
        }
    }


    /// <summary>
    /// Performs any actions associated with landing
    /// </summary>
    private void PerformLanding()
    {
        curMoveState = MovementType.Landing;

        //Reset jumps
        numJumpsPerformed = 0;

        PlayMoveAnimation(MovementType.Landing);
    }

    #endregion


    #region Movement Actions

    /// <summary>
    /// Velocity applied to rb based on jumpInfluence and current jumpVelocity
    /// </summary>
    /// <param name="jumpData"></param>
    /// <param name="jumpInfluence"></param>
    private void VerticalJumpAction(JumpData jumpData, float jumpInfluence)
    {
        curMoveState = MovementType.Jump;

        rb.velocity = new Vector3(rb.velocity.x, jumpData.JumpVelocity * jumpInfluence, rb.velocity.z);
        numJumpsPerformed++;

        PlayMoveAnimation(jumpData.Type);        
    }


    /// <summary>
    /// Velocity applied based on the normal of the environment the sceneobject is slideing on.<br/>
    /// Velocity is based on the jumpInfluence and current jump velocity.
    /// </summary>
    /// <param name="jumpData"></param>
    /// <param name="jumpInfluence"></param>
    private void SlidingJumpAction(JumpData jumpData, float jumpInfluence)
    {
        if (TryGetSlopeAngle(out Vector3 slopeAngle))
        {
            curMoveState = MovementType.Jump;

            Vector3 normal = Vector3.Cross(slopeAngle, -transform.forward).normalized;
            rb.velocity = normal * (jumpData.JumpVelocity * jumpInfluence);
            numJumpsPerformed++;

            CheckTurnAround();

            PlayMoveAnimation(jumpData.Type);
        }
    }


    private void AirJumpAction(AirJumpData airJumpData, float jumpInfluence)
    {
        curMoveState = MovementType.AirJump;

        rb.velocity = new Vector3(rb.velocity.x, airJumpData.AirJumpVelocity * jumpInfluence, rb.velocity.z);
        numJumpsPerformed++;

        CheckTurnAround();

        PlayMoveAnimation(airJumpData.Type);                   
    }

    #endregion


    private void FixedUpdate()
    {
        //Grounded Status
        CheckGroundedStatus();

        //Gravity Scaler
        if (curGroundedState == GroundedState.Airborn)
            ApplyGravity();

        //Check Movement Action
        switch (curGroundedState)
        {
            case GroundedState.Grounded:
                
                UpdateGroundMovement();                
                break;

            case GroundedState.Sliding:

                UpdateSlidingMovement();
                break;

            case GroundedState.Airborn:
                                
                UpdateAirMovement();                
                break;
        }        
    }
  

    /// <summary>
    /// Mimic rb.UseGravity but allows the gravity to be scaled
    /// </summary>
    private void ApplyGravity()
    {
        if (rb.velocity.y < 0)
            rb.AddForce(Physics.gravity * rb.mass * CurMovementCollection.GetGravityScaler());
        else
            rb.AddForce(Physics.gravity * rb.mass);
    }


    private void CheckGroundedStatus()
    {
        if (TryGetSlopeAngle(out Vector3 slopeAngle))
        {
            float angle = Vector3.Angle(transform.right, slopeAngle);
            
            switch (curGroundedState)
            {
                case GroundedState.Grounded:
                case GroundedState.Sliding:

                    if (angle > maxSlopeAngle)
                        curGroundedState = GroundedState.Sliding;
                    else
                        curGroundedState = GroundedState.Grounded;
                    break;

                case GroundedState.Airborn:

                    if (angle > maxSlopeAngle)
                        curGroundedState = GroundedState.Sliding;
                    else
                        curGroundedState = GroundedState.Grounded;

                    PerformLanding();
                    break;
            }
        }

        else
        {
            curGroundedState = GroundedState.Airborn;
        }
    }

    #region Ground Movement

    private void UpdateGroundMovement()
    {
        if (sceneObj.StateHandler.ChangeState(MOVESTATE) && 
            curMoveState == MovementType.Move)
        {
            CheckTurnAround();

            if (CurMovementCollection.ContainsMovementType(MovementType.Move))
                UpdateGroundAcceleration();
        }

        UpdateGroundDecceleration();        
    }


    /// <summary>
    /// Update velocity while on the ground
    /// Based on current movement collection and horizontal influence try to speed up, slow down or stop
    /// </summary>
    private void UpdateGroundAcceleration()
    {      
        //Ground slope
        if (TryGetSlopeAngle(out Vector3 slope))
        {
            //Apply Movement based on influence
            if (horizontalInfluence != 0)
            {
                //Animation
                PlayMoveAnimation(MovementType.Move);

                //Drag
                rb.drag = 0;

                //Cap Velocity based on horizontal influence
                float targetVelocity = CurMovementCollection.GetMaxXVelocity() * Mathf.Abs(horizontalInfluence);

                //Update velocity based on slope
                rb.velocity += slope * Mathf.Abs(horizontalInfluence) * CurMovementCollection.GetXAcceleration() * Time.fixedDeltaTime;

                //Cant exceed target velocity
                if (rb.velocity.magnitude > targetVelocity)
                {
                    rb.velocity = slope * targetVelocity;
                }

                sceneObj.AnimationHandler.SetFloatPerameter("Velocity", Mathf.Abs(rb.velocity.x) / CurMovementCollection.GetMaxXVelocity());
            }                        
        }
    }


    private void UpdateGroundDecceleration()
    {
        if (horizontalInfluence == 0)
        {
            if (rb.velocity.x != 0)
            {
                Vector3 dragForce = rb.velocity.normalized * CurMovementCollection.GetGroundedXDeceleration();
                rb.velocity -= dragForce * Time.fixedDeltaTime;

                if ((sceneObj.IsFacingRightDirection() && rb.velocity.x <= 0) ||
                    (!sceneObj.IsFacingRightDirection() && rb.velocity.x >= 0))
                {
                    rb.velocity = Vector3.zero;
                    sceneObj.StateHandler.ResetState();
                }

                sceneObj.AnimationHandler.SetFloatPerameter("Velocity", Mathf.Abs(rb.velocity.x) / CurMovementCollection.GetMaxXVelocity());
            }
        }
    }


    private void UpdateSlidingMovement()
    {
        //Ground slope
        if (TryGetSlopeAngle(out Vector3 slope))
        {
            //Reverse slope downward
            if (slope.y > 0)
                slope *= -1;

            rb.velocity += slope * slidingAcceleration;

            if (rb.velocity.y < 0 && rb.velocity.magnitude > slidingMaxVelocity)            
            {
                rb.velocity = slope * slidingMaxVelocity;
            }
        }
    }

    #endregion

    /// <summary>
    /// Update velocity while in the air
    /// </summary>
    private void UpdateAirMovement()
    {
        if (sceneObj.StateHandler.ChangeState(MOVESTATE) &&
            CurMovementCollection.ContainsMovementType(MovementType.Move))
        {
            if (curMoveState == MovementType.FreeFall ||
                curMoveState == MovementType.AirJump)
                UpdateAirAcceleration();
        }


        //Vertical Movement
        //if (verticalInfluence < 0f)
        //{
            //rb.velocity += transform.up * verticalInfluence * curMoveData.FastFallVelocity * Time.fixedDeltaTime;
        //}
    }


    private void UpdateAirAcceleration()
    {
        //Apply Movement based on influence
        if (horizontalInfluence != 0)
        {
            //Cap Velocity based on horizontal influence
            float targetVelocity = CurMovementCollection.GetMaxXVelocity() * horizontalInfluence;

            //Accelerate
            if (sceneObj.IsFacingRightDirection() && horizontalInfluence > 0 ||
                !sceneObj.IsFacingRightDirection() && horizontalInfluence < 0)
            {
                rb.velocity += Vector3.right * horizontalInfluence * CurMovementCollection.GetXAcceleration() * Time.fixedDeltaTime;
            }

            //Deccelerate
            else
                rb.velocity += Vector3.right * horizontalInfluence * CurMovementCollection.GetArialXDeceleration() * Time.fixedDeltaTime;

            //Cant exceed target velocity
            if ((horizontalInfluence > 0 && rb.velocity.x > targetVelocity) ||
                (horizontalInfluence < 0 && rb.velocity.x < targetVelocity))
            {
                rb.velocity = new Vector3(targetVelocity, rb.velocity.y);
            }
        }
    }


    #region Animation

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
            if (move.Type == MovementType.Move)
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

    #endregion
}
