using UnityEditor.PackageManager.UI;
using UnityEngine;

public class ActionBuffer<T>
{
    private T bufferedValue;
    private float expireWindow = 0;
    private float expireTime = -1f;
    private bool hasValue = false;

    public bool HasBuffered => hasValue && Time.time <= expireTime;

    public ActionBuffer(float expireWindow)
    {
        this.expireWindow = expireWindow;
    }

    public void Buffer(T value)
    {
        bufferedValue = value;
        expireTime = Time.time + expireWindow;
        hasValue = true;
    }

    public bool TryConsume(out T value)
    {
        if (hasValue && Time.time <= expireTime)
        {
            value = bufferedValue;
            Clear();
            return true;
        }

        value = default;
        Clear();
        return false;
    }

    public void Clear()
    {
        hasValue = false;
        bufferedValue = default;
        expireTime = -1f;
    }
}
