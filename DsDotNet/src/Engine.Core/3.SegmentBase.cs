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

    public Tag TagStart { get; internal set; }
    public Tag TagReset { get; internal set; }
    public Tag TagEnd { get; internal set; }
    public Tag Going { get; internal set; } // Flag or Tag
    public Tag Ready { get; internal set; } // Flag or Tag

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

    internal CancellationTokenSource MovingCancellationTokenSource { get; set; }


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
    public static void CancelGoing(this SegmentBase segment)
    {
        segment.MovingCancellationTokenSource.Cancel();
        segment.MovingCancellationTokenSource = null;
    }
    public static void CancelHoming(this SegmentBase segment)
    {
        segment.MovingCancellationTokenSource.Cancel();
        segment.MovingCancellationTokenSource = null;
    }

    public static bool IsChildrenStatusAllWith(this SegmentBase segment, Status4 status) =>
        segment.ChildStatusMap.Values.Select(tpl => tpl.Item2).All(st => st == status);
    public static bool IsChildrenStatusAnyWith(this SegmentBase segment, Status4 status) =>
        segment.ChildStatusMap.Values.Select(tpl => tpl.Item2).Any(st => st == status);

    public static void PrintPortInfos(this SegmentBase seg)
    {
        var s = seg.TagStart?.Name;
        var r = seg.TagReset?.Name;
        var e = seg.TagEnd?.Name;
        Global.Logger.Debug($"Tags for segment [{seg.QualifiedName}]:({s}, {r}, {e})");
    }

    public static IEnumerable<PortInfo> GetAllPorts(this SegmentBase segment)
    {
        var s = segment;
        yield return s.PortS;
        yield return s.PortR;
        yield return s.PortE;
    }
}
