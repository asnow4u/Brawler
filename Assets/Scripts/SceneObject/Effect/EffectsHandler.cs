using UnityEngine;

[RequireComponent(typeof(HurtBoxHandler))]
public class EffectsHandler : MonoBehaviour, IEffects
{
    IHurtBoxHandler hurtBoxHandler;

    [Header("Launch Effects")]
    [SerializeField] private ParticleSystem launchTrail;

    [Range(0, 1f)]
    [SerializeField] private float velocityScale = 1f;
    [SerializeField] private float minRate = 10;
    [SerializeField] private float maxRate = 100;
    [SerializeField] private float minLifeTimeStart = 0.15f;
    [SerializeField] private float maxLifeTimeStart = 0.5f;
    [SerializeField] private float maxLaunchSpeed = 20;


    private void Awake()
    {
        hurtBoxHandler = GetComponent<IHurtBoxHandler>();

        RegisterToEvents();
    }

    private void RegisterToEvents()
    {
        hurtBoxHandler.HitStunStateChangedEvent += OnHitStunStateChanged;
    }

    private void OnDestroy()
    {
        UnregisterFromEvents();
    }

    private void UnregisterFromEvents()
    {
        hurtBoxHandler.HitStunStateChangedEvent -= OnHitStunStateChanged;
    }

    private void OnHitStunStateChanged(HitStunState state)
    {
        if (state >= HitStunState.Launch)
            launchTrail.Play();
        else
            launchTrail.Stop();
    }


    private void Update()
    {
        if (launchTrail.isPlaying)
            UpdateLaunchTrail();
    }

    private void UpdateLaunchTrail()
    {
        Vector3 velocity = hurtBoxHandler.EvaluateHitStunVelocity();
        Vector3 direction = velocity.normalized;
        float normalizedSpeed = velocity.magnitude / maxLaunchSpeed;

        var velocityOverTime = launchTrail.velocityOverLifetime;        
        velocityOverTime.x = -velocity.x * velocityScale;
        velocityOverTime.y = -velocity.y * velocityScale;

        var emission = launchTrail.emission;
        emission.rateOverTime = Mathf.Lerp(minRate, maxRate, normalizedSpeed);

        var main = launchTrail.main;
        main.startLifetime = Mathf.Lerp(minLifeTimeStart, maxLifeTimeStart, normalizedSpeed);


        if (velocity.sqrMagnitude > 0.01f)
        {
            launchTrail.transform.rotation = Quaternion.LookRotation(Vector3.forward, velocity);
        }
    }
}