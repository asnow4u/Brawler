using System;

public class AttackHandler : IDealDamage
{    
    private AttackData curAttackData;
    private Weapon curWeapon;

    private Func<int> GetAnimationFrame;
    private Action HandleChangeAttack;

    public AttackHandler(AttackData attackData, Weapon weapon, Func<int> getAnimationFrameMethod, Action handleChargeAttackTimeTrigger)
    {
        curAttackData = attackData;
        curWeapon = weapon;
        GetAnimationFrame = getAnimationFrameMethod;
        HandleChangeAttack = handleChargeAttackTimeTrigger;
    }

    #region ISourceHitData
    
    public float Influence => curAttackData.Influence;
    public float Damage => curAttackData.GetAttackDamage(GetAnimationFrame());
    public float LaunchAngle => curAttackData.LaunchAngle;

    #endregion

    /// <summary>
    /// Check for animation triggers that need to fire
    /// </summary>
    public void CheckForAnimationTriggers()
    {
        if (curAttackData == null || curWeapon == null || HandleChangeAttack == null || GetAnimationFrame == null) 
            return;

        int curAnimationFrame = GetAnimationFrame.Invoke();

        foreach (AnimationTrigger trigger in curAttackData.GetAttackTriggers())
        {
            if (!trigger.WasTriggered && curAnimationFrame >= trigger.TriggerFrame)
                ExecuteTrigger(trigger);
        }
    }


    /// <summary>
    /// Execute <paramref name="trigger"/>
    /// </summary>
    private void ExecuteTrigger(AnimationTrigger trigger)
    {
        trigger.WasTriggered = true;

        switch (trigger.TriggerType)
        {
            case AnimationTriggerType.EnableCollider:
                curWeapon.OpenAttackWindow(this);
                break;

            case AnimationTriggerType.DisableCollider:
                curWeapon.CloseAttackWindow();
                break;

            case AnimationTriggerType.ChargeAction:
                HandleChangeAttack.Invoke();
                break;
        }
    }
}
