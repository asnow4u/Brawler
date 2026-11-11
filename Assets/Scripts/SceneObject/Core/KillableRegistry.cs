using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Every live IKillable in the scene.</summary>
public static class KillableRegistry
{
    private static readonly List<IKillable> killables = new List<IKillable>();

    public static IReadOnlyList<IKillable> Killables => killables;

    public static event Action<IKillable> UnregisteredEvent;

    public static void Register(IKillable killable)
    {
        if (killable != null && !killables.Contains(killable))
            killables.Add(killable);
    }

    public static void Unregister(IKillable killable)
    {
        if (killables.Remove(killable))
            UnregisteredEvent?.Invoke(killable);
    }

    /// <summary>Clears the registry when entering play mode without a domain reload.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnLoad()
    {
        killables.Clear();
        UnregisteredEvent = null;
    }
}
