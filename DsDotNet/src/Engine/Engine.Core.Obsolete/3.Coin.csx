namespace Engine.Core.Obsolete;

/// <summary> Segment Or Call base </summary>
[DebuggerDisplay("{ToText()}")]
public abstract class Coin : Named, ICoin
{
    public virtual bool Value { get; set; }
    public virtual bool Evaluate() => Value;

    /*
     * Do not store Paused property (getter only, no setter)
     */
    public virtual bool Paused { get; }
    public virtual Cpu Cpu { get; set; }

    public Coin(string name)
        : base(name)
    {
    }


    bool IsChildrenStartPoint() => true;

    public virtual string[] NameComponents { get; }
    public virtual string QualifiedName => NameComponents.Combine();
    public override string ToString() => ToText();
    public override string ToText() => $"{QualifiedName}[{this.GetType().Name}]";
    public virtual void Going() => throw new Exception("ERROR");
}

