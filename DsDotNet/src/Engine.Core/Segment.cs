using System.Collections.Generic;
using System.Linq;

namespace Engine.Core
{
    public class Segment : SegmentOrCallBase, IWithSREPorts, ITxRx
    {
        public RootFlow ContainerFlow;
        public ChildFlow ChildFlow;

        public PortS PortS { get; set; }
        public PortR PortR { get; set; }
        public PortE PortE { get; set; }

        public IEnumerable<Call> Children =>
            ChildFlow?.Edges
            .SelectMany(e => e.Vertices)
            .OfType<Call>()
            .Distinct()
            ;


        public Segment(string name, RootFlow containerFlow)
            : base(name)
        {
            ContainerFlow = containerFlow;
            ChildFlow = new ChildFlow($"_{name}", this);
            containerFlow.Segments.Add(this);

            PortS = new PortS(this);
            PortR = new PortR(this);
            PortE = new PortE(this);
        }
    }

}
