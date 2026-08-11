using UnityEngine;

public enum BufferedInput
{
    Jump,
    Dash,
    Attack,
    SwapWeapon,
}

public static class InputSets
{
    //Everything that can claim an attack cancel once a hit lands.
    public static readonly BufferedInput[] AttackHitCancelInputs =
    {
        BufferedInput.Jump,
        BufferedInput.Dash,
        BufferedInput.SwapWeapon,
    };
}

public struct InputRecord
{
    public float Time;
    public float Value;
    public Vector2 Direction;
    public bool Consumed;
}

public interface IInputBuffer
{
    public float BufferWindow { get; }

    public void Buffer(BufferedInput input, float value = 0f, Vector2 direction = default);

    public bool Peek(BufferedInput input);
    
    public bool TryConsume(BufferedInput input);
    public bool TryConsume(BufferedInput input, out InputRecord record);

    public float LastPressTime(BufferedInput input);
    public bool TryGetNewestLive(BufferedInput[] inputs, out BufferedInput newest);

    public void Clear(BufferedInput input);
    public void ClearAll();
}
