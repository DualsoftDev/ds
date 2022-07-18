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
        public List<PRootFlow> RootFlows = new List<PRootFlow>();
        public List<PTask> Tasks = new List<PTask>();

        /// <summary> (Alias Target) => {Alias1, Alias2, .. } map </summary>
        internal Dictionary<string, string[]> _strBackwardAliasMap = new Dictionary<string, string[]>();
        /// <summary> Alias1 => (Alias Target), Alias2 => (Alias Target) </summary>
        public Dictionary<string, string> AliasNameMap = new Dictionary<string, string>();
        public Dictionary<string, PAlias> Aliases = new Dictionary<string, PAlias>();

        public PSystem(string name, PModel model)
            : base(name)
        {
            Model = model;
            model.Systems.Add(this);
        }
    }

    public abstract class PFlow : PNamed, IPWallet
    {
        public List<PSegment> Segments = new List<PSegment>();
        public IEnumerable<IPVertex> Children => Segments.Concat(Edges.SelectMany(e => e.Sources.Concat(new[] { e.Target }))).ToArray();
        public List<PEdge> Edges = new List<PEdge>();
        public bool IsEmptyFlow => Segments.Count == 0 && Edges.Count == 0;

        internal Dictionary<PCallPrototype, PCall> CallInstanceMap = new Dictionary<PCallPrototype, PCall>();

        protected PFlow(string name)
            : base(name)
        {
        }
    }

    public class PRootFlow : PFlow
    {
        public PSystem System;
        public PRootFlow(string name, PSystem system)
            : base(name)
        {
            System = system;
            system.RootFlows.Add(this);
        }
    }

    public class PChildFlow : PFlow
    {
        public PChildFlow(string name)
            : base(name)
        {
        }
    }

    public static class PFlowExtension
    {
        public static PSystem GetSystem(this PFlow flow)
        {
            switch (flow)
            {
                case PRootFlow rf: return rf.System;
                case PSegment seg: return seg.ContainerFlow.System;
                default:
                    throw new Exception("ERROR");
            }
        }
    }

    public class PTask : PNamed
    {
        public PSystem System;
        public List<PCallPrototype> Calls = new List<PCallPrototype>();

        public PTask(string name, PSystem system)
            : base(name)
        {
            System = system;
            system.Tasks.Add(this);
        }
    }
    public class PCpu : PNamed
    {
        public PModel Model;
        public PRootFlow[] RootFlows;
        public PCpu(string name, PRootFlow[] flows, PModel model) : base(name) {
            RootFlows = flows;
            Model = model;
            model.Cpus.Add(this);
        }
    }

    public interface IPVertex { }
    public interface IPWallet {}
    public interface IPCoin : IPVertex {}


    public class PSegment : PChildFlow, IPCoin, IPWallet
    {
        public PRootFlow ContainerFlow;
        public IEnumerable<IPVertex> Children {
            get
            {
                return
                    Edges
                    .SelectMany(e => e.Sources.Concat(new[] { e.Target }))
                    //.OfType<PCall>()
                    .Distinct()
                    ;
            }
        }

        public PSegment(string name, PRootFlow containerFlow)
            : base(name)
        {
            ContainerFlow = containerFlow;
            containerFlow.Segments.Add(this);
        }
    }

    public class PAlias: PNamed, IPCoin
    {
        public IPCoin AliasTarget { get; set; }
        public string AliasTargetName;
        public PFlow ContainerFlow;
        public PAlias(string name, PFlow containerFlow, string aliasTarget)
            : base(name)
        {
            AliasTargetName = aliasTarget;
            ContainerFlow = containerFlow;
            containerFlow.GetSystem().Aliases.Add(name, this);
        }
    }

    public class PCallBase : PNamed, IPCoin
    {
        public PSegment[] TXs;
        public PSegment[] RXs;

        public PCallBase(string name) : base(name) {}
    }


    public class PCallPrototype : PCallBase
    {
        public PTask Task;

        public PCallPrototype(string name, PTask task)
            : base(name)
        {
            Task = task;
            task.Calls.Add(this);
        }
    }

    public class PCall : PCallBase
    {
        public PCallPrototype Prototype;
        public PFlow Container;
        public PCall(string name, PFlow container, PCallPrototype prototype) : base(name)
        {
            Prototype = prototype;
            Container = container;
        }
    }

    [DebuggerDisplay("{ToText()}")]
    public class PEdge
    {
        public PFlow ContainerFlow;
        public IPVertex[] Sources;
        public IPVertex Target;
        public IEnumerable<IPVertex> Vertices => Sources.Concat(new[] { Target });

        public string Operator;

        public PEdge(PFlow containerFlow, IPVertex[] sources, string operator_, IPVertex target)
        {
            ContainerFlow = containerFlow;
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
        static IPCoin FindCoin(this PModel model, string systemName, string flowOrTaskName, string segmentOrCallName, bool isSegment)
        {
            var system = model.Systems.First(s => s.Name == systemName);
            if (isSegment)
            {
                var flow = system.RootFlows.FirstOrDefault(f => f.Name == flowOrTaskName);
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
            model.FindCoin(systemName, flowName, segmentName, true) as PSegment;

        public static PCallPrototype FindCall(this PModel model, string systemName, string taskName, string callName) =>
            model.FindCoin(systemName, taskName, callName, false) as PCallPrototype;

        public static PSegment FindSegment(this PModel model, string fqSegmentName)
        {
            if (fqSegmentName == "_")
                return null;

            var names = fqSegmentName.Split(new[] { '.' });
            Debug.Assert(names.Length == 3);
            (var sysName, var flowName, var segmentName) = (names[0], names[1], names[2]);
            return model.FindSegment(sysName, flowName, segmentName);
        }

        public static IPCoin FindCoin(this PModel model, string fqSegmentName)
        {
            var seg = model.FindSegment(fqSegmentName);
            if (seg != null)
                return seg;

            return model.FindCall(fqSegmentName);
        }

        public static PSegment[] FindSegments(this PModel model, string fqSegmentNames)
        {
            return
                fqSegmentNames
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(segName => FindSegment(model, segName))
                    .ToArray()
                    ;
        }

        public static PCallPrototype FindCall(this PModel model, string fqCallName)
        {
            var names = fqCallName.Split(new[] { '.' });
            (var sysName, var taskName, var callName) = (names[0], names[1], names[2]);
            return model.FindCall(sysName, taskName, callName);
        }
    }
}
