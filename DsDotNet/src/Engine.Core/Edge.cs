using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Engine.Core
{
    [DebuggerDisplay("{ToText()}")]
    public abstract class Edge : IEdge
    {
        public Flow ContainerFlow;

        /// <summary> Conjuction </summary>
        public IVertex[] Sources { get; internal set; }
        public IVertex Target { get; internal set; }
        public IEnumerable<IVertex> Vertices => Sources.Concat(new[] { Target });

        public bool Value { get => Sources.All(v => v.Value); set => throw new NotImplementedException(); }
        public CpuBase OwnerCpu { get => ContainerFlow.Cpu; set => throw new NotImplementedException(); }

        public string Operator;

        public Edge(Flow containerFlow, IVertex[] sources, string operator_, IVertex target)
        {
            Debug.Assert(sources.All(s => s != null));
            Debug.Assert(target != null);

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


    /// '>' or '>>'
    public abstract class SetEdge : Edge, ISetEdge
    {
        public SetEdge(Flow containerFlow, IVertex[] sources, string operator_, IVertex target)
            : base(containerFlow, sources, operator_, target)
        { }
    }
    public abstract class ResetEdge : Edge, IResetEdge
    {
        public ResetEdge(Flow containerFlow, IVertex[] sources, string operator_, IVertex target)
            : base(containerFlow, sources, operator_, target)
        { }
    }
    public class WeakSetEdge : SetEdge, IWeakEdge
    {
        public WeakSetEdge(Flow containerFlow, IVertex[] sources, string operator_, IVertex target)
            : base(containerFlow, sources, operator_, target)
        { }
    }
    public class StrongSetEdge : SetEdge, IStrongEdge
    {
        public StrongSetEdge(Flow containerFlow, IVertex[] sources, string operator_, IVertex target)
            : base(containerFlow, sources, operator_, target)
        { }
    }
    public class WeakResetEdge : ResetEdge, IWeakEdge
    {
        public WeakResetEdge(Flow containerFlow, IVertex[] sources, string operator_, IVertex target)
            : base(containerFlow, sources, operator_, target)
        { }
    }
    public class StrongResetEdge : ResetEdge, IStrongEdge
    {
        public StrongResetEdge(Flow containerFlow, IVertex[] sources, string operator_, IVertex target)
            : base(containerFlow, sources, operator_, target)
        { }
    }


    public static class EdgeExtension
    {
        public static IEnumerable<(IBit, IBit)> CollectForwardDependancy(this Edge edge)
        {
            foreach(var s in edge.Sources)
            {
                switch(s)
                {
                    case Segment seg:
                        yield return (seg.TagE, edge);
                        break;

                    case Call call:
                        foreach (var t in call.RxTags)
                            yield return (t, edge);
                        break;
                    default:
                        throw new Exception("ERROR");
                }
            }

            switch(edge.Target)
            {
                case Segment seg:
                    yield return (edge, seg.TagS);
                    break;

                case Call call:
                    foreach(var tx in call.TxTags)
                        yield return (edge, tx);

                    break;
                default:
                    throw new Exception("ERROR");
            }
        }
    }
}
