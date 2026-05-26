using UnityEngine;

[RequireComponent(typeof(HurtBoxHandler))]
[RequireComponent(typeof(Rigidbody))]
public class EffectsHandler : MonoBehaviour, IEffects
{
        IHurtBoxHandler hurtBoxHandler;
    private new Rigidbody rigidbody;

    [Header("Launch Effects")]
    [SerializeField] private ParticleSystem launchTrail;

    [Range(0, 1f)]
    [SerializeField] private float velocityScale = 1f;
    [SerializeField] private float minRate = 10;
    [SerializeField] private float maxRate = 100;
    [SerializeField] private float minLifeTimeStart = 0.15f;
    [SerializeField] private float maxLifeTimeStart = 0.5f;
    [SerializeField] private float maxLaunchSpeed = 20;
    [Tooltip("Minimum rigidbody speed required for the launch trail to emit.")]
    [SerializeField] private float minTrailVelocity = 15f;

    private void Awake()
    {
        hurtBoxHandler = GetComponent<IHurtBoxHandler>();
        rigidbody = GetComponent<Rigidbody>();

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
        if (state == HitStunState.Launch)
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
        Vector3 velocity = rigidbody.linearVelocity;
        float speed = velocity.magnitude;

        var emission = launchTrail.emission;

        if (speed < minTrailVelocity)
        {
            emission.rateOverTime = 0;
            return;
        }

        float normalizedSpeed = Mathf.Clamp01(speed / maxLaunchSpeed);

        var velocityOverTime = launchTrail.velocityOverLifetime;
        velocityOverTime.x = -velocity.x * velocityScale;
        velocityOverTime.y = -velocity.y * velocityScale;

        emission.rateOverTime = Mathf.Lerp(minRate, maxRate, normalizedSpeed);

        var main = launchTrail.main;
        main.startLifetime = Mathf.Lerp(minLifeTimeStart, maxLifeTimeStart, normalizedSpeed);

        launchTrail.transform.rotation = Quaternion.LookRotation(Vector3.forward, velocity);
    }
}