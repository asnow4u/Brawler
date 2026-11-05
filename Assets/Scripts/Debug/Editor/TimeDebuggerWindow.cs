using UnityEditor;
using UnityEngine;

/// <summary>Editor window that drives TimeDebugController: freeze, resume, step and slow motion.</summary>
internal sealed class TimeDebuggerWindow : EditorWindow
{
    private TimeStepUnit stepUnit = TimeStepUnit.PhysicsTick;
    private float frameStepFps = 60f;
    private float slowMotionScale = 1f;
    private int multiStepCount = 5;

    [MenuItem("Window/Time Debugger")]
    private static void Open()
    {
        GetWindow<TimeDebuggerWindow>("Time Debugger");
    }

    private void OnEnable()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    private void OnPlayModeStateChanged(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.ExitingPlayMode && TimeDebugController.Instance != null)
            TimeDebugController.Instance.Resume();

        Repaint();
    }

    private void OnInspectorUpdate()
    {
        if (Application.isPlaying)
            Repaint();
    }

    private void OnGUI()
    {
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to use the Time Debugger.", MessageType.Info);
            return;
        }

        HandleKeys();

        TimeDebugController controller = TimeDebugController.Instance;
        bool isFrozen = controller != null && controller.IsFrozen;

        DrawStatus(controller, isFrozen);
        DrawTimeControls(isFrozen);
        DrawStepSettings();
        DrawSlowMotion();
        DrawReadout(controller, isFrozen);
    }


    #region Drawing

    private void DrawStatus(TimeDebugController controller, bool isFrozen)
    {
        string status = !isFrozen ? "Running" : controller.IsStepping ? "Stepping" : "Frozen";
        EditorGUILayout.LabelField("Status", status, EditorStyles.boldLabel);
    }

    private void DrawTimeControls(bool isFrozen)
    {
        EditorGUILayout.Space(4);

        if (GUILayout.Button(isFrozen ? "Resume  [F]" : "Freeze  [F]", GUILayout.Height(28)))
            ToggleFreeze();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Step  [→ / .]", GUILayout.Height(24)))
            Step(1);

        multiStepCount = Mathf.Max(1, EditorGUILayout.IntField(multiStepCount, GUILayout.Width(40), GUILayout.Height(24)));

        if (GUILayout.Button("Step ×" + multiStepCount, GUILayout.Height(24)))
            Step(multiStepCount);

        EditorGUILayout.EndHorizontal();
    }

    private void DrawStepSettings()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Step", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();

        stepUnit = (TimeStepUnit)EditorGUILayout.EnumPopup("Unit", stepUnit);

        if (stepUnit == TimeStepUnit.PhysicsTick)
            EditorGUILayout.LabelField("Step Size", Time.fixedDeltaTime.ToString("0.0000") + " s  (" + Mathf.RoundToInt(1f / Time.fixedDeltaTime) + " Hz)");
        else
            frameStepFps = Mathf.Max(1f, EditorGUILayout.FloatField("FPS", frameStepFps));

        if (EditorGUI.EndChangeCheck())
            ApplySettings();
    }

    private void DrawSlowMotion()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Slow Motion", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        slowMotionScale = EditorGUILayout.Slider("Time Scale", slowMotionScale, 0.05f, 1f);

        if (EditorGUI.EndChangeCheck())
            TimeDebugController.GetOrCreate().SlowMotionScale = slowMotionScale;
    }

    private void DrawReadout(TimeDebugController controller, bool isFrozen)
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Readout", EditorStyles.boldLabel);

        EditorGUILayout.LabelField("Game Time", Time.time.ToString("0.000") + " s");

        if (!isFrozen)
            return;

        EditorGUILayout.LabelField("Steps Since Freeze", controller.StepsSinceFreeze.ToString());
        EditorGUILayout.LabelField("Time Since Freeze", controller.GameTimeSinceFreeze.ToString("0.000") + " s");
        EditorGUILayout.LabelField("FixedUpdates Last Step", controller.FixedUpdatesInLastStep.ToString());

        if (controller.FixedUpdatesInLastStep > 1)
            EditorGUILayout.HelpBox("The last step ran more than one FixedUpdate.", MessageType.Warning);
    }

    #endregion


    #region Actions

    private void HandleKeys()
    {
        Event e = Event.current;

        if (e.type != EventType.KeyDown || GUIUtility.keyboardControl != 0)
            return;

        switch (e.keyCode)
        {
            case KeyCode.F:
                ToggleFreeze();
                e.Use();
                break;

            case KeyCode.RightArrow:
            case KeyCode.Period:
                Step(1);
                e.Use();
                break;
        }
    }

    private void ToggleFreeze()
    {
        TimeDebugController controller = ApplySettings();

        if (controller.IsFrozen)
            controller.Resume();
        else
            controller.Freeze();
    }

    private void Step(int count)
    {
        ApplySettings().Step(count);
    }

    private TimeDebugController ApplySettings()
    {
        TimeDebugController controller = TimeDebugController.GetOrCreate();
        controller.StepUnit = stepUnit;
        controller.FrameStepFps = frameStepFps;
        controller.SlowMotionScale = slowMotionScale;
        return controller;
    }

    #endregion
}
