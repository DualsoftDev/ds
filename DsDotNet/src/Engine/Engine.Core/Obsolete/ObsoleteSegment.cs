//using System.Threading;
//namespace Engine.Core.Obsolete
//{

//    internal CancellationTokenSource MovingCancellationTokenSource { get; set; }

//    public static class SegmentExtension
//    {
//        public static void CancelGoing(this SegmentBase segment)
//        {
//            segment.MovingCancellationTokenSource.Cancel();
//            segment.MovingCancellationTokenSource = null;
//        }
//        public static void CancelHoming(this SegmentBase segment)
//        {
//            segment.MovingCancellationTokenSource.Cancel();
//            segment.MovingCancellationTokenSource = null;
//        }

//        public static bool IsChildrenStatusAllWith(this SegmentBase segment, Status4 status) =>
//            segment.ChildStatusMap.Values.Select(tpl => tpl.Item2).All(st => st == status);
//        public static bool IsChildrenStatusAnyWith(this SegmentBase segment, Status4 status) =>
//            segment.ChildStatusMap.Values.Select(tpl => tpl.Item2).Any(st => st == status);

//        public static IEnumerable<PortInfo> GetAllPorts(this SegmentBase segment)
//        {
//            var s = segment;
//            yield return s.PortS;
//            yield return s.PortR;
//            yield return s.PortE;
//        }
//    }
//}
