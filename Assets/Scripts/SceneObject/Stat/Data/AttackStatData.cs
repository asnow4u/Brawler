using UnityEngine;

public class AttackStatData
{
    public GameObject WeaponRootGameObject;
    public ParticleSystem SwingEffect;

    public class AttackStats
    {
        public int State;
        public int Type;
        public AnimationClip Animation;
        public float Influence;
        public float LaunchAngle;
        public float HitStunTime;

        public float Damage;
        
        public AttackStats(int state, AttackData data)
        {
            State = state;
            Type = (int)data.AttackType;
            Animation = data.AnimationData.Animation;
            Influence = data.Influence;
            LaunchAngle = data.LaunchAngle;
            HitStunTime = data.HitPauseTime;
            this.Damage = data.Damage;
        }

        // public float GetAttackDamage(float frame)
        // {
        //     return damageCurve.Evaluate(frame);
        // }
    }

    public AttackStats UpTilt;
    public AttackStats ForwardTilt;
    public AttackStats DownTilt;
    public AttackStats UpAir;
    public AttackStats ForwardAir;
    public AttackStats DownAir;
}


