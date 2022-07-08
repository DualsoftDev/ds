using System.Collections.Generic;
using System.Linq;

namespace Engine.Core
{
    public class Call : SegmentOrCallBase
    {
        public Task Task;
        public List<ITxRx> TXs = new List<ITxRx>();
        public ITxRx RX;
        public IEnumerable<ITxRx> TxRxs => TXs.Concat(new[] { RX });

        public Call(string name, Task task)
            : base(name)
        {
            Task = task;
            task.Calls.Add(this);
        }
    }
}
