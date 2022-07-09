using log4net;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine.Core
{
    public abstract class Flow : Named, ISegmentOrFlow
    {
        public DsSystem System { get; set; }
        public Cpu Cpu { get; set; }

        /// <summary>Edge 를 통해 알 수 없는 isolated segement/call 등을 포함 </summary>
        public List<SegmentOrCallBase> Children { get; } = new List<SegmentOrCallBase>();
        List<Edge> _edges = new List<Edge>();

        public IEnumerable<Edge> Edges => _edges;
        public void AddEdge(Edge edge)
        {
            this.CheckAddable(edge);
            _edges.Add(edge);
        }

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


    public static class FlowHelper
    {
        static ILog Logger => Global.Logger;
        public static IEnumerable<IVertex> CollectVertices(this Flow flow) =>
            flow.Edges.SelectMany(e => e.Vertices)
            .Concat(flow.Children)
            .Distinct()
            ;

        struct Causal
        {
            IVertex Source;
            IVertex Target;
            bool IsReset;
            public Causal(IVertex source, IVertex target, bool isReset)
            {
                Source = source;
                Target = target;
                IsReset = isReset;
            }

            public override string ToString()
            {
                var op = IsReset ? "|>" : ">";
                return $"{Source} {op} {Target}";
            }
        }

        static IEnumerable<Causal> CollectArrow(this Edge edge)
        {
            bool isReset(string causalOperator)
            {
                switch (causalOperator)
                {
                    case ">":
                    case ">>":
                        return false;
                    case "|>":
                    case "|>>":
                        return true;
                    default:
                        throw new Exception("ERROR");
                }
            }
            var e = edge;
            foreach (var s in e.Sources)
                yield return new Causal(s, e.Target, isReset(e.Operator))
                    ;
        }

        static IEnumerable<Causal> CollectArrow(this Flow flow)
        {
            foreach (var e in flow.Edges)
                foreach (var c in e.CollectArrow())
                    yield return c;
                        ;
        }

        internal static void CheckAddable(this Flow flow, Edge edge)
        {
            var duplicate = flow.CollectArrow().Intersect(edge.CollectArrow()).ToArray();
            if (duplicate.Any())
                throw new Exception($"ERROR: duplicated causals: {duplicate[0]}");
        }

        public static void PrintFlow(this Flow flow)
        {
            Logger.Debug($"== Flow {flow.System.Name}::{flow.Name}");
            foreach (var v in flow.CollectVertices())
                Logger.Debug(v.ToString());
        }
    }
}
