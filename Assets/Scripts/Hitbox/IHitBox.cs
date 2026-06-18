using System;
using UnityEngine;

public interface IHitBox
{
    void ActivateHitBox();
    void DeactivateHitBox();
    void SetOwner(Guid ownerID);
    event Action<IHitBox, IHurtBox, Vector3> OnCollisionEntered;
}
