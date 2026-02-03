using Game.SceneObjects.ActionStates;
using Game.SceneObjects.Animation;
using Game.SceneObjects.Attack;
using Game.SceneObjects.Collision;
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
    [RequireComponent(typeof(CollisionHandler))]
    public abstract class SceneObject : MonoBehaviour
    {
        [Header("SceneObject")]
        public Guid UniqueId;
        public SceneObjectType ObjectType;

        [Header("Ground Status")]
        [SerializeField] private GroundedState curGroundedState;

        [Header("Climb Status")]
        [SerializeField] private ClimbState curClimbState;

        [Header("Movement Settings")]
        [SerializeField] private SceneObjectData baseData;

        //Logger
        private SceneObjectLogger logger;

        //Required Component Handlers
        public ActionStateHandler ActionStateHandler { get; protected set; }
        public MovementHandler MovementHandler { get; protected set; }
        public UIHandler UIHandler { get; private set; }
        public DamageHandler DamageHandler { get; protected set; }
        public CollisionHandler CollisionHandler { get; protected set; }

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
        public float Mass => Rb.mass + (EquipmentHandler != null ? EquipmentHandler.GetEquipmentMass() : 0);
        public float MassRatio => Mathf.Clamp(Mass, 0, baseData.MaxMass) / baseData.MaxMass;        
        public bool IsFrozen { get; private set; } = false;

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
            if (baseData == null)
                throw new MissingReferenceException("SceneObject Must Contain A SceneObjectData");
            if (!baseData.IsValid())
                throw new ArgumentException("SceneObject Base Data is not valid");

            UniqueId = Guid.NewGuid();
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
            CollisionHandler = GetComponent<CollisionHandler>();

            MovementHandler = new MovementHandler(baseData, this);
        }

        /// <summary>
        /// Initialize all handlers
        /// </summary>
        private void SetupHandlerEvents()
        {
            ActionStateHandler.RegisterToEvents();
            UIHandler.RegisterToEvents();
            DamageHandler.RegisterToEvents();
            CollisionHandler.RegisterToEvents();

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
            CollisionHandler.Setup();

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
            if (Physics.Raycast(CollisionHandler.Collider.bounds.center, Vector3.down, out RaycastHit hit, CollisionHandler.Collider.bounds.extents.y + 0.1f, LayerMask.GetMask("Environment")))
                return true;

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
            Bounds bounds = CollisionHandler.Collider.bounds;
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


        #region Freeze

        public void Freeze()
        {
            MovementHandler.PauseMovement();
            AnimationHandler?.PauseAnimation();
            IsFrozen = true;
        }
        
        public void UnFreeze()
        {
            MovementHandler.ResumeMovement();
            AnimationHandler?.ResumeAnimation();
            IsFrozen = false;
        }

        #endregion

        #region Clean up

        public void OnDestroy()
        {
            ActionStateHandler.UnregisterToEvents();
            UIHandler.UnregisterToEvents();
            DamageHandler.UnregisterToEvents();
            CollisionHandler.UnregisterToEvents();  

            EquipmentHandler?.UnregisterToEvents();
            InteractionHandler?.UnregisterToEvents();

            MovementInputHandler?.UnregisterToEvents();
            AttackInputHandler?.UnregisterToEvents();

            //NOTE: needs to happen after move and attack handlers
            AnimationHandler?.UnregisterToEvents();
        }

        #endregion
    }
}

