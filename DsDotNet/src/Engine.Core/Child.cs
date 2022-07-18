using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;

namespace Engine.Core
{
    [DebuggerDisplay("{ToString()}")]
    public class Child : Named, IVertex
    {
        public Segment Parent { get; }
        public Coin Coin { get; }
        public bool IsCall => Coin is Call;
        public bool IsAlias { get; set; }
        // 부모가 바라본 child 상태
        public Status4 Status
        {
            get => Parent.ChildStatusMap[this];
            set => Parent.ChildStatusMap[this] = value;
        }

        CompositeDisposable _disposables = new CompositeDisposable();
        public Child(Coin coin, Segment parent)
            :base(coin.Name)
        {
            Parent = parent;
            Coin = coin;
            QualifiedName = $"{parent.QualifiedName}_{coin.Name}";
            Parent.ChildVertices.Add(this);
            //switch(coin)
            //{
            //    case Call call when call.RxTags.Any():

            //}
        }

        public string QualifiedName { get; }
        public bool Value { get => Coin.Value; set => Coin.Value = value; }
        public CpuBase OwnerCpu { get => Coin.OwnerCpu; set => throw new NotImplementedException(); }

        public override string ToString() => (IsCall ? "" : "==") + Coin.ToText();
        public void Going()
        {
            //Coin.Going();

            switch(Coin)
            {
                case ExSegmentCall extSeg:
                    extSeg.Going();
                    break;
                case Call call:
                    call.Going();
                    break;
                default:
                    throw new Exception("ERROR");
            }
        }
    }

    public static class ChildExtension
    {
    }
}
