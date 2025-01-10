using UnityEngine;

namespace Game.SceneObjects.Attack
{
    [CreateAssetMenu(fileName = "AttackCollection", menuName = "ScriptableObjects/Attack/Collection")]
    public class AttackCollection : ScriptableObject
    {
        public WeaponData WeaponData;

        public AttackCollection(WeaponData weaponData)
        {
            this.WeaponData = weaponData;
        }


        #region Attack Data

        public bool TryGetAttackByType(AttackType attackType, out AttackData attack)
        {
            switch (attackType)
            {
                case AttackType.UpTilt:
                    if (WeaponData.UpTilt != null)
                    {
                        attack = WeaponData.UpTilt;
                        return true;
                    }
                    break;

                case AttackType.ForwardTilt:
                    if (WeaponData.ForwardTilt != null)
                    {
                        attack = WeaponData.ForwardTilt;
                        return true;
                    }
                    break;

                case AttackType.DownTilt:
                    if (WeaponData.DownTilt != null)
                    {
                        attack = WeaponData.DownTilt;
                        return true;
                    }
                    break;

                case AttackType.UpAir:
                    if (WeaponData.UpAir != null)
                    {
                        attack = WeaponData.UpAir;
                        return true;
                    }
                    break;

                case AttackType.ForwardAir:
                    if (WeaponData.ForwardAir != null)
                    {
                        attack = WeaponData.ForwardAir;
                        return true;
                    }
                    break;

                case AttackType.DownAir:
                    if (WeaponData.DownAir != null)
                    {
                        attack = WeaponData.DownAir;
                        return true;
                    }
                    break;
            }

            attack = null;
            return false;
        }   
    

        public bool TryGetAttackByAnimation(AnimationClip animation, out AttackData attack)
        {
            if (WeaponData.ForwardTilt != null && WeaponData.ForwardTilt.Animation == animation)
            {
                attack = WeaponData.ForwardTilt;
                return true;
            }

            if (WeaponData.UpTilt != null && WeaponData.UpTilt.Animation == animation)
            {
                attack = WeaponData.UpTilt;
                return true;
            }

            if (WeaponData.DownTilt != null && WeaponData.DownTilt.Animation == animation)
            {
                attack = WeaponData.DownTilt;
                return true;
            }

            if (WeaponData.ForwardAir != null && WeaponData.ForwardAir.Animation == animation)
            {
                attack = WeaponData.ForwardAir;
                return true;
            }

            if (WeaponData.UpAir != null && WeaponData.UpAir.Animation == animation)
            {
                attack = WeaponData.UpAir;
                return true;
            }

            if (WeaponData.DownAir != null && WeaponData.DownAir.Animation == animation)
            {
                attack = WeaponData.DownAir;
                return true;
            }

            attack = null;
            return false;
        }

        #endregion
    }
}

