using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MovementHandler))]
internal sealed class MovementHandlerEditor : Editor
{
    private const int MaxTrail = 150;       // ~2.5s of samples at 60fps
    private const float GraphHeight = 170f;

    private readonly List<Vector2> influenceTrail = new List<Vector2>();
    private readonly List<Vector2> velocityTrail = new List<Vector2>();
    private float velocityRange = 1f;        // auto-scaled to fit the trail

    private static readonly Color BgColor = new Color(0.12f, 0.12f, 0.13f);
    private static readonly Color AxisColor = new Color(1f, 1f, 1f, 0.20f);
    private static readonly Color GridColor = new Color(1f, 1f, 1f, 0.07f);
    private static readonly Color BorderColor = new Color(1f, 1f, 1f, 0.15f);
    private static readonly Color InfluenceColor = new Color(0.30f, 0.80f, 1f);
    private static readonly Color VelocityColor = new Color(1f, 0.75f, 0.25f);

    // Repaint every editor frame while playing so the graphs animate live.
    public override bool RequiresConstantRepaint() => Application.isPlaying;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Movement Debugger", EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode for live movement data.", MessageType.Info);
            influenceTrail.Clear();
            velocityTrail.Clear();
            return;
        }

        MovementHandler handler = (MovementHandler)target;
        Rigidbody rb = handler.GetComponent<Rigidbody>();

        serializedObject.Update();
        float h = GetFloat("horizontalInfluence");
        float v = GetFloat("verticalInfluence");
        float j = GetFloat("jumpInfluence");
        Vector2 vel = rb != null ? new Vector2(rb.linearVelocity.x, rb.linearVelocity.y) : Vector2.zero;

        // Sample once per frame (Layout pass runs once; avoids double-sampling on Repaint/input passes).
        if (Event.current.type == EventType.Layout)
        {
            Append(influenceTrail, new Vector2(h, v));
            Append(velocityTrail, vel);
        }

        // Numeric readouts
        EditorGUILayout.LabelField("State", handler.CurMovementState.ToString());
        EditorGUILayout.LabelField("Influence", string.Format("H {0:0.00}    V {1:0.00}    Jump {2:0.00}", h, v, j));
        EditorGUILayout.LabelField("Velocity", string.Format("X {0:0.00}    Y {1:0.00}    |{2:0.00}|", vel.x, vel.y, vel.magnitude));

        // Influence graph (fixed -1..1 range)
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Influence  ( X = H,  Y = V )", EditorStyles.miniBoldLabel);
        Rect infRect = GUILayoutUtility.GetRect(0, GraphHeight, GUILayout.ExpandWidth(true));
        DrawGraph(infRect, influenceTrail, new Vector2(h, v), 1.05f, InfluenceColor);

        // Velocity graph (auto-scaled)
        EditorGUILayout.Space(6);
        UpdateVelocityRange();
        EditorGUILayout.LabelField(string.Format("Velocity  ( X, Y )   scale \u00B1{0:0.0}", velocityRange), EditorStyles.miniBoldLabel);
        Rect velRect = GUILayoutUtility.GetRect(0, GraphHeight, GUILayout.ExpandWidth(true));
        DrawGraph(velRect, velocityTrail, vel, velocityRange, VelocityColor);
    }

    private float GetFloat(string prop)
    {
        SerializedProperty p = serializedObject.FindProperty(prop);
        return p != null ? p.floatValue : 0f;
    }

    private static void Append(List<Vector2> trail, Vector2 point)
    {
        trail.Add(point);
        if (trail.Count > MaxTrail)
            trail.RemoveAt(0);
    }

    // Grow instantly to fit the biggest sample; shrink slowly so the scale is stable to read.
    private void UpdateVelocityRange()
    {
        float max = 1f;
        for (int i = 0; i < velocityTrail.Count; i++)
            max = Mathf.Max(max, Mathf.Abs(velocityTrail[i].x), Mathf.Abs(velocityTrail[i].y));

        float target = max * 1.1f;
        velocityRange = target > velocityRange ? target : Mathf.Lerp(velocityRange, target, 0.05f);
    }

    private static void DrawGraph(Rect rect, List<Vector2> trail, Vector2 current, float range, Color color)
    {
        EditorGUI.DrawRect(rect, BgColor);

        // Quarter grid
        for (int i = 1; i < 4; i++)
        {
            float gx = rect.x + rect.width * i / 4f;
            float gy = rect.y + rect.height * i / 4f;
            EditorGUI.DrawRect(new Rect(gx, rect.y, 1f, rect.height), GridColor);
            EditorGUI.DrawRect(new Rect(rect.x, gy, rect.width, 1f), GridColor);
        }

        // Center axes
        float cx = rect.x + rect.width * 0.5f;
        float cy = rect.y + rect.height * 0.5f;
        EditorGUI.DrawRect(new Rect(cx, rect.y, 1f, rect.height), AxisColor);
        EditorGUI.DrawRect(new Rect(rect.x, cy, rect.width, 1f), AxisColor);

        // Trail (older = fainter)
        int count = trail.Count;
        for (int i = 0; i < count; i++)
        {
            Vector2 p = DataToPixel(trail[i], rect, range);
            Color c = color;
            c.a = (i + 1f) / count * 0.5f;
            EditorGUI.DrawRect(new Rect(p.x - 1.5f, p.y - 1.5f, 3f, 3f), c);
        }

        // Current point
        Vector2 cur = DataToPixel(current, rect, range);
        EditorGUI.DrawRect(new Rect(cur.x - 3.5f, cur.y - 3.5f, 7f, 7f), color);

        DrawBorder(rect, BorderColor);
    }

    private static Vector2 DataToPixel(Vector2 data, Rect rect, float range)
    {
        float nx = Mathf.Clamp(data.x / range, -1f, 1f);
        float ny = Mathf.Clamp(data.y / range, -1f, 1f);
        float px = rect.x + (nx * 0.5f + 0.5f) * rect.width;
        float py = rect.y + (1f - (ny * 0.5f + 0.5f)) * rect.height;
        return new Vector2(px, py);
    }

    private static void DrawBorder(Rect r, Color c)
    {
        EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, 1f), c);
        EditorGUI.DrawRect(new Rect(r.x, r.yMax - 1f, r.width, 1f), c);
        EditorGUI.DrawRect(new Rect(r.x, r.y, 1f, r.height), c);
        EditorGUI.DrawRect(new Rect(r.xMax - 1f, r.y, 1f, r.height), c);
    }
}
