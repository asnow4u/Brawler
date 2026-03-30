using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ISceneObject))]
[RequireComponent(typeof(IActionState))]
[RequireComponent(typeof(IStats))]
[RequireComponent(typeof(Rigidbody))]
internal class HurtBoxHandler : MonoBehaviour, IHurtBoxHandler
{
    //Dependecies
    private ISceneObject sceneObject;
    private IActionState actionState;
    private IStats stats;

    //Componenets
    private Rigidbody rb;

    private float deccelerationRate;

    //Hurt boxs
    private HurtBox[] hurtBoxes;

    //Immunity
    [SerializeField] private float immunityTime = 1.0f;
    private Dictionary<Guid, float> immunityList = new Dictionary<Guid, float>();

    //Damage
    [SerializeField] protected float damageTaken = 0;
    //"Base amount of acceleration that will be applied anytime taking a hit"
    const float minKnockBackAcceleration = 10f;
    //"The exponential growth of knockback based on damage"
    const float exGrowth = 2.8f;

    private void Awake()
    {
        sceneObject = GetComponent<ISceneObject>();
        if (sceneObject == null)
            Debug.LogError("No SceneObject found on " + gameObject.name, gameObject);

        actionState = GetComponent<IActionState>();
        if (actionState == null)
            Debug.LogError("No IActionState found on " + gameObject.name, gameObject);

        stats = GetComponent<IStats>();
        if (stats == null)
            Debug.LogError("Not IStats found on " + gameObject.name, gameObject);

        rb = GetComponent<Rigidbody>();

        //Get hurtboxs
        hurtBoxes = GetComponentsInChildren<HurtBox>(true);

        RegisterToEvents();
    }

    private void RegisterToEvents()
    {
        stats.MovementStatsChangedEvent += OnMovementStatsChanged;

        foreach (HurtBox hurtBox in hurtBoxes)
            hurtBox.OnHitEvent += OnHit;
    }

    private void OnDestroy() 
    {
        UnRegisterFromEvents();
    }

    private void UnRegisterFromEvents()
    {
        stats.MovementStatsChangedEvent += OnMovementStatsChanged;

        foreach (HurtBox hurtBox in hurtBoxes)
            hurtBox.OnHitEvent -= OnHit;
    }

    private void OnMovementStatsChanged(MovementStatData statData)
    {
        deccelerationRate = statData.AerialUpYDecceleration;
    }

    private void OnHit(HitData hitData)
    {
        if (hitData == null)
            return;

        if (immunityList.ContainsKey(hitData.SceneObjectID))
            return;

        damageTaken += hitData.Damage;

        //Launch knockback
        Vector3 launchVelocity = CalculateKnockbackVelocity(hitData.Influence, damageTaken, hitData.LauchAngle, rb.mass);

        rb.linearVelocity = launchVelocity;

        float hitStunTimer = Mathf.Abs(launchVelocity.y / deccelerationRate);
        actionState.SetHitStun(hitStunTimer);
    }

    public Vector3 CalculateKnockbackVelocity(float influence, float totalDamage, float launchAngle, float mass)
    {
        float minForce = mass * minKnockBackAcceleration;
        float damageForce = minForce + influence * (Mathf.Pow(totalDamage, exGrowth) / mass);

        float xLaunch = Mathf.Cos(launchAngle * Mathf.Deg2Rad);
        float yLaunch = Mathf.Sin(launchAngle * Mathf.Deg2Rad);
        Vector3 launchDirection = new Vector2(xLaunch, yLaunch);

        return launchDirection * damageForce / mass;
    }

    private void Update()
    {
        UpdateImmunityList();
    }

    public void SetImmunityFrom(Guid sceneObjectID)
    {
        if (immunityList.ContainsKey(sceneObjectID))
            immunityList[sceneObjectID] = immunityTime;
        else
            immunityList.Add(sceneObjectID, immunityTime);
    }

    public void UpdateImmunityList()
    {
        foreach (var kvp in new Dictionary<Guid, float>(immunityList))
        {
            immunityList[kvp.Key] -= Time.deltaTime;

            if (immunityList[kvp.Key] <= 0)
                immunityList.Remove(kvp.Key);
        }
    }
}
