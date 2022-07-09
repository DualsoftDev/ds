using log4net;

using System.Collections.Generic;
using System.Linq;

namespace Engine.Core
{
    public abstract class Flow : Named, ISegmentOrFlow
    {
        public DsSystem System { get; set; }
        public Cpu Cpu { get; set; }

        /// <summary>Edge 를 통해 알 수 없는 isolated segement/call 등을 포함 </summary>
        public List<SegmentOrCallBase> Children = new List<SegmentOrCallBase>();
        public List<Edge> Edges = new List<Edge>();

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
        public static void PrintFlow(this Flow flow)
        {
            Logger.Debug($"== Flow {flow.System.Name}::{flow.Name}");
            foreach (var v in flow.CollectVertices())
                Logger.Debug(v.ToString());
        }
    }
}
