using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.HID;

public enum GroundedState { Airborn, Grounded, Sliding }

public class MovementInputHandler : MonoBehaviour
{
    const ActionState.State MOVESTATE = ActionState.State.Moving;

    //Movement State Data
    [Header("State")]
    [SerializeField] private MovementType curMoveState = MovementType.Move;
    [SerializeField] private string curMoveAnimationState;
    [SerializeField] private GroundedState groundedState;
    public bool IsGrounded = true;

    //Movement Properties
    [Header("Properties")]
    [SerializeField] private float maxSlopeAngle;

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
            horizontalInfluence = Mathf.Clamp(moveInfluence.x, -1, 1);
            verticalInfluence = Mathf.Clamp(moveInfluence.y, -1, 1);                             
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
               JumpAction((JumpData)jump, jumpInfluence);                
            }
        }
        
        else
        {
            if (CurMovementCollection.TryGetMovementByType(MovementType.AirJump, out MovementData airJump))
            {
                AirJumpAction((AirJumpData)airJump, jumpInfluence);                
            }
        }        
    }


    private void JumpAction(JumpData jumpData, float jumpInfluence)
    {
        if (sceneObj.StateHandler.ChangeState(MOVESTATE))
        {
            ChangeMoveState(jumpData.Type);
            groundedState = GroundedState.Airborn;

            rb.velocity = new Vector3(rb.velocity.x, jumpData.JumpVelocity * jumpInfluence, rb.velocity.z);
            numJumpsPerformed++;

            PlayMoveAnimation(jumpData.Type);
        }
    }


    private void AirJumpAction(AirJumpData airJumpData, float jumpInfluence)
    {
        if (sceneObj.StateHandler.ChangeState(MOVESTATE))
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
    }


    /// <summary>
    /// Performs any actions associated with landing
    /// </summary>
    private void PerformLand()
    {       
        curMoveState = MovementType.Move;

        //Reset jumps
        numJumpsPerformed = 0;

        if (horizontalInfluence == 0)
            sceneObj.StateHandler.ResetState();      
    }

    #endregion


    private void CheckTurnAround()
    {
        if (sceneObj.IsFacingRightDirection() && horizontalInfluence < 0) 
        {
            sceneObj.TurnAround();

            if (rb.velocity.x > 0)
                rb.velocity = new Vector3(rb.velocity.x * -1, rb.velocity.y, rb.velocity.z);
        }

        else if(!sceneObj.IsFacingRightDirection() && horizontalInfluence > 0)
        {
            sceneObj.TurnAround();

            if (rb.velocity.x < 0)
                rb.velocity = new Vector3(rb.velocity.x * -1, rb.velocity.y, rb.velocity.z);
        }
    }


    private void FixedUpdate()
    {
        //IsGrounded
        //CheckGroundedStatus();

        //Gravity Scaler
        ApplyGravity();

        //Check for landing
        if (IsGrounded && rb.velocity.y <= 0 && (curMoveState == MovementType.Jump || curMoveState == MovementType.AirJump))
            PerformLand();        

        //Check Action State and Movement data
        if (sceneObj.StateHandler.ChangeState(MOVESTATE) && CurMovementCollection.ContainsMovementType(MovementType.Move))
        {
            if (IsGrounded && (curMoveState == MovementType.Move || curMoveState == MovementType.Land))
            {
                CheckTurnAround();
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
    /// Attempt to get the current slope of the environment 
    /// </summary>
    /// <param name="slopeAngle"></param>
    /// <returns></returns>
    private bool TryGetSlopeAngle(out Vector3 slopeAngle)
    {
        if (groundedState != GroundedState.Airborn)
        {
            if (Physics.SphereCast(collider.bounds.center, collider.bounds.extents.x, Vector3.down, out RaycastHit hit, collider.bounds.extents.y + 0.001f, ~LayerMask.NameToLayer("Environment")))
            {
                slopeAngle = Vector3.Cross(hit.normal, transform.forward).normalized;
                Debug.DrawRay(hit.point, slopeAngle, Color.green);
                return true;
            }
        }

        slopeAngle = Vector3.zero;
        return false;
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


    /// <summary>
    /// Update velocity while on the ground
    /// Based on current movement collection and horizontal influence try to speed up, slow down or stop
    /// </summary>
    private void UpdateGroundMovement()
    {        
       
        
        //Ground slope
        if (TryGetSlopeAngle(out Vector3 slope))
        {

            //Apply Movement based on influence
            if (horizontalInfluence != 0)
            {
                //Animation
                PlayMoveAnimation(MovementType.Move);

                //Cap Velocity based on horizontal influence
                float targetVelocity = CurMovementCollection.GetMaxXVelocity() * Mathf.Abs(horizontalInfluence);

                //Update velocity based on slope
                rb.velocity += slope * Mathf.Abs(horizontalInfluence) * CurMovementCollection.GetXAcceleration() * Time.fixedDeltaTime;

                //Cant exceed target velocity
                if (rb.velocity.magnitude > targetVelocity)
                {
                    rb.velocity = slope * targetVelocity;
                }
            }

            //Stopping
            else if (rb.velocity.magnitude != 0)
            {
                if (rb.velocity.magnitude - (CurMovementCollection.GetGroundedXDeceleration() * Time.fixedDeltaTime) < 0)
                {
                    rb.velocity = Vector3.zero;
                    sceneObj.StateHandler.ResetState();
                }

                else
                {
                    rb.velocity -= slope * CurMovementCollection.GetGroundedXDeceleration() * Time.fixedDeltaTime;
                }
            }

            sceneObj.AnimationHandler.SetFloatPerameter("Velocity", Mathf.Abs(rb.velocity.x) / CurMovementCollection.GetMaxXVelocity());             
        }
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








    private void OnCollisionEnter(Collision collision)
    {
        if (groundedState == GroundedState.Airborn)
        {
            if (collision.gameObject.layer == LayerMask.NameToLayer("Environment"))
            {
                if (Physics.SphereCast(collider.bounds.center, collider.bounds.extents.x, Vector3.down, out RaycastHit hit, collider.bounds.extents.y + 0.001f, ~LayerMask.NameToLayer("Environment")))
                {
                    IsGrounded = true;
                    groundedState = GroundedState.Grounded;
                    Debug.Log(groundedState);
                }
            }
        }
    }



    private List<ContactPoint> environmentalCollisionPoints = new List<ContactPoint>();

    private void OnCollisionStay(Collision collision)
    {        
        //collision.GetContacts(environmentalCollisionPoints);

        //foreach (ContactPoint contact in environmentalCollisionPoints)
        //{
        //    if (Vector3.Angle(contact.normal, Vector3.up) > maxSlopeAngle)
        //        groundedState = GroundedState.Sliding;
        //    else
        //        Debug.Break();
        //}        
    }


    private void OnCollisionExit(Collision collision)
    {
        if (groundedState != GroundedState.Airborn)
        {
            if (!Physics.SphereCast(collider.bounds.center, collider.bounds.extents.x, Vector3.down, out RaycastHit hit, collider.bounds.extents.y + 1f, ~LayerMask.NameToLayer("Environment")))
            {
                groundedState = GroundedState.Airborn;
                Debug.Log(groundedState);
                IsGrounded = false;
            }
        }
    }
}
