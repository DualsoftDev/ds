using System.Collections.Generic;
using System.Linq;

namespace Engine.Core
{
    public class CallBase : SegmentOrCallBase
    {
        public List<ITxRx> TXs = new List<ITxRx>();
        public ITxRx RX;

        public CallBase(string name) : base(name) {}

        public IEnumerable<ITxRx> TxRxs => TXs.Concat(new[] { RX });

    }

    public class CallPrototype : CallBase
    {
        public Task Task;
        public CallPrototype(string name, Task task)
            : base(name)
        {
            Task = task;
            task.CallPrototypes.Add(this);
        }

    }

    public class Call : CallBase
    {
        public ISegmentOrFlow Container;
        public Task Task;
        public Call(string name) : base(name)
        {

        }
    }

}
