using System.Reactive.Disposables;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;

namespace Engine.Core;

[DebuggerDisplay("{ToText(),nq}")]
public partial class Segment : ChildFlow, IVertex, ICoin, IWallet, ITxRx, ITagSREContainer// Coin
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
    public string QualifiedName => $"{ContainerFlow?.QualifiedName}_{Name}";


    public PortInfoStart PortS { get; set; }
    public PortInfoReset PortR { get; set; }
    public PortInfoEnd PortE { get; set; }

    TagSREContainer _tagSREContainer = new();
    public IEnumerable<Tag> TagsStart => _tagSREContainer.TagsStart;
    public IEnumerable<Tag> TagsReset => _tagSREContainer.TagsReset;
    public IEnumerable<Tag> TagsEnd => _tagSREContainer.TagsEnd;
    public Tag Going { get; internal set; } // Flag or Tag
    public Tag Ready { get; internal set; } // Flag or Tag

    public void AddStartTags(params Tag[] tags) => _tagSREContainer.AddStartTags(tags);
    public void AddResetTags(params Tag[] tags) => _tagSREContainer.AddResetTags(tags);
    public void AddEndTags(params Tag[] tags) => _tagSREContainer.AddEndTags(tags);
    public Action<IEnumerable<Tag>> AddTagsFunc => _tagSREContainer.AddTagsFunc;
    internal string DefaultStartTagAddress { get; set; }
    internal string DefaultResetTagAddress { get; set; }
    internal string DefaultEndTagAddress { get; set; }

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
    //private Segment(string name, RootFlow containerFlow)
    //    : this(containerFlow.Cpu, name)
    //{
    //    ContainerFlow = containerFlow;
    //    _cpu = ContainerFlow.Cpu;
    //    //containerFlow.ChildVertices.Add(this);
    //    containerFlow.AddChildVertex(this);
    //}

    internal Segment(Cpu cpu, string name)
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

    //public Status4 Status
    //{
    //    get
    //    {
    //        var s = PortS.Value;
    //        var r = PortR.Value;
    //        var e = PortE.Value;

    //        //if (seg.Paused)
    //        //{
    //        //    Debug.Assert(!s && !r);
    //        //    return e ? Status4.Homing : Status4.Going;
    //        //}

    //        if (e)
    //            return r ? Status4.Homing : Status4.Finished;

    //        Debug.Assert(!e);
    //        if (s)
    //            return r ? Status4.Ready : Status4.Going;

    //        Debug.Assert(!s && !e);
    //        return Status4.Ready;
    //    }
    //}


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

    public static void OnChildEndTagChanged(this Segment segment, BitChange bc)
    {
        var tag = bc.Bit as Tag;
        var child = segment.Children.Where(c => c.TagsEnd.Any(t => t.Name == tag.Name));
    }


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



        // segment 내의 child call 에 대한 RX tag 변경 시, child origin 검사 및 child 의 status 변경 저장하도록 event handler 등록
        var endTags = segment.Children.SelectMany(c => c.TagsEnd).ToArray();
        var endTagNames = endTags.Select(t => t.Name).ToHashSet();

        var subs =
            Global.BitChangedSubject
                .Where(bc => bc.Bit is Tag && endTagNames.Contains(((Tag)bc.Bit).Name))
                .Subscribe(bc =>
                {
                    segment.OnChildEndTagChanged(bc);
                });
        segment.Disposables.Add(subs);

        segment.PrintPortInfos();
    }

    public static void PrintPortInfos(this Segment seg)
    {
        IEnumerable<string> spit()
        {
            var tagNamesS = string.Join("\r\n\t\t", seg.TagsStart.Select(t => t.Name));
            var tagNamesR = string.Join("\r\n\t\t", seg.TagsReset.Select(t => t.Name));
            var tagNamesE = string.Join("\r\n\t\t", seg.TagsEnd.Select(t => t.Name));
            if (tagNamesS.NonNullAny())
                yield return $"j\r\n\tStart Tags:\r\n\t\t{tagNamesS}";
            if (tagNamesR.NonNullAny())
                yield return $"j\r\n\tReset Tags:\r\n\t\t{tagNamesR}";
            if (tagNamesE.NonNullAny())
                yield return $"j\r\n\tEnd Tags:\r\n\t\t{tagNamesE}";
        }
        var str = string.Concat(spit());
        Global.Logger.Debug($"Tags for segment [{seg.QualifiedName}]:{str}");
    }

    public static IEnumerable<Tag> GetSRETags(this Segment segment)
    {
        var s = segment;
        foreach (var t in s.TagsStart)
            yield return t;
        foreach (var t in s.TagsReset)
            yield return t;
        foreach (var t in s.TagsEnd)
            yield return t;
    }

    public static IEnumerable<PortInfo> GetAllPorts(this Segment segment)
    {
        var s = segment;
        yield return s.PortS;
        yield return s.PortR;
        yield return s.PortE;
    }


    //public static IEnumerable<Tag> GetSREGRTags(this Segment segment)
    //{
    //    var s = segment;
    //    foreach (var t in s.GetSRETags())
    //        yield return t;

    //    if (s.Going is not null)
    //        yield return s.Going;
    //    if (s.Ready is not null)
    //        yield return s.Ready;
    //}

    public static void CreateSREGR(this Segment segment, Cpu cpu, PortInfoStart sp, PortInfoReset rp, PortInfoEnd ep, Tag going, Tag ready)
    {
        var s = segment;
        var n = s.QualifiedName;
        s.PortS = sp ?? new PortInfoStart(cpu, s, $"PortInfoS_{n}", new Flag(cpu, $"PortSDefaultPlan_{n}"), null);
        s.PortR = rp ?? new PortInfoReset(cpu, s, $"PortInfoR_{n}", new Flag(cpu, $"PortRDefaultPlan_{n}"), null);
        s.PortE = ep ?? PortInfoEnd.Create(cpu, s, $"PortInfoE_{n}", null);
        s.Going = going ?? new Tag(cpu, s, $"Going_{n}", TagType.Going);
        s.Ready = ready ?? new Tag(cpu, s, $"Ready_{n}", TagType.Ready);
    }
}
