using System;
using System.Diagnostics;

namespace Engine.Core
{
    [DebuggerDisplay("{ToString()}")]
    public class Child : Named, IVertex
    {
        public Segment Parent { get; }
        public Coin Coin { get; }
        public bool IsCall => Coin is Call;
        // 부모가 바라본 child 상태
        public Status4 Status => Parent.ChildStatusMap[this];
        public Child(Coin coin, Segment parent)
            :base(coin.Name)
        {
            Parent = parent;
            Coin = coin;
            QualifiedName = $"{parent.QualifiedName}_{coin.Name}";
        }

        public string QualifiedName { get; }
        public bool Value { get => Coin.Value; set => Coin.Value = value; }
        public CpuBase OwnerCpu { get => Coin.OwnerCpu; set => throw new NotImplementedException(); }

        public override string ToString() => (IsCall ? "" : "==") + Coin.ToText();
    }

    public static class ChildExtension
    {
        public static void Going(this Child child) => child.Coin.Going();
    }
}
