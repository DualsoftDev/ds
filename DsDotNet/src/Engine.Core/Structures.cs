using System;
using System.Collections.Generic;
using System.Text;

namespace Engine.Core
{
    public class Model
    {
        public List<DsSystem> Systems = new List<DsSystem>();
        public List<Cpu> Cpus = new List<Cpu>();
    }

    public class Named: INamed
    {
        public string Name { get; set; }

        public Named(string name)
        {
            Name = name;
        }
        public override string ToString() => Name;
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
        public bool Paused { get; set; }
        public virtual CpuBase OwnerCpu { get; set; }

        public SegmentOrCallBase(string name)
            :base(name)
        {
        }


        bool IsChildrenStartPoint() => true;

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
