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
        [SerializeField] private SceneObjectData baseData;

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
        public float Mass => Rb.mass + EquipmentHandler.GetEquipmentMass();
        public float MassRatio => Mathf.Clamp(Mass, 0, baseData.MaxMass) / baseData.MaxMass;
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
            if (!baseData.IsValid())
                throw new ArgumentException("SceneObject Base Data is not valid");

            UniqueId = Guid.NewGuid().ToString();
            curGroundedState = GroundedState.Grounded;
            curClimbState = ClimbState.Unavailable;

            Rb.mass = baseData.MinMass;
            Rb.linearDamping = 0;

            GetHandlers();
            logger = new SceneObjectLogger(this);
            SetupHandlerEvents(); 
            SetUpHandlers();        
        }


        /// <summary>
        /// Grab all handlers from gameobject
        /// </summary>
        protected virtual void GetHandlers()
        {
            ActionStateHandler = GetComponent<ActionStateHandler>();
            UIHandler = GetComponent<UIHandler>();
            DamageHandler = GetComponent<DamageHandler>();

            if (baseData != null)
                MovementHandler = new MovementHandler(baseData, this);
            else
                throw new MissingReferenceException("SceneObject Must Contain A MovementData");
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

            AnimationHandler?.RegisterToEvents();
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
  
        #endregion


        #region Update

        protected virtual void Update()
        {
            InteractionHandler?.CheckForInteractables();
        }


        protected virtual void FixedUpdate()
        {
            CheckClimbingState();
            CheckGroundedState();

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
        private void CheckGroundedState()
        {            
            switch (curGroundedState)
            {
                case GroundedState.Grounded:
                    UpdateGroundedState();
                    break;

                case GroundedState.Airborn:
                    UpdateAirbornState();
                    break;

                case GroundedState.Climbing:
                    UpdateClimbingState();
                    break;
            }
        }


        private void UpdateGroundedState()
        {
            //Switch to climbing
            if (curClimbState == ClimbState.Climbing)
                //curClimbState == ClimbState.Available &&
                //MovementInputHandler != null && MovementInputHandler.VerticalInfluence > 0)
            {
                curGroundedState = GroundedState.Climbing;
                GroundedStateChangedEvent?.Invoke(curGroundedState);
            }

            //Switch to airborn
            else if (!GroundCheck())
            {
                curGroundedState = GroundedState.Airborn;
                GroundedStateChangedEvent?.Invoke(curGroundedState);
            }
        }

        private void UpdateAirbornState()
        {
            //Switch to climbing
            if (curClimbState == ClimbState.Climbing)
                //curClimbState == ClimbState.Available &&
                //MovementInputHandler != null && MovementInputHandler.VerticalInfluence != 0)
            {
                curGroundedState = GroundedState.Climbing;
                GroundedStateChangedEvent?.Invoke(curGroundedState);
            }

            //Switch to grounded
            else if (GroundCheck())
            {
                curGroundedState = GroundedState.Grounded;
                GroundedStateChangedEvent?.Invoke(curGroundedState);
            }
        }
        
        private void UpdateClimbingState()
        {
            if (curClimbState != ClimbState.Climbing)
            {
                curGroundedState = GroundCheck() ? GroundedState.Grounded : GroundedState.Airborn;
                GroundedStateChangedEvent?.Invoke(curGroundedState);
            }
        }


        /// <summary>
        /// Casts 10 rays based on the left/right most point of the collider to determine if touching the ground
        /// </summary>
        private bool GroundCheck()
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

        #endregion


        #region Climb State

        private void CheckClimbingState()
        {
            //Must have movement input handler
            if (MovementInputHandler == null ||
                MovementInputHandler.CurrentMovementCollection.GetMovementData<ClimbMoveInputData>() == null)
                return;

            switch (curClimbState)
            {
                case ClimbState.Unavailable:
                    UpdateUnavailableClimbState();
                    break;
                case ClimbState.Available:
                    UpdateAvailableClimbState();
                    break;
                case ClimbState.Climbing:
                    UpdateClimbingClimbState();
                    break;
            }
        }

        private void UpdateUnavailableClimbState()
        {
            if (ClimbSurfaceCheck())
                SetClimbState(ClimbState.Available);
        }

        private void UpdateAvailableClimbState()
        {
            if (!ClimbSurfaceCheck())
                SetClimbState(ClimbState.Unavailable);

            else if (MovementInputHandler.VerticalInfluence != 0)
            {
                // NOTE: Need to climb upwards while on the ground
                if (CurGroundedState == GroundedState.Grounded && MovementInputHandler.VerticalInfluence < 0)
                    return;                

                // Attacking
                if (ActionStateHandler.CurActionState == ActionState.Attacking)
                    return;

                SetClimbState(ClimbState.Climbing);
            }
        }

        private void UpdateClimbingClimbState()
        {
            if (!ClimbSurfaceCheck())
                SetClimbState(ClimbState.Unavailable);

            // Hit ground while climbing (NOTE: Dont check based on groundedState since climbState is updated first)
            else if (GroundCheck() && !(MovementInputHandler.VerticalInfluence > 0))
                SetClimbState(ClimbState.Available);

            // Jump while climbing
            else if (MovementInputHandler.CurMovementInputData != null && MovementInputHandler.CurMovementInputData.Type == MovementType.Jump)
                SetClimbState(ClimbState.Available);

            // Attack action performed
            else if (ActionStateHandler.CurActionState == ActionState.Attacking)
                SetClimbState(ClimbState.Available);
        }

        private bool ClimbSurfaceCheck()
        {
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

            return hits.Length > 0;
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
                Rb.useGravity = false;
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

