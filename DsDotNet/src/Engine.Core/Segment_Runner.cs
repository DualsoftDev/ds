using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core
{
    partial class Segment
    {
        public void EvaluatePort(Port port, bool newValue)
        {
            if (port.Value == newValue)
                return;

            var sp = port as PortS;
            var rp = port as PortR;
            var ep = port as PortE;

            var rf = IsResetFirst;
            var st = Status;
            //var paused = Paused;

            // start port 와 reset port 동시 눌림
            var duplicate =
                newValue && ((sp != null && PortR.Value) || (rp != null && PortS.Value));

            Port effectivePort = port;
            if (duplicate)
                effectivePort = rf ? (Port)PortR : PortS;


            effectivePort.Value = newValue;
            switch (effectivePort, newValue, st)
            {
                case (PortS _, true, Status4.Ready): Going(); break;
                case (PortS _, false, Status4.Ready): pause(); break;
                case (PortR _, true, Status4.Finished): homing(); break;
                case (PortR _, false, Status4.Finished): pause(); break;
                case (PortR _, true, Status4.Going): homing(); break;
                case (PortR _, false, Status4.Going): pause(); break;

                case (PortE _, true, Status4.Going): finish(); break;
                case (PortE _, false, Status4.Homing): ready(); break;


                case (PortR _, true, Status4.Ready): break;
                case (PortR _, false, Status4.Ready):
                    if (PortS.Value)
                        Going();
                    break;
                case (PortS _, true, Status4.Finished): break;
                case (PortS _, false, Status4.Finished):
                    if (PortR.Value)
                        homing();
                    break;

                default:
                    throw new Exception("ERROR");
            }


            void homing() { }
            void pause() { }
            void finish() { }
            void ready() { }
        }

        void Going()
        {
            Debug.Assert(PortS.Value);

            // 1. Ready 상태에서의 clean start
            // 2. Going pause (==> Ready 로 해석) 상태에서의 resume start

            var allFinished = this.IsChildrenStatusAllWith(Status4.Finished);
            if (allFinished)
            {
                PortE.Value = true;
                Debug.Assert(Status == Status4.Finished);
                return;
            }

            var anyHoming = this.IsChildrenStatusAnyWith(Status4.Homing);
            if (anyHoming)
            {
                Debug.Assert(this.IsChildrenStatusAllWith(Status4.Homing));      // 하나라도 homing 이면, 모두 homing
                if (this.IsChildrenOrigin())
                {
                    var map = ChildStatusMap;
                    var keys = map.Keys.ToArray();
                    foreach (var key in keys)
                        map[key] = Status4.Ready;
                }
            }

            var allReady = this.IsChildrenStatusAllWith(Status4.Ready);
            var anyGoing = this.IsChildrenStatusAnyWith(Status4.Going);
            if (allReady || anyGoing)
            {
                if (allReady)
                {
                    // do origin check
                }

                var v_oes = TraverseOrder;
                foreach (var ve in v_oes)
                {
                    var child = ve.Vertex as Child;
                    var es = ve.OutgoingEdges;
                    switch (child.Status)
                    {
                        // child call 을 "잘" 시켜야 한다.
                        case Status4.Ready:
                            child.Going();
                            break;
                        case Status4.Going:
                        case Status4.Finished:
                            break;
                        default:
                            throw new Exception("ERROR");
                    }
                }
            }


            if (this.IsChildrenStatusAnyWith(Status4.Homing))
            {

            }
        }
    }
}
