namespace Engine.Core;

public class Model
{
    public List<DsSystem> Systems = new();
    public List<CpuBase> Cpus { get; } = new();
}

public static class ModelExtension
{
    public static T FindObject<T>(this Model model, string qualifiedName) where T : class
    {
        var tokens = qualifiedName.Split(new[] { '.' });
        var n = tokens.Length;
        var sys = model.Systems.FirstOrDefault(s => s.Name == tokens[0]);
        if (n == 1 || sys == null)
            return sys as T;

        var task = sys.Tasks.FirstOrDefault(t => t.Name == tokens[1]);
        if (task != null)
        {
            if (n == 2)
                return task as T;
            var callProto = task.CallPrototypes.FirstOrDefault(c => c.Name == tokens[2]);
            Debug.Assert(n == 3);
            return callProto as T;
        }

        var flow = sys.RootFlows.FirstOrDefault(f => f.Name == tokens[1]);
        if (flow != null)
        {
            if (n == 2)
                return flow as T;

            var unit =
                flow.ChildVertices.FirstOrDefault(v => v switch
                {
                    RootCall call => call.Name  == tokens[2],
                    Segment seg   => seg.Name   == tokens[2],
                    Child child   => child.Name == tokens[2],
                    _             => throw new Exception("ERROR"),
                });

            if (n == 3)
                return unit as T;

            var grandsonName = String.Join(".", tokens.Skip(3));
            return unit switch
            {
                Segment seg => seg.Children.FirstOrDefault(grandson => grandson.Name == grandsonName) as T,
                _ => throw new Exception("ERROR"),
            };
        }

        return null;
    }
}

[DebuggerDisplay("{ToText()}")]
public class Named: INamed
{
    public string Name { get; set; }

    public Named(string name)
    {
        Name = name;
    }
    public virtual string ToText() => $"{Name}[{this.GetType().Name}]";
    //public override string ToString() => Name;
}


public class DsSystem : Named
{
    public Model Model;
    public List<RootFlow> RootFlows = new();
    public List<DsTask> Tasks = new();
    public DsSystem(string name, Model model)
        : base(name)
    {
        Model = model;
        model.Systems.Add(this);
    }
}


[DebuggerDisplay("{ToText()}")]
public class DsTask : Named
{
    public DsSystem System;
    public List<CallPrototype> CallPrototypes = new();

    public DsTask(string name, DsSystem system)
        : base(name)
    {
        System = system;
        system.Tasks.Add(this);
    }

    public string QualifiedName => $"{System.Name}_{Name}";
    public override string ToText() => $"{QualifiedName}[{this.GetType().Name}], #call proto={CallPrototypes.Count}";
}


/// <summary> Segment Or Call base </summary>
[DebuggerDisplay("{ToText()}")]
public abstract class Coin : Named, ICoin
{
    public virtual bool Value { get; set; }

    /*
     * Do not store Paused property (getter only, no setter)
     */
    public virtual bool Paused { get; }
    public virtual CpuBase OwnerCpu { get; set; }

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

