using Engine.Common;

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
        public Status4 Status
        {
            get
            {
                var s = PortS.Value;
                var r = PortR.Value;
                var e = PortE.Value;

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
        }

        public override bool Paused
        {
            get {
                var childStarted =
                    ChildStatusMap.Values.Any(s => s.IsOneOf(Status4.Going, Status4.Finished))
                    ;
                return (Status == Status4.Ready && childStarted);
            }
        }

        public Dictionary<Child, Status4> ChildStatusMap { get; internal set; }
        internal Status4 GetChildStatus(IVertex child)
        {
            Child child_ = null;
            switch (child)
            {
                case Child ch: child_ = ch; break;
                case Coin coin: child_ = CoinChildMap[coin]; break;
                default:
                    throw new Exception("ERROR");
            }
            return ChildStatusMap[child_];
        }

        public bool IsChildrenOrigin()
        {
            return true;
        }
    }
}
