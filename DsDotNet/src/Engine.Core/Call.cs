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
        public Flow Container;
        public override bool Value => Prototype.Value;
        public override string QualifiedName => this.GetQualifiedName();
        public Tag[] TxTags { get; set; }
        public Tag[] RxTags { get; set; }

        public IEnumerable<ITxRx> TXs => Prototype.TXs;
        public IEnumerable<ITxRx> RXs => Prototype.RXs;
        public IEnumerable<ITxRx> TxRxs => TXs.Concat(RXs);

        public Call(string name, Flow flow, CallPrototype protoType) : base(name)
        {
            Prototype = protoType;
            Container = flow;
            flow.ChildVertices.Add(this);
        }

        public override void Going() => TxTags.Iter(t => t.Value = true);
    }

    public class CallAlias : Call, IAlias
    {
        public string AliasTargetName;
        public CallAlias(string name, string aliasTargetName, Flow container, CallPrototype protoType)
            : base(name, container, protoType)
        {
            AliasTargetName = aliasTargetName;
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

        //// 필요한가?
        //public static bool IsPaused(this Call call)
        //{
        //    var rxs = call.RxTags;
        //    var txs = call.TxTags;

        //    var containerFlow = call.Container as RootFlow;
        //    var txing = txs.All(t => t.Value);
        //    var rxed = rxs.All(t => t.Value);

        //    var going = call.RGFH == Status4.Going;
        //    var homing = call.RGFH == Status4.Homing;
        //    if (containerFlow != null)
        //    {
        //        // root 에 배치된 call
        //        return going && !txing && !rxed;
        //    }

        //    var containerSeg = call.Container as Segment;
        //    var parentPaused = containerSeg.Paused;
        //    if (!parentPaused)
        //        return false;

        //    var parentGoing = containerSeg.RGFH == Status4.Going;
        //    var parentHoming = containerSeg.RGFH == Status4.Homing;

        //    return
        //           (parentGoing  && going  && !txing && !rxed  ) // going paused
        //        || (parentHoming && homing ) // homing paused. todo : check ???
        //        ;
        //}


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
