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
        public CpuBase Cpu { get; set; }

        /// <summary>Edge 를 통해 알 수 없는 isolated segement/call 등을 포함 </summary>
        HashSet<IVertex> _childVertices = new HashSet<IVertex>();
        public IEnumerable<IVertex> ChildVertices => _childVertices;
        public void AddChildVertices(IEnumerable<IVertex> children)// 임시
        {
            foreach (var child in children)
                AddChildVertex(child);
        }
        public void AddChildVertex(IVertex child)
        {
            Debug.Assert(this is RootFlow || child is Child);
            Debug.Assert(!(child is CallPrototype));
            _childVertices.Add(child);
        }

        public GraphInfo GraphInfo { get; set; }

        public bool IsEmptyFlow => Edges.IsNullOrEmpty() && ChildVertices.IsNullOrEmpty();
        
        List<Edge> _edges = new List<Edge>();
        public IEnumerable<Edge> Edges => _edges;

        public IEnumerable<ICoin> Coins => ChildVertices.OfType<ICoin>();
        public IEnumerable<ICoin> IsolatedCoins => this.CollectIsolatedCoins();

        public void AddEdge(Edge edge)
        {
            this.CheckAddable(edge);
            _edges.Add(edge);

            //edge.Sources.Iter(s => ChildVertices.Add(s));
            edge.Sources.Iter(s => AddChildVertex(s));
            //ChildVertices.Add(edge.Target);
            AddChildVertex(edge.Target);
        }

        protected Flow(string name)
            : base(name)
        {
        }
    }



    public class RootFlow : Flow
    {
        public DsSystem System { get; set; }
        public string QualifiedName => $"{System.Name}_{Name}";
        public RootFlow(string name, DsSystem system)
            : base(name)
        {
            System = system;
            system.RootFlows.Add(this);
        }

        public IEnumerable<Segment> RootSegments => ChildVertices.OfType<Segment>();
    }

    public class ChildFlow : Flow
    {
        public ChildFlow(string name)
            : base(name)
        {
        }

        public IEnumerable<Child> Children => ChildVertices.OfType<Child>();
    }


    public static class FlowExtension
    {
        static ILog Logger => Global.Logger;

        public static DsSystem GetSystem(this Flow flow)
        {
            switch(flow)
            {
                case RootFlow rf: return rf.System;
                case Segment seg: return seg.ContainerFlow.System;
                default:
                    throw new Exception("ERROR");
            }
        }

        //public static void Epilogue(this Flow flow)
        //{
        //    var allVertices =
        //        flow.Edges
        //            .SelectMany(e => e.Vertices)
        //            .Concat(flow.ChildVertices)
        //            .Distinct()
        //            ;

        //    flow.ChildVertices.Clear();
        //    //allVertices.Iter(v => flow.ChildVertices.Add(v));
        //    allVertices.Iter(v => flow.AddChildVertex(v));
        //}

        public static IEnumerable<ICoin> CollectIsolatedCoins(this Flow flow)
        {
            var verticesFromEdge = flow.Edges.SelectMany(e => e.Vertices);
            return flow.ChildVertices
                .Except(verticesFromEdge)
                .OfType<ICoin>()
                ;
        }

        //public static IEnumerable<IVertex> CollectVertices(this Flow flow) =>
        //    flow.Edges.SelectMany(e => e.Vertices)
        //    .Concat(flow.ChildVertices)
        //    .Distinct()
        //    ;

        public static IEnumerable<ExSegmentCall> CollectExternalRealSegment(this ChildFlow childFlow)
        {
            var exSegments = childFlow.Children.Select(c => c.Coin).OfType<ExSegmentCall>();
            return exSegments;
        }
        public static IEnumerable<Child> CollectAlises(this ChildFlow childFlow) =>
            childFlow.Children.Where(c => c.IsAlias)
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
            Logger.Debug($"== {active}Flow {flow.GetSystem().Name}::{flow.Name}");
            foreach (var v in flow.ChildVertices)
                Logger.Debug(v.ToString());
        }
    }
}
