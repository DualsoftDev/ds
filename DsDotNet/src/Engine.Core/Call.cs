using Engine.Common;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Engine.Core
{
    public abstract class CallBase : Coin
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


    /// <summary> Call.  Derived = {SubCall, RootCall.} </summary>
    public abstract class Call : CallBase
    {
        public CallPrototype Prototype;
        public Flow Container;
        public override bool Value => Prototype.Value;
        public override string QualifiedName => this.GetQualifiedName();

        public Call(string name, Flow flow, CallPrototype protoType) : base(name)
        {
            Prototype = protoType;
            Container = flow;
        }

        //public override void Going() => TxTags.Iter(t => t.Value = true);
    }

    /// <summary> Segment 내에 배치된 call </summary>
    public class SubCall : Call
    {
        public Child ContainerChild { get; set; }
        public SubCall(string name, ChildFlow flow, CallPrototype protoType)
            : base(name, flow, protoType)
        {}
    }

    /// <summary> Root 에 배치된 Call </summary>
    public class RootCall : Call
    {
        public List<Tag> TxTags { get; } = new List<Tag>();
        public List<Tag> RxTags { get; } = new List<Tag>();
        public RootCall(string name, RootFlow flow, CallPrototype protoType)
            : base(name, flow, protoType)
        {
            // root flow 에서만 child vertices 에 추가.   (child flow 에서는 Child 로 wrapping 해서 추가됨.)
            flow.ChildVertices.Add(this);
        }
    }




    /// <summary> 외부 segment 에 대한 호출 </summary>
    [DebuggerDisplay("[{ToText()}]")]
    public class ExSegmentCall: Coin
    {
        public Segment ExternalSegment;
        public Child ContainerChild { get; set; }

        public ExSegmentCall(string aliasName, Segment externalSegment)
            : base(aliasName)
        {
            ExternalSegment = externalSegment;
        }
        public override string ToText() => $"{Name}={ExternalSegment.QualifiedName}";

    }

    public static class CallExtension
    {
        public static string GetQualifiedName(this ICoin coin)
        {
            switch(coin)
            {
                case RootCall rootCall:
                    var rootFlow = rootCall.Container;
                    var system = rootFlow.GetSystem();
                    return $"{system.Name}.{rootFlow.Name}.{rootCall.Name}";

                case Child child:
                    return child.QualifiedName;

                case Call call:
                    return call.Container switch
                    {
                        Segment seg   => $"{seg.QualifiedName}_{call.Name}",
                        RootFlow flow => $"{flow.QualifiedName}_{call.Name}",
                        _             => throw new Exception("ERROR"),
                    };
                default:
                    throw new Exception("ERROR");
            }
        }
    }

}
