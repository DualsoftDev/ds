using Engine.Core.Obsolete;

namespace Engine.Core;

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
    public virtual string QualifiedName { get; }
    public override string ToString() => ToText();
    public override string ToText() => $"{QualifiedName}[{this.GetType().Name}]";
    public virtual void Going() => throw new Exception("ERROR");
}

