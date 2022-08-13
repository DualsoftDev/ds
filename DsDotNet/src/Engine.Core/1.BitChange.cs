namespace Engine.Core;

// class or record?
public class BitChange
{
    public IBit Bit { get; }
    public bool NewValue { get; }
    /// <summary>IBit or string description</summary>
    public object Cause { get; }
    public bool Applied { get; internal set; }
    public DateTime Time { get; }
    public ExceptionHandler OnError { get; set; }
    public BitChange(IBit bit, bool newValue, object cause = null, ExceptionHandler onError =null, bool applied = false)
    {
        Debug.Assert(cause is null || cause is IBit || cause is string);
        Bit = bit;
        NewValue = newValue;
        Applied = applied;
        Time = DateTime.Now;
        Cause = cause;
        OnError = onError;
        if (cause == null)
            Console.WriteLine();
    }

    public string CauseRepr => Cause switch
    {
        IBit b => $"{b.GetName()}={b.Value}",
        string s => s,
        null => null,
        _ => throw new Exception("ERROR"),
    };

    public override string ToString() => $"{Bit.GetName()}={Bit}={NewValue}";
}



public record OpcTagChange
{
    public string TagName { get; }
    public bool Value { get; }
    public OpcTagChange(string tagName, bool value)
    {
        TagName = tagName;
        Value = value;
    }
}
