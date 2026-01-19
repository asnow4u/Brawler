using Game.SceneObjects.ActionStates;
using System.Collections.Generic;
using UnityEngine;

namespace Game.SceneObjects.Damage
{
    public enum HitStunState { None, Start, Launch, End }


    public class DamageHandler : SceneObjectHandler, ITakeDamage
    {
        [Header("Damage")]
        [SerializeField] protected float damageTaken;        
        [Tooltip("Base amount of acceleration that will be applied anytime taking a hit")]
        const float minKnockBackAcceleration = 10f;        
        [Tooltip("The exponential growth of knockback based on damage")]
        const float exGrowth = 2.8f;

        [Header("HitStun")]
        [SerializeField] private HitStunState hitStunState;
        [SerializeField] private float hitStunTimer;

        [Header("Bounce")]
        [SerializeField] private float bounceDegrade = 0.9f;
        private const float bounceCheckOffset = 0.1f; //Distance to offset raycast to avoid pre collision detection
        private const float groundBounceVelocityThreshold = 20f;

        [Header("RagDoll")]
        [SerializeField] private GameObject ragdollRoot;
        private Ragdoll ragdoll;

        //Immunity
        private Dictionary<string, float> immunityList = new Dictionary<string, float>();

        //KillZones
        private KillZone[] killZones;

        private Collider collider => GetComponent<Collider>();


        #region Getters

        public HitStunState HitStunState => hitStunState;
        public float HitStunTimer => hitStunTimer;

        public SceneObject SceneObject => sceneObject;

        #endregion


        #region Initialize

        public override void Setup()
        {
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
            UpdateImmunityList();
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
        public void HitByAttack(Vector3 attackPoint, float influence, float attackDamage, float launchAngle)
        {
            //Damage bubble                    
            UIFactory.Instance.SpawnDamageBubble(attackPoint, attackDamage);

            //Damage
            AddDamage(attackDamage);

            //Launch knockback
            Vector3 launchVelocity = CalculateKnockbackVelocity(influence, damageTaken, launchAngle, sceneObject.Mass);
            
            ApplyLaunchForce(launchVelocity);
            CalculateBounce(launchVelocity);

            //HitStun
            ApplyHitStun(launchVelocity);

            SetUpKillZone();
        }


        /// <summary>
        /// Calculate launch velocity
        /// </summary>
        public Vector3 CalculateKnockbackVelocity(float influence, float totalDamage, float launchAngle, float mass)
        {
            float minForce = mass * minKnockBackAcceleration;
            float damageForce = minForce + influence * (Mathf.Pow(totalDamage, exGrowth) / mass);

            float xLaunch = Mathf.Cos(launchAngle * Mathf.Deg2Rad);
            float yLaunch = Mathf.Sin(launchAngle * Mathf.Deg2Rad);
            Vector3 launchDirection = new Vector2(xLaunch, yLaunch);

            //TODO: bounce
            
            return launchDirection * damageForce / mass;
        }


        /// <summary>
        /// Apply the calculated <paramref name="launchForce"/> to the sceneObject
        /// </summary>
        private void ApplyLaunchForce(Vector3 launchVelocity)
        {
            //if (ragdoll != null && ragdoll.enabled)
            //{
            //    //Set mass to coreRigidbody to get similar force effect
            //    float mass = ragdoll.RB.mass;
            //    ragdoll.RB.mass = sceneObject.CoreRigidBody.mass;

            //    ragdoll.RB.AddForce(launchForce, ForceMode.Impulse);

            //    ragdoll.RB.mass = mass;
            //}

            //else
            sceneObject.Rb.linearVelocity = launchVelocity; 
        }

        #endregion


        #region Immunity

        public bool CheckForImmunity(SceneObject attacker)
        {
            return immunityList.ContainsKey(attacker.UniqueId);
        }

        public void SetImmunity(string sceneObjectID, float duration)
        {
            if (!immunityList.ContainsKey(sceneObjectID))
                immunityList.Add(sceneObjectID, duration);
        }

        public void UpdateImmunityList()
        {
            foreach (var kvp in new Dictionary<string, float>(immunityList))
            {
                immunityList[kvp.Key] -= Time.fixedDeltaTime;
                if (immunityList[kvp.Key] <= 0)
                    immunityList.Remove(kvp.Key);
            }
        }

        #endregion


        #region Bounce

        private void CalculateBounce(Vector3 velocity)
        {            
            Bounds bounds = collider.bounds;

            //Calculate the direction and distance of the velocity
            Vector3 direction = velocity.normalized;
            float distance = velocity.magnitude * Time.fixedDeltaTime;

            //Get bound points for bounce check
            List<Vector3> boundPoints = new List<Vector3>();
            float centralZ = (bounds.max.z + bounds.min.z) / 2;

            //X Direction
            if (direction.x > 0)
            {
                boundPoints.Add(new Vector3(bounds.max.x, bounds.max.y, centralZ)); // Top-right
                boundPoints.Add(new Vector3(bounds.max.x, bounds.center.y, centralZ));  // Right-center
                boundPoints.Add(new Vector3(bounds.max.x, bounds.min.y, centralZ)); // Bottom-right
            }
            else if (direction.x < 0)
            {
                boundPoints.Add(new Vector3(bounds.min.x, bounds.max.y, centralZ)); // Top-left
                boundPoints.Add(new Vector3(bounds.min.x, bounds.center.y, centralZ));  // Left-center
                boundPoints.Add(new Vector3(bounds.min.x, bounds.min.y, centralZ)); // Bottom-left
            }

            //Y Direction
            if (direction.y > 0)
            {
                boundPoints.Add(new Vector3(bounds.min.x, bounds.max.y, centralZ)); // Top-left
                boundPoints.Add(new Vector3(bounds.center.x, bounds.max.y, centralZ)); // Top-center
                boundPoints.Add(new Vector3(bounds.max.x, bounds.max.y, centralZ)); // Top-right
            }
            else if (direction.y < 0)
            {
                boundPoints.Add(new Vector3(bounds.min.x, bounds.min.y, centralZ)); // Bottom-left
                boundPoints.Add(new Vector3(bounds.center.x, bounds.min.y, centralZ)); // Bottom-center
                boundPoints.Add(new Vector3(bounds.max.x, bounds.min.y, centralZ)); // Bottom-right
            }

            if (boundPoints.Count == 0)
                return;

            //Environment check
            List<Vector3> hitNormals = new List<Vector3>();
            foreach (var point in boundPoints)
            {
                Vector3 offset = -direction * bounceCheckOffset;

                if (Physics.Raycast(point + offset, direction, out RaycastHit hit, distance + bounceCheckOffset, LayerMask.GetMask("Environment")))
                    hitNormals.Add(hit.normal);
            }
            
            if (hitNormals.Count == 0)
                return;

            //Calculate bounce velocity
            Vector3 averageNormal = Vector3.zero;
            foreach (var normal in hitNormals)
                averageNormal += normal;
            averageNormal /= hitNormals.Count;
            
            Vector3 bounceVelocity = Vector3.Reflect(velocity, averageNormal) * bounceDegrade;

            //Prevent small bounces on ground
            if (bounceVelocity.magnitude < groundBounceVelocityThreshold &&
                velocity.y < 0 && bounceVelocity.y > 0)
            {
                return;
            }

            sceneObject.Rb.linearVelocity = bounceVelocity;
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

            hitStunTimer = Mathf.Abs(launchForce.y / (Physics.gravity.y - sceneObject.MovementHandler.AerialXDeccelerationRate));
        }


        /// <summary>
        /// Updates everything based on the state of hitstun
        /// </summary>
        private void HitStunStateUpdate()
        {
            if (hitStunState != HitStunState.None)
            {
                hitStunTimer -= Time.fixedDeltaTime;

                //Bounce
                CalculateBounce(sceneObject.Rb.linearVelocity);

                if (hitStunState == HitStunState.Start)
                {                    
                    //Ragdoll
                    //if (ragdoll != null)
                    //{
                    //    if (!ragdoll.enabled && sceneObject.CoreRigidBody.velocity.magnitude > 10)
                    //        EnableRagdoll();

                    //    if (hitStunTimer < ragdoll.ExitTransitionTime)
                    //        DisableRagdoll();
                    //} 

                    hitStunState = HitStunState.Launch;
                }

                if (hitStunState == HitStunState.Launch)
                {
                    if (hitStunTimer < 0)
                        hitStunState = HitStunState.End;
                }


                if (hitStunState == HitStunState.End)
                {
                    RemoveKillZones();
                    hitStunState = HitStunState.None;
                    sceneObject.ActionStateHandler.ChangeState(ActionState.Idle);
                }               
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
        /// Spawn killzones for this sceneObject
        /// </summary>
        private void SetUpKillZone()
        {
            if (killZones == null)
                killZones = KillZoneFactory.instance.SpawnKillZones(sceneObject.transform);
        }


        /// <summary>
        /// Destroy current killzones
        /// </summary>
        private void RemoveKillZones()
        {
            if (killZones != null)
            {
                foreach (KillZone killZone in killZones)
                    Destroy(killZone.gameObject);

               killZones = null;
            }
        }

        #endregion

        private void OnDestroy()
        {
            RemoveKillZones();
        }


        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            Bounds bounds = collider.bounds;
            float centralZ = (bounds.max.z + bounds.min.z) / 2;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(bounds.center, bounds.size);

            //Corners
            Vector3 topRightPoint = new Vector3(bounds.max.x, bounds.max.y, centralZ);
            Vector3 topLeftPoint = new Vector3(bounds.min.x, bounds.max.y, centralZ);
            Vector3 bottomRightPoint = new Vector3(bounds.max.x, bounds.min.y, centralZ);
            Vector3 bottomLeftPoint = new Vector3(bounds.min.x, bounds.min.y, centralZ);

            float xSpacing = (bounds.max.x - bounds.min.x) / 4;
            float ySpacing = (bounds.max.y - bounds.min.y) / 4;
            Vector3 point;
            
            //Sides
            for (int i=0; i<5; i++)
            {
                //Top
                point = new Vector3(bounds.min.x + i * xSpacing, bounds.max.y, centralZ);
                Gizmos.DrawSphere(point, 0.05f);

                //Bottom
                point = new Vector3(bounds.min.x + i * xSpacing, bounds.min.y, centralZ);
                Gizmos.DrawSphere(point, 0.05f);

                //Right
                point = new Vector3(bounds.max.x, bounds.min.y + i * ySpacing, centralZ);
                Gizmos.DrawSphere(point, 0.05f);

                //Left
                point = new Vector3(bounds.min.x, bounds.min.y + i * ySpacing, centralZ);
                Gizmos.DrawSphere(point, 0.05f);

            }
        }      

        #endregion
    }
}
