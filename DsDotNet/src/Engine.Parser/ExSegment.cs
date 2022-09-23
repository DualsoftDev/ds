namespace Engine.Parser
{
    internal class ExSegment
    {
        private string v;
        private SegmentBase exSeg;

        public ExSegment(string v, SegmentBase exSeg)
        {
            this.v = v;
            this.exSeg = exSeg;
        }
    }
}