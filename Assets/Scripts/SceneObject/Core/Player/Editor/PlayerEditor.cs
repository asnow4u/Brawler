using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Player))]
internal sealed class PlayerEditor : Editor
{
    [Serializable]
    private enum TimelineActionType
    {
        MoveRight,
        MoveLeft,
        Jump,
        AttackLeft,
        AttackRight,
        AttackUp,
        AttackDown,
    }

    [Serializable]
    private struct TimelineAction
    {
        public int id;
        public TimelineActionType action;
        public float startTimeSeconds;
        public float durationSeconds;
    }

    [Serializable]
    private sealed class TimelineFile
    {
        public int version = 1;
        public List<TimelineAction> actions = new();
    }

    private sealed class PlayerEditorState : ScriptableObject
    {
        public bool debugMode;
        public List<TimelineAction> actions = new();
    }

    private const double AttackRepeatSeconds = 0.20;
    private const double OneShotJumpSeconds = 0.06;

    private PlayerEditorState state;
    private double timelineStartTime;
    private bool timelinePlaying;
    private readonly HashSet<int> firedInstantActionIds = new();

    private Vector2 manualMovement;
    private bool manualJumpHeld;
    private bool manualAttackLeftHeld;
    private bool manualAttackRightHeld;
    private bool manualAttackUpHeld;
    private bool manualAttackDownHeld;

    private readonly Dictionary<TimelineActionType, double> nextAttackAllowedTime = new();

    private int nextActionId = 1;
    private double oneShotJumpReleaseTimelineTime = -1;

    private void OnEnable()
    {
        state = CreateInstance<PlayerEditorState>();
        state.hideFlags = HideFlags.HideAndDontSave;

        EditorApplication.update += OnEditorUpdate;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

        if (Application.isPlaying)
            StopAndResetInputs();

        if (state != null)
            DestroyImmediate(state);
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Player Input Debugger", EditorStyles.boldLabel);

        bool newDebugMode = EditorGUILayout.Toggle("Debug Mode", state.debugMode);
        if (newDebugMode != state.debugMode)
        {
            state.debugMode = newDebugMode;

            if (!state.debugMode)
                StopAndResetInputs();
        }

        if (!state.debugMode)
            return;

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to use Player Input Debugger", MessageType.Error);
            return;
        }

        DrawManualControls();
        DrawTimelineControls();
    }

    private void DrawManualControls()
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Manual", EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(timelinePlaying))
        {
            EditorGUILayout.BeginHorizontal();
            bool moveLeftHeld = GUILayout.RepeatButton("Move Left");
            bool moveRightHeld = GUILayout.RepeatButton("Move Right");
            EditorGUILayout.EndHorizontal();

            Vector2 desiredMovement = Vector2.zero;
            if (moveLeftHeld && !moveRightHeld)
                desiredMovement = Vector2.left;
            else if (moveRightHeld && !moveLeftHeld)
                desiredMovement = Vector2.right;

            manualMovement = desiredMovement;

            manualJumpHeld = GUILayout.RepeatButton("Jump");

            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            manualAttackLeftHeld = GUILayout.RepeatButton("Attack Left");
            manualAttackRightHeld = GUILayout.RepeatButton("Attack Right");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            manualAttackUpHeld = GUILayout.RepeatButton("Attack Up");
            manualAttackDownHeld = GUILayout.RepeatButton("Attack Down");
            EditorGUILayout.EndHorizontal();

            if (timelinePlaying)
                EditorGUILayout.HelpBox("Manual controls are disabled while Timeline is playing.", MessageType.Info);
        }
    }

    private void DrawTimelineControls()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Timeline", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Add"))
            AddActionDefault();

        if (GUILayout.Button("Sort"))
            SortActionsByTime();

        if (GUILayout.Button("Clear"))
        {
            state.actions.Clear();
            firedInstantActionIds.Clear();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Load JSON"))
            LoadActionsFromJson();

        if (GUILayout.Button("Save JSON"))
            SaveActionsToJson();

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        using (new EditorGUI.DisabledScope(state.actions.Count == 0))
        {
            if (GUILayout.Button("Play"))
                PlayTimelineFromStart();
        }

        if (GUILayout.Button("Stop"))
            StopAndResetInputs();

        EditorGUILayout.EndHorizontal();

        if (timelinePlaying)
        {
            double t = Time.time - timelineStartTime;
            EditorGUILayout.LabelField("Time", t.ToString("0.000") + " sec");
        }

        EditorGUILayout.Space(4);
        DrawTimelineList();

        if (state.actions.Count == 0)
            EditorGUILayout.HelpBox("Add actions, then Play to run them in sequence.", MessageType.Info);
    }

    private void DrawTimelineList()
    {
        if (state.actions.Count > 0)
        {
            const float indexWidth = 36f;
            const float timeWidth = 72f;
            const float actionWidth = 120f;
            const float durationWidth = 72f;
            const float deleteWidth = 24f;

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(6);
                EditorGUILayout.LabelField("#", EditorStyles.miniLabel, GUILayout.Width(indexWidth - 6f));
                EditorGUILayout.LabelField("Time", EditorStyles.miniLabel, GUILayout.Width(timeWidth));
                EditorGUILayout.LabelField("Action", EditorStyles.miniLabel, GUILayout.Width(actionWidth));
                EditorGUILayout.LabelField("Dur", EditorStyles.miniLabel, GUILayout.Width(durationWidth));
                GUILayout.FlexibleSpace();
                GUILayout.Space(deleteWidth);
            }

            EditorGUILayout.Space(2);
        }

        for (int i = 0; i < state.actions.Count; i++)
        {
            TimelineAction a = state.actions[i];
            EnsureActionId(ref a);

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();

            const float indexWidth = 36f;
            const float timeWidth = 72f;
            const float actionWidth = 120f;
            const float durationWidth = 72f;
            const float deleteWidth = 24f;

            EditorGUILayout.LabelField("#" + (i + 1), GUILayout.Width(indexWidth));

            a.startTimeSeconds = Mathf.Max(0f, EditorGUILayout.FloatField(a.startTimeSeconds, GUILayout.Width(timeWidth)));
            a.action = (TimelineActionType)EditorGUILayout.EnumPopup(a.action, GUILayout.Width(actionWidth));
            a.durationSeconds = Mathf.Max(0f, EditorGUILayout.FloatField(a.durationSeconds, GUILayout.Width(durationWidth)));

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("X", GUILayout.Width(deleteWidth)))
            {
                state.actions.RemoveAt(i);
                firedInstantActionIds.Remove(a.id);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }

            EditorGUILayout.EndHorizontal();

            if (a.action == TimelineActionType.Jump && a.durationSeconds <= 0f)
                EditorGUILayout.HelpBox("Jump with Dur=0 will be treated as a very short tap.", MessageType.Info);

            if ((a.action == TimelineActionType.MoveLeft || a.action == TimelineActionType.MoveRight) && a.durationSeconds <= 0f)
                EditorGUILayout.HelpBox("Move with Dur=0 stays active until another Move action.", MessageType.Info);

            EditorGUILayout.EndVertical();

            state.actions[i] = a;
        }
    }

    private void AddActionDefault()
    {
        state.actions.Add(new TimelineAction
        {
            id = nextActionId++,
            action = TimelineActionType.MoveRight,
            startTimeSeconds = 0f,
            durationSeconds = 1f,
        });
    }

    private void SortActionsByTime()
    {
        state.actions.Sort((a, b) => a.startTimeSeconds.CompareTo(b.startTimeSeconds));
    }

    private void PlayTimelineFromStart()
    {
        StopAndResetInputs();
        timelinePlaying = true;
        timelineStartTime = Time.time;
        firedInstantActionIds.Clear();
        oneShotJumpReleaseTimelineTime = -1;
    }

    private void StopAndResetInputs()
    {
        timelinePlaying = false;
        firedInstantActionIds.Clear();
        oneShotJumpReleaseTimelineTime = -1;

        manualMovement = Vector2.zero;
        manualJumpHeld = false;
        manualAttackLeftHeld = false;
        manualAttackRightHeld = false;
        manualAttackUpHeld = false;
        manualAttackDownHeld = false;

        IMovementInputEditor movement = target as IMovementInputEditor;
        if (movement != null)
        {
            movement.DebugMovementInput(Vector2.zero);
            movement.DebugJumpInput(0f);
        }
    }

    private void OnPlayModeStateChanged(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.ExitingPlayMode || change == PlayModeStateChange.EnteredEditMode)
            StopAndResetInputs();
    }

    private void OnEditorUpdate()
    {
        if (state == null || !state.debugMode)
            return;

        if (!Application.isPlaying)
        {
            if (timelinePlaying)
                StopAndResetInputs();

            return;
        }

        if (target == null)
            return;

        IMovementInputEditor movement = target as IMovementInputEditor;
        IAttackInputEditor attack = target as IAttackInputEditor;
        if (movement == null || attack == null)
            return;

        if (timelinePlaying)
            ApplyTimeline(movement, attack);
        else
            ApplyManual(movement, attack);

        Repaint();
    }

    private void ApplyManual(IMovementInputEditor movement, IAttackInputEditor attack)
    {
        movement.DebugMovementInput(manualMovement);
        movement.DebugJumpInput(manualJumpHeld ? 1f : 0f);

        if (manualAttackLeftHeld)
            TryFireAttack(attack, TimelineActionType.AttackLeft);
        if (manualAttackRightHeld)
            TryFireAttack(attack, TimelineActionType.AttackRight);
        if (manualAttackUpHeld)
            TryFireAttack(attack, TimelineActionType.AttackUp);
        if (manualAttackDownHeld)
            TryFireAttack(attack, TimelineActionType.AttackDown);
    }

    private void ApplyTimeline(IMovementInputEditor movement, IAttackInputEditor attack)
    {
        double t = Time.time - timelineStartTime;

        Vector2 moveHeldVector = GetTimelineMovementVector(t);
        bool jumpHeld = GetTimelineJumpHeld(t);

        movement.DebugMovementInput(moveHeldVector);
        movement.DebugJumpInput(jumpHeld ? 1f : 0f);

        for (int i = 0; i < state.actions.Count; i++)
        {
            TimelineAction a = state.actions[i];
            EnsureActionId(ref a);
            state.actions[i] = a;

            if (a.durationSeconds > 0f)
                continue;

            if (t < a.startTimeSeconds)
                continue;

            if (!firedInstantActionIds.Add(a.id))
                continue;

            FireInstantAction(attack, a, t);
        }

        if (oneShotJumpReleaseTimelineTime >= 0 && t >= oneShotJumpReleaseTimelineTime)
            oneShotJumpReleaseTimelineTime = -1;

        double timelineEndSeconds = GetTimelineEndSeconds();
        if (oneShotJumpReleaseTimelineTime >= 0)
            timelineEndSeconds = Math.Max(timelineEndSeconds, oneShotJumpReleaseTimelineTime);

        if (state.actions.Count > 0 && t >= timelineEndSeconds)
            StopAndResetInputs();
    }

    private double GetTimelineEndSeconds()
    {
        if (state.actions.Count == 0)
            return 0;

        double end = 0;

        for (int i = 0; i < state.actions.Count; i++)
        {
            TimelineAction a = state.actions[i];

            double actionEnd = a.startTimeSeconds;
            switch (a.action)
            {
                case TimelineActionType.Jump:
                    actionEnd += a.durationSeconds > 0f ? a.durationSeconds : OneShotJumpSeconds;
                    break;
                case TimelineActionType.MoveLeft:
                case TimelineActionType.MoveRight:
                    actionEnd += Mathf.Max(0f, a.durationSeconds);
                    break;
                default:
                    break;
            }

            end = Math.Max(end, actionEnd);
        }

        return end;
    }

    private Vector2 GetTimelineMovementVector(double t)
    {
        float bestStickyTime = float.NegativeInfinity;
        Vector2 stickyMove = Vector2.zero;

        float bestHeldTime = float.NegativeInfinity;
        Vector2 heldMove = Vector2.zero;

        for (int i = 0; i < state.actions.Count; i++)
        {
            TimelineAction a = state.actions[i];

            if (a.action != TimelineActionType.MoveLeft && a.action != TimelineActionType.MoveRight)
                continue;

            Vector2 dir = a.action == TimelineActionType.MoveLeft ? Vector2.left : Vector2.right;

            if (a.durationSeconds <= 0f)
            {
                if (t >= a.startTimeSeconds && a.startTimeSeconds >= bestStickyTime)
                {
                    bestStickyTime = a.startTimeSeconds;
                    stickyMove = dir;
                }
            }
            else
            {
                if (t >= a.startTimeSeconds && t < (double)a.startTimeSeconds + a.durationSeconds && a.startTimeSeconds >= bestHeldTime)
                {
                    bestHeldTime = a.startTimeSeconds;
                    heldMove = dir;
                }
            }
        }

        if (!float.IsNegativeInfinity(bestHeldTime))
            return heldMove;

        if (!float.IsNegativeInfinity(bestStickyTime))
            return stickyMove;

        return Vector2.zero;
    }

    private bool GetTimelineJumpHeld(double t)
    {
        bool heldByDuration = false;

        for (int i = 0; i < state.actions.Count; i++)
        {
            TimelineAction a = state.actions[i];
            if (a.action != TimelineActionType.Jump)
                continue;

            if (a.durationSeconds > 0f)
            {
                if (t >= a.startTimeSeconds && t < (double)a.startTimeSeconds + a.durationSeconds)
                    heldByDuration = true;
            }
            else
            {
                if (t >= a.startTimeSeconds && firedInstantActionIds.Contains(a.id))
                    continue;

                if (t >= a.startTimeSeconds && firedInstantActionIds.Add(a.id))
                {
                    oneShotJumpReleaseTimelineTime = t + OneShotJumpSeconds;
                    heldByDuration = true;
                }
            }
        }

        if (oneShotJumpReleaseTimelineTime >= 0)
            heldByDuration |= t < oneShotJumpReleaseTimelineTime;

        return heldByDuration;
    }

    private void FireInstantAction(IAttackInputEditor attack, TimelineAction a, double t)
    {
        switch (a.action)
        {
            case TimelineActionType.AttackLeft:
            case TimelineActionType.AttackRight:
            case TimelineActionType.AttackUp:
            case TimelineActionType.AttackDown:
                TryFireAttack(attack, a.action, allowRateLimit: false);
                break;
            case TimelineActionType.Jump:
            case TimelineActionType.MoveLeft:
            case TimelineActionType.MoveRight:
                break;
            default:
                break;
        }
    }

    private void TryFireAttack(IAttackInputEditor attack, TimelineActionType type, bool allowRateLimit = true)
    {
        double now = Time.time;

        if (allowRateLimit && nextAttackAllowedTime.TryGetValue(type, out double nextAllowed) && now < nextAllowed)
            return;

        switch (type)
        {
            case TimelineActionType.AttackLeft:
                attack.DebugAttackInput(new Vector2(-1, 0));
                break;
            case TimelineActionType.AttackRight:
                attack.DebugAttackInput(new Vector2(1, 0));
                break;
            case TimelineActionType.AttackUp:
                attack.DebugAttackInput(new Vector2(0, 1));
                break;
            case TimelineActionType.AttackDown:
                attack.DebugAttackInput(new Vector2(0, -1));
                break;
            default:
                return;
        }

        nextAttackAllowedTime[type] = now + AttackRepeatSeconds;
    }

    private void SaveActionsToJson()
    {
        string defaultDir = GetDefaultJsonDirectory();
        Directory.CreateDirectory(defaultDir);

        string path = EditorUtility.SaveFilePanel("Save Player Timeline", defaultDir, "player_timeline", "json");
        if (string.IsNullOrWhiteSpace(path))
            return;

        TimelineFile file = new()
        {
            version = 1,
            actions = new List<TimelineAction>(state.actions),
        };

        string json = JsonUtility.ToJson(file, prettyPrint: true);
        File.WriteAllText(path, json);
        AssetDatabase.Refresh();
    }

    private void LoadActionsFromJson()
    {
        string defaultDir = GetDefaultJsonDirectory();
        Directory.CreateDirectory(defaultDir);

        string path = EditorUtility.OpenFilePanel("Load Player Timeline", defaultDir, "json");
        if (string.IsNullOrWhiteSpace(path))
            return;

        string json = File.ReadAllText(path);
        TimelineFile file = JsonUtility.FromJson<TimelineFile>(json);
        if (file == null || file.actions == null)
        {
            Debug.LogError("Failed to parse timeline JSON: " + path);
            return;
        }

        state.actions = new List<TimelineAction>(file.actions);
        firedInstantActionIds.Clear();
        RecomputeNextActionId();
    }

    private static string GetDefaultJsonDirectory()
    {
        return Path.Combine(Application.dataPath, "Scripts", "SceneObject", "Core", "Player", "Editor", "Json");
    }

    private void EnsureActionId(ref TimelineAction a)
    {
        if (a.id != 0)
            return;

        a.id = nextActionId++;
    }

    private void RecomputeNextActionId()
    {
        int maxId = 0;
        for (int i = 0; i < state.actions.Count; i++)
            maxId = Mathf.Max(maxId, state.actions[i].id);

        nextActionId = Mathf.Max(1, maxId + 1);
    }
}
