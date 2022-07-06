using System;
using System.Collections.Generic;
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

    public class Flow : Named
    {
        public DsSystem System;
        public List<Segment> Segments = new List<Segment>();
        public List<Edge> Edges = new List<Edge>();

        public Flow(string name, DsSystem system)
            : base(name)
        {
            System = system;
            system.Flows.Add(this);
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

    public interface IVertex {}

    public class Segment : Named, IVertex
    {
        /// container flow
        public Flow Flow;
        public Segment(string name, Flow flow)
            : base(name)
        {
            Flow = flow;
            flow.Segments.Add(this);
        }
    }

    public class RootSegment: Segment
    {
        public Flow ChildFlow;
        public List<Segment> Children = new List<Segment>();
        public RootSegment(string name, Flow flow)
            : base(name, flow)
        {
            ChildFlow = new Flow($"_{name}", Flow.System);
        }
}

    public class Call : Named, IVertex
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

    public class Edge
    {
        public IVertex[] Sources;
        public IVertex Target;

        public Edge(IVertex[] sources, IVertex target)
        {
            Sources = sources;
            Target = target;
        }
    }

    public static class ModelUtil
    {
        static IVertex FindSegmentOrCall(this Model model, string systemName, string flowOrTaskName, string segmentOrCallName, bool isSegment)
        {
            try
            {
                var system = model.Systems.First(s => s.Name == systemName);
                if (isSegment)
                    return system.Flows
                        .First(f => f.Name == flowOrTaskName)
                        .Segments.First(s => s.Name == segmentOrCallName)
                        ;

                return system.Tasks
                    .First(t => t.Name == flowOrTaskName)
                    .Calls.First(c => c.Name == segmentOrCallName)
                    ;
            }
            catch (Exception)
            {
                return null;
            }
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
