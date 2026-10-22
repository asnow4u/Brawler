using System;
using UnityEngine;

public interface IHitBoxHandler
{
    public event Action<HitSenderData> OnHitConnected;
    public event Action<HitSenderData, HitResponse> OnHitBlocked;
}
