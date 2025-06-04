using System.Collections.Generic;
using UnityEngine;
using System;
using Game.SceneObjects.Equipment;
using Game.SceneObjects.ActionStates;
using Game.SceneObjects.Movement;
using Game.SceneObjects.Attack;
using Game.SceneObjects.Animation;
using Game.SceneObjects.Damage;
using Game.UI.SceneObject;
using Game.Interactable;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

namespace Game.SceneObjects
{
    public enum SceneObjectType { Player, Enemy, Object }
    public enum GroundedState { Airborn, Grounded }
    public enum ClimbState { Unavailable, Available, Climbing }

    public enum Direction { Right, Left, Up, Down }

    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(EquipmentHandler))]
    [RequireComponent(typeof(MovementInputHandler))]
    [RequireComponent(typeof(AttackInputHandler))]
    [RequireComponent(typeof(ActionStateHandler))]
    [RequireComponent(typeof(AnimationHandler))]
    [RequireComponent(typeof(UIHandler))]
    [RequireComponent(typeof(DamageHandler))]
    public abstract class SceneObject : MonoBehaviour, IInputControl
    {
        [Header("SceneObject")]
        public string UniqueId;
        public SceneObjectType ObjectType;

        [Header("Ground Status")]
        [SerializeField] private GroundedState curGroundedState;

        [Header("Climb Status")]
        [SerializeField] private ClimbState curClimbState;

        //Logger
        private SceneObjectLogger logger;

        //Handlers
        private ActionStateHandler actionStateHandler;
        private EquipmentHandler equipmentHandler;
        private MovementInputHandler movementInputHandler;
        private AttackInputHandler attackInputHandler;
        private AnimationHandler animationHandler;
        private UIHandler uiHandler;
        private DamageHandler damageHandler;

        public InteractionHandler InteractionHandler;

        //Getters
        public SceneObjectLogger Logger => logger;
        public ActionStateHandler ActionStateHandler => actionStateHandler;
        public EquipmentHandler EquipmentHandler => equipmentHandler;
        public MovementInputHandler MovementInputHandler => movementInputHandler;
        public AttackInputHandler AttackInputHandler => attackInputHandler;
        public AnimationHandler AnimationHandler => animationHandler;
        public UIHandler UIHandler => uiHandler;
        public DamageHandler DamageHandler => damageHandler;

    
        public GroundedState CurGroundedState => curGroundedState;
        public ClimbState CurClimbState => curClimbState;
        public Rigidbody Rb => GetComponent<Rigidbody>();
        public Collider Collider => GetComponent<Collider>();


        //Events
        public event Action<GroundedState> GroundedStateChangedEvent;
        public event Action<ClimbState, ClimbState> ClimbStateChangedEvent;


        #region Initialize

        private void Awake()
        {
            try
            {
                Initialize();
            }

            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    

        /// <summary>
        /// Create Unique ID
        /// Setup handlers
        /// </summary>
        protected virtual void Initialize()
        {               
            UniqueId = Guid.NewGuid().ToString();
            curGroundedState = GroundedState.Grounded;
            curClimbState = ClimbState.Unavailable;

            Rb.linearDamping = 0;

            GetHandlers();
            SetUpHandlers();
            SetupHandlerEvents(); 
        
            logger = new SceneObjectLogger(this);
        }


        /// <summary>
        /// Grab all handlers from gameobject
        /// </summary>
        private void GetHandlers()
        {
            InteractionHandler = new InteractionHandler();

            actionStateHandler = GetComponent<ActionStateHandler>();
            animationHandler = GetComponent<AnimationHandler>();
            uiHandler = GetComponent<UIHandler>();
            equipmentHandler = GetComponent<EquipmentHandler>();
            movementInputHandler = GetComponent<MovementInputHandler>();
            attackInputHandler = GetComponent<AttackInputHandler>();
            damageHandler = GetComponent<DamageHandler>();
        }


        /// <summary>
        /// Set up all handlers <br/>
        /// Set up events before performing actions
        /// </summary>
        private void SetUpHandlers()
        {
            actionStateHandler.Setup();    
            equipmentHandler.Setup();
            movementInputHandler.Setup();        
            attackInputHandler.Setup();
            animationHandler.Setup();                
            uiHandler.Setup();
            damageHandler.Setup();
        }


        /// <summary>
        /// Initialize all handlers
        /// </summary>
        private void SetupHandlerEvents()
        {
            actionStateHandler.RegisterToEvents();
            uiHandler.RegisterToEvents();
            equipmentHandler.RegisterToEvents();
            movementInputHandler.RegisterToEvents();
            attackInputHandler.RegisterToEvents();

            //NOTE: needs to happen after move and attack handlers
            animationHandler.RegisterToEvents(); 

            damageHandler.RegisterToEvents();
        }
  
        #endregion


        #region Update

        protected virtual void FixedUpdate()
        {  
            //Grounded Status
            CheckGroundedStatus();

            //Climb State
            UpdateClimbState();

            movementInputHandler.UpdateMovement();
            attackInputHandler.HandleUpdate();

            DamageHandler.HandleUpdate();
        }

        #endregion


        #region Ground State

        /// <summary>
        /// Use raycasts to determine current status of the ground
        /// </summary>
        private void CheckGroundedStatus()
        {
            if (IsGrounded())
            {
                if (curGroundedState != GroundedState.Grounded)
                {
                    curGroundedState = GroundedState.Grounded;
                    GroundedStateChangedEvent?.Invoke(curGroundedState);
                }
            }

            else
            {
                if (curGroundedState != GroundedState.Airborn)
                {
                    curGroundedState = GroundedState.Airborn;
                    GroundedStateChangedEvent?.Invoke(curGroundedState);
                }
            }
        }


        /// <summary>
        /// Casts 10 rays based on the left/right most point of the collider to determine if touching the ground
        /// </summary>
        public bool IsGrounded()
        {            
            if (Rb.linearVelocity.y > 0.001f) //Use of epsilon to prevent false positive due to floating point persision errors
                return false;

            List<RaycastHit> hits = new List<RaycastHit>();

            //Create raycasts
            Vector3 leftSidePoint = Collider.bounds.center + Vector3.left * Collider.bounds.extents.x;
            Vector3 rightSidePoint = Collider.bounds.center + Vector3.right * Collider.bounds.extents.x;
            float spaceBetweenRays = (rightSidePoint.x - leftSidePoint.x) / 10;

            //Raycast
            for (int i = 0; i < 10; i++)
            {
                Vector3 origin = leftSidePoint + Vector3.right * spaceBetweenRays * i;

                if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, Collider.bounds.extents.y + 0.1f, LayerMask.GetMask("Environment")))
                {
                    if (hit.transform.TryGetComponent(out TwoWayPlatform platform))
                    {
                        if (!platform.IsSceneObjectCollisionIgnored(this))
                            hits.Add(hit);
                    }
                    else
                        hits.Add(hit);
                }
            }

            if (hits.Count > 0)
            {
                //Average normals
                Vector3 avgNormal = Vector3.zero;

                foreach (RaycastHit hit in hits)
                    avgNormal += hit.normal;

                avgNormal /= 10;

                return true;
            }

            return false;
        }

        #endregion


        #region Climb State

        /// <summary>
        /// Update the climb state of the sceneObject
        /// </summary>
        private void UpdateClimbState()
        {
            //Update climb state if the collection has climb data                
            if (movementInputHandler.TryGetCurrentMovementCollection(out MovementCollection curMovementCollection) && curMovementCollection.ClimbData != null)
            {
                Bounds bounds = Collider.bounds;
                Vector3 center = bounds.center;
                Vector3 halfExtents = bounds.extents;

                Collider[] hits = Physics.OverlapBox(
                    center,
                    halfExtents,
                    Quaternion.identity,
                    LayerMask.GetMask("Climbable"),
                    QueryTriggerInteraction.Collide
                );

                if (CurClimbState == ClimbState.Unavailable)
                {
                    if (hits.Length > 0)
                        SetClimbState(ClimbState.Available);
                }

                if (CurClimbState == ClimbState.Available)
                {
                    if (hits.Length == 0)
                        SetClimbState(ClimbState.Unavailable);

                    else if (movementInputHandler.VerticalInfluence != 0)
                    {
                        // NOTE: Need to climb upwards while on the ground
                        if (CurGroundedState == GroundedState.Grounded && movementInputHandler.VerticalInfluence < 0)
                            return;

                        // Moving upwards
                        if (ActionStateHandler.CurActionState == ActionState.Moving && Rb.linearVelocity.y > 0)
                            return;

                        // Attacking
                        if (ActionStateHandler.CurActionState == ActionState.Attacking)
                            return;
                                                    
                        SetClimbState(ClimbState.Climbing);
                    }
                }

                if (CurClimbState == ClimbState.Climbing)
                {
                    if (hits.Length == 0)
                        SetClimbState(ClimbState.Unavailable);

                    // Hit ground while climbing
                    else if (movementInputHandler.VerticalInfluence < 0 && CurGroundedState == GroundedState.Grounded)
                        SetClimbState(ClimbState.Available);

                    // Jump action performed
                    else if (ActionStateHandler.CurActionState == ActionState.Moving)
                    {
                        if ((movementInputHandler.CurMoveState == MovementType.Jump || movementInputHandler.CurMoveState == MovementType.AirJump) && Rb.linearVelocity.y > 0)
                            SetClimbState(ClimbState.Available);
                    }

                    // Attack action performed
                    else if (ActionStateHandler.CurActionState == ActionState.Attacking)
                        SetClimbState(ClimbState.Available);
                }
            }
        }


        /// <summary>
        /// Set the climb state of the sceneObject to <paramref name="state"/>
        /// </summary>
        private void SetClimbState(ClimbState state)
        {
            if (curClimbState == state)
                return;

            ClimbState prevClimbState = curClimbState;
            curClimbState = state;

            if (curClimbState == ClimbState.Climbing)
            {
                Rb.linearVelocity = Vector3.zero;
                Rb.useGravity = false;
            }

            else
                Rb.useGravity = true;                    

            ClimbStateChangedEvent?.Invoke(prevClimbState, curClimbState);                                        
        }

        #endregion


        #region RigidBody

        /// <summary>
        /// Returns a list of active rigidbody where the associated collider is not trigger
        /// </summary>
        public List<Rigidbody> ActiveRigidbodies 
        { 
            get 
            {
                List<Rigidbody> activeRbs = new List<Rigidbody>();

                foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>())
                {
                    if (rb.TryGetComponent(out Collider collider))
                    {
                        if (collider.enabled == true && collider.isTrigger == false)
                            activeRbs.Add(rb);
                    }
                }

                return activeRbs;
            } 
        }

        #endregion


        #region Direction

        /// <returns>
        /// Whether the sceneObject is facing the right direction
        /// </returns>
        public bool IsFacingRightDirection
        {
            get
            {
                float angleRightDiff = Vector3.Angle(transform.right, Vector3.right);
                float angleLeftDiff = Vector3.Angle(transform.right, Vector3.left);

                if (angleRightDiff < angleLeftDiff)
                {
                    return true;
                }

                return false;
            }
        }


        /// <summary>
        /// Turn the sceneObject around 
        /// </summary>
        public void TurnAround()
        {
            if (IsFacingRightDirection)
                transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            else
                transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            
            UIHandler.RotateDisplayText();
        }
      
        #endregion


        #region Collision Detection

        /// <summary>
        /// Try to detect if a collider exists
        /// </summary>
        public bool TryDetectCollision(Direction direction, float dist, LayerMask mask, out Collider collidingCollider)
        {        
            collidingCollider = null;

            switch (direction) 
            { 
                case Direction.Left:
                    collidingCollider = LeftSideCollisionDetection(dist, mask);
                    break;

                case Direction.Right:
                    collidingCollider = RightSideCollisionDetection(dist, mask);
                    break;

                case Direction.Up:
                    collidingCollider = UpSideCollisionDetection(dist, mask); 
                    break;

                case Direction.Down:
                    collidingCollider= DownSideCollisionDetection(dist, mask);
                    break;        
            }

            return collidingCollider != null;
        }  


        /// <summary>
        /// Check right side for any collisions
        /// </summary>
        private Collider RightSideCollisionDetection(float dist, LayerMask mask)
        {
            Vector3 point1 = Collider.bounds.center + Vector3.up * Collider.bounds.extents.y;
            Vector3 point2 = Collider.bounds.center + Vector3.down * Collider.bounds.extents.y;
            float spaceBetweenRays = (point1.y - point2.y) / 10;

            for (int i = 0; i < 10; i++)
            {
                Vector3 origin = point1 + Vector3.down * spaceBetweenRays * i;

                if (Physics.Raycast(origin, Vector3.right, out RaycastHit hit, Collider.bounds.extents.x + dist, mask))
                {
                    return hit.collider;
                }
            }

            return null;
        }


        /// <summary>
        /// Check left side for any collisions
        /// </summary>
        private Collider LeftSideCollisionDetection(float dist, LayerMask mask)
        {
            Vector3 point1 = Collider.bounds.center + Vector3.up * Collider.bounds.extents.y;
            Vector3 point2 = Collider.bounds.center + Vector3.down * Collider.bounds.extents.y;
            float spaceBetweenRays = (point1.y - point2.y) / 10;

            for (int i = 0; i < 10; i++)
            {
                Vector3 origin = point1 + Vector3.down * spaceBetweenRays * i;

                if (Physics.Raycast(origin, Vector3.left, out RaycastHit hit, Collider.bounds.extents.x + dist, mask))
                {
                    return hit.collider;
                }
            }

            return null;
        }


        /// <summary>
        /// Check up for any collisions
        /// </summary>
        private Collider UpSideCollisionDetection(float dist, LayerMask mask)
        {
            Vector3 point1 = Collider.bounds.center + Vector3.right * Collider.bounds.extents.x;
            Vector3 point2 = Collider.bounds.center + Vector3.left * Collider.bounds.extents.x;
            float spaceBetweenRays = (point1.x - point2.x) / 10;

            for (int i = 0; i < 10; i++)
            {
                Vector3 origin = point1 + Vector3.left * spaceBetweenRays * i;

                if (Physics.Raycast(origin, Vector3.up, out RaycastHit hit, Collider.bounds.extents.y + dist, mask))
                {
                    return hit.collider;
                }
            }

            return null;
        }


        /// <summary>
        /// Check down for any collisions
        /// </summary>
        private Collider DownSideCollisionDetection(float dist, LayerMask mask)
        {
            Vector3 point1 = Collider.bounds.center + Vector3.right * Collider.bounds.extents.x;
            Vector3 point2 = Collider.bounds.center + Vector3.left * Collider.bounds.extents.x;
            float spaceBetweenRays = (point1.x - point2.x) / 10;

            for (int i = 0; i < 10; i++)
            {
                Vector3 origin = point1 + Vector3.left * spaceBetweenRays * i;

                if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, Collider.bounds.extents.y + dist, mask))
                {
                    return hit.collider;
                }
            }

            return null;
        }

        #endregion


        #region Inputs

        /// <inheritdoc/>
        public abstract void PerformMovement(Vector2 movement);

        /// <inheritdoc/>
        public abstract void StopHorizontalMovement();

        /// <inheritdoc/>
        public abstract void PerformVerticalJump(float jumpStrength);

        /// <inheritdoc/>
        public abstract void StopJumpMovement();

        /// <inheritdoc/>
        public abstract bool IsHorizontalMovementActive();

        /// <inheritdoc/>
        public abstract bool IsVerticalJumpActive();

        /// <inheritdoc/>
        public abstract void PerformUpAttack();

        /// <inheritdoc/>
        public abstract void PerformDownAttack();

        /// <inheritdoc/>
        public abstract void PerformLeftAttack();

        /// <inheritdoc/>
        public abstract void PerformRightAttack();

        /// <inheritdoc/>
        public abstract bool IsUpAttackActive();

        /// <inheritdoc/>
        public abstract bool IsDownAttackActive();

        /// <inheritdoc/>
        public abstract bool IsLeftAttackActive();

        /// <inheritdoc/>
        public abstract bool IsRightAttackActive();

        /// <inheritdoc/>
        public abstract void PerformInteraction();

        /// <inheritdoc/>
        public abstract bool IsInteractionActive();

        #endregion


        #region Clean up

        public void OnDestroy()
        {
            actionStateHandler.UnregisterToEvents();
            uiHandler.UnregisterToEvents();
            equipmentHandler.UnregisterToEvents();
            movementInputHandler.UnregisterToEvents();
            attackInputHandler.UnregisterToEvents();

            //NOTE: needs to happen after move and attack handlers
            animationHandler.UnregisterToEvents();

            damageHandler.UnregisterToEvents();
        }      

        #endregion
    }
}

