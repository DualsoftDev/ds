//추후 참조
//using Dsu.PLC;
//using Dsu.PLC.LS;
//using Dsu.PLC.Common;

using System;
using System.Collections.Generic;

namespace Control.OPC
{
    internal class LsConnection
    {
        private LsConnectionParameters param;

        public LsConnection(LsConnectionParameters param)
        {
            this.param = param;
        }

        public int PerRequestDelay { get; internal set; }

        internal void AddMonitoringTags(List<LsTag> readOnlyBits)
        {
            throw new NotImplementedException();
        }

        internal bool Connect()
        {
            throw new NotImplementedException();
        }
    }
}