namespace Engine.Parser
{
    internal class RootCall : Child
    {
        private string callName;
        private RootFlow rootFlow;
        private CallPrototype cp;

        public RootCall(string callName, RootFlow rootFlow, CallPrototype cp)
        {
            this.callName = callName;
            this.rootFlow = rootFlow;
            this.cp = cp;
        }
    }
}