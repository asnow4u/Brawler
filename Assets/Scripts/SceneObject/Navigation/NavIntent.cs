/// <summary>What NavAgent wants held this tick.</summary>
public struct NavIntent
{
    /// <summary>Horizontal influence: -1, 0 or 1.</summary>
    public float Horizontal;

    /// <summary>Vertical influence: -1, 0 or 1.</summary>
    public float Vertical;

    /// <summary>Whether jump is held.</summary>
    public bool HoldJump;

    public static NavIntent None => new NavIntent();
}
