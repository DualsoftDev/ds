using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Engine.Core
{
    public class Model
    {
        public List<DsSystem> Systems = new List<DsSystem>();
        public List<Cpu> Cpus = new List<Cpu>();
    }

    public static class ModelExtension
    {
        public static void Epilogue(this Model model)
        {
            var rootFlows = model.Systems.SelectMany(sys => sys.RootFlows);
            var subFlows = rootFlows.SelectMany(rf => rf.SubFlows);
            var allFlows = rootFlows.Cast<Flow>().Concat(subFlows);
            foreach (var flow in allFlows)
                flow.BuildGraphInfo();
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
        public virtual string ToText() => Name;
        //public override string ToString() => Name;
    }


    public class DsSystem : Named
    {
        public Model Model;
        public List<RootFlow> RootFlows = new List<RootFlow>();
        public List<Task> Tasks = new List<Task>();
        public DsSystem(string name, Model model)
            : base(name)
        {
            Model = model;
            model.Systems.Add(this);
        }
    }


    public class Task : Named
    {
        public DsSystem System;
        public List<CallPrototype> CallPrototypes = new List<CallPrototype>();

        public Task(string name, DsSystem system)
            : base(name)
        {
            System = system;
            system.Tasks.Add(this);
        }
    }


    public abstract class SegmentOrCallBase : Named, ISegmentOrCall
    {
        public virtual bool Value { get; set; }
        public virtual bool Paused { get; set; }
        public virtual CpuBase OwnerCpu { get; set; }

        public SegmentOrCallBase(string name)
            :base(name)
        {
        }


        bool IsChildrenStartPoint() => true;
        public override string ToString() => ToText();
        public virtual string ToText() => $"{Name}: cpu={OwnerCpu?.Name}";

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

}
