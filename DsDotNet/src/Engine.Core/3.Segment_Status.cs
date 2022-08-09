namespace Engine.Core;

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
            var s = PortInfoS.Value;
            var r = PortInfoR.Value;
            var e = PortInfoE.Value;

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

    public bool Paused
    {
        get {
            var childStarted =
                ChildStatusMap.Values.Any(s => s.IsOneOf(Status4.Going, Status4.Finished))
                ;
            return (Status == Status4.Ready && childStarted);
        }
    }

    public Dictionary<Child, Status4> ChildStatusMap { get; internal set; }

}
