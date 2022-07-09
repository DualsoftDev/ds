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
        public List<Flow> Flows = new List<Flow>();
        public List<Task> Tasks = new List<Task>();
        public DsSystem(string name, Model model)
            : base(name)
        {
            Model = model;
            model.Systems.Add(this);
        }
    }

    public abstract class Flow : Named, ISegmentOrFlow
    {
        public DsSystem System { get; set; }
        public Cpu Cpu { get; set; }
        public List<Edge> Edges = new List<Edge>();

        protected Flow(string name, DsSystem system)
            : base(name)
        {
            System = system;
            system.Flows.Add(this);
        }
    }

    public class RootFlow : Flow
    {
        /// <summary>Edge 를 통해 알 수 없는 isolated root segement 등을 포함 </summary>
        public List<Segment> Children = new List<Segment>();
        public RootFlow(string name, DsSystem system)
            : base(name, system)
        {
        }
    }

    public class ChildFlow : Flow
    {
        public Segment ContainerSegment;
        public ChildFlow(string name, Segment segment)
            : base(name, segment.ContainerFlow.System)
        {
            ContainerSegment = segment;
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


    public abstract class SegmentOrCallBase : Named, IWithRGFH, ISegmentOrCall
    {
        public Status4 RGFH { get; set; } = Status4.Homing;
        public SegmentOrCallBase(string name)
            :base(name)
        {
        }


        bool IsChildrenStartPoint() => true;

        public virtual bool ChangeR()
        {
            if (RGFH == Status4.Ready)
                return true;

            if (RGFH == Status4.Homing)
            {
                if (IsChildrenStartPoint())
                {
                    RGFH = Status4.Ready;
                    return true;
                }
            }
            return false;
        }

        public virtual bool ChangeG()
        {
            if (RGFH == Status4.Going)
                return true;

            if (RGFH == Status4.Ready)
            {
                RGFH = Status4.Going;
                return true;
            }
            return false;
        }

        public virtual bool ChangeF()
        {
            if (RGFH == Status4.Finished)
                return true;

            if (RGFH == Status4.Going)
            {
                RGFH = Status4.Finished;
                return true;
            }
            return false;
        }

        public virtual bool ChangeH()
        {
            if (RGFH == Status4.Homing)
                return true;

            if (RGFH == Status4.Finished)
            {
                RGFH = Status4.Homing;
                return true;
            }
            return false;
        }
    }

}
