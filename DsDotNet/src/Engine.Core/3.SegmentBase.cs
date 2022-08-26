namespace Engine.Core;

using System.Threading;

/// <summary>Segment 생성 함수.  Segment 에서 상속받은 class 객체를 생성하기 위함. (e.g Engine.Runner.FsSegment)</summary>
public delegate SegmentBase SegmentCreator(string segmentName, RootFlow rootFlow);

[DebuggerDisplay("{ToText(),nq}")]
public abstract partial class SegmentBase : ChildFlow, IVertex, ICoin, IWallet, ITxRx
{
    public RootFlow ContainerFlow { get; internal set; }
    Cpu _cpu;
    public new Cpu Cpu {
        get => _cpu;
        set
        {
            if (ContainerFlow != null)
                Debug.Assert(value == ContainerFlow.Cpu);
            _cpu = value;
        }
    }
    public string QualifiedName =>
        (ContainerFlow == null)
        ? Name
        : $"{ContainerFlow.QualifiedName}_{Name}"
        ;

    public PortInfoStart PortS { get; set; }
    public PortInfoReset PortR { get; set; }
    public PortInfoEnd PortE { get; set; }

    /// <summary>Plan tag for start</summary>
    public TagP TagPStart { get; internal set; }
    /// <summary>Plan tag for reset</summary>
    public TagP TagPReset { get; internal set; }
    /// <summary>Plan tag for end</summary>
    public TagP TagPEnd { get; internal set; }
    /// <summary>Actual tag for start</summary>
    public TagA TagAStart { get; internal set; }
    /// <summary>Actual tag for reset</summary>
    public TagA TagAReset { get; internal set; }
    /// <summary>Actual tag for end</summary>
    public TagA TagAEnd { get; internal set; }


    public Tag Going { get; internal set; } // Flag or Tag
    public Tag Ready { get; internal set; } // Flag or Tag

    internal Tuple<string, string, string> Addresses { get; set; } = new Tuple<string, string, string>(null, null, null);

    public bool IsResetFirst { get; internal set; } = true;

    public virtual void Epilogue()
    {
        // child 의 최초 상태 등록 : null (vs Homing?)
        ChildStatusMap =
            Children
            .ToDictionary(child => child, _ => (false, (Status4?)null))// Status4.Homing)
            ;
    }



    public bool Value { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    //public bool Value { get => PortE.Value; set => throw new NotImplementedException(); }
    public virtual bool Evaluate() => Value;

    internal CompositeDisposable Disposables = new();


    /// <summary>Segment 생성 함수.  Segment 에서 상속받은 class 객체를 생성하기 위함. (e.g Engine.Runner.FsSegment)</summary>
    public static SegmentCreator Create { get; set; } =
        (string name, RootFlow containerFlow) =>
        {
            Debug.Assert(Global.IsInUnitTest);        // should be overriden if not unit test
            var seg = new DummySegment(containerFlow.Cpu, name) { ContainerFlow = containerFlow };
            containerFlow.AddChildVertex(seg);
            return seg;
        };


    internal SegmentBase(Cpu cpu, string name, string startTagName=null, string resetTagName=null, string endTagName=null)
        : base(cpu, name)
    {
        _cpu = cpu;
    }


    public Status4 Status =>
        (PortS.Value, PortR.Value, PortE.Value) switch
        {
            (false, false, false) => Status4.Ready,  //??
            (true, false, false) => Status4.Going,
            (_, false, true) => Status4.Finished,
            (_, true, _) => Status4.Homing,
        };

    public bool IsOriginating { get; set; }
    public override string ToString() => ToText();
    public override string ToText()
    {
        var c = ChildVertices == null ? 0 : ChildVertices.Count();
        return $"{QualifiedName}[{this.GetType().Name}] ={Cpu?.Name}, #children={c}";
    }
}

class DummySegment: SegmentBase
{
    public DummySegment(Cpu cpu, string name, string startTagName=null, string resetTagName=null, string endTagName=null)
        : base(cpu, name, startTagName, resetTagName, endTagName)
    {
    }
}


public static class SegmentExtension
{
    public static void PrintPortPlanTags(this SegmentBase seg)
    {
        var s = seg.TagPStart?.Name;
        var r = seg.TagPReset?.Name;
        var e = seg.TagPEnd?.Name;
        Global.Logger.Debug($"Tags for segment [{seg.QualifiedName}]:({s}, {r}, {e})");
    }
}