using System.Reactive.Disposables;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;

namespace Engine.Core;

[DebuggerDisplay("{ToText(),nq}")]
public partial class Segment : ChildFlow, IVertex, ICoin, IWallet, ITxRx
{
    public RootFlow ContainerFlow { get; internal set; }
    Cpu _cpu;
    public Cpu Cpu {
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

    public Tag TagStart { get; }
    public Tag TagReset { get; }
    public Tag TagEnd { get; }
    public Tag Going { get; internal set; } // Flag or Tag
    public Tag Ready { get; internal set; } // Flag or Tag

    public bool IsResetFirst { get; internal set; } = true;

    public Child[] Inits { get; internal set; }
    public Child[] Lasts { get; internal set; }
    public IVertex[] ChildrenOrigin { get; internal set; }
    public VertexAndOutgoingEdges[] TraverseOrder { get; internal set; }
    public bool Value { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    //public bool Value { get => PortE.Value; set => throw new NotImplementedException(); }
    public virtual bool Evaluate() => Value;

    internal CancellationTokenSource MovingCancellationTokenSource { get; set; }


    internal CompositeDisposable Disposables = new();


    /// <summary>Segment 생성 함수.  Segment 에서 상속받은 class 객체를 생성하기 위함. (e.g Engine.Runner.FsSegment)</summary>
    public static Func<string, RootFlow, Segment> Create { get; set; } =
        (string name, RootFlow containerFlow) =>
        {
            Debug.Assert(false);        // should be overriden
            var seg = new Segment(containerFlow.Cpu, name) { ContainerFlow = containerFlow };
            containerFlow.AddChildVertex(seg);
            return seg;
        };


    internal Segment(Cpu cpu, string name, string startTagName=null, string resetTagName=null, string endTagName=null)
        : base(cpu, name)
    {
        _cpu = cpu;
        var uid = EmLinq.UniqueId;
        var ns = startTagName ?? $"Start_{name}_{uid()}";
        var nr = resetTagName ?? $"Reset_{name}_{uid()}";
        var ne = endTagName   ?? $"End_{name}_{uid()}";
        TagStart = new Tag(cpu, this, ns, TagType.Q | TagType.Start) { InternalName = "Start"};
        TagReset = new Tag(cpu, this, nr, TagType.Q | TagType.Reset) { InternalName = "Reset" };
        TagEnd   = new Tag(cpu, this, ne, TagType.I | TagType.End)   { InternalName = "End" };
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


public static class SegmentExtension
{
    public static void CancelGoing(this Segment segment)
    {
        segment.MovingCancellationTokenSource.Cancel();
        segment.MovingCancellationTokenSource = null;
    }
    public static void CancelHoming(this Segment segment)
    {
        segment.MovingCancellationTokenSource.Cancel();
        segment.MovingCancellationTokenSource = null;
    }

    public static bool IsChildrenStatusAllWith(this Segment segment, Status4 status) =>
        segment.ChildStatusMap.Values.Select(tpl => tpl.Item2).All(st => st == status);
    public static bool IsChildrenStatusAnyWith(this Segment segment, Status4 status) =>
        segment.ChildStatusMap.Values.Select(tpl => tpl.Item2).Any(st => st == status);

    //public static void OnChildEndTagChanged(this Segment segment, BitChange bc)
    //{
    //    var tag = bc.Bit as Tag;
    //    var child = segment.Children.Where(c => c.TagsEnd.Any(t => t.Name == tag.Name));
    //}


    public static void Epilogue(this Segment segment)
    {
        // child 의 최초 상태 등록 : null (vs Homing?)
        segment.ChildStatusMap =
            segment.Children
            .ToDictionary(child => child, _ => (false, (Status4?)null))// Status4.Homing)
            ;

        // Graph 정보 추출 & 저장
        var gi = segment.GraphInfo;
        segment.Inits = gi.Inits.OfType<Child>().ToArray();
        segment.Lasts = gi.Lasts.OfType<Child>().ToArray();
        segment.TraverseOrder = gi.TraverseOrders;

        segment.PrintPortInfos();
    }

    public static void PrintPortInfos(this Segment seg)
    {
        var s = seg.TagStart?.Name;
        var r = seg.TagReset?.Name;
        var e = seg.TagEnd?.Name;
        Global.Logger.Debug($"Tags for segment [{seg.QualifiedName}]:({s}, {r}, {e})");
    }

    public static IEnumerable<PortInfo> GetAllPorts(this Segment segment)
    {
        var s = segment;
        yield return s.PortS;
        yield return s.PortR;
        yield return s.PortE;
    }


    public static void CreateSREGR(this Segment segment, Cpu cpu, PortInfoStart sp, PortInfoReset rp, PortInfoEnd ep, Tag going, Tag ready)
    {
        var s = segment;
        var n = s.QualifiedName;
        Debug.Assert((new object[] { s.PortS, s.PortR, s.PortE, s.Going, s.Ready, }).ForAll(b => b is null));
        s.PortS = sp ?? new PortInfoStart(cpu, s, $"PortInfoS_{n}", new Flag(cpu, $"PortSDefaultPlan_{n}"), null);
        s.PortR = rp ?? new PortInfoReset(cpu, s, $"PortInfoR_{n}", new Flag(cpu, $"PortRDefaultPlan_{n}"), null);
        s.PortE = ep ?? PortInfoEnd.Create(cpu, s, $"PortInfoE_{n}", null);
        s.Going = going ?? new Tag(cpu, s, $"Going_{n}", TagType.Going);
        s.Ready = ready ?? new Tag(cpu, s, $"Ready_{n}", TagType.Ready);

    }
}
