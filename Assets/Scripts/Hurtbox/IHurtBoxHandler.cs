using System;
using UnityEngine;

public interface IHurtBoxHandler
{
    public event Action<HitData> OnHitEvent;
}
