using Opc.Ua;
using Opc.Ua.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace OPC.DSClient
{
    public class OpcTagManager
    {
        public BindingList<OpcTag> OpcTags { get; } = new();
        public List<OpcTag> OpcFolderTags { get; } = new();

        public void Clear()
        {
            OpcTags.Clear();
            OpcFolderTags.Clear();
        }

        public void LoadTags(Session session)
        {
            if (session == null || !session.Connected)
                throw new InvalidOperationException("Session is not connected.");

            var rootNodeId = FindNodeIdByName(session, ObjectIds.ObjectsFolder, "Dualsoft");
            if (rootNodeId != null)
                BrowseAndAddVariables(session, rootNodeId, "Dualsoft");
        }

        private NodeId? FindNodeIdByName(Session session, NodeId parentNodeId, string name)
        {
            session.Browse(
                null, null, parentNodeId, 0u, BrowseDirection.Forward,
                ReferenceTypeIds.HierarchicalReferences, true,
                (uint)NodeClass.Object, out _, out var references);

            foreach (var reference in references)
            {
                if (reference.DisplayName.Text == name)
                    return ExpandedNodeId.ToNodeId(reference.NodeId, session.NamespaceUris);
            }
            return null;
        }

        private void BrowseAndAddVariables(Session session, NodeId parentNodeId, string currentPath)
        {
            session.Browse(
                null, null, parentNodeId, 0u, BrowseDirection.Forward,
                ReferenceTypeIds.HierarchicalReferences, true,
                (uint)NodeClass.Object | (uint)NodeClass.Variable, out _, out var references);

            var nodeIds = new List<NodeId>();
            foreach (var reference in references)
                nodeIds.Add(ExpandedNodeId.ToNodeId(reference.NodeId, session.NamespaceUris));

            var dataTypeMap = GetDataTypes(session, nodeIds);

            foreach (var reference in references)
            {
                var nodeId = ExpandedNodeId.ToNodeId(reference.NodeId, session.NamespaceUris);
                var nodePath = $"{currentPath}/{reference.DisplayName.Text}";
                var isFolder = reference.TypeDefinition?.Equals(ObjectTypeIds.FolderType) == true;
                var dataType = dataTypeMap.TryGetValue(nodeId, out var dtName) ? dtName : "Unknown";

                var tag = new OpcTag
                {
                    Path = nodePath,
                    ParentPath = currentPath,
                    Name = reference.DisplayName.Text,
                    Value = "N/A",
                    DataType = isFolder ? "Folder" : dataType,
                    IsFolder = isFolder,
                    Timestamp = "Unknown"
                };

                if (isFolder)
                {
                    OpcFolderTags.Add(tag);
                    BrowseAndAddVariables(session, nodeId, nodePath);
                }
                else
                {
                    OpcTags.Add(tag);
                    AddMonitoredItem(session, tag, nodeId);
                }
            }
        }

        private Dictionary<NodeId, string> GetDataTypes(Session session, List<NodeId> nodeIds)
        {
            var dataTypeMap = new Dictionary<NodeId, string>();
            var nodesToRead = new ReadValueIdCollection();

            foreach (var nodeId in nodeIds)
            {
                if (!NodeId.IsNull(nodeId))
                {
                    nodesToRead.Add(new ReadValueId
                    {
                        NodeId = nodeId,
                        AttributeId = Attributes.DataType
                    });
                }
            }

            if (nodesToRead.Count == 0) return dataTypeMap;

            session.Read(null, 0, TimestampsToReturn.Neither, nodesToRead, out var results, out _);

            for (int i = 0; i < results.Count; i++)
            {
                if (results[i].WrappedValue.Value is NodeId dataTypeNodeId)
                {
                    var dataTypeNode = session.NodeCache.FetchNode(dataTypeNodeId);
                    dataTypeMap[nodeIds[i]] = dataTypeNode?.DisplayName.Text ?? "Unknown";
                }
                else
                {
                    dataTypeMap[nodeIds[i]] = "Unknown";
                }
            }
            return dataTypeMap;
        }
        private void AddMonitoredItem(Session session, OpcTag tag, NodeId nodeId)
        {
            try
            {
                // 기본 구독 확인 및 생성
                var subscription = session.DefaultSubscription ?? CreateDefaultSubscription(session);

                if (!session.Subscriptions.Contains(subscription))
                {
                    session.AddSubscription(subscription);
                    subscription.Create();
                }

                // MonitoredItem 생성
                var monitoredItem = new MonitoredItem(subscription.DefaultItem)
                {
                    DisplayName = tag.Name,
                    StartNodeId = nodeId,
                    AttributeId = Attributes.Value,
                    SamplingInterval = 1000
                };

                // 알림 이벤트 핸들러
                monitoredItem.Notification += (item, args) =>
                {
                    foreach (var value in item.DequeueValues())
                    {
                        tag.Value = value.Value;
                        tag.Timestamp = value.SourceTimestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    }
                };

                subscription.AddItem(monitoredItem);
                subscription.ApplyChanges();
            }
            catch (ServiceResultException ex)
            {
                Console.WriteLine($"Error creating subscription or monitored item: {ex.Message}");
            }
        }

        private Subscription CreateDefaultSubscription(Session session)
        {
            try
            {
                var subscription = new Subscription
                {
                    DisplayName = "OpcTagSubscription",
                    PublishingInterval = 1000
                };
                session.AddSubscription(subscription);
                subscription.Create();
                return subscription;
            }
            catch (ServiceResultException ex)
            {
                Console.WriteLine($"Error creating default subscription: {ex.Message}");
                throw;
            }
        }

    }
}
