namespace Engine.Base.Obsolete;


[DebuggerDisplay("{ToText()}")]
public class Named : INamed
{
    public string Name { get; set; }

    public Named(string name)
    {
        Name = name;
    }
    public virtual string ToText() => $"{Name}[{GetType().Name}]";
    public override string ToString() => ToText();
}

