using System;
using UnityEngine;

public interface IHitBox
{
    void ActivateHitBox();
    void DeactivateHitBox();
    void SetOwner(Guid ownerID);

    /// <summary>Returns the base hitbox's lowest and highest point along the weapon root's Y axis. False if the hitbox cannot be measured on that axis.</summary>
    bool TryGetBaseSpan(Transform weaponRoot, out float min, out float max);

    /// <summary>Lengthens the base hitbox by extension along the weapon root's +Y axis, keeping its -Y end in place.</summary>
    void ApplyStretch(Transform weaponRoot, float extension);

    /// <summary>Restores the base hitbox size and center.</summary>
    void ResetStretch();

    event Action<IHitBox, IHurtBox, Vector3> OnCollisionEntered;
}
