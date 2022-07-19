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

        public Call(string name, Flow flow, CallPrototype protoType) : base(name)
        {
            Prototype = protoType;
            Container = flow;



            //flow.ChildVertices.Add(this);
            //flow.AddChildVertex(this);

            // child flow 에서는 Child 로 wrapping 해서 추가됨.
            if (flow is RootFlow)
                flow.ChildVertices.Add(this);
        }

        public override void Going() => TxTags.Iter(t => t.Value = true);
    }


    [DebuggerDisplay("[{ToText()}]")]
    public class ExSegmentCall: Coin
    {
        public Segment ExternalSegment;

        public ExSegmentCall(string aliasName, Segment externalSegment)
            : base(aliasName)
        {
            ExternalSegment = externalSegment;
        }
        public override string ToText() => $"{Name}={ExternalSegment.QualifiedName}";

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
    }

}
