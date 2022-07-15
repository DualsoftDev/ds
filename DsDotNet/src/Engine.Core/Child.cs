using System;
using System.Diagnostics;

namespace Engine.Core
{
    [DebuggerDisplay("{ToString()}")]
    public class Child : IVertex
    {
        public Coin Coin { get; }
        public bool IsCall => Coin is Call;
        // child 본연의 상태
        public Status4 Status {
            get
            {
                switch (Coin)
                {
                    case Segment seg: return seg.RGFH;
                    case Call call: return call.RGFH;
                    default:
                        throw new Exception("ERROR");
                }
            }
        }
        public Child(Coin coin)
        {
            Coin = coin;
        }

        public virtual string QualifiedName { get; }
        public bool Value { get => Coin.Value; set => Coin.Value = value; }
        public CpuBase OwnerCpu { get => Coin.OwnerCpu; set => throw new NotImplementedException(); }

        public override string ToString() => (IsCall ? "" : "==") + Coin.ToText();
    }
}
