namespace Engine.Core;

// class or record?
public class BitChange
{
    public IBit Bit { get; }
    public bool NewValue { get; }
    /// <summary>IBit or string description</summary>
    public object Cause { get; }
    public DateTime Time { get; }
    public ExceptionHandler OnError { get; set; }
    public string Guid { get; set; }
    public BitChange(IBit bit, bool newValue, object cause = null, ExceptionHandler onError =null)
    {
        Debug.Assert(bit != null);
        Debug.Assert(cause is null || cause is IBit || cause is string);
        //Debug.Assert(bit.Value != newValue);

        Guid = System.Guid.NewGuid().ToString().Substring(0, 4);
        Bit = bit;
        NewValue = newValue;
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

    public override string ToString() => $"{Bit.GetName()}={Bit}={NewValue} by {CauseRepr}";
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
