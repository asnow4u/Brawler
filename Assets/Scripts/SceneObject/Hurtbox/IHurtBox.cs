using System;
using UnityEngine;

public interface IHurtBox
{
    public Guid OwnerID { get; }
    public void Hit(HitData hitData);    
}
