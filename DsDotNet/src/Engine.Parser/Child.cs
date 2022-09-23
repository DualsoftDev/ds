namespace Engine.Parser
{
    internal class Child
    {
        private SubCall subCall;
        private SegmentBase parenting;

        public Child(SubCall subCall, SegmentBase parenting)
        {
            this.subCall = subCall;
            this.parenting = parenting;
        }
    }
}