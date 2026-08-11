using UnityEngine;

public class ProjectileLauncher : MonoBehaviour
{
    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 10f;

    [Header("Fire Rate")]
    [SerializeField] private float fireInterval = 2f;
    private float nextFireTime;

    [Header("Hit Data")]
    [SerializeField] private float baseForce = 1000f;
    [SerializeField] private float influence = 0.5f;
    [SerializeField] private float damage = 8f;
    [SerializeField] private float stunTime = 0.1f;
    [SerializeField] private int effectIndex = 0;

    private void Awake()
    {
        if (projectilePrefab == null)
            Debug.LogWarning("ProjectileLauncher: No projectile prefab assigned.", gameObject);
    }

    private void Update()
    {
        if (Time.time >= nextFireTime)
        {
            Fire();
            nextFireTime = Time.time + fireInterval;
        }
    }

    private void Fire()
    {
        GameObject instance = Instantiate(projectilePrefab, transform.position, Quaternion.identity);

        Projectile projectile = instance.GetComponent<Projectile>();
        if (projectile == null)
        {
            Debug.LogError("ProjectileLauncher: Prefab does not have a Projectile component.", gameObject);
            Destroy(instance);
            return;
        }

        Vector3 velocity = transform.right * projectileSpeed;
        HitData hitData = new HitData(baseForce, influence, 0f, damage, stunTime, transform.position, effectIndex);

        projectile.Initialize(velocity, hitData);
    }
}
