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
        :base(name)
    {
    }


    bool IsChildrenStartPoint() => true;
    public virtual string QualifiedName { get; }
    public override string ToString() => ToText();
    public override string ToText() => $"{QualifiedName}[{this.GetType().Name}]";
    public virtual void Going() => throw new Exception("ERROR");

    //public virtual bool ChangeR()
    //{
    //    if (RGFH == Status4.Ready)
    //        return true;

    //    if (RGFH == Status4.Homing)
    //    {
    //        if (IsChildrenStartPoint())
    //        {
    //            RGFH = Status4.Ready;
    //            return true;
    //        }
    //    }
    //    return false;
    //}

    //public virtual bool ChangeG()
    //{
    //    if (RGFH == Status4.Going)
    //        return true;

    //    if (RGFH == Status4.Ready)
    //    {
    //        RGFH = Status4.Going;
    //        return true;
    //    }
    //    return false;
    //}

    //public virtual bool ChangeF()
    //{
    //    if (RGFH == Status4.Finished)
    //        return true;

    //    if (RGFH == Status4.Going)
    //    {
    //        RGFH = Status4.Finished;
    //        return true;
    //    }
    //    return false;
    //}

    //public virtual bool ChangeH()
    //{
    //    if (RGFH == Status4.Homing)
    //        return true;

    //    if (RGFH == Status4.Finished)
    //    {
    //        RGFH = Status4.Homing;
    //        return true;
    //    }
    //    return false;
    //}
}

