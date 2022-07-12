using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Engine.Core
{
    public class Segment : SegmentOrCallBase, ISegmentOrFlow, IWithSREPorts, ITxRx
    {
        public RootFlow ContainerFlow;
        public ChildFlow ChildFlow;
        public override CpuBase OwnerCpu { get => ContainerFlow.Cpu; set => throw new NotImplementedException(); }


        public PortS PortS { get; set; }
        public PortR PortR { get; set; }
        public PortE PortE { get; set; }
        public Port[] AllPorts => new Port[] { PortS, PortR, PortE };

        public Tag TagS { get; set; }
        public Tag TagR { get; set; }
        public Tag TagE { get; set; }
        public Status4 RGFH => this.GetStatus();

        public bool IsResetFirst { get; internal set; } = true;
        public IEnumerable<Call> Children =>
            ChildFlow?.Edges
            .SelectMany(e => e.Vertices)
            .OfType<Call>()
            .Distinct()
            ;


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
         */
        public static Status4 GetStatus(this Segment segment)
        {
            var seg = segment;
            var s = seg.PortS.Value;
            var r = seg.PortR.Value;
            var e = seg.PortE.Value;

            if (seg.Paused)
            {
                Debug.Assert(!s && !r);
                return e ? Status4.Homing : Status4.Going;
            }

            if (e)
                return r ? Status4.Homing : Status4.Finished;

            Debug.Assert(!e);
            if (s)
                return r ? Status4.Ready : Status4.Going;

            Debug.Assert(!s && !e);
            return Status4.Ready;
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
            var paused = segment.Paused;

            var duplicate =
                newValue && ( (sp != null && segment.PortR.Value) || (rp != null && segment.PortS.Value));

            Port effectivePort = port;
            if (duplicate)
                effectivePort = rf ? (Port)segment.PortR : segment.PortS;


            effectivePort.Value = newValue;
            if (paused)
            {
                switch (effectivePort, newValue, st)
                {
                    case (PortS _, true, Status4.Going): resume(); break;
                    case (PortS _, false, Status4.Going): pause(); break;
                    case (PortR _, true, Status4.Homing): resume(); break;
                    case (PortR _, false, Status4.Homing): pause(); break;
                }

            }

            switch (effectivePort, newValue, st)
            {
                case (PortS _, true,  Status4.Ready)   : going() ; break;
                case (PortS _, false, Status4.Ready)   : pause() ; break;
                case (PortR _, true,  Status4.Finished): homing(); break;
                case (PortR _, false, Status4.Finished): pause() ; break;

                case (PortE _, true,  Status4.Going)   : finish(); break;
                case (PortE _, false, Status4.Homing)  : ready() ; break;
            }

            void going()
            {

            }
            void homing() { }
            void pause() { }
            void resume() { }
            void finish() { }
            void ready() { }
        }
    }
}
