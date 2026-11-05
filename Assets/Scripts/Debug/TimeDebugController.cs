using UnityEngine;

public enum TimeStepUnit { PhysicsTick, Frame }

/// <summary>Freezes game time and advances it in exact steps. Restores normal time when destroyed.</summary>
[DefaultExecutionOrder(-32000)]
public class TimeDebugController : MonoBehaviour
{
    private static TimeDebugController instance;
    public static TimeDebugController Instance => instance;

    private bool isFrozen = false;
    private TimeStepUnit stepUnit = TimeStepUnit.PhysicsTick;
    private float frameStepFps = 60f;
    private float slowMotionScale = 1f;

    private int pendingSteps = 0;
    private int fixedUpdatesThisFrame = 0;
    private int fixedUpdatesInLastStep = 0;
    private int stepsSinceFreeze = 0;
    private float gameTimeAtFreeze = 0f;

    public bool IsFrozen => isFrozen;
    public bool IsStepping => pendingSteps > 0;
    public int StepsSinceFreeze => stepsSinceFreeze;
    public int FixedUpdatesInLastStep => fixedUpdatesInLastStep;
    public float GameTimeSinceFreeze => isFrozen ? Time.time - gameTimeAtFreeze : 0f;

    public TimeStepUnit StepUnit
    {
        get => stepUnit;
        set => stepUnit = value;
    }

    public float FrameStepFps
    {
        get => frameStepFps;
        set => frameStepFps = Mathf.Max(1f, value);
    }

    /// <summary>Time scale used while not frozen. Applied immediately when not frozen.</summary>
    public float SlowMotionScale
    {
        get => slowMotionScale;
        set
        {
            slowMotionScale = Mathf.Clamp(value, 0.01f, 1f);

            if (!isFrozen)
                Time.timeScale = slowMotionScale;
        }
    }

    private float StepDeltaTime => stepUnit == TimeStepUnit.PhysicsTick ? Time.fixedDeltaTime : 1f / frameStepFps;


    public static TimeDebugController GetOrCreate()
    {
        if (instance != null)
            return instance;

        GameObject controllerObject = new GameObject("TimeDebugController");
        controllerObject.hideFlags = HideFlags.HideInHierarchy;
        DontDestroyOnLoad(controllerObject);

        instance = controllerObject.AddComponent<TimeDebugController>();
        return instance;
    }

    private void OnDestroy()
    {
        if (instance != this)
            return;

        Time.captureDeltaTime = 0f;
        Time.timeScale = 1f;
        instance = null;
    }


    public void Freeze()
    {
        if (isFrozen)
            return;

        isFrozen = true;
        pendingSteps = 0;
        stepsSinceFreeze = 0;
        fixedUpdatesInLastStep = 0;
        gameTimeAtFreeze = Time.time;

        Time.timeScale = 0f;
        Time.captureDeltaTime = StepDeltaTime;
    }

    public void Resume()
    {
        if (!isFrozen)
            return;

        isFrozen = false;
        pendingSteps = 0;

        Time.captureDeltaTime = 0f;
        Time.timeScale = slowMotionScale;
    }

    /// <summary>Advances game time by the given number of steps, freezing first if needed.</summary>
    public void Step(int count = 1)
    {
        if (count <= 0)
            return;

        if (!isFrozen)
            Freeze();

        pendingSteps += count;
        Time.captureDeltaTime = StepDeltaTime;
        Time.timeScale = 1f;
    }


    private void FixedUpdate()
    {
        fixedUpdatesThisFrame++;
    }

    /// <summary>Completes a step after the frame has run. A PhysicsTick step completes once a FixedUpdate has run.</summary>
    private void LateUpdate()
    {
        if (isFrozen && pendingSteps > 0 && Time.deltaTime > 0f)
        {
            bool stepComplete = stepUnit == TimeStepUnit.Frame || fixedUpdatesThisFrame > 0;

            if (stepComplete)
            {
                fixedUpdatesInLastStep = fixedUpdatesThisFrame;
                pendingSteps--;
                stepsSinceFreeze++;

                if (pendingSteps == 0)
                    Time.timeScale = 0f;
            }
        }

        fixedUpdatesThisFrame = 0;
    }
}
