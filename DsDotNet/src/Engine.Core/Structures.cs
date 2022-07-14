using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Engine.Core
{
    public class Model
    {
        public List<DsSystem> Systems = new List<DsSystem>();
        public List<Cpu> Cpus = new List<Cpu>();
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
        public List<DsTask> Tasks = new List<DsTask>();
        public DsSystem(string name, Model model)
            : base(name)
        {
            Model = model;
            model.Systems.Add(this);
        }
    }


    public class DsTask : Named
    {
        public DsSystem System;
        public List<CallPrototype> CallPrototypes = new List<CallPrototype>();

        public DsTask(string name, DsSystem system)
            : base(name)
        {
            System = system;
            system.Tasks.Add(this);
        }
    }


    /// <summary> Segment Or Call base </summary>
    public abstract class Coin : Named, ICoin
    {
        public virtual bool Value { get; set; }

        /*
         * Do not store Paused property (getter only, no setter)
         */
        public virtual bool Paused { get; }
        public virtual CpuBase OwnerCpu { get; set; }

        public virtual IWallet Wallet => throw new NotImplementedException();

        public Coin(string name)
            :base(name)
        {
        }


        bool IsChildrenStartPoint() => true;
        public virtual string QualifiedName { get; }
        public override string ToString() => ToText();
        public override string ToText() => $"{QualifiedName}: cpu={OwnerCpu?.Name}";

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
