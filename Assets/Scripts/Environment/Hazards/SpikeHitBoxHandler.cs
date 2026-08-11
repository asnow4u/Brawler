using UnityEngine;
using System;
using System.Collections.Generic;

public class SpikeHitBoxHandler : HitBoxHandler
{
    [SerializeField] private float baseForce = 1000;
    [SerializeField] private float influence = 0;
    [SerializeField] private float launchAngle = 0;
    [SerializeField] private float damage = 0;
    [SerializeField] private float hitStunTime = 0.1f;
    [SerializeField] private int effectIndex = 3;
    [SerializeField] private float resetTime = 0.2f;

    private Dictionary<Guid, float> resetTimers = new Dictionary<Guid, float>();

    protected override IHitBox[] CollectHitboxs()
    {
        return GetComponentsInChildren<IHitBox>();
    }

    private void Start()
    {
        EnableHitBoxs();
    }

    private void Update()
    {
        foreach (Guid id in new List<Guid>(resetTimers.Keys))
        {
            resetTimers[id] -= Time.deltaTime;
            if (resetTimers[id] <= 0)
            {
                resetTimers.Remove(id);
                sceneObjectsHit.Remove(id);
            }
        }
    }

    protected override void OnHit(IHitBox hitBox, IHurtBox hurtBox, Vector3 hitPoint)
    {
        if (sceneObjectsHit.Contains(hurtBox.OwnerID))
            return;

        sceneObjectsHit.Add(hurtBox.OwnerID);
        resetTimers.Add(hurtBox.OwnerID, resetTime);

        HitData data = new HitData(                       
            baseForce,
            influence, 
            launchAngle, 
            damage, 
            hitStunTime, 
            hitPoint,
            effectIndex);

        hurtBox.Hit(data);
    }
}
