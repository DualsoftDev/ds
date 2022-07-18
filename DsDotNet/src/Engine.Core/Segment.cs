using Engine.Common;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Engine.Core
{
    //[DebuggerDisplay("{ToText(),nq}")]
    [DebuggerDisplay("XXX")]
    public partial class Segment : ChildFlow, IVertex, ICoin, IWallet, IWithSREPorts, ITxRx// Coin
    {
        public RootFlow ContainerFlow { get; }
        public CpuBase OwnerCpu { get => ContainerFlow.Cpu; set => throw new NotImplementedException(); }
        public string QualifiedName => $"{ContainerFlow.QualifiedName}_{Name}";


        public PortS PortS { get; set; }
        public PortR PortR { get; set; }
        public PortE PortE { get; set; }
        public Port[] AllPorts => new Port[] { PortS, PortR, PortE };

        public Tag TagS { get; set; }
        public Tag TagR { get; set; }
        public Tag TagE { get; set; }

        public bool IsResetFirst { get; internal set; } = true;
        public IEnumerable<Call> CallChildren => ChildVertices.OfType<Call>();


        public Child[] Children { get; internal set; }
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
            containerFlow.ChildVertices.Add(this);

            PortS = new PortS(this);
            PortR = new PortR(this);
            PortE = new PortE(this);
        }

        public override string ToString() => ToText();
        public override string ToText()
        {
            var c = ChildVertices == null ? 0 : ChildVertices.Count();
            return $"{Name}: cpu={OwnerCpu?.Name}, #children={c}";
        }
    }


    public static class SegmentExtension
    {

        public static bool IsChildrenStatusAllWith(this Segment segment, Status4 status) => segment.ChildStatusMap.Values.All(st => st == status);
        public static bool IsChildrenStatusAnyWith(this Segment segment, Status4 status) => segment.ChildStatusMap.Values.Any(st => st == status);

        public static void OnChildRxTagChanged(this Segment segment, BitChange bc)
        {
            var tag = bc.Bit as Tag;
            var calls = segment.CallChildren.Where(c => c.RxTags.Any(t => t.Name == tag.Name));
        }


        public static void Epilogue(this Segment segment)
        {
            // coin -> child map
            var ccMap =
                segment.ChildVertices.OfType<Coin>()
                    .ToDictionary(coin => coin, coin => new Child(coin, segment))
                    ;
            segment.CoinChildMap = ccMap;
            segment.Children = ccMap.Values.ToArray();
            segment.ChildStatusMap =
                segment.Children
                .ToDictionary(child => child, _ => Status4.Homing)
                ;

            // call or segment 를 'Child' class 로 wrapping
            IVertex convert(IVertex old)
            {
                var coin = old as Coin;
                if (coin != null && ccMap.ContainsKey(coin))
                    return ccMap[coin];
                return old;
            }

            // { Graph 정보 추출 & 저장
            var gi = segment.GraphInfo;
            foreach (var ves in gi.TraverseOrders)
            {
                ves.Vertex = convert(ves.Vertex);
                foreach (var oe in ves.OutgoingEdges)
                {
                    oe.Sources = oe.Sources.Select(s => convert(s)).ToArray();
                    oe.Target = convert(oe.Target);
                }
            }
            segment.Inits = gi.Inits.OfType<Coin>().Select(convert).Cast<Child>().ToArray();
            segment.Lasts = gi.Lasts.Select(convert).Cast<Child>().ToArray();
            segment.TraverseOrder = gi.TraverseOrders;
            // }


            // segment 내의 child call 에 대한 RX tag 변경 시, child origin 검사 및 child 의 status 변경 저장하도록 event handler 등록
            var rxs = segment.CallChildren.SelectMany(c => c.RxTags).ToArray();
            var rxNames = rxs.Select(t => t.Name).ToHashSet();

            var subs =
                Global.BitChangedSubject
                    .Where( bc => bc.Bit is Tag && rxNames.Contains(((Tag)bc.Bit).Name ) )
                    .Subscribe(bc => {
                        segment.OnChildRxTagChanged(bc);
                    });
            segment.Disposables.Add(subs);
        }
    }
}
