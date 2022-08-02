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
    public Status4 Status
    {
        get => Parent.ChildStatusMap[this];
        set => Parent.ChildStatusMap[this] = value;
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
}
