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

        //Knockback at 0 damage. Influence scales growth from here.
        public float BaseForce;

        //Attacker side - how long this object's animation freezes on connect
        public float HitPauseTime;

        //Target side - how long the object that was hit stays in hitstun
        public float HitStunTime;

        public float Damage;
        
        public AttackStats(int state, AttackData data)
        {
            State = state;
            Type = (int)data.AttackType;
            Animation = data.AnimationData.Animation;
            BaseForce = data.BaseForce;
            Influence = data.Influence;
            LaunchAngle = data.LaunchAngle;
            HitPauseTime = data.HitPauseTime;
            HitStunTime = data.HitStunTime;
            Damage = data.Damage;
        }
    }

    public AttackStats UpTilt;
    public AttackStats ForwardTilt;
    public AttackStats DownTilt;
    public AttackStats UpAir;
    public AttackStats ForwardAir;
    public AttackStats DownAir;
}


