using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Engine.Core
{
    [DebuggerDisplay("{ToText()}")]
    public abstract class Edge : IEdge
    {
        public ISegmentOrCall[] Sources;
        public ISegmentOrCall Target;
        public IEnumerable<ISegmentOrCall> Vertices => Sources.Concat(new[] { Target });
        public string Operator;

        public Edge(ISegmentOrCall[] sources, string operator_, ISegmentOrCall target)
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


    /// '>' or '>>'
    public abstract class SetEdge : Edge
    {
        public SetEdge(ISegmentOrCall[] sources, string operator_, ISegmentOrCall target)
            : base(sources, operator_, target)
        { }
    }
    public abstract class ResetEdge : Edge, IReset
    {
        public ResetEdge(ISegmentOrCall[] sources, string operator_, ISegmentOrCall target)
            : base(sources, operator_, target)
        { }
    }
    public class WeakSetEdge : SetEdge
    {
        public WeakSetEdge(ISegmentOrCall[] sources, string operator_, ISegmentOrCall target)
            : base(sources, operator_, target)
        { }
    }
    public class StrongSetEdge : SetEdge, IStrong
    {
        public StrongSetEdge(ISegmentOrCall[] sources, string operator_, ISegmentOrCall target)
            : base(sources, operator_, target)
        { }
    }
    public class WeakResetEdge : ResetEdge
    {
        public WeakResetEdge(ISegmentOrCall[] sources, string operator_, ISegmentOrCall target)
            : base(sources, operator_, target)
        { }
    }
    public class StrongResetEdge : ResetEdge, IStrong
    {
        public StrongResetEdge(ISegmentOrCall[] sources, string operator_, ISegmentOrCall target)
            : base(sources, operator_, target)
        { }
    }


}
