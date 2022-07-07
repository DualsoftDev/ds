using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DsParser
{
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
        public RootSegment Segment; // container
        public ChildFlow(string name, RootSegment segment)
            : base(name, segment.Flow.System)
        {
            Segment = segment;
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
        /// container flow
        public RootFlow Flow;
        public Segment(string name, RootFlow flow)
            : base(name)
        {
            Flow = flow;
            flow.Segments.Add(this);
        }
    }

    public class RootSegment: Segment
    {
        public ChildFlow ChildFlow;
        public IEnumerable<Call> Children =>
            ChildFlow?.Edges
            .SelectMany(e => e.Sources.Concat(new[] { e.Target }))
            .OfType<Call>()
            .Distinct()
            ;
        public RootSegment(string name, RootFlow flow)
            : base(name, flow)
        {
            ChildFlow = new ChildFlow($"_{name}", this);
        }
}

    public class Call : Named, ISegmentOrCall
    {
        public Task Task;
        public RootSegment TX;
        public RootSegment RX;

        public Call(string name, Task task)
            : base(name)
        {
            Task = task;
            task.Calls.Add(this);
        }
    }

    [DebuggerDisplay("{ToText()}")]
    public class Edge
    {
        public ISegmentOrCall[] Sources;
        public ISegmentOrCall Target;

        public Edge(ISegmentOrCall[] sources, ISegmentOrCall target)
        {
            Sources = sources;
            Target = target;
        }
        public string ToText()
        {
            var ss = string.Join(", ", Sources.Select(s => s.ToString()));
            return $"{ss} -> {Target}";
        }
    }

    public static class ModelUtil
    {
        static ISegmentOrCall FindSegmentOrCall(this Model model, string systemName, string flowOrTaskName, string segmentOrCallName, bool isSegment)
        {
            var system = model.Systems.First(s => s.Name == systemName);
            if (isSegment)
            {
                var flow = system.Flows.FirstOrDefault(f => f.Name == flowOrTaskName);
                if (flow == null)
                    return null;

                return flow
                    .Segments.FirstOrDefault(s => s.Name == segmentOrCallName)
                    ;
            }

            var task = system.Tasks.FirstOrDefault(t => t.Name == flowOrTaskName);
            if (task == null)
                return null;

            return task.Calls.FirstOrDefault(c => c.Name == segmentOrCallName);
        }
        public static Segment FindSegment(this Model model, string systemName, string flowName, string segmentName) =>
            model.FindSegmentOrCall(systemName, flowName, segmentName, true) as Segment;

        public static Call FindCall(this Model model, string systemName, string taskName, string callName) =>
            model.FindSegmentOrCall(systemName, taskName, callName, false) as Call;

        public static Segment FindSegment(this Model model, string fqSegmentName)
        {
            var names = fqSegmentName.Split(new[] { '.' });
            (var sysName, var flowName, var segmentName) = (names[0], names[1], names[2]);
            return model.FindSegment(sysName, flowName, segmentName);
        }

        public static Call FindCall(this Model model, string fqCallName)
        {
            var names = fqCallName.Split(new[] { '.' });
            (var sysName, var taskName, var callName) = (names[0], names[1], names[2]);
            return model.FindCall(sysName, taskName, callName);
        }
    }
}
