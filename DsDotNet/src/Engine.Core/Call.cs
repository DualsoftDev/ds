using System.Collections.Generic;
using System.Linq;

namespace Engine.Core
{
    public class CallBase : SegmentOrCallBase
    {
        public CallBase(string name) : base(name) {}
    }

    public class CallPrototype : CallBase
    {
        public Task Task;
        public List<ITxRx> TXs = new List<ITxRx>();
        public ITxRx RX;
        public bool Value => ((Tag)RX).Value;   // todo TAG 아닌 경우 처리 필요함.

        public CallPrototype(string name, Task task)
            : base(name)
        {
            Task = task;
            task.CallPrototypes.Add(this);
        }

    }

    public class Call : CallBase
    {
        public CallPrototype Prototype;
        public ISegmentOrFlow Container;

        public IEnumerable<ITxRx> TXs => Prototype.TXs;
        public ITxRx RX => Prototype.RX;
        public IEnumerable<ITxRx> TxRxs => TXs.Concat(new[] { RX });

        public Call(string name, ISegmentOrFlow container, CallPrototype protoType) : base(name)
        {
            Prototype = protoType;
            Container = container;

            Flow flow = container as Flow;
            var containerSegment = container as Segment;
            if (flow == null && containerSegment != null)
                flow = containerSegment.ChildFlow;

            flow.Children.Add(this);
        }
    }

}
