using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DsParser
{
    public class PModel
    {
        public List<PSystem> Systems = new List<PSystem>();
        public List<PCpu> Cpus = new List<PCpu>();
    }

    public class PNamed
    {
        public string Name;

        public PNamed(string name)
        {
            Name = name;
        }
        public override string ToString() => Name;
    }


    public class PSystem : PNamed
    {
        public PModel Model;
        public List<PFlow> Flows = new List<PFlow>();
        public List<PTask> Tasks = new List<PTask>();
        public PSystem(string name, PModel model)
            : base(name)
        {
            Model = model;
            model.Systems.Add(this);
        }
    }

    public abstract class PFlow : PNamed
    {
        public PSystem System;
        public List<PSegment> Segments = new List<PSegment>();
        public List<PEdge> Edges = new List<PEdge>();

        protected PFlow(string name, PSystem system)
            : base(name)
        {
            System = system;
            system.Flows.Add(this);
        }
    }

    public class PRootFlow : PFlow
    {
        public PRootFlow(string name, PSystem system)
            : base(name, system)
        {
        }
    }

    public class PChildFlow : PFlow
    {
        public PSegment ContainerSegment;
        public PChildFlow(string name, PSegment segment)
            : base(name, segment.ContainerFlow.System)
        {
            ContainerSegment = segment;
        }
    }

    public class PTask : PNamed
    {
        public PSystem System;
        public List<PCall> Calls = new List<PCall>();

        public PTask(string name, PSystem system)
            : base(name)
        {
            System = system;
            system.Tasks.Add(this);
        }
    }
    public class PCpu
    {
        public List<PFlow> Flows = new List<PFlow>();
    }

    public interface ISegmentOrCall {}

    public class PSegment : PNamed, ISegmentOrCall
    {
        public PRootFlow ContainerFlow;
        public PChildFlow ChildFlow;
        public IEnumerable<PCall> Children =>
            ChildFlow?.Edges
            .SelectMany(e => e.Sources.Concat(new[] { e.Target }))
            .OfType<PCall>()
            .Distinct()
            ;

        public PSegment(string name, PRootFlow containerFlow)
            : base(name)
        {
            ContainerFlow = containerFlow;
            ChildFlow = new PChildFlow($"_{name}", this);
            containerFlow.Segments.Add(this);
        }
    }


    public class PCall : PNamed, ISegmentOrCall
    {
        public PTask Task;
        public PSegment TX;
        public PSegment RX;

        public PCall(string name, PTask task)
            : base(name)
        {
            Task = task;
            task.Calls.Add(this);
        }
    }

    [DebuggerDisplay("{ToText()}")]
    public class PEdge
    {
        public ISegmentOrCall[] Sources;
        public ISegmentOrCall Target;
        public string Operator;

        public PEdge(ISegmentOrCall[] sources, string operator_, ISegmentOrCall target)
        {
            Sources = sources;
            Target = target;
            Operator = operator_;
        }
        public string ToText()
        {
            var ss = string.Join(", ", Sources.Select(s => s.ToString()));
            return $"{ss} {Operator} {Target}";
        }
    }

    public static class PModelUtil
    {
        static ISegmentOrCall FindSegmentOrCall(this PModel model, string systemName, string flowOrTaskName, string segmentOrCallName, bool isSegment)
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
        public static PSegment FindSegment(this PModel model, string systemName, string flowName, string segmentName) =>
            model.FindSegmentOrCall(systemName, flowName, segmentName, true) as PSegment;

        public static PCall FindCall(this PModel model, string systemName, string taskName, string callName) =>
            model.FindSegmentOrCall(systemName, taskName, callName, false) as PCall;

        public static PSegment FindSegment(this PModel model, string fqSegmentName)
        {
            var names = fqSegmentName.Split(new[] { '.' });
            (var sysName, var flowName, var segmentName) = (names[0], names[1], names[2]);
            return model.FindSegment(sysName, flowName, segmentName);
        }

        public static PCall FindCall(this PModel model, string fqCallName)
        {
            var names = fqCallName.Split(new[] { '.' });
            (var sysName, var taskName, var callName) = (names[0], names[1], names[2]);
            return model.FindCall(sysName, taskName, callName);
        }
    }
}
