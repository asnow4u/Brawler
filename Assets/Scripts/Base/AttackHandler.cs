using SceneObj.Animation;
using SceneObj.Router;
using UnityEngine;

namespace SceneObj.Attack
{
    public class AttackHandler : MonoBehaviour
    {
        private ActionRouter router;
        private AttackCollection curAttackCollection; //TODO: Later fetched from equipment handler to grab weapon

        public void SetUpHandler(ActionRouter actionRouter)
        {
            router = actionRouter;
            SetUpAttacks(transform.GetChild(0).gameObject);
        }

        public void SetUpAttacks(GameObject weapon)
        {
            SetAttackCollection(weapon.GetComponent<AttackCollection>());
            SetAnimationAttackEvents(weapon.GetComponent<AttackAnimationEventListener>());
        }

        private void SetAttackCollection(AttackCollection collection)
        {
            curAttackCollection = collection;
        }

        private void SetAnimationAttackEvents(AttackAnimationEventListener listener)
        {
            listener.AttackAnimationStarted += OnAttackAnimationStarted;
            listener.AttackAnimationEnded += OnAttackAnimationEnded;
            listener.AttackAnimationCollidersEnabled += OnAttackCollidersEnabled;
            listener.AttackAnimationCollidersDisabled += OnAttackCollidersDisabled;
        }

        #region Attack Performed Events

        private void OnAttackAnimationStarted(AnimationClip clip)
        {
            
        }

        private void OnAttackAnimationEnded(AnimationClip clip)
        {            

        }

        private void OnAttackCollidersEnabled(AnimationClip clip)
        {
            if (curAttackCollection.GetAttackByClip(clip, out AttackCollection.Attack attack))
            {
                attack.EnableColliders();
            }
        }

        private void OnAttackCollidersDisabled(AnimationClip clip)
        {
            if (curAttackCollection.GetAttackByClip(clip, out AttackCollection.Attack attack))
            {
                attack.DisableColliders();
            }
        }

        #endregion

        #region Perform Attack

        private string GetAttackFrom(AttackType.Type attackType)
        {
            if (curAttackCollection.GetAttackByType(attackType, out AttackCollection.Attack attack))
            {
                return attack.animationClip.name;            
            }

            return null;
        }

        public string PerformUpTiltAttack()
        {            
            return GetAttackFrom(AttackType.Type.upTilt);
        }

        public string PerformDownTiltAttack()
        {
            return GetAttackFrom(AttackType.Type.downTilt);
        }

        public string PerformForwardTiltAttack()
        {
            return GetAttackFrom(AttackType.Type.forwardTilt);
        }

        public string PerformUpAirAttack()
        {
            return GetAttackFrom(AttackType.Type.upAir);
        }

        public string PerformDownAirAttack()
        {
            return GetAttackFrom(AttackType.Type.downAir);
        }

        public string PerformForwardAirAttack()
        {
            return GetAttackFrom(AttackType.Type.forwardAir);
        }

        public string PerformBackAirAttack()
        {
            return GetAttackFrom(AttackType.Type.backAir);
        }

        #endregion
    }
}
