using Game.SceneObjects.ActionStates;
using UnityEngine;

namespace Game.SceneObjects.Damage
{
    public enum HitStunState { None, Start, Launch, Deccelerate, End }


    public class DamageHandler : SceneObjectHandler, ITakeDamage
    {
        [Header("Damage")]
        [SerializeField] protected float damageTaken;

        [Header("Knockback")]
        [SerializeField]private KnockbackCalculator knockbackHandler;

        [Header("HitStun")]
        [SerializeField] private HitStunState hitStunState;
        [SerializeField] private float hitStunTimer;

        [Header("RagDoll")]
        [SerializeField] private GameObject ragdollRoot;
        private Ragdoll ragdoll;

        [Header("Bounce")]
        [SerializeField] private float bounceDegrade = 0.9f;


        //KillZones
        private KillZone[] killZones;

        private Collider collider => GetComponent<Collider>();


        #region Getters

        public HitStunState HitStunState => hitStunState;
        public float HitStunTimer => hitStunTimer;

        #endregion


        #region Initialize

        public override void Setup()
        {
            base.Setup();

            knockbackHandler = new KnockbackCalculator();

            if (ragdollRoot != null)
            {
                ragdoll = ragdollRoot.AddComponent<Ragdoll>();
                ragdoll.Initialize(sceneObject);
            }
        }


        public override void RegisterToEvents()
        { }


        public override void UnregisterToEvents()
        { }

        #endregion


        #region Update

        public void HandleUpdate()
        {
            HitStunStateUpdate();
            RagdollUpdate();
        }

        #endregion


        #region Damage

        /// <summary>
        /// Add an amount of damage based on the provided percent <\br>
        /// </summary>
        /// <param name="percent"></param>
        public void AddDamage(float percent)
        {
            damageTaken += percent;

            sceneObject.UIHandler.UpdateDamageDisplay(damageTaken);
        }


        /// <summary>
        /// Remove an amount of damage based on the provided percent <\br>
        /// Cant drop below 0
        /// </summary>
        /// <param name="percent"></param>
        public void RemoveDamage(float percent)
        {
            damageTaken -= percent;

            if (damageTaken < 0)
                damageTaken = 0;
        }


        /// <summary>
        /// Reset any damage that was previously taken
        /// </summary>
        public void ResetDamage()
        {
            damageTaken = 0;
        }


        /// <summary>
        /// Handle being hit by an attack. <br/>
        /// UI to be displayed on <paramref name="attackPoint"/> <br/>
        /// Damage and Launch force calculated and applied based on <paramref name="influence"/>, <paramref name="attackDamage"/> and <paramref name="launchAngle"/>
        /// </summary>
        public void HitByAttack(float influence, Vector3 attackPoint, float attackDamage, float launchAngle)
        {
            //Damage bubble                    
            UIFactory.Instance.SpawnDamageBubble(attackPoint, attackDamage);

            //Damage
            //AddDamage(attackDamage);

            //Launch knockback
            Vector3 launchForce = 2 * knockbackHandler.CalculateForceKnockBack(influence, damageTaken, launchAngle, sceneObject.Rb);
            ApplyLaunchForce(launchForce);
           
            //HitStun
            ApplyHitStun(launchForce);
        }


        /// <summary>
        /// Apply the stored force
        /// </summary>
        private void ApplyLaunchForce(Vector3 launchForce)
        {
            if (ragdoll != null && ragdoll.enabled)
            {
                //Set mass to coreRigidbody to get similar force effect
                float mass = ragdoll.RB.mass;
                ragdoll.RB.mass = sceneObject.CoreRigidBody.mass;

                ragdoll.RB.AddForce(launchForce, ForceMode.Impulse);

                ragdoll.RB.mass = mass;
            }

            else
                sceneObject.CoreRigidBody.AddForce(launchForce, ForceMode.Impulse);
        }

        #endregion


        #region HitStun

        /// <summary>
        /// Calculate <see cref="hitStunTimer"/> based on the <paramref name="launchForce"/>
        /// </summary>
        private void ApplyHitStun(Vector3 launchForce)
        {            
            if (hitStunState == HitStunState.None)
            {
                sceneObject.ActionStateHandler.ChangeState(ActionState.HitStun);
                hitStunState = HitStunState.Start;
            }
            
            //NOTE: Hitstun based on 80% of time to apex
            hitStunTimer = 0.3f * ((-launchForce.y / sceneObject.CoreRigidBody.mass) / Physics.gravity.y);
            Debug.Log("HitStunTimer: " + hitStunTimer);
        }


        /// <summary>
        /// Updates everything based on the state of hitstun
        /// </summary>
        private void HitStunStateUpdate()
        {
            if (hitStunState != HitStunState.None)
            {
                //Bounce
                //PerdictHitStunBounce();

                if (hitStunState == HitStunState.Start)
                { 
                    //SetUpKillZone();

                    //Ragdoll
                    //if (ragdoll != null)
                    //{
                    //    if (!ragdoll.enabled && sceneObject.CoreRigidBody.linearVelocity.magnitude > 10)
                    //        EnableRagdoll();

                    //    if (hitStunTimer < ragdoll.ExitTransitionTime)
                    //        DisableRagdoll();
                    //} 

                    hitStunState = HitStunState.Launch;
                }

                if (hitStunState == HitStunState.Launch)
                {
                    if (hitStunTimer < 1f)
                        hitStunState = HitStunState.Deccelerate;
                }

                if (hitStunState == HitStunState.Deccelerate)
                {
                    if (hitStunTimer < 0)
                        hitStunState = HitStunState.End;
                }

                if (hitStunState == HitStunState.End)
                {
                    DebugSphereFactory.SpawnDebugSphere(sceneObject.transform.position);

                    DestroyKillZones();
                    hitStunState = HitStunState.None;
                    sceneObject.ActionStateHandler.ChangeState(ActionState.Idle);

                    DebugSphereFactory.SpawnDebugSphere(sceneObject.transform.position);
                }

                hitStunTimer -= Time.deltaTime;
            }
        }


        /// <summary>
        /// Looks ahead to help calculate a bounce
        /// </summary>
        public void PerdictHitStunBounce()
        {
            if (ragdoll != null && ragdoll.enabled)
                ragdoll.CheckRagdollBounce(collider.bounds, bounceDegrade);

            else
                CheckCoreBounce(collider.bounds);
        }


        /// <summary>
        /// Check if a bounce is going to occure. Adjust velocity accordingly
        /// </summary>
        /// <param name="bounds"></param>
        private void CheckCoreBounce(Bounds bounds)
        {
            Vector3 velocity = sceneObject.CoreRigidBody.linearVelocity;
            float distance = velocity.magnitude * Time.fixedDeltaTime;
            Vector3 direction = velocity.normalized;

            if (Physics.BoxCast(bounds.center, bounds.extents, direction, out RaycastHit hit, Quaternion.identity, distance, LayerMask.GetMask("Environment")))
            {
                Vector3 bounceVelocity = Vector3.Reflect(velocity, hit.normal) * bounceDegrade;
                sceneObject.CoreRigidBody.linearVelocity = bounceVelocity;
            }
        }

        #endregion


        #region Ragdoll

        private void EnableRagdoll()
        {
            if (ragdoll != null)
            {
                if (!ragdoll.enabled)
                    ragdoll.enabled = true;
            }
        }


        private void DisableRagdoll()
        {
            if (ragdoll != null)
            {
                if (ragdoll.enabled)
                    ragdoll.enabled = false;
            }
        }


        private void RagdollUpdate()
        {
            if (ragdoll != null)
            {
                if (ragdoll.enabled)
                    RepositionToRagDoll();
            }
        }


        /// <summary>
        /// Position sceneObject to realign with moving ragdoll
        /// </summary>
        private void RepositionToRagDoll()
        {
            Vector3 currentHipPos = ragdollRoot.transform.position;

            //Move transform to ragdoll root
            transform.position = ragdollRoot.transform.position - ragdoll.PosOffset;

            ragdollRoot.transform.position = currentHipPos;
        }

        #endregion


        #region KillZone

        /// <summary>
        /// Spawn killzones
        /// </summary>
        private void SetUpKillZone()
        {
            if (killZones == null)
            {
                killZones = KillZoneFactory.instance.SpawnGhostKillZones(sceneObject.UniqueId);
            }
        }


        /// <summary>
        /// Destroy current killzones
        /// </summary>
        private void DestroyKillZones()
        {
            if (killZones != null)
            {
                foreach (KillZone killZone in killZones)
                {
                    Destroy(killZone.gameObject);
                }
            }

            killZones = null;
        }

        #endregion

        private void OnDestroy()
        {
            DestroyKillZones();
        }
    }
}
