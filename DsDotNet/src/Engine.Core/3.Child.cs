namespace Engine.Core;

/// <summary> Segment 내에 배치된 `Child`.  SubCall 또는 ExSegmentCall 를 Coin 으로 갖는 wrapper</summary>
///
///
[DebuggerDisplay("{ToText()}")]
public class Child : Named, IVertex, ICoin, ITagSREContainer
{
    public Segment Parent { get; }
    /// <summary>Call or ExSegmentCall</summary>
    public Coin Coin { get; }
    public bool IsCall => Coin is SubCall;
    public bool IsAlias { get; set; }
    // 부모가 바라본 child 상태
    public Status4? Status
    {
        get => Parent.ChildStatusMap[this].Item2;
        set => Parent.ChildStatusMap[this] = (Parent.ChildStatusMap[this].Item1, value);
    }
    public bool IsFlipped
    {
        get => Parent.ChildStatusMap[this].Item1;
        set => Parent.ChildStatusMap[this] = (value, Parent.ChildStatusMap[this].Item2);
    }

    TagSREContainer _tagSREContainer = new();
    public IEnumerable<Tag> TagsStart => _tagSREContainer.TagsStart;
    public IEnumerable<Tag> TagsReset => _tagSREContainer.TagsReset;
    public IEnumerable<Tag> TagsEnd => _tagSREContainer.TagsEnd;
    public void AddStartTags(params Tag[] tags) => _tagSREContainer.AddStartTags(tags);
    public void AddResetTags(params Tag[] tags) => _tagSREContainer.AddResetTags(tags);
    public void AddEndTags(params Tag[] tags) => _tagSREContainer.AddEndTags(tags);
    public Action<IEnumerable<Tag>> AddTagsFunc => _tagSREContainer.AddTagsFunc;


    CompositeDisposable _disposables = new();
    public Child(Coin coin, Segment parent)
        :base(coin.Name)
    {
        Parent = parent;
        Coin = coin;
        QualifiedName = $"{parent.QualifiedName}_{coin.Name}";
        Parent.AddChildVertex(this);
        //switch(coin)
        //{
        //    case Call call when call.RxTags.Any():

        //}
    }

    public string QualifiedName { get; }
    public bool Value { get => Coin.Value; set => Coin.Value = value; }
    public virtual bool Evaluate() => Value;

    public Cpu Cpu { get => Parent.Cpu; set => throw new NotImplementedException(); }

    public override string ToString() => ToText();
    public override string ToText() => $"{QualifiedName}[{this.GetType().Name}] : " + (IsCall ? "CALL" : "ExSegmentCall");
    //public void Going()
    //{
    //    //Coin.Going();

    //    switch(Coin)
    //    {
    //        case ExSegmentCall extSeg:
    //            //extSeg.Going();
    //            break;
    //        case SubCall call:
    //            //call.Going();
    //            break;
    //        default:
    //            throw new Exception("ERROR");
    //    }
    //    TagsStart.Iter(t => t.Value = true);
    //}
}

public static class ChildExtension
{
    //static IEnumerable<Tag> getStartTags(this Coin coin) =>
    //    coin switch
    //    {
    //        ExSegmentCall extSeg => extSeg.ExternalSegment.TagsStart,
    //        SubCall call => call.Prototype.TXs.OfType<Segment>().SelectMany(seg => seg.TagsStart),
    //        _ => throw new Exception("ERROR"),
    //    };
    //static IEnumerable<Tag> getResetTags(this Coin coin) =>
    //    coin switch
    //    {
    //        ExSegmentCall extSeg => extSeg.ExternalSegment.TagsReset,
    //        //SubCall call => Enumerable.Empty<Tag>(),
    //        _ => throw new Exception("ERROR"),
    //    };
    //static IEnumerable<Tag> getEndTags(this Coin coin) =>
    //    coin switch
    //    {
    //        ExSegmentCall extSeg => extSeg.ExternalSegment.TagsEnd,
    //        SubCall call => call.Prototype.RXs.OfType<Segment>().SelectMany(seg => seg.TagsEnd),
    //        _ => throw new Exception("ERROR"),
    //    };

    //public static IEnumerable<Tag> GetStartTags(this Coin coin) => getStartTags(coin).Where(t => t.Type.HasFlag(TagType.Flow));
    //public static IEnumerable<Tag> GetResetTags(this Coin coin) => getResetTags(coin).Where(t => t.Type.HasFlag(TagType.Flow));
    //public static IEnumerable<Tag> GetEndTags(this Coin coin) => getEndTags(coin).Where(t => t.Type.HasFlag(TagType.Flow));

    //public static IEnumerable<Tag> GetStartTags(this Child child) => child.Coin.GetStartTags();
    //public static IEnumerable<Tag> GetResetTags(this Child child) => child.Coin.GetResetTags();
    //public static IEnumerable<Tag> GetEndTags(this Child child) => child.Coin.GetEndTags();
    public static IEnumerable<Tag> GetStartTags(this Child child) => child.TagsStart;
    public static IEnumerable<Tag> GetResetTags(this Child child) => child.TagsReset;
    public static IEnumerable<Tag> GetEndTags(this Child child) => child.TagsEnd;
}
