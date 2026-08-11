using System;
using UnityEngine;

public class InputBuffer : IInputBuffer
{    
    private readonly InputRecord[] records;    

    private readonly float bufferWindow;
    public float BufferWindow => bufferWindow;

    public InputBuffer(float bufferWindow)
    {
        this.bufferWindow = bufferWindow;

        records = new InputRecord[Enum.GetValues(typeof(BufferedInput)).Length];

        for (int i = 0; i < records.Length; i++)
        {
            records[i].Time = -1000f;
            records[i].Consumed = true;
        }
    }

    //A newer press replaces whatever was held. The old one is gone whether it was consumed or not.
    public void Buffer(BufferedInput input, float value = 0f, Vector2 direction = default)
    {
        int index = (int)input;

        records[index].Time = Time.time;
        records[index].Value = value;
        records[index].Direction = direction;
        records[index].Consumed = false;
    }

    public bool Peek(BufferedInput input)
    {
        return IsLive((int)input);
    }

    public bool TryConsume(BufferedInput input)
    {
        return TryConsume(input, out _);
    }

    public bool TryConsume(BufferedInput input, out InputRecord record)
    {
        int index = (int)input;
        record = records[index];

        if (!IsLive(index))
            return false;
            
        records[index].Consumed = true;
        return true;
    }

    public float LastPressTime(BufferedInput input)
    {
        return records[(int)input].Time;
    }

    public bool TryGetNewestLive(BufferedInput[] inputs, out BufferedInput newest)
    {
        newest = default;
        float newestTime = float.NegativeInfinity;
        bool found = false;

        for (int i = 0; i < inputs.Length; i++)
        {
            int index = (int)inputs[i];

            if (!IsLive(index) || records[index].Time <= newestTime)
                continue;

            newest = inputs[i];
            newestTime = records[index].Time;
            found = true;
        }

        return found;
    }

    public void Clear(BufferedInput input)
    {
        records[(int)input].Consumed = true;
    }

    public void ClearAll()
    {
        for (int i = 0; i < records.Length; i++)
            records[i].Consumed = true;
    }

    private bool IsLive(int index)
    {
        return !records[index].Consumed &&
               Time.time - records[index].Time <= bufferWindow;
    }
}
