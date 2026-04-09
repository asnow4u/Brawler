using System;
using UnityEngine;

public interface IHurtBox
{
    public Guid SceneObjectID { get; }
    public void Hit(HitData hitData);    
}
