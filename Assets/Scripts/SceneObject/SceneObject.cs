using Game.SceneObjects.ActionStates;
using Game.SceneObjects.Animation;
using Game.SceneObjects.Attack;
using Game.SceneObjects.Damage;
using Game.SceneObjects.Equipment;
using Game.SceneObjects.Movement;
using Game.UI.SceneObject;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.SceneObjects
{
    public enum SceneObjectType { Player, Enemy, Object }
    public enum GroundedState { Airborn, Grounded, Climbing }
    public enum ClimbState { Unavailable, Available, Climbing }

    public enum Direction { Right, Left, Up, Down }

    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(ActionStateHandler))]
    [RequireComponent(typeof(UIHandler))]
    [RequireComponent(typeof(DamageHandler))]
    public abstract class SceneObject : MonoBehaviour
    {
        [Header("SceneObject")]
        public string UniqueId;
        public SceneObjectType ObjectType;

        [Header("Ground Status")]
        [SerializeField] private GroundedState curGroundedState;

        [Header("Climb Status")]
        [SerializeField] private ClimbState curClimbState;

        [Header("Movement Settings")]
        [SerializeField] private MovementData MovementData;

        //Logger
        private SceneObjectLogger logger;

        //Required Handlers
        public ActionStateHandler ActionStateHandler { get; protected set; }
        public MovementHandler MovementHandler { get; protected set; }
        public UIHandler UIHandler { get; private set; }
        public DamageHandler DamageHandler { get; protected set; }

        //Other Handlers
        public EquipmentHandler EquipmentHandler { get; protected set; }
        public InteractionHandler InteractionHandler { get; protected set; }
        public MovementInputHandler MovementInputHandler { get; protected set; }
        public AttackInputHandler AttackInputHandler { get; protected set; }
        public AnimationHandler AnimationHandler { get; protected set; }

        //Getters
        public SceneObjectLogger Logger => logger;    
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
        protected virtual void GetHandlers()
        {
            ActionStateHandler = GetComponent<ActionStateHandler>();                       
            UIHandler = GetComponent<UIHandler>();            
            DamageHandler = GetComponent<DamageHandler>();

            if (MovementData != null)
                MovementHandler = new MovementHandler(MovementData, this);
            else
                throw new MissingReferenceException("SceneObject Must Contain A MovementData");
        }

        /// <summary>
        /// Set up all handlers <br/>
        /// Set up events before performing actions
        /// </summary>
        private void SetUpHandlers()
        {
            ActionStateHandler.Setup();
            MovementHandler.Setup();
            UIHandler.Setup();
            DamageHandler.Setup();

            EquipmentHandler?.Setup();
            InteractionHandler?.Setup();
            MovementInputHandler?.Setup();
            AttackInputHandler?.Setup();
            AnimationHandler?.Setup();
        }

        /// <summary>
        /// Initialize all handlers
        /// </summary>
        private void SetupHandlerEvents()
        {
            ActionStateHandler.RegisterToEvents();
            UIHandler.RegisterToEvents();
            DamageHandler.RegisterToEvents();
                        
            EquipmentHandler?.RegisterToEvents();
            InteractionHandler?.RegisterToEvents();
            
            MovementInputHandler?.RegisterToEvents();
            AttackInputHandler?.RegisterToEvents();

            //NOTE: needs to happen after move and attack handlers
            AnimationHandler?.RegisterToEvents();
        }
  
        #endregion


        #region Update

        protected virtual void Update()
        {
            InteractionHandler?.CheckForInteractables();
        }


        protected virtual void FixedUpdate()
        {  
            //Grounded Status
            CheckGroundedStatus();

            MovementHandler.UpdateMovement();            

            if (AttackInputHandler != null)
                AttackInputHandler.HandleUpdate();

            DamageHandler.HandleUpdate();
        }

        #endregion


        #region Ground State

        /// <summary>
        /// Use raycasts to determine current status of the ground
        /// </summary>
        private void CheckGroundedStatus()
        {
            if (IsClimbing())
            {
                if (curGroundedState != GroundedState.Climbing)
                {
                    curGroundedState = GroundedState.Climbing;
                    GroundedStateChangedEvent?.Invoke(curGroundedState);
                }
            }

            else if (IsGrounded())
            {
                //Reset vertical velocity if the sceneObject is grounded
                //Check for hitstun was to resolve an issue where the where the damage velocity in the y direction would be set to 0
                if (ActionStateHandler.CurActionState != ActionState.HitStun)
                    Rb.linearVelocity = new Vector3(Rb.linearVelocity.x, 0f, Rb.linearVelocity.z);
                
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
        private bool IsGrounded()
        {            
            if (Physics.Raycast(Collider.bounds.center, Vector3.down, out RaycastHit hit, Collider.bounds.extents.y + 0.1f, LayerMask.GetMask("Environment")))
            {
                if (hit.transform.TryGetComponent(out TwoWayPlatform platform) && !platform.IsSceneObjectCollisionIgnored(this))
                    return true;
                else
                    return true;
            }

            return false;
        }


        private bool IsClimbing()
        {
            UpdateClimbState();
            return curClimbState == ClimbState.Climbing;
        }

        #endregion


        #region Climb State

        /// <summary>
        /// Update the climb state of the sceneObject
        /// </summary>
        private void UpdateClimbState()
        {
            if (MovementInputHandler == null || MovementInputHandler.CurrentMovementCollection.ClimbData == null) return;

            //Update climb state if the collection has climb data                            
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

                else if (MovementInputHandler.VerticalInfluence != 0)
                {
                    // NOTE: Need to climb upwards while on the ground
                    if (CurGroundedState == GroundedState.Grounded && MovementInputHandler.VerticalInfluence < 0)
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
                else if (MovementInputHandler.VerticalInfluence < 0 && CurGroundedState == GroundedState.Grounded)
                    SetClimbState(ClimbState.Available);

                // Jump action performed
                else if (ActionStateHandler.CurActionState == ActionState.Moving)
                {
                    if ((MovementInputHandler.CurMoveInputState == MovementType.Jump || MovementInputHandler.CurMoveInputState == MovementType.AirJump) && Rb.linearVelocity.y > 0)
                        SetClimbState(ClimbState.Available);
                }

                // Attack action performed
                else if (ActionStateHandler.CurActionState == ActionState.Attacking)
                    SetClimbState(ClimbState.Available);
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


        #region Clean up

        public void OnDestroy()
        {
            ActionStateHandler.UnregisterToEvents();
            UIHandler.UnregisterToEvents();
            DamageHandler.UnregisterToEvents();

            EquipmentHandler?.UnregisterToEvents();
            InteractionHandler?.UnregisterToEvents();

            MovementInputHandler?.UnregisterToEvents();
            AttackInputHandler?.UnregisterToEvents();

            //NOTE: needs to happen after move and attack handlers
            AnimationHandler?.UnregisterToEvents();
        }

        #endregion


        #region Debug

        private void OnDrawGizmosSelected()
        {
            DrawGroundedCheck();
        }


        private void DrawGroundedCheck()
        {
            if (Physics.Raycast(Collider.bounds.center, Vector3.down, out RaycastHit hit, Collider.bounds.extents.y + 0.1f, LayerMask.GetMask("Environment")))
            {
                if (hit.transform.TryGetComponent(out TwoWayPlatform platform) && !platform.IsSceneObjectCollisionIgnored(this))
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(Collider.bounds.center, hit.point);
                }
                else
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(Collider.bounds.center, hit.point);
                }
            }

            else
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(Collider.bounds.center, new Vector3(Collider.bounds.center.x, Collider.bounds.extents.y + 0.1f, Collider.bounds.center.z));
            }
        }

        #endregion
    }
}

