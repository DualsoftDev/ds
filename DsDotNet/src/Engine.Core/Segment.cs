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

        public bool TryChangePort(Port port, bool newValue)
        {
            bool tryChangePort()
            {
                Debug.Assert(port.Value != newValue);
                port.Value = newValue;

                switch (port)
                {
                    case PortS portS:
                        if (newValue)
                        {
                            if ((RGFH == Status4.Ready || RGFH == Status4.Going) && (!IsResetFirst || !PortR.Value))
                            {
                                Paused = false;
                                return ChangeG();
                            }
                        }
                        else if (RGFH == Status4.Going)
                            Paused = true;
                        break;

                    case PortR portR:
                        if (newValue)
                        {
                            if ((RGFH == Status4.Finished || RGFH == Status4.Homing) && (IsResetFirst || !PortS.Value))
                            {
                                Paused = false;
                                return ChangeH();
                            }
                        }
                        else if (RGFH == Status4.Homing)
                            Paused = true;
                        break;
                    case PortE portE when RGFH == Status4.Going && PortS.Value:
                        return true;
                }

                if (IsResetFirst && PortR.Value && port == PortR)

                if (newValue)
                {
                }

                return false;
            }

            if (!tryChangePort())
                return false;

            port.Value = newValue;
            return true;
        }
    }

}
