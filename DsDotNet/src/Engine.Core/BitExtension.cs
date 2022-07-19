using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core
{
    public static class BitExtension
    {
        public static void Evaluate(this IBit bit)
        {
            var cpu = bit.OwnerCpu;
            var prevs = cpu.BackwardDependancyMap[bit].ToArray();
            var newValue = prevs.Any(b => b.Value);
            var current = bit.Value;
            if (current == newValue)
                return;

            switch(bit)
            {
                case Tag tag:
                    break;
                case Port port:
                    var seg = port.OwnerSegment;
                    seg.EvaluatePort(port, newValue);
                    break;

                case Flag flag:
                    break;
                default:
                    throw new Exception("ERROR");

            }
        }
    }
}
