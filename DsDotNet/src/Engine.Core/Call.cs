using Engine.Common;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Engine.Core
{
    public class CallBase : Coin
    {
        public CallBase(string name) : base(name) {}
    }

    public class CallPrototype : CallBase
    {
        public DsTask Task;

        /// <summary> 주로 target system 의 segment </summary>
        public List<ITxRx> TXs = new List<ITxRx>(); // empty 이면 '_' 를 의미
        public List<ITxRx> RXs = new List<ITxRx>(); // empty 이면 '_' 를 의미
        public IVertex ResetSrouce;

        public override bool Value
        {
            get {
                bool getRxValue(ITxRx rx)
                {
                    switch (rx)
                    {
                        case Segment seg: return seg.TagE.Value;
                        case IBit bit: return bit.Value;   // todo TAG 아닌 경우 처리 필요함.
                    }
                    throw new Exception("Unknown type ERROR");
                }
                return RXs.All(getRxValue);
            }
            set => throw new Exception("XXXX ERROR");
        }

        public CallPrototype(string name, DsTask task)
            : base(name)
        {
            Task = task;
            task.CallPrototypes.Add(this);
        }

    }

    public class Call : CallBase
    {
        public CallPrototype Prototype;
        public IWallet Container;
        public override bool Value => Prototype.Value;

        // call 은 상태 저장.  segment 는 상태 동적 계산
        public Status4 RGFH { get; set; } = Status4.Homing;
        // Do not store Paused property
        public override bool Paused => this.IsPaused();

        public string QualifiedName => this.GetQualifiedName();
        public Tag[] TxTags { get; set; }
        public Tag[] RxTags { get; set; }

        public IEnumerable<ITxRx> TXs => Prototype.TXs;
        public IEnumerable<ITxRx> RXs => Prototype.RXs;
        public IEnumerable<ITxRx> TxRxs => TXs.Concat(RXs);

        public Call(string name, IWallet container, CallPrototype protoType) : base(name)
        {
            Prototype = protoType;
            Container = container;

            Flow flow = container as Flow;
            var containerSegment = container as Segment;
            if (flow == null && containerSegment != null)
                flow = containerSegment.ChildFlow;

            flow.ChildVertices.Add(this);
        }
    }

    public static class CallExtension
    {
        public static string GetQualifiedName(this Call call)
        {
            switch(call.Container)
            {
                case Segment seg:
                    return $"{seg.QualifiedName}_{call.Name}";
                case RootFlow flow:
                    return $"{flow.QualifiedName}_{call.Name}";
                default:
                    throw new Exception("ERROR");
            }
        }

        // 필요한가?
        public static bool IsPaused(this Call call)
        {
            var rxs = call.RxTags;
            var txs = call.TxTags;

            var containerFlow = call.Container as RootFlow;
            var txing = txs.All(t => t.Value);
            var rxed = rxs.All(t => t.Value);

            var going = call.RGFH == Status4.Going;
            var homing = call.RGFH == Status4.Homing;
            if (containerFlow != null)
            {
                // root 에 배치된 call
                return going && !txing && !rxed;
            }

            var containerSeg = call.Container as Segment;
            var parentPaused = containerSeg.Paused;
            if (!parentPaused)
                return false;

            var parentGoing = containerSeg.RGFH == Status4.Going;
            var parentHoming = containerSeg.RGFH == Status4.Homing;

            return
                   (parentGoing  && going  && !txing && !rxed  ) // going paused
                || (parentHoming && homing ) // homing paused. todo : check ???
                ;
        }

        public static void Going(this Call call)
        {
            if (call.RGFH == Status4.Ready)
                call.RGFH = Status4.Going;

            Debug.Assert(call.RGFH == Status4.Going);
            call.TxTags.Iter(t => t.Value = true);
        }

        public static IEnumerable<Tag> GetTxTags(this Call call)
        {
            var tags = call.OwnerCpu.Tags;
            foreach (var tx in call.TXs)
            {
                switch(tx)
                {
                    case Segment seg:
                        yield return tags[seg.TagS.Name];
                        break;
                    case Tag tag:
                        yield return tags[tag.Name];
                        break;
                    default:
                        throw new Exception("ERROR");
                }
            }
        }

        public static IEnumerable<Tag> GetRxTags(this Call call)
        {
            var tags = call.OwnerCpu.Tags;
            foreach (var rx in call.RXs)
            {
                switch (rx)
                {
                    case Segment seg:
                        yield return tags[seg.TagE.Name];
                        break;
                    case Tag tag:
                        yield return tags[tag.Name];
                        break;
                    default:
                        throw new Exception("ERROR");
                }
            }
        }

        public static IEnumerable<Tag> GetTxRxTags(this Call call) =>
            GetTxTags(call).Concat(GetRxTags(call));
    }

}
