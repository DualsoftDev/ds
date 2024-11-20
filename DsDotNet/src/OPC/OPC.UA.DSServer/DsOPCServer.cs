using System;
using System.Collections.Generic;
using System.Timers;
using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Server;
using static Engine.Core.Interface;
using Timer = System.Timers.Timer;

namespace OPC.UA.DSServer
{
    public class DsOPCServer : StandardServer
    {
        Storages _dsStorages;
        public DsOPCServer(Storages dsStorages)
        {
            _dsStorages = dsStorages;
        }   
        // NodeManager를 생성하여 주소 공간 관리
        protected override MasterNodeManager CreateMasterNodeManager(
            IServerInternal server,
            ApplicationConfiguration configuration)
        {
            IList<INodeManager> nodeManagers = new List<INodeManager>
            {
                new DsNodeManager(server, configuration, _dsStorages)
            };
            return new MasterNodeManager(server, configuration, null, nodeManagers.ToArray());
        }
    }
}
