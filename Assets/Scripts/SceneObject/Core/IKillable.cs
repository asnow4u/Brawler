using System;
using UnityEngine;

public enum KillZoneSide { Left, Right, Bottom }

public interface IKillable
{
    public Guid UniqueID { get; }
    public Bounds Bounds { get; }

    /// <summary>Removes the object after it passed the kill zone on the given side.</summary>
    public void Kill(KillZoneSide side);
}
