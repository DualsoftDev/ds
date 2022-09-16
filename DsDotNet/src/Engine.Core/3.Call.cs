namespace Engine.Core;

public abstract class CallBase : Coin
{
    public CallBase(string name) : base(name) { }
}



public class Xywh
{
    public Xywh(int x, int y, int? w, int? h)
    {
        X = x;
        Y = y;
        W = w;
        H = h;
    }

    public int X { get; }
    public int Y { get; }
    public int? W { get; }
    public int? H { get; }
}

public class CallPrototype : CallBase
{
    public RootFlow RootFlow { get; }
    /// <summary> 주로 target system 의 segment </summary>
    public List<ITxRx> TXs = new(); // empty 이면 '_' 를 의미
    public List<ITxRx> RXs = new(); // empty 이면 '_' 를 의미
    public IVertex ResetSrouce;

    public override bool Value
    {
        get
        {
            bool getRxValue(ITxRx rx)
            {
                switch (rx)
                {
                    case SegmentBase seg: return seg.TagPEnd.Value;
                    case IBit bit: return bit.Value;   // todo TAG 아닌 경우 처리 필요함.
                }
                throw new Exception("Unknown type ERROR");
            }
            return RXs.All(getRxValue);
        }
        set => throw new Exception("ERROR");
    }

    public CallPrototype(string name, RootFlow flow)
        : base(name)
    {
        RootFlow = flow;
        flow.CallPrototypes.Add(this);
    }

    public override string[] NameComponents => RootFlow.NameComponents.Concat(new[] {Name }).ToArray();
    public Xywh Xywh { get; set; }

}


/// <summary> Call.  Derived = {SubCall, RootCall.} </summary>
[DebuggerDisplay("{ToText()}")]
public abstract class Call : CallBase
{
    public CallPrototype Prototype;
    public Flow Container;
    public override bool Value => Prototype.Value;
    public override string QualifiedName => this.GetQualifiedName();
    public override Cpu Cpu { get => Container.Cpu; set => throw new Exception("ERROR"); }

    public Call(string name, Flow flow, CallPrototype protoType) : base(name)
    {
        Prototype = protoType;
        Container = flow;
    }

    //public override void Going() => TxTags.Iter(t => t.Value = true);
    public override string ToText() => $"{QualifiedName}[{this.GetType().Name}]";
}

/// <summary> Segment 내에 배치된 call </summary>
public class SubCall : Call
{
    public Child ContainerChild { get; set; }
    public SubCall(string name, ChildFlow flow, CallPrototype protoType)
        : base(name, flow, protoType)
    { }
}

/// <summary> Root 에 배치된 Call </summary>
public class RootCall : Call
{
    TagDic _txTags = new();
    TagDic _rxTags = new();

    public IEnumerable<Tag> TxTags => _txTags.Values;
    public IEnumerable<Tag> RxTags => _rxTags.Values;

    void AddTags(TagDic dic, IEnumerable<Tag> tags)
    {
        foreach (var tag in tags)
        {
            Assert(tag.Cpu == Cpu);     // ! call 이므로 다른 system 호출용 tag 여야 함
            dic.Add(tag.Name, tag);
        }

    }
    public void AddRxTags(IEnumerable<Tag> tags) => AddTags(_rxTags, tags);
    public void AddTxTags(IEnumerable<Tag> tags) => AddTags(_txTags, tags);

    public RootCall(string name, RootFlow flow, CallPrototype protoType)
        : base(name, flow, protoType)
    {
        // root flow 에서만 child vertices 에 추가.   (child flow 에서는 Child 로 wrapping 해서 추가됨.)
        flow.AddChildVertex(this);
    }
}




/// <summary> 외부 segment 에 대한 호출 </summary>
[DebuggerDisplay("[{ToText()}]")]
public class ExSegment : Coin
{
    public SegmentBase ExternalSegment;
    public Child ContainerChild { get; set; }

    public ExSegment(string aliasName, SegmentBase externalSegment)
        : base(aliasName)
    {
        ExternalSegment = externalSegment;
    }
    public override string ToText() => $"{Name}={ExternalSegment.QualifiedName}";

}

public static class CallExtension
{
    public static string GetQualifiedName(this IVertex vertex)
    {
        return vertex switch
        {
            ICoin coin => coin.GetQualifiedName(),
            _ => vertex.ToString(),
        };
    }
    public static string GetQualifiedName(this ICoin coin)
    {
        switch (coin)
        {
            case RootCall rootCall:
                var rootFlow = rootCall.Container;
                var system = rootFlow.GetSystem();
                return $"{system.Name}.{rootFlow.Name}.{rootCall.Name}";
            case SegmentBase rootSegment:
                return rootSegment.QualifiedName;
            case CallPrototype cp:
                return cp.QualifiedName;

            case Child child:
                return child.QualifiedName;

            case Call call:
                return call.Container switch
                {
                    SegmentBase seg   => $"{seg.QualifiedName}.{call.Name}",
                    RootFlow flow => $"{flow.QualifiedName}.{call.Name}",
                    _             => throw new Exception("ERROR"),
                };
            default:
                throw new Exception("ERROR");
        }
    }
}

