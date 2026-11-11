using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plays a kill effect on the edge of the view where an object was killed, aimed toward the view center.
/// Destroys each effect once its longest particle lifetime has passed.
/// </summary>
public class KillEffectSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private KillZoneManager killZoneManager;
    [SerializeField] private KillEffectSettings settings;
    [Tooltip("Camera the effect is moved toward by the settings' Z offset. Uses the main camera when empty.")]
    [SerializeField] private Camera viewCamera;

    private readonly List<KillEffectInstance> activeEffects = new List<KillEffectInstance>();
    private KillEffectBuilder builder;


    #region Initialize

    private void Awake()
    {
        builder = new KillEffectBuilder(transform);

        if (viewCamera == null)
            viewCamera = Camera.main;

        if (killZoneManager == null)
            Debug.LogError("KillEffectSpawner needs a KillZoneManager.", gameObject);

        if (settings == null)
            Debug.LogError("KillEffectSpawner needs a KillEffectSettings asset.", gameObject);
    }

    private void OnEnable()
    {
        if (killZoneManager != null)
            killZoneManager.KilledEvent += OnKilled;
    }

    private void OnDisable()
    {
        if (killZoneManager != null)
            killZoneManager.KilledEvent -= OnKilled;
    }

    private void OnDestroy()
    {
        foreach (KillEffectInstance effect in activeEffects)
            effect.Destroy();

        activeEffects.Clear();
        builder?.Dispose();
    }

    #endregion


    #region Play

    private void OnKilled(IKillable killed, KillZoneSide side)
    {
        Play(killed.Bounds.center, side);
    }

    /// <summary>Plays the effect on the view edge of the given side, at the point closest to worldPoint.</summary>
    public void Play(Vector3 worldPoint, KillZoneSide side)
    {
        if (settings == null || killZoneManager == null || !killZoneManager.HasView)
            return;

        Rect view = killZoneManager.ViewRect;
        Vector2 inward = InwardNormal(side);
        Vector2 edgePoint = ClampToEdge(worldPoint, side, view) + inward * settings.EdgeInset;

        Vector2 toCenter = view.center - edgePoint;
        Vector2 direction = toCenter.sqrMagnitude > 1e-6f ? toCenter.normalized : inward;
        direction = Vector2.Lerp(direction, inward, settings.InwardBias);
        direction = direction.sqrMagnitude > 1e-6f ? direction.normalized : inward;

        Vector3 origin = MoveTowardCamera(new Vector3(edgePoint.x, edgePoint.y, 0f), settings.ZOffset);

        activeEffects.Add(builder.Play(settings, origin, direction));
    }

    /// <summary>Point on the side's view edge, with the other axis clamped into the view.</summary>
    private static Vector2 ClampToEdge(Vector3 point, KillZoneSide side, Rect view)
    {
        switch (side)
        {
            case KillZoneSide.Left:
                return new Vector2(view.xMin, Mathf.Clamp(point.y, view.yMin, view.yMax));
            case KillZoneSide.Right:
                return new Vector2(view.xMax, Mathf.Clamp(point.y, view.yMin, view.yMax));
            default:
                return new Vector2(Mathf.Clamp(point.x, view.xMin, view.xMax), view.yMin);
        }
    }

    private static Vector2 InwardNormal(KillZoneSide side)
    {
        switch (side)
        {
            case KillZoneSide.Left:
                return Vector2.right;
            case KillZoneSide.Right:
                return Vector2.left;
            default:
                return Vector2.up;
        }
    }

    /// <summary>Moves the point along the line to the camera, so it keeps its screen position. Moves along -Z without a camera.</summary>
    private Vector3 MoveTowardCamera(Vector3 point, float distance)
    {
        Vector3 toCamera = viewCamera != null ? viewCamera.transform.position - point : Vector3.back;

        if (toCamera.sqrMagnitude < 1e-6f)
            return point;

        return point + toCamera.normalized * distance;
    }

    #endregion


    #region Cleanup

    private void Update()
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            if (!activeEffects[i].IsFinished)
                continue;

            activeEffects[i].Destroy();
            activeEffects.RemoveAt(i);
        }
    }

    #endregion
}
