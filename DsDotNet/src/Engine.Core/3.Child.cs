namespace Engine.Core;

/// <summary> Segment 내에 배치된 `Child`.  SubCall 또는 ExSegmentCall 를 Coin 으로 갖는 wrapper</summary>
///
///
[DebuggerDisplay("{ToText()}")]
public class Child : Named, IVertex, ICoin
{
    public SegmentBase Parent { get; }
    /// <summary>Call or ExSegmentCall</summary>
    public Coin Coin { get; }
    public bool IsCall => Coin is SubCall;
    public bool IsAlias { get; set; }
    // 부모가 바라본 child 상태
    public Status4? Status
    {
        get => Parent.ChildStatusMap[this].Item2;
        set
        {
            var (flipped, oldState) = Parent.ChildStatusMap[this];
            if (oldState != value)
            {
                Global.ChildStatusChangedSubject.OnNext(new ChildStatusChange(this, value.Value, flipped));
                Parent.ChildStatusMap[this] = (Parent.ChildStatusMap[this].Item1, value);
            }
        }
    }
    public bool IsFlipped
    {
        get => Parent.ChildStatusMap[this].Item1;
        set => Parent.ChildStatusMap[this] = (value, Parent.ChildStatusMap[this].Item2);
    }
    /// <summary>Going 시 원위치 맞추기 작업 중 flag.  Debugging purpose</summary>
    public bool DbgIsOriginating { get; set; }


    /// <summary> Start tag 는 Call 인 경우, 복수의 TX 를 허용해야 한다. </summary>
    public List<Tag> TagsStart { get; set; }
    /// <summary> Reset 은 ExSegment call 을 위한 것으로, reset tag 는 하나만 존재할 수 있다.</summary>
    public Tag TagReset { get; set; }
    /// <summary> End tag 는 Call 인 경우, 복수의 RX 를 허용해야 한다. </summary>
    public List<Tag> TagsEnd { get; set; }



    CompositeDisposable _disposables = new();
    public Child(Coin coin, SegmentBase parent)
        :base(coin.Name)
    {
        Parent = parent;
        Coin = coin;
        QualifiedName = $"{parent.QualifiedName}_{coin.Name}";
        Parent.AddChildVertex(this);
    }

    public string QualifiedName { get; }
    public bool Value { get => Coin.Value; set => Coin.Value = value; }
    public virtual bool Evaluate() => Value;

    public Cpu Cpu { get => Parent.Cpu; set => throw new NotImplementedException(); }

    public override string ToString() => ToText();
    public override string ToText() => $"{QualifiedName}[{this.GetType().Name}] : " + (IsCall ? "CALL" : "ExSegmentCall");
}
