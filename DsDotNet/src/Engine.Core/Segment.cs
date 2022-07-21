using Engine.Common;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Engine.Core
{
    [DebuggerDisplay("{ToText(),nq}")]
    public partial class Segment : ChildFlow, IVertex, ICoin, IWallet, IWithSREPorts, ITxRx, ITagSREContainer// Coin
    {
        public RootFlow ContainerFlow { get; }
        public CpuBase OwnerCpu { get => ContainerFlow.Cpu; set => throw new NotImplementedException(); }
        public string QualifiedName => $"{ContainerFlow.QualifiedName}_{Name}";


        public PortS PortS { get; set; }
        public PortR PortR { get; set; }
        public PortE PortE { get; set; }
        public Port[] AllPorts => new Port[] { PortS, PortR, PortE };

        TagSREContainer _tagSREContainer = new TagSREContainer();
        public IEnumerable<Tag> TagsStart => _tagSREContainer.TagsStart;
        public IEnumerable<Tag> TagsReset => _tagSREContainer.TagsReset;
        public IEnumerable<Tag> TagsEnd => _tagSREContainer.TagsEnd;

        public void AddStartTags(params Tag[] tags) => _tagSREContainer.AddStartTags(tags);
        public void AddResetTags(params Tag[] tags) => _tagSREContainer.AddResetTags(tags);
        public void AddEndTags(params Tag[] tags) => _tagSREContainer.AddEndTags(tags);
        public Action<IEnumerable<Tag>> AddTagsFunc => _tagSREContainer.AddTagsFunc;


        public Tag TagS { get; set; }
        public Tag TagR { get; set; }
        public Tag TagE { get; set; }

        public bool IsResetFirst { get; internal set; } = true;

        public Child[] Inits { get; internal set; }
        public Child[] Lasts { get; internal set; }
        public VertexAndOutgoingEdges[] TraverseOrder { get; internal set; }
        internal Dictionary<Coin, Child> CoinChildMap { get; set; }
        public bool Value { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }


        internal CompositeDisposable Disposables = new CompositeDisposable();

        public Segment(string name, RootFlow containerFlow)
            : base(name)
        {
            ContainerFlow = containerFlow;
            //containerFlow.ChildVertices.Add(this);
            containerFlow.AddChildVertex(this);

            PortS = new PortS(this);
            PortR = new PortR(this);
            PortE = new PortE(this);
        }

        public override string ToString() => ToText();
        public override string ToText()
        {
            var c = ChildVertices == null ? 0 : ChildVertices.Count();
            return $"{QualifiedName}: cpu={OwnerCpu?.Name}, #children={c}";
        }
    }


    public static class SegmentExtension
    {

        public static bool IsChildrenStatusAllWith(this Segment segment, Status4 status) => segment.ChildStatusMap.Values.All(st => st == status);
        public static bool IsChildrenStatusAnyWith(this Segment segment, Status4 status) => segment.ChildStatusMap.Values.Any(st => st == status);

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
        }
    }
}
