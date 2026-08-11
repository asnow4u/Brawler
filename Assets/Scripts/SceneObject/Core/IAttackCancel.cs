using System;

public interface IAttackCancel
{
    public event Action<BufferedInput> PerformedAttackCancel;
}
