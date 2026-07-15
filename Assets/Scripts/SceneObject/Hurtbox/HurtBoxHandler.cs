using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ISceneObject))]
[RequireComponent(typeof(Rigidbody))]
public abstract class HurtBoxHandler : MonoBehaviour, IHurtBoxHandler, IHurtBoxHandlerEditor
{
    private ISceneObject sceneObject;
    protected Rigidbody rb;
    //Hurt boxs
    protected IHurtBox[] hurtBoxes;

    protected List<Guid> lastHitBy;
    public Guid[] LastHitBy => lastHitBy.ToArray();

    [Header("Damage")]
    [SerializeField] protected float damageTaken = 0;
    
    [Header("Knockback")]
    [Tooltip("Base knockback force applied to any attack (before mass division). Sets the minimum-feel velocity at 0% damage.")]
    [SerializeField] private float baseForce = 1200f;
    [Tooltip("Exponential growth of damage-scaled knockback. Lower = smoother curve, higher = sharper ramp at high damage.")]
    [SerializeField] private float exGrowth = 2.0f;
    [Tooltip("Multiplier on the damage-scaled portion. Controls how much added force comes from damage scaling.")]
    [SerializeField] private float damageForceScale = 0.2f;
    protected Vector3 knockBackVelocity;    

    public event Action<KnockBackHitData> OnHitEvent;

    protected virtual void Awake()
    {
        sceneObject = GetComponent<ISceneObject>();        
        rb = GetComponent<Rigidbody>();
        lastHitBy = new List<Guid>();

        //Get hurtboxs
        hurtBoxes = CollectHurtBoxs();
        if (hurtBoxes.Length == 0)
            Debug.LogWarning("HurtBoxHandler: No Hurtboxs where found on", gameObject);

        RegisterToEvents();
    }

    protected virtual IHurtBox[] CollectHurtBoxs()
    {
        return GetComponentsInChildren<HurtBox>(true);
    }

    protected virtual void RegisterToEvents()
    {
        foreach (HurtBox hurtBox in hurtBoxes)
            hurtBox.OnHitEvent += OnHit;
    }

    private void OnDestroy()
    {
        UnRegisterFromEvents();
    }

    protected virtual void UnRegisterFromEvents()
    {
        foreach (HurtBox hurtBox in hurtBoxes)
            hurtBox.OnHitEvent -= OnHit;
    }    

    protected virtual void OnHit(HitData hitData)
    {
        if (hitData == null)
            return;

        //Damage and Knockback
        damageTaken += hitData.Damage;

        float hitBaseForce = hitData is SceneObjectCollisionHitData collisionHitData
            ? collisionHitData.BaseForce
            : baseForce;

        knockBackVelocity = CalculateKnockbackVelocity(hitData.Influence, damageTaken, hitData.LauchAngle, rb.mass, hitBaseForce);

        OnHitEvent?.Invoke(new KnockBackHitData(knockBackVelocity, hitData));
    }

    private Vector3 CalculateKnockbackVelocity(float influence, float totalDamage, float launchAngle, float mass, float hitBaseForce)
    {
        float force = hitBaseForce + influence * Mathf.Pow(totalDamage, exGrowth) * damageForceScale;
        float velocity = force / mass;

        float xLaunch = Mathf.Cos(launchAngle * Mathf.Deg2Rad);
        float yLaunch = Mathf.Sin(launchAngle * Mathf.Deg2Rad);
        Vector3 launchDirection = new Vector2(xLaunch, yLaunch);

        return launchDirection * velocity;
    }    

    #region Editor Debug

    [Header("Debug")]
    [SerializeField, HideInInspector] private bool debugMode;
    [SerializeField, HideInInspector] private float debugInfluence = 1f;
    [SerializeField, HideInInspector] private float debugLaunchAngle = 45f;
    [SerializeField, HideInInspector] private float debugDamage = 10f;
    [SerializeField, HideInInspector] private float debugDelaySeconds = 0f;

    public bool DebugMode => debugMode;
    public float DebugInfluence => debugInfluence;
    public float DebugLaunchAngle => debugLaunchAngle;
    public float DebugDamage => debugDamage;
    public float DebugDelaySeconds => debugDelaySeconds;

    public void SetDebugMode(bool value)
    {
        debugMode = value;
    }

    public void SetDebugInfluence(float value)
    {
        debugInfluence = Mathf.Clamp01(value);
    }

    public void SetDebugLaunchAngle(float value)
    {
        debugLaunchAngle = Mathf.Clamp(value, 0f, 360f);
    }

    public void SetDebugDamage(float value)
    {
        debugDamage = value;
    }

    public void SetDebugDelaySeconds(float value)
    {
        debugDelaySeconds = Mathf.Max(0f, value);
    }

    public void ApplyDebugDamage()
    {
        if (debugDelaySeconds <= 0f)
        {
            HitData immediateHitData = new HitData(debugInfluence, debugLaunchAngle, debugDamage, 0f, transform.position, 0);
            OnHit(immediateHitData);
            return;
        }

        StartCoroutine(ApplyDebugDamageAfterDelay(debugDelaySeconds));
    }

    private IEnumerator ApplyDebugDamageAfterDelay(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);

        HitData hitData = new HitData(debugInfluence, debugLaunchAngle, debugDamage, 0f, transform.position, 0);
        OnHit(hitData);
    }

    #endregion
}
