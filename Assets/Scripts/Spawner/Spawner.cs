using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns a number of instances of a SceneObject prefab at its own position on Start. Spawns a
/// replacement after the respawn delay when the KillZoneManager kills one of its instances.
/// </summary>
public class Spawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private KillZoneManager killZoneManager;

    [Header("Spawn")]
    [SerializeField] private SceneObject prefab;
    [Min(0)]
    [SerializeField] private int instanceCount = 1;
    [Tooltip("Time between a kill and its replacement spawning, in seconds.")]
    [Min(0f)]
    [SerializeField] private float respawnDelay = 1f;

    private readonly HashSet<Guid> instanceIDs = new HashSet<Guid>();


    #region Initialize

    private void Awake()
    {
        if (prefab == null)
            Debug.LogError("Spawner needs a prefab.", gameObject);

        if (killZoneManager == null)
            Debug.LogError("Spawner needs a KillZoneManager.", gameObject);
        else
            killZoneManager.KilledEvent += OnKilled;
    }

    private void Start()
    {
        for (int i = 0; i < instanceCount; i++)
            Spawn();
    }

    private void OnDestroy()
    {
        if (killZoneManager != null)
            killZoneManager.KilledEvent -= OnKilled;
    }

    #endregion


    #region Spawn

    private void OnKilled(IKillable killable, KillZoneSide side)
    {
        if (!instanceIDs.Remove(killable.UniqueID))
            return;

        StartCoroutine(RespawnAfterDelay());
    }

    private IEnumerator RespawnAfterDelay()
    {
        if (respawnDelay > 0f)
            yield return new WaitForSeconds(respawnDelay);

        Spawn();
    }

    /// <summary>Instantiates the prefab at the spawner's position and records its ID.</summary>
    private void Spawn()
    {
        if (prefab == null)
            return;

        SceneObject instance = Instantiate(prefab, transform.position, prefab.transform.rotation);
        instanceIDs.Add(instance.UniqueID);
    }

    #endregion
}
