using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Places Left, Right and Bottom kill zones a margin outside the camera view on the Z = 0 plane.
/// Kills an object past a zone while its hitstun is in Launch or Travel. Kills any object that falls
/// below the bottom of the level bounds, hit or not.
/// </summary>
public class KillZoneManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Camera the zones follow. Uses the main camera when empty.")]
    [SerializeField] private Camera viewCamera;
    [Tooltip("Camera confiner bounds. Its bottom edge is the level bottom.")]
    [SerializeField] private PolygonCollider2D levelBounds;

    [Header("Margins")]
    [Tooltip("Distance outside the left of the view, in world units.")]
    [SerializeField] private float leftMargin = 1.5f;
    [Tooltip("Distance outside the right of the view, in world units.")]
    [SerializeField] private float rightMargin = 1.5f;
    [Tooltip("Distance outside the bottom of the view and below the level bottom, in world units.")]
    [SerializeField] private float bottomMargin = 1.5f;

    private static readonly Plane GamePlane = new Plane(Vector3.forward, Vector3.zero);
    private static readonly Vector2[] ViewportCorners = { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };

    private readonly Dictionary<IKillable, ISOHurtBoxHandler> hurtBoxHandlers = new Dictionary<IKillable, ISOHurtBoxHandler>();
    private readonly HashSet<IKillable> killed = new HashSet<IKillable>();

    /// <summary>Fires once per object, right after it is killed.</summary>
    public event Action<IKillable, KillZoneSide> KilledEvent;

    /// <summary>Visible camera area on the Z = 0 plane, without margins. Valid while HasView is true.</summary>
    public Rect ViewRect { get; private set; }

    /// <summary>Whether ViewRect and the zones were set on the last FixedUpdate.</summary>
    public bool HasView => hasZones;

    private float leftX;
    private float rightX;
    private float viewTopY;
    private float bottomY;
    private float levelBottomY;
    private bool hasZones;


    #region Initialize

    private void Awake()
    {
        if (viewCamera == null)
            viewCamera = Camera.main;

        if (viewCamera == null)
            Debug.LogError("KillZoneManager needs a camera.", gameObject);

        if (levelBounds == null)
            Debug.LogWarning("KillZoneManager has no level bounds. Falls without a hit will not kill.", gameObject);

        KillableRegistry.UnregisteredEvent += OnKillableUnregistered;
    }

    private void OnDestroy()
    {
        KillableRegistry.UnregisteredEvent -= OnKillableUnregistered;
    }

    private void OnKillableUnregistered(IKillable killable)
    {
        hurtBoxHandlers.Remove(killable);
        killed.Remove(killable);
    }

    #endregion


    #region Kill Check

    private void FixedUpdate()
    {
        hasZones = TryUpdateZones();

        if (!hasZones)
            return;

        IReadOnlyList<IKillable> killables = KillableRegistry.Killables;

        for (int i = killables.Count - 1; i >= 0; i--)
        {
            IKillable killable = killables[i];

            if (killed.Contains(killable) || !TryGetKillSide(killable, out KillZoneSide side))
                continue;

            killed.Add(killable);
            killable.Kill(side);
            KilledEvent?.Invoke(killable, side);
        }
    }

    /// <summary>The side the object passed, when it is in Launch or Travel past a zone or below the level bottom.</summary>
    private bool TryGetKillSide(IKillable killable, out KillZoneSide side)
    {
        Vector3 center = killable.Bounds.center;

        if (levelBounds != null && center.y < levelBottomY)
        {
            side = KillZoneSide.Bottom;
            return true;
        }

        side = default;

        if (!IsLaunched(killable))
            return false;

        if (center.x < leftX)
            side = KillZoneSide.Left;
        else if (center.x > rightX)
            side = KillZoneSide.Right;
        else if (center.y < bottomY)
            side = KillZoneSide.Bottom;
        else
            return false;

        return true;
    }

    private bool IsLaunched(IKillable killable)
    {
        if (!hurtBoxHandlers.TryGetValue(killable, out ISOHurtBoxHandler hurtBoxHandler))
        {
            hurtBoxHandler = killable is Component component ? component.GetComponent<ISOHurtBoxHandler>() : null;
            hurtBoxHandlers[killable] = hurtBoxHandler;
        }

        if (hurtBoxHandler == null)
            return false;

        HitStunState state = hurtBoxHandler.CurHitStunState;
        return state == HitStunState.Launch || state == HitStunState.Travel;
    }

    #endregion


    #region Zones

    /// <summary>Sets the zones from where the view corners meet the Z = 0 plane, and the level bottom.</summary>
    private bool TryUpdateZones()
    {
        if (viewCamera == null)
            return false;

        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);

        foreach (Vector2 corner in ViewportCorners)
        {
            Ray ray = viewCamera.ViewportPointToRay(new Vector3(corner.x, corner.y, 0f));

            if (!GamePlane.Raycast(ray, out float enter))
                return false;

            Vector2 point = ray.GetPoint(enter);
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }

        ViewRect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);

        leftX = min.x - leftMargin;
        rightX = max.x + rightMargin;
        viewTopY = max.y;
        bottomY = min.y - bottomMargin;

        if (levelBounds != null)
            levelBottomY = levelBounds.bounds.min.y - bottomMargin;

        return true;
    }

    #endregion


    #region Debug

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || !hasZones)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(leftX, bottomY, 0f), new Vector3(leftX, viewTopY, 0f));
        Gizmos.DrawLine(new Vector3(rightX, bottomY, 0f), new Vector3(rightX, viewTopY, 0f));
        Gizmos.DrawLine(new Vector3(leftX, bottomY, 0f), new Vector3(rightX, bottomY, 0f));

        if (levelBounds == null)
            return;

        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(new Vector3(leftX, levelBottomY, 0f), new Vector3(rightX, levelBottomY, 0f));
    }

    #endregion
}
