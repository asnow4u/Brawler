using System;
using UnityEngine;

public interface IHitBox
{
    bool IsActive { get; }

    void ActivateHitBox();
    void DeactivateHitBox();
    void SetOwner(Guid ownerID);

    /// <summary>Returns the base hitbox's lowest and highest point along the weapon root's Y axis. False if the hitbox cannot be measured on that axis.</summary>
    bool TryGetBaseSpan(Transform weaponRoot, out float min, out float max);

    /// <summary>Lengthens the base hitbox by extension along the weapon root's +Y axis, keeping its -Y end in place.</summary>
    void ApplyStretch(Transform weaponRoot, float extension);

    /// <summary>Restores the base hitbox size and center.</summary>
    void ResetStretch();

    /// <summary>Returns the current hitbox shape as a world-space segment from its weapon -Y end to its +Y end, and its widest cross-section. False until the length axis has been measured.</summary>
    bool TryGetLengthSegment(out Vector3 start, out Vector3 end, out float width);

    event Action<IHitBox, IHurtBox, Vector3> OnCollisionEntered;
}
