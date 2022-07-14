using Engine.Common;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;

namespace Engine.Core
{
    [DebuggerDisplay("{ToText(),nq}")]
    public class Segment : SegmentOrCallBase, ISegmentOrFlow, IWithSREPorts, ITxRx
    {
        public RootFlow ContainerFlow { get; }
        public ChildFlow ChildFlow { get; set; }
        public override CpuBase OwnerCpu { get => ContainerFlow.Cpu; set => throw new NotImplementedException(); }


        public PortS PortS { get; set; }
        public PortR PortR { get; set; }
        public PortE PortE { get; set; }
        public Port[] AllPorts => new Port[] { PortS, PortR, PortE };

        public Tag TagS { get; set; }
        public Tag TagR { get; set; }
        public Tag TagE { get; set; }
        public Status4 RGFH => this.GetStatus();
        public override bool Paused => this.IsPaused();
        public string QualifiedName => $"{ContainerFlow.QualifiedName}_{Name}";

        public bool IsResetFirst { get; internal set; } = true;
        public IEnumerable<Call> Children => ChildFlow == null ? Enumerable.Empty<Call>() : ChildFlow.Calls;

        public Segment(string name, RootFlow containerFlow)
            : base(name)
        {
            ContainerFlow = containerFlow;
            ChildFlow = new ChildFlow($"_{name}", this);
            containerFlow.Children.Add(this);

            PortS = new PortS(this);
            PortR = new PortR(this);
            PortE = new PortE(this);
        }

        public void SetSRETags(Tag s, Tag r, Tag e)
        {
            TagS = s;
            TagR = r;
            TagE = e;
        }

        public override string ToString() => ToText();
        public override string ToText()
        {
            var c = Children == null ? 0 : Children.Count();
            return $"{Name}: cpu={OwnerCpu?.Name}, #children={c}";
        }
    }


    public static class SegmentExtension
    {
        /*
          ----------------------
            Status   SP  RP  EP
          ----------------------
              R      x   -   x
                     o   o   x
              G      o   x   x
              F      -   x   o
              H      -   o   o
          ----------------------
          - 'o' : ON, 'x' : Off, '-' 는 don't care
          - 내부에서 Reset First 로만 해석

          - 실행/Resume 은 Child call status 보고 G 이거나 R 인 것부터 수행
         */
        public static Status4 GetStatus(this Segment segment)
        {
            var seg = segment;
            var s = seg.PortS.Value;
            var r = seg.PortR.Value;
            var e = seg.PortE.Value;

            //if (seg.Paused)
            //{
            //    Debug.Assert(!s && !r);
            //    return e ? Status4.Homing : Status4.Going;
            //}

            if (e)
                return r ? Status4.Homing : Status4.Finished;

            Debug.Assert(!e);
            if (s)
                return r ? Status4.Ready : Status4.Going;

            Debug.Assert(!s && !e);
            return Status4.Ready;
        }

        public static bool IsPaused(this Segment segment)
        {
            var st = segment.GetStatus();
            var childStarted = segment.Children.Any(c => c.RGFH.IsOneOf(Status4.Going, Status4.Finished));
            return (st == Status4.Ready && childStarted);
        }

        public static IEnumerable<Status4> CollectChildrenStatus(this Segment segment) => segment.Children.Select(call => call.RGFH);

        public static bool IsChildrenStatusAllWith(this Segment segment, Status4 status) => segment.CollectChildrenStatus().All(st => st == status);
        public static bool IsChildrenStatusAnyWith(this Segment segment, Status4 status) => segment.CollectChildrenStatus().Any(st => st == status);

        public static bool IsChildrenOrigin(this Segment segment)
        {
            return true;
        }

        public static void OnChildRxTagChanged(this Segment segment, BitChange bc)
        {
            var tag = bc.Bit as Tag;
            var calls = segment.Children.Where(c => c.RxTags.Any(t => t.Name == tag.Name));
            Console.WriteLine();
        }


        public static void Epilogue(this Segment segment)
        {
            // segment 내의 child call 에 대한 RX tag 변경 시, child origin 검사 및 child 의 status 변경 저장하도록 event handler 등록
            var rxs = segment.Children.SelectMany(c => c.RxTags).ToArray();
            var rxNames = rxs.Select(t => t.Name).ToHashSet();

            var subs =
            Global.BitChangedSubject
                .Where( bc => bc.Bit is Tag && rxNames.Contains(((Tag)bc.Bit).Name ) )
                .Subscribe(bc => {
                    segment.OnChildRxTagChanged(bc);
                });
        }



        public static void EvaluatePort(this Segment segment, Port port, bool newValue)
        {
            if (port.Value == newValue)
                return;

            var sp = port as PortS;
            var rp = port as PortR;
            var ep = port as PortE;

            var rf = segment.IsResetFirst;
            var st = segment.GetStatus();
            //var paused = segment.Paused;

            var duplicate =
                newValue && ( (sp != null && segment.PortR.Value) || (rp != null && segment.PortS.Value));

            Port effectivePort = port;
            if (duplicate)
                effectivePort = rf ? (Port)segment.PortR : segment.PortS;


            effectivePort.Value = newValue;
            switch (effectivePort, newValue, st)
            {
                case (PortS _, true,  Status4.Ready)   : going() ; break;
                case (PortS _, false, Status4.Ready)   : pause() ; break;
                case (PortR _, true,  Status4.Finished): homing(); break;
                case (PortR _, false, Status4.Finished): pause() ; break;
                case (PortR _, true,  Status4.Going)   : homing(); break;
                case (PortR _, false, Status4.Going)   : pause(); break;

                case (PortE _, true,  Status4.Going)   : finish(); break;
                case (PortE _, false, Status4.Homing)  : ready() ; break;


                case (PortR _, true, Status4.Ready): break;
                case (PortR _, false, Status4.Ready):
                    if (segment.PortS.Value)
                        going();
                    break;
                case (PortS _, true, Status4.Finished): break;
                case (PortS _, false, Status4.Finished):
                    if (segment.PortR.Value)
                        homing();
                    break;

                default:
                    throw new Exception("ERROR");
            }

            void going()
            {
                Debug.Assert(segment.PortS.Value);

                // 1. Ready 상태에서의 clean start
                // 2. Going pause (==> Ready 로 해석) 상태에서의 resume start
                var gi = segment.ChildFlow.GraphInfo;
                var inits = gi.Inits;

                var allFinished = segment.IsChildrenStatusAllWith(Status4.Finished);
                if (allFinished)
                {
                    segment.PortE.Value = true;
                    Debug.Assert(segment.RGFH == Status4.Finished);
                    return;
                }

                var anyHoming = segment.IsChildrenStatusAnyWith(Status4.Homing);
                if (anyHoming)
                {
                    Debug.Assert(segment.IsChildrenStatusAllWith(Status4.Homing));      // 하나라도 homing 이면, 모두 homing
                    if (segment.IsChildrenOrigin())
                    {
                        Console.WriteLine();
                        segment.Children.Iter(c => c.RGFH = Status4.Ready);
                    }
                }

                var allReady = segment.IsChildrenStatusAllWith(Status4.Ready);
                var anyGoing = segment.IsChildrenStatusAnyWith(Status4.Going);
                if (allReady || anyGoing)
                {
                    if (allReady)
                    {
                        // do origin check
                    }

                    var v_oes = gi.TraverseOrders;
                    foreach (var ve in v_oes)
                    {
                        var call = ve.Vertex as Call;
                        var es = ve.OutgoingEdges;
                        switch(call.RGFH)
                        {
                            // child call 을 "잘" 시켜야 한다.
                            case Status4.Ready:
                                call.Going();
                                break;
                            case Status4.Going:
                            case Status4.Finished:
                                break;
                            default:
                                throw new Exception("ERROR");
                        }
                        Console.WriteLine();
                    }
                }


                if (segment.IsChildrenStatusAnyWith(Status4.Homing))
                {

                }
                Console.WriteLine();
            }
            void homing() { }
            void pause() { }
            void finish() { }
            void ready() { }
        }
    }
}
