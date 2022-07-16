using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core
{
    public class SegmentAlias : Coin
    {
        public SegmentAlias(string name, Segment aliasTarget)
            : base(name)
        {
            AliasTarget = aliasTarget;
        }

        public Segment AliasTarget { get; }
    }
}
