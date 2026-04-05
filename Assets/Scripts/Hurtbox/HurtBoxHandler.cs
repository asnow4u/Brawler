using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ISceneObject))]
[RequireComponent(typeof(ActionStateHandler))]
[RequireComponent(typeof(StatHandler))]
[RequireComponent(typeof(Rigidbody))]
public class HurtBoxHandler : MonoBehaviour, IHurtBoxHandler
{
    //Dependecies
    private ISceneObject sceneObject;
    private IActionState actionState;
    private IStats statHandler;

    //Componenets
    private Rigidbody rb;

    //Hurt boxs
    private HurtBox[] hurtBoxes;

    //Damage
    [SerializeField] protected float damageTaken = 0;
    //"Base amount of acceleration that will be applied anytime taking a hit"
    const float minKnockBackAcceleration = 10f;
    //"The exponential growth of knockback based on damage"
    const float exGrowth = 2.8f;

    //Hit stun
    private float deccelerationRate;
    [SerializeField] private float hitStopTimer = 0.033f;
    private Coroutine hitStunTimerCoroutine;

    //Immunity
    private float immunityTime = 1.0f;
    private Dictionary<Guid, float> immunityList = new Dictionary<Guid, float>();


    private void Awake()
    {
        sceneObject = GetComponent<ISceneObject>();
        actionState = GetComponent<IActionState>();
        statHandler = GetComponent<IStats>();
        rb = GetComponent<Rigidbody>();

        //Get hurtboxs
        hurtBoxes = GetComponentsInChildren<HurtBox>(true);

        RegisterToEvents();
    }

    private void RegisterToEvents()
    {
        statHandler.MovementStatsChangedEvent += OnMovementStatsChanged;

        foreach (HurtBox hurtBox in hurtBoxes)
            hurtBox.OnHitEvent += OnHit;
    }

    private void OnDestroy() 
    {
        UnRegisterFromEvents();
    }

    private void UnRegisterFromEvents()
    {
        statHandler.MovementStatsChangedEvent += OnMovementStatsChanged;

        foreach (HurtBox hurtBox in hurtBoxes)
            hurtBox.OnHitEvent -= OnHit;
    }

    private void Update()
    {
        UpdateImmunityList();
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
        float hitStunTime = Mathf.Abs(launchVelocity.y / deccelerationRate);        
        ApplyHitStun(hitStunTime);
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

    #region HitStun

    private void ApplyHitStun(float hitStunTime)
    {
        if (hitStunTimerCoroutine != null)
            StopCoroutine(hitStunTimerCoroutine);

        hitStunTimerCoroutine = StartCoroutine(HitStunTimer(hitStunTime));
    }

    private IEnumerator HitStunTimer(float timer)
    {
        actionState.ChangeHitStunState(HitStunState.Stop);
        yield return new WaitForSeconds(hitStopTimer);

        actionState.ChangeHitStunState(HitStunState.Launch);
        yield return new WaitForSeconds(timer);

        actionState.ChangeHitStunState(HitStunState.Null);
    }

    #endregion

    #region Immunity

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

    #endregion


    [ContextMenu("Test Hit")]
    public void TestHit()
    {
        //Test
        HitData hitTestData = new HitData(Guid.NewGuid(), 1, 45, 20);
        OnHit(hitTestData);
    }
}
