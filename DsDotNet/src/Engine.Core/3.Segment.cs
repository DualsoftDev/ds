using System.Reactive.Disposables;
using System.Threading;

namespace Engine.Core;

[DebuggerDisplay("{ToText(),nq}")]
public partial class Segment : ChildFlow, IVertex, ICoin, IWallet, ITxRx, ITagSREContainer// Coin
{
    public RootFlow ContainerFlow { get; }
    public Cpu Cpu { get => ContainerFlow.Cpu; set => throw new NotImplementedException(); }
    public string QualifiedName => $"{ContainerFlow.QualifiedName}_{Name}";


    public PortInfoStart PortInfoS { get; set; }
    public PortInfoReset PortInfoR { get; set; }
    public PortInfoEnd PortInfoE { get; set; }

    TagSREContainer _tagSREContainer = new();
    public IEnumerable<Tag> TagsStart => _tagSREContainer.TagsStart;
    public IEnumerable<Tag> TagsReset => _tagSREContainer.TagsReset;
    public IEnumerable<Tag> TagsEnd => _tagSREContainer.TagsEnd;
    public Tag TagGoing { get; internal set; }
    public Tag TagReady { get; internal set; }

    public void AddStartTags(params Tag[] tags) => _tagSREContainer.AddStartTags(tags);
    public void AddResetTags(params Tag[] tags) => _tagSREContainer.AddResetTags(tags);
    public void AddEndTags(params Tag[] tags) => _tagSREContainer.AddEndTags(tags);
    public Action<IEnumerable<Tag>> AddTagsFunc => _tagSREContainer.AddTagsFunc;


    public bool IsResetFirst { get; internal set; } = true;

    public Child[] Inits { get; internal set; }
    public Child[] Lasts { get; internal set; }
    public VertexAndOutgoingEdges[] TraverseOrder { get; internal set; }
    internal Dictionary<Coin, Child> CoinChildMap { get; set; }
    public bool Value { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    //public bool Value { get => PortE.Value; set => throw new NotImplementedException(); }
    public virtual bool Evaluate() => Value;

    internal CancellationTokenSource MovingCancellationTokenSource { get; set; }


    internal CompositeDisposable Disposables = new();

    public Segment(string name, RootFlow containerFlow)
        : base(containerFlow.Cpu, name)
    {
        ContainerFlow = containerFlow;
        //containerFlow.ChildVertices.Add(this);
        containerFlow.AddChildVertex(this);

        // todo : Segment Port 생성
        //PortInfoS = new PortInfoStart(this);
        //PortInfoR = new PortInfoReset(this);
        //PortInfoE = new PortInfoEnd(this);
    }

    internal Segment(string name)
        : base(null, name)
    {}

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
        segment.ChildStatusMap.Values.All(st => st == status);
    public static bool IsChildrenStatusAnyWith(this Segment segment, Status4 status) =>
        segment.ChildStatusMap.Values.Any(st => st == status);

    public static void OnChildEndTagChanged(this Segment segment, BitChange bc)
    {
        var tag = bc.Bit as Tag;
        var child = segment.Children.Where(c => c.TagsEnd.Any(t => t.Name == tag.Name));
    }


    public static void Epilogue(this Segment segment)
    {
        segment.ChildStatusMap =
            segment.Children
            .ToDictionary(child => child, _ => Status4.Homing)
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

    public static IEnumerable<Tag> GetSREGRTags(this Segment segment)
    {
        var s = segment;
        foreach (var t in s.GetSRETags())
            yield return t;

        if (s.TagGoing is not null)
            yield return s.TagGoing;
        if (s.TagReady is not null)
            yield return s.TagReady;
    }

}
