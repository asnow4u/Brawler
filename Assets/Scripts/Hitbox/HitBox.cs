using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
internal class HitBox : MonoBehaviour, IHitBox
{
    private Collider collider;

    public event Action<IHurtBox> OnCollisionEntered;

    private void Awake()
    {
        collider = GetComponent<Collider>();
        DeactivateHitBox();
    }

    public void ActivateHitBox()
    {
        collider.enabled = true;
    }

    public void DeactivateHitBox()
    {
        collider.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent(out IHurtBox hurtBox))
            OnCollisionEntered?.Invoke(hurtBox);
    }    
}