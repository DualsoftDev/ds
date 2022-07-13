using System;
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

        /// <summary> 주로 target system 의 segment </summary>
        public List<ITxRx> TXs = new List<ITxRx>(); // empty 이면 '_' 를 의미
        public ITxRx RX;    // null 이면 '_' 를 의미
        public override bool Value
        {
            get {
                switch(RX)
                {
                    case Segment seg: return seg.TagE.Value;
                    case IBit bit: return bit.Value;   // todo TAG 아닌 경우 처리 필요함.
                }
                throw new Exception("Unknown type ERROR");
            }
            set => throw new Exception("XXXX ERROR");
        }

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
        public override bool Value => Prototype.Value;

        // call 은 상태 저장.  segment 는 상태 동적 계산
        public Status4 RGFH { get; set; } = Status4.Homing;


        /*
         * Do not store Paused property
         */
        //private bool _paused;
        //public override bool Paused {
        //    get => _paused;
        //    set {
        //        if (value != _paused)
        //        {
        //            _paused = value;
        //            // call pause 시에 TX 신호 끄기
        //            if (value)
        //                this.GetTxTags().Iter(txTag =>
        //                {
        //                    txTag.Value = false;
        //                    OwnerCpu.OnTagChanged(txTag, false);
        //                });
        //        }
        //    }
        //}


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

    public static class CallExtension
    {
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
        public static Tag GetRxTag(this Call call)
        {
            var tags = call.OwnerCpu.Tags;
            switch (call.RX)
            {
                case Segment seg:
                    return tags[seg.TagE.Name];
                case Tag tag:
                    return tags[tag.Name];
                default:
                    throw new Exception("ERROR");
            }
        }

        public static IEnumerable<Tag> GetTxRxTags(this Call call) =>
            GetTxTags(call).Concat(new[] { GetRxTag(call) })
            ;
    }

}
