using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Activates enhancements when their trigger occurs. Listens to the events other handlers expose,
/// converts them to enhancement triggers, and runs every enhancement whose trigger matches.
/// </summary>
[RequireComponent(typeof(ISceneObject))]
public class EnhancementHandler : MonoBehaviour
{
    // Dependencies
    private ISceneObject sceneObject;
    private IStats statHandler;
    private IAnimationEvent animationEventHandler;
    private IAttack attackHandler;
    private IAttackHitBoxHandler attackHitBoxHandler;

    private readonly List<EnhancementStatData.EnhancementStats> enhancements = new List<EnhancementStatData.EnhancementStats>();

    private AttackStatData curAttackStatData;
    private AttackState curAttackState = AttackState.Null;


    #region Initialize

    private void Awake()
    {
        sceneObject = GetComponent<ISceneObject>();

        statHandler = GetComponent<IStats>();
        if (statHandler == null)
            Debug.LogError("EnhancementHandler: No IStats component found", gameObject);

        // Optional Components
        animationEventHandler = GetComponentInChildren<IAnimationEvent>();
        attackHandler = GetComponent<IAttack>();
        attackHitBoxHandler = GetComponent<IAttackHitBoxHandler>();

        RegisterToEvents();
    }

    private void OnDestroy()
    {
        UnregisterFromEvents();
    }

    private void RegisterToEvents()
    {
        if (statHandler != null)
        {
            statHandler.EnhancementStatsChangedEvent += OnEnhancementStatsChanged;
            statHandler.AttackStatsChangedEvent += OnAttackStatsChanged;
        }

        if (animationEventHandler != null)
            animationEventHandler.OnAnimationEventFiredEvent += OnAnimationEventFired;

        if (attackHandler != null)
            attackHandler.AttackStateChangedEvent += OnAttackStateChanged;
    }

    private void UnregisterFromEvents()
    {
        if (statHandler != null)
        {
            statHandler.EnhancementStatsChangedEvent -= OnEnhancementStatsChanged;
            statHandler.AttackStatsChangedEvent -= OnAttackStatsChanged;
        }

        if (animationEventHandler != null)
            animationEventHandler.OnAnimationEventFiredEvent -= OnAnimationEventFired;

        if (attackHandler != null)
            attackHandler.AttackStateChangedEvent -= OnAttackStateChanged;
    }

    #endregion


    #region Event Listeners

    /// <summary>Replaces the active enhancements.</summary>
    private void OnEnhancementStatsChanged(EnhancementStatData enhancementStats)
    {
        enhancements.Clear();
        enhancements.AddRange(enhancementStats.Enhancements);
    }

    private void OnAttackStatsChanged(AttackStatData attackStats)
    {
        curAttackStatData = attackStats;
    }

    private void OnAttackStateChanged(AttackState attackState)
    {
        curAttackState = attackState;
    }

    private void OnAnimationEventFired(AnimationEventState eventState)
    {
        if (eventState == AnimationEventState.Spawn)
            Activate(EnhancementTrigger.Attack);
    }

    #endregion


    #region Activation

    /// <summary>Runs every enhancement with the given trigger.</summary>
    private void Activate(EnhancementTrigger trigger)
    {
        foreach (EnhancementStatData.EnhancementStats enhancement in enhancements)
        {
            if (enhancement.Trigger != trigger)
                continue;

            Spawn(enhancement);
        }
    }

    /// <summary>Spawns the enhancement's prefab at the weapon and launches it along its trajectory.</summary>
    private void Spawn(EnhancementStatData.EnhancementStats enhancement)
    {
        GameObject instance = Instantiate(enhancement.SpawnPrefab, SpawnPosition(), Quaternion.identity);

        if (!instance.TryGetComponent(out Projectile projectile))
        {
            Debug.LogError($"EnhancementHandler: {enhancement.SpawnPrefab.name} has no Projectile component.", gameObject);
            Destroy(instance);
            return;
        }

        IgnoreCollisionsWith(instance);

        if (attackHitBoxHandler != null)
            attackHitBoxHandler.IgnoreForCurrentAttack(projectile.UniqueID);

        projectile.Launch(sceneObject.UniqueID, LaunchAngle(enhancement.Trajectory), enhancement.Trajectory);
    }

    /// <summary>The weapon's position, or the center of the scene object when there is no weapon.</summary>
    private Vector3 SpawnPosition()
    {
        GameObject weaponRoot = curAttackStatData?.WeaponRootGameObject;

        Vector3 position = weaponRoot != null ? weaponRoot.transform.position : sceneObject.Bounds.center;
        position.z = 0f;

        return position;
    }

    /// <summary>Stops physical collision between the spawned object and this scene object.</summary>
    private void IgnoreCollisionsWith(GameObject instance)
    {
        Collider[] ownColliders = GetComponentsInChildren<Collider>();
        Collider[] spawnedColliders = instance.GetComponentsInChildren<Collider>();

        foreach (Collider spawned in spawnedColliders)
        {
            foreach (Collider own in ownColliders)
                Physics.IgnoreCollision(spawned, own);
        }
    }

    /// <summary>The trajectory's angle for the current attack, mirrored when facing left.</summary>
    private float LaunchAngle(TrajectoryData trajectory)
    {
        float angle = trajectory.GetAngle((int)curAttackState);

        return sceneObject.IsFacingRightDirection ? angle : 180f - angle;
    }

    #endregion
}
