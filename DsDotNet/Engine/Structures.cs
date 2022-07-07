using System;
using System.Collections.Generic;
using System.Linq;
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


    public class Model
    {
        public List<DsSystem> Systems = new List<DsSystem>();
        public List<Cpu> Cpus = new List<Cpu>();
    }

    public class Named
    {
        public string Name;

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
    public class Cpu
    {
        public List<Flow> Flows = new List<Flow>();
    }


    public interface ISegmentOrCall {}

    public class Segment : Named, ISegmentOrCall
    {
        public RootFlow ContainerFlow;
        public ChildFlow ChildFlow;

        public Status4 Status { get; set; } = Status4.Homing;
        public PortS PortS { get; set; }
        public PortR PortR { get; set; }
        public PortE PortE { get; set; }

        public IEnumerable<Call> Children =>
            ChildFlow?.Edges
            .SelectMany(e => e.Sources.Concat(new[] { e.Target }))
            .OfType<Call>()
            .Distinct()
            ;

        public Segment(string name, RootFlow containerFlow)
            : base(name)
        {
            ContainerFlow = containerFlow;
            ChildFlow = new ChildFlow($"_{name}", this);
            containerFlow.Segments.Add(this);

            PortS = new PortS(this);
            PortR = new PortR(this);
            PortE = new PortE(this);
        }
    }


    public class Call : Named, ISegmentOrCall
    {
        public Task Task;
        public Segment TX;
        public Segment RX;
        public Status4 Status { get; set; } = Status4.Homing;

        public Call(string name, Task task)
            : base(name)
        {
            Task = task;
            task.Calls.Add(this);
        }
    }

}
