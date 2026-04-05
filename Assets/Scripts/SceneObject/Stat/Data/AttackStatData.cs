using UnityEngine;

public class AttackStatData
{
    public GameObject WeaponRootGameObject;

    public class AttackStats
    {
        public AttackState State;
        public AnimationClip Animation;
        public float Influence;
        public float LaunchAngle;

        private AnimationCurve damageCurve;
        
        public AttackStats(AttackState state, AttackData data)
        {
            State = state;
            Animation = data.Animation;
            Influence = data.Influence;
            LaunchAngle = data.LaunchAngle;
            this.damageCurve = data.DamageCurve;
        }

        public float GetAttackDamage(int frame)
        {
            return damageCurve.Evaluate(frame);
        }
    }

    public AttackStats UpTilt;
    public AttackStats ForwardTilt;
    public AttackStats DownTilt;
    public AttackStats UpAir;
    public AttackStats ForwardAir;
    public AttackStats DownAir;
}


