using System;
using System.Collections.Generic;
using System.Text;

namespace Engine
{
    public enum Status4
    {
        Ready = 0,
        Going,
        Finished,
        Homing
    }

    public interface IWithRGFH
    {
        Status4 RGFH { get; set; }
        bool ChangeR();
        bool ChangeG();
        bool ChangeF();
        bool ChangeH();
    }

    public interface IWithSREPorts
    {
        PortS PortS { get; set; }
        PortR PortR { get; set; }
        PortE PortE { get; set; }
    }


    public class Model
    {
        public List<DsSystem> Systems = new List<DsSystem>();
        public List<Cpu> Cpus = new List<Cpu>();
    }

    public interface INamed
    {
        string Name { get; set; }
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

    public abstract class Flow : Named
    {
        public DsSystem System;
        public List<Segment> Segments = new List<Segment>();
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
        public List<Call> Calls = new List<Call>();

        public Task(string name, DsSystem system)
            : base(name)
        {
            System = system;
            system.Tasks.Add(this);
        }
    }
    public class Cpu : Named
    {
        public Flow[] Flows;
        public Cpu(string name, Flow[] flows) : base(name) { Flows = flows; }

    }


    public interface ISegmentOrCall {}

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
