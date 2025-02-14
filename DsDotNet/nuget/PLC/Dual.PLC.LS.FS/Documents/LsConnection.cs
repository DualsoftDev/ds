using System;
using System.Collections.Generic;
using Dsu.PLC.Common;
using XGCommLib;


namespace Dsu.PLC.LS
{
    public class LsConnection : ConnectionBase
    {
        /// COM factory
        private CommObjectFactory _factory = new CommObjectFactory();
        public CommObject COMObject { get; set; }

        private LsConnectionParameters _connectionParametersLS = null;
        public override IConnectionParameters ConnectionParameters
        {
            get { return _connectionParametersLS; }
            set { _connectionParametersLS = (LsConnectionParameters)value; }
        }

        private LsCpu _cpu;
        public override ICpu Cpu { get { return _cpu; } }

        public LsConnection(LsConnectionParameters parameters)
            : base(parameters)
        {
            _connectionParametersLS = parameters;
        }

        public override bool Connect()
        {
            COMObject = _factory.GetMLDPCommObject($"{_connectionParametersLS.Ip}:{_connectionParametersLS.Port}");     // e.g "192.168.0.105:2004"
            int result = COMObject.Connect();
            if (result != 1)
                throw new Exception("Failed to connect.");

            _cpu = new LsCpu(COMObject);
            return true;
        }

        public override bool Disconnect()
        {
            try
            {
                if (COMObject != null)
                    return COMObject.Disconnect() == 1;
            }
            finally
            {
                COMObject = null;
            }

            return false;
        }

	    public override TagBase CreateTag(string name) => new LsTag(this, name);

	    public override void InvalidateMonitoringTargets()
        {
            //throw new NotImplementedException();
        }

        internal override IEnumerable<ChannelRequestExecutor> Channelize(IEnumerable<TagBase> tags)
        {
            var channel = new LsChannelRequestExecutor(this, tags);
            yield return channel;
        }


        public object ReadATag(string address) => ReadATag(new LsTag(this, address));
        public override object ReadATag(ITag tag) => ReadATag(new LsTag(this, tag.Name));

        public object ReadATag(LsTag tag)
        {
            throw new NotImplementedException();
            //if (!COMObject.ReadTag(tag.UserTag))
            //    throw new Exception("Failed");

            //return tag.UserTag.Value();
        }

    }
}
