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
        public IVertex[] Sources;
        public IVertex Target;
        public IEnumerable<IVertex> Vertices => Sources.Concat(new[] { Target });
        public string Operator;

        public Edge(Flow containerFlow, IVertex[] sources, string operator_, IVertex target)
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


    /// '>' or '>>'
    public abstract class SetEdge : Edge
    {
        public SetEdge(Flow containerFlow, IVertex[] sources, string operator_, IVertex target)
            : base(containerFlow, sources, operator_, target)
        { }
    }
    public abstract class ResetEdge : Edge, IReset
    {
        public ResetEdge(Flow containerFlow, IVertex[] sources, string operator_, IVertex target)
            : base(containerFlow, sources, operator_, target)
        { }
    }
    public class WeakSetEdge : SetEdge
    {
        public WeakSetEdge(Flow containerFlow, IVertex[] sources, string operator_, IVertex target)
            : base(containerFlow, sources, operator_, target)
        { }
    }
    public class StrongSetEdge : SetEdge, IStrong
    {
        public StrongSetEdge(Flow containerFlow, IVertex[] sources, string operator_, IVertex target)
            : base(containerFlow, sources, operator_, target)
        { }
    }
    public class WeakResetEdge : ResetEdge
    {
        public WeakResetEdge(Flow containerFlow, IVertex[] sources, string operator_, IVertex target)
            : base(containerFlow, sources, operator_, target)
        { }
    }
    public class StrongResetEdge : ResetEdge, IStrong
    {
        public StrongResetEdge(Flow containerFlow, IVertex[] sources, string operator_, IVertex target)
            : base(containerFlow, sources, operator_, target)
        { }
    }
}
