using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AnimationHandler))]
internal sealed class AnimationHandlerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        IAnimationEditor handler = (IAnimationEditor)target;

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Animation Debugger", EditorStyles.boldLabel);

        DrawDebugToggle(handler);

        if (!handler.DebugMode)
            return;

        bool inPlayMode = Application.isPlaying;
        if (!inPlayMode)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to use Animation Debugger", MessageType.Error);
            return;
        }

        DrawAttackStateDropdown(handler);

        if (handler.DebugAttackState == AttackState.Null)
        {
            EditorGUILayout.HelpBox("Select an Attack State to debug", MessageType.Info);
            return;
        }

        if (handler.CurrentDebugAnimationClip == null)
        {
            EditorGUILayout.HelpBox("Animation dosent exist for " + handler.DebugAttackState, MessageType.Warning);
            return;
        }

        DrawPlaybackControls(handler);
        DrawFrameControls(handler);
        DrawDebugInfo(handler);
    }

    private static void DrawDebugToggle(IAnimationEditor handler)
    {
        bool newDebugMode = EditorGUILayout.Toggle("Debug Mode", handler.DebugMode);
        if (newDebugMode == handler.DebugMode)
            return;

        handler.SetDebugMode(newDebugMode);
    }

    private static void DrawAttackStateDropdown(IAnimationEditor handler)
    {
        AttackState newState = (AttackState)EditorGUILayout.EnumPopup("Attack State", handler.DebugAttackState);
        if (newState == handler.DebugAttackState)
            return;

        handler.SetDebugAttackState(newState);
    }

    private static void DrawPlaybackControls(IAnimationEditor handler)
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Play"))
            handler.PlayDebugAnimation();

        if (GUILayout.Button("Pause"))
            handler.PauseDebugAnimation();

        EditorGUILayout.EndHorizontal();
    }

    private static void DrawFrameControls(IAnimationEditor handler)
    {
        EditorGUILayout.Space(4);
        
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("<<"))
            handler.JumpDebugAnimationToStart();

        if (GUILayout.Button("<"))
            handler.StepDebugAnimationBackward();

        if (GUILayout.Button(">"))
            handler.StepDebugAnimationForward();

        if (GUILayout.Button(">>"))
            handler.JumpDebugAnimationToEnd();

        EditorGUILayout.EndHorizontal();
    }

    private static void DrawDebugInfo(IAnimationEditor handler)
    {
        EditorGUILayout.Space(6);

        int currentFrame = Mathf.FloorToInt(handler.CurrentDebugAnimationTime * handler.CurrentDebugAnimationClip.frameRate);

        EditorGUILayout.LabelField("Current Time", handler.CurrentDebugAnimationTime.ToString("0.000"));
        EditorGUILayout.LabelField("Current Frame", currentFrame.ToString());
    }
}

