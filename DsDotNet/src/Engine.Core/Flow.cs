using Engine.Common;

using log4net;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Engine.Core
{
    public abstract class Flow : Named, IWallet
    {
        public DsSystem System { get; set; }
        public CpuBase Cpu { get; set; }

        /// <summary>Edge 를 통해 알 수 없는 isolated segement/call 등을 포함 </summary>
        internal List<IVertex> ChildVertices { get; } = new List<IVertex>();

        List<Edge> _edges = new List<Edge>();
        public GraphInfo GraphInfo { get; set; }

        public bool IsEmptyFlow => _edges.IsNullOrEmpty() && ChildVertices.IsNullOrEmpty();
        public IEnumerable<Edge> Edges => _edges;

        public IEnumerable<ICoin> Coins => ChildVertices.OfType<ICoin>();
        public IEnumerable<ICoin> IsolatedCoins => this.CollectIsolatedCoins();

        public void AddEdge(Edge edge)
        {
            this.CheckAddable(edge);
            _edges.Add(edge);
        }

        protected Flow(string name, DsSystem system)
            : base(name)
        {
            System = system;
        }
    }



    public class RootFlow : Flow
    {
        public List<ChildFlow> SubFlows = new List<ChildFlow>();
        public string QualifiedName => $"{System.Name}_{Name}";
        public RootFlow(string name, DsSystem system)
            : base(name, system)
        {
            system.RootFlows.Add(this);
        }
    }

    public class ChildFlow : Flow
    {
        public Segment ContainerSegment;
        //public IEnumerable<Call> Calls => Children.OfType<Call>();
        public ChildFlow(string name, Segment segment)
            : base(name, segment.ContainerFlow.System)
        {
            Debug.Assert(segment.ContainerFlow.SubFlows.All(sf => sf.Name != name));
            segment.ContainerFlow.SubFlows.Add(this);
            ContainerSegment = segment;
        }
    }


    public static class FlowExtension
    {
        static ILog Logger => Global.Logger;

        public static void Epilogue(this Flow flow)
        {
            var allVertices =
                flow.Edges
                    .SelectMany(e => e.Vertices)
                    .Concat(flow.ChildVertices)
                    .Distinct()
                    ;

            flow.ChildVertices.Clear();
            flow.ChildVertices.AddRange(allVertices);
        }

        public static IEnumerable<ICoin> CollectIsolatedCoins(this Flow flow)
        {
            var verticesFromEdge = flow.Edges.SelectMany(e => e.Vertices);
            return flow.ChildVertices
                .Except(verticesFromEdge)
                .OfType<ICoin>()
                ;
        }

        public static IEnumerable<IVertex> CollectVertices(this Flow flow) =>
            flow.Edges.SelectMany(e => e.Vertices)
            .Concat(flow.ChildVertices)
            .Distinct()
            ;

        public static IEnumerable<SegmentAlias> CollectExternalRealSegment(this ChildFlow childFlow)
        {
            var exSegments = childFlow.ChildVertices.OfType<SegmentAlias>();
            Debug.Assert(exSegments.All(s => s.AliasTarget.ContainerFlow.System != childFlow.System));
            return exSegments;
        }
        public static IEnumerable<CallAlias> CollectCallAlises(this ChildFlow childFlow) => childFlow.ChildVertices.OfType<CallAlias>();
        public static IEnumerable<IAlias> CollectAlises(this ChildFlow childFlow) =>
            childFlow.CollectExternalRealSegment().Cast<IAlias>()
            .Concat(childFlow.CollectCallAlises())
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

        /// <summary>
        /// 중복 정의 check
        /// e.g "A, B > C; A > C"
        /// </summary>
        internal static void CheckAddable(this Flow flow, Edge edge)
        {
            var duplicate = flow.CollectArrow().Intersect(edge.CollectArrow()).ToArray();
            if (duplicate.Any())
                throw new Exception($"ERROR: duplicated causals: {duplicate[0]}");
        }

        public static void PrintFlow(this Flow flow, bool isActive)
        {
            var active = isActive ? "Active " : "";
            Logger.Debug($"== {active}Flow {flow.System.Name}::{flow.Name}");
            foreach (var v in flow.CollectVertices())
                Logger.Debug(v.ToString());
        }
    }
}
