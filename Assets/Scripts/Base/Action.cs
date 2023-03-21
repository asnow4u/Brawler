using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AnimatorStateMachine))]
[RequireComponent(typeof(Movement))]
public abstract class Action : MonoBehaviour
{
    protected AnimatorStateMachine animator;
    protected Movement movement;

    protected List<Weapon> curWeapons;
    
    void Start()
    {
        animator = GetComponent<AnimatorStateMachine>();
        movement = GetComponent<Movement>();
    }

    public void SetUpAttackAction(List<Weapon> weapons)
    {
        this.curWeapons = weapons;
    }


    protected abstract void PerformUpAttack();

    protected abstract void PerformDownAttack();

    protected abstract void PerformLeftAttack();

    protected abstract void PerformRightAttack();

    public void AttackStarted(string attackName)
    {
        foreach (Weapon weapon in curWeapons)
        {
            weapon.SetCurAttack(attackName);
        }
    }

    public void AttackEnded(string attackName)
    {
        foreach (Weapon weapon in curWeapons)
        {
            weapon.ResetCurAttack();
        }
    }

    public void AttackCollidersEnabled()
    {
        foreach (Weapon weapon in curWeapons)
        {
            weapon.EnableColliders();
        }
    }

    public void AttackCollidersDisabled()
    {
        foreach (Weapon weapon in curWeapons)
        {
            weapon.DisableColliders();
        }
    }

}
