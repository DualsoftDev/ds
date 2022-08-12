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
