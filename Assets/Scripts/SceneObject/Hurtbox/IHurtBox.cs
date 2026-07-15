using System;
using UnityEngine;

public interface IHurtBox
{
    public Guid OwnerID { get; }
    public Vector3 Velocity { get; }
    public void Hit(HitData hitData);    
}
