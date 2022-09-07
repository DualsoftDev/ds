using Microsoft.FSharp.Core;

namespace Engine.OPC
{
    internal class LsConnectionParameters
    {
        private string v1;
        private FSharpOption<ushort> fSharpOption;
        private double v2;

        public LsConnectionParameters(string v1, FSharpOption<ushort> fSharpOption,  double v2)
        {
            this.v1 = v1;
            this.fSharpOption = fSharpOption;
            this.v2 = v2;
        }
    }
}