namespace Engine.Parser
{
    internal class SubCall
    {
        private string callName;
        private SegmentBase parenting;
        private CallPrototype cp;

        public SubCall(string callName, SegmentBase parenting, CallPrototype cp)
        {
            this.callName = callName;
            this.parenting = parenting;
            this.cp = cp;
        }
    }
}