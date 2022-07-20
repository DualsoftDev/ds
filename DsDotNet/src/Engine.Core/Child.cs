using Engine.Common;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;

namespace Engine.Core
{
    /// <summary> Segment 내에 배치된 `Child`.  SubCall 또는 ExSegmentCall 를 Coin 으로 갖는 wrapper</summary>
    /// 
    /// 
    [DebuggerDisplay("{ToString()}")]
    public class Child : Named, IVertex, ICoin
    {
        public Segment Parent { get; }
        /// <summary>Call or ExSegmentCall</summary>
        public Coin Coin { get; }
        public bool IsCall => Coin is SubCall;
        public bool IsAlias { get; set; }
        // 부모가 바라본 child 상태
        public Status4 Status
        {
            get => Parent.ChildStatusMap[this];
            set => Parent.ChildStatusMap[this] = value;
        }

        public List<Tag> TagsStart { get; } = new List<Tag>();
        public List<Tag> TagsReset { get; } = new List<Tag>();
        public List<Tag> TagsEnd { get; } = new List<Tag>();

        CompositeDisposable _disposables = new CompositeDisposable();
        public Child(Coin coin, Segment parent)
            :base(coin.Name)
        {
            Parent = parent;
            Coin = coin;
            QualifiedName = $"{parent.QualifiedName}_{coin.Name}";
            Parent.AddChildVertex(this);
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
                    //extSeg.Going();
                    break;
                case SubCall call:
                    //call.Going();
                    break;
                default:
                    throw new Exception("ERROR");
            }
            TagsStart.Iter(t => t.Value = true);
        }
    }

    public static class ChildExtension
    {
    }
}
