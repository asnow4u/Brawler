/// <summary>Traversal capabilities and limits of one agent, used to filter the graph during search.</summary>
public struct NavAgentProfile
{
    /// <summary>Collider height in world units.</summary>
    public float Height;

    /// <summary>Has grounded movement.</summary>
    public bool CanWalk;

    /// <summary>Has ledge climbing.</summary>
    public bool CanClimb;

    /// <summary>Has a grounded jump.</summary>
    public bool HasGroundJump;

    /// <summary>Airborne physics. Filled whether or not the agent can jump.</summary>
    public AirborneEnvelope Airborne;


    public bool CanJump => HasGroundJump && Airborne.Valid;
    public bool CanFall => CanWalk && Airborne.Valid;

    /// <summary>Tallest ledge this agent can climb: 70% of its height.</summary>
    public float ClimbLimit => Height * (1f - RequiredClimbPercentage);

    // Must match InputMovementHandler.requiredLedgeClimbPercentage.
    public const float RequiredClimbPercentage = 0.3f;


    /// <summary>A walker and climber of the given height, with no airborne physics.</summary>
    public static NavAgentProfile FromHeight(float height) => new NavAgentProfile
    {
        Height = height,
        CanWalk = true,
        CanClimb = true,
    };
}
