using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(HurtBoxHandler))]
internal sealed class HurtBoxHandlerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        Object unityObject = target;
        IHurtBoxHandlerEditor handler = (IHurtBoxHandlerEditor)target;

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Hurtbox Debugger", EditorStyles.boldLabel);

        DrawDebugToggle(unityObject, handler);

        if (!handler.DebugMode)
            return;

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to use Hurtbox Debugger", MessageType.Error);
            return;
        }

        DrawDebugFields(unityObject, handler);
        DrawApplyDamage(handler);
    }

    private static void DrawDebugToggle(Object unityObject, IHurtBoxHandlerEditor handler)
    {
        bool newDebugMode = EditorGUILayout.Toggle("Debug Mode", handler.DebugMode);
        if (newDebugMode == handler.DebugMode)
            return;

        Undo.RecordObject(unityObject, "Toggle Hurtbox Debug Mode");
        handler.SetDebugMode(newDebugMode);
        EditorUtility.SetDirty(unityObject);
        SceneView.RepaintAll();
    }

    private static void DrawDebugFields(Object unityObject, IHurtBoxHandlerEditor handler)
    {
        float newBaseForce = EditorGUILayout.FloatField("Base Force", handler.DebugBaseForce);
        if (!Mathf.Approximately(newBaseForce, handler.DebugBaseForce))
        {
            Undo.RecordObject(unityObject, "Change Hurtbox Base Force");
            handler.SetDebugBaseForce(newBaseForce);
            EditorUtility.SetDirty(unityObject);
        }
            
        float newInfluence = EditorGUILayout.Slider("Influence", handler.DebugInfluence, 0f, 1f);
        if (!Mathf.Approximately(newInfluence, handler.DebugInfluence))
        {
            Undo.RecordObject(unityObject, "Change Hurtbox Influence");
            handler.SetDebugInfluence(newInfluence);
            EditorUtility.SetDirty(unityObject);
        }

        float newLaunchAngle = EditorGUILayout.Slider("Launch Angle", handler.DebugLaunchAngle, 0f, 360f);
        if (!Mathf.Approximately(newLaunchAngle, handler.DebugLaunchAngle))
        {
            Undo.RecordObject(unityObject, "Change Hurtbox Launch Angle");
            handler.SetDebugLaunchAngle(newLaunchAngle);
            EditorUtility.SetDirty(unityObject);
            SceneView.RepaintAll();
        }

        EditorGUILayout.HelpBox("0\u00B0 = Right, 90\u00B0 = Up, 180\u00B0 = Left, 270\u00B0 = Down", MessageType.None);

        float newDamage = EditorGUILayout.FloatField("Damage", handler.DebugDamage);
        if (!Mathf.Approximately(newDamage, handler.DebugDamage))
        {
            Undo.RecordObject(unityObject, "Change Hurtbox Damage");
            handler.SetDebugDamage(newDamage);
            EditorUtility.SetDirty(unityObject);
        }

        float newDelaySeconds = EditorGUILayout.FloatField("Delay (s)", handler.DebugDelaySeconds);
        newDelaySeconds = Mathf.Max(0f, newDelaySeconds);
        if (!Mathf.Approximately(newDelaySeconds, handler.DebugDelaySeconds))
        {
            Undo.RecordObject(unityObject, "Change Hurtbox Delay");
            handler.SetDebugDelaySeconds(newDelaySeconds);
            EditorUtility.SetDirty(unityObject);
        }
    }

    private static void DrawApplyDamage(IHurtBoxHandlerEditor handler)
    {
        EditorGUILayout.Space(4);

        bool inPlayMode = Application.isPlaying;
        using (new EditorGUI.DisabledScope(!inPlayMode))
        {
            if (GUILayout.Button("Apply Damage"))
                handler.ApplyDebugDamage();
        }

        if (!inPlayMode)
            EditorGUILayout.HelpBox("Enter Play Mode to apply damage.", MessageType.Info);
    }

    private void OnSceneGUI()
    {
        if (!Application.isPlaying)
            return;

        if (targets.Length != 1)
            return;

        HurtBoxHandler hurtBoxHandler = (HurtBoxHandler)target;
        IHurtBoxHandlerEditor handler = (IHurtBoxHandlerEditor)target;

        if (!handler.DebugMode)
            return;

        Bounds bounds = GetBestBounds(hurtBoxHandler);
        Vector3 start = bounds.center;

        float length = Mathf.Max(bounds.extents.magnitude * 1.5f, 1f);
        float angle = Mathf.Clamp(handler.DebugLaunchAngle, 0f, 360f);

        Vector3 direction = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0f);
        Vector3 end = start + direction.normalized * length;

        Handles.color = new Color(1f, 0.6f, 0.15f, 1f);
        Handles.DrawAAPolyLine(3f, start, end);

        float capSize = Mathf.Clamp(length * 0.15f, 0.15f, 1.25f);
        Handles.ConeHandleCap(0, end, Quaternion.LookRotation(direction, Vector3.forward), capSize, EventType.Repaint);
    }

    private static Bounds GetBestBounds(HurtBoxHandler handler)
    {
        ISceneObject sceneObject = handler.GetComponent<ISceneObject>();
        if (sceneObject != null)
            return sceneObject.Bounds;

        Collider collider = handler.GetComponentInChildren<Collider>();
        if (collider != null)
            return collider.bounds;

        Renderer renderer = handler.GetComponentInChildren<Renderer>();
        if (renderer != null)
            return renderer.bounds;

        return new Bounds(handler.transform.position, Vector3.one);
    }
}
