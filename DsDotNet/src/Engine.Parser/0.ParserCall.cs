
using Engine.Base;
using Engine.Common;
using Engine.Parser;
using System.Reactive.Disposables;
using System.Windows.Forms.ComponentModel.Com2Interop;
using static Engine.Core.InterfaceClass;
using static Engine.Cpu.Interface;

namespace Engine.Parser;


/// <summary> Segment Or Call base </summary>
[DebuggerDisplay("{ToText()}")]
public abstract class Coin : Named, ICoin
{
    public virtual bool Value { get; set; }
    public virtual bool Evaluate() => Value;

    /*
     * Do not store Paused property (getter only, no setter)
     */
    public virtual bool Paused { get; }
    public virtual ParserCpu Cpu { get; set; }

    public Coin(string name)
        : base(name)
    {
    }


    bool IsChildrenStartPoint() => true;
    public virtual string QualifiedName { get; }
    public override string ToString() => ToText();
    public override string ToText() => $"{QualifiedName}[{this.GetType().Name}]";
    public virtual void Going() => throw new Exception("ERROR");
}


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
    public ParserRootFlow RootFlow { get; }
    /// <summary> 주로 target system 의 segment </summary>
    public List<ITxRx> TXs = new(); // empty 이면 '_' 를 의미
    public List<ITxRx> RXs = new(); // empty 이면 '_' 를 의미
    public IVertex ResetSrouce;

    public override bool Value
    {
        get
        {
            return true;
            //bool getRxValue(ITxRx rx)
            //{
            //    switch (rx)
            //    {
            //        case ParserSegment seg: return seg.TagPEnd.Value;
            //        case IBit bit: return bit.Value;   // todo TAG 아닌 경우 처리 필요함.
            //    }
            //    throw new Exception("Unknown type ERROR");
            //}
            //return RXs.All(getRxValue);
        }
        set => throw new Exception("ERROR");
    }

    public CallPrototype(string name, ParserRootFlow flow)
        : base(name)
    {
        RootFlow = flow;
        flow.CallPrototypes.Add(this);
    }

    public override string QualifiedName => $"{RootFlow.QualifiedName}.{Name}";

    public Xywh Xywh { get; set; }

}


/// <summary> Call.  Derived = {SubCall, RootCall.} </summary>
[DebuggerDisplay("{ToText()}")]
public abstract class Call : CallBase
{
    public CallPrototype Prototype;
    public Flow Container;
    public override bool Value => Prototype.Value;
    public override string QualifiedName => Container.Name;//this.GetQualifiedName();
    //public override ParserCpu Cpu { get => Container.Cpu; set => throw new Exception("ERROR"); }

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
    public SubCall(string name, ParserChildFlow flow, CallPrototype protoType)
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
            //Assert(tag.Cpu == Cpu);     // ! call 이므로 다른 system 호출용 tag 여야 함
            dic.Add(tag.Name, tag);
        }

    }
    public void AddRxTags(IEnumerable<Tag> tags) => AddTags(_rxTags, tags);
    public void AddTxTags(IEnumerable<Tag> tags) => AddTags(_txTags, tags);

    public RootCall(string name, RootFlow flow, CallPrototype protoType)
        : base(name, flow, protoType)
    {
        // root flow 에서만 child vertices 에 추가.   (child flow 에서는 Child 로 wrapping 해서 추가됨.)
        //flow.AddChildVertex(this);
    }
}




/// <summary> 외부 segment 에 대한 호출 </summary>
[DebuggerDisplay("[{ToText()}]")]
public class ExSegment : Coin
{
    public ParserSegment ExternalSegment;
    public Child ContainerChild { get; set; }

    public ExSegment(string aliasName, ParserSegment externalSegment)
        : base(aliasName)
    {
        ExternalSegment = externalSegment;
    }
    public override string ToText() => Name;//$"{Name}={ExternalSegment.QualifiedName}";

}



/// <summary> Segment 내에 배치된 `Child`.  SubCall 또는 ExSegmentCall 를 Coin 으로 갖는 wrapper</summary>
///
///
[DebuggerDisplay("{ToText()}")]
public class Child : Named, IVertex, ICoin
{
    public ParserSegment Parent { get; }
    /// <summary>Call or ExSegmentCall</summary>
    public Coin Coin { get; }
    public bool IsCall => Coin is SubCall;
    public bool IsAlias { get; set; }
    // 부모가 바라본 child 상태
    
    /// <summary>Going 시 원위치 맞추기 작업 중 flag.  Debugging purpose</summary>
    public bool DbgIsOriginating { get; set; }


    /// <summary> Start tag 는 Call 인 경우, 복수의 TX 를 허용해야 한다. </summary>
    public List<Tag> TagsStart { get; set; }
    /// <summary> Reset 은 ExSegment call 을 위한 것으로, reset tag 는 하나만 존재할 수 있다.</summary>
    public Tag TagReset { get; set; }
    /// <summary> End tag 는 Call 인 경우, 복수의 RX 를 허용해야 한다. </summary>
    public List<Tag> TagsEnd { get; set; }



    CompositeDisposable _disposables = new();
    public Child(Coin coin, ParserSegment parent)
        : base(coin.Name)
    {
        Parent = parent;
        Coin = coin;
        //NameComponents = parent.NameComponents.Append(coin.Name).ToArray();
        //Parent.AddChildVertex(this);
    }

    public string[] NameComponents { get; }
    public string QualifiedName => NameComponents.Combine();
    public bool Value { get => Coin.Value; set => Coin.Value = value; }
    public virtual bool Evaluate() => Value;

    //public ParserCpu Cpu { get => Parent.Cpu; set => throw new NotImplementedException(); }

    public override string ToString() => ToText();
    public override string ToText() => $"{QualifiedName}[{this.GetType().Name}] : " + (IsCall ? "CALL" : "ExSegmentCall");
}
