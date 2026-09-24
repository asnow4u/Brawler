using UnityEngine;

/// <summary>Holds a single spot. The enemy returns to it and stands still while idle.</summary>
internal class EnemyPost : IEnemyHome
{
    private readonly Transform owner;

    private Vector3 post;
    private bool initialized;

    private Vector3 Anchor => initialized ? post : owner.position;


    public EnemyPost(Transform owner)
    {
        this.owner = owner;
    }

    public void Initialize(Vector3 spawnPosition)
    {
        post = spawnPosition;
        initialized = true;
    }

    public float DistanceFrom(Vector3 worldPosition)
    {
        return Vector3.Distance(Anchor, worldPosition);
    }

    public Vector3 GetReturnDestination(Vector3 currentPosition)
    {
        return Anchor;
    }

    public bool TryGetIdleDestination(Vector3 currentPosition, bool arrived, bool failed, out Vector3 destination)
    {
        destination = Anchor;
        return false;
    }

    public void ReHome(Vector3 worldPosition)
    {
        post = worldPosition;
        initialized = true;
    }

    public void DrawHomeGizmos(float disengageRadius)
    {
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(Anchor, disengageRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(Anchor, Vector3.one * 0.3f);
    }
}
