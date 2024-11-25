using DevExpress.Mvvm.Native;
using Opc.Ua;
using Opc.Ua.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace OPC.DSClient
{
    public class OpcTagManager
    {
        public BindingList<OpcDsTag> OpcTags { get; } = new();
        public List<OpcDsTag> OpcFolderTags { get; } = new();
        public string OpcDsText { get; set; } = string.Empty;
        public string OpcJsonText { get; set; } = string.Empty;
        Dictionary<string, OpcDsTag> _dicTagKeyPath = new();

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

            List<OpcDsTag> tempOpcs = [.. OpcTags, .. OpcFolderTags];
            _dicTagKeyPath = tempOpcs.ToDictionary(folder => folder.Path, folder => folder);
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

            foreach (var reference in references)
            {
                var nodeId = ExpandedNodeId.ToNodeId(reference.NodeId, session.NamespaceUris);
                var browseName = reference.BrowseName.Name;
                var name = reference.DisplayName.Text;
                var nodePath = $"{currentPath}/{name}";
                var isFolder = reference.TypeDefinition?.Equals(ObjectTypeIds.FolderType) == true;

                string tagKindDefinition = Regex.Match(browseName, @"\(([^)]*)\)")?.Groups[1]?.Value ?? "";
                string qualifiedName = tagKindDefinition == ""
                    ? browseName.TrimStart('[').TrimEnd(']')
                    : browseName.Substring($"({tagKindDefinition})".Length);

                var tag = new OpcDsTag
                {
                    Path = nodePath,
                    ParentPath = currentPath,
                    Name = name,
                    Value = "N/A",
                    DataType = isFolder ? "Folder" : "Unknown",
                    IsFolder = isFolder,
                    Timestamp = "Unknown",
                    QualifiedName = qualifiedName,
                    TagKindDefinition = tagKindDefinition
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

        private void AddMonitoredItem(Session session, OpcDsTag tag, NodeId nodeId)
        {
            try
            {
                var subscription = session.DefaultSubscription ?? CreateDefaultSubscription(session);

                if (!session.Subscriptions.Contains(subscription))
                {
                    session.AddSubscription(subscription);
                    subscription.Create();
                }

                var monitoredItem = new MonitoredItem(subscription.DefaultItem)
                {
                    DisplayName = tag.Name,
                    StartNodeId = nodeId,
                    AttributeId = Attributes.Value,
                    SamplingInterval = 1000
                };

                if (tag.Name.EndsWith(".ds") || tag.Name.EndsWith(".json"))
                { // text데이터는 초기 값을 읽기
                    var initialValue = session.ReadValue(nodeId);
                    UpdateTagValue(tag, initialValue);
                }

                // 알림 이벤트 핸들러
                monitoredItem.Notification += (item, args) =>
                {
                    foreach (var value in item.DequeueValues())
                    {
                        UpdateTagValue(tag, value);
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

        private void UpdateTagValue(OpcDsTag tag, DataValue value)
        {
            if (tag.Name.EndsWith(".ds"))
            {
                OpcDsText = value.Value?.ToString() ?? string.Empty;
            }
            else if (tag.Name.EndsWith(".json"))
            {
                OpcJsonText = value.Value?.ToString() ?? string.Empty;
            }
            else
            {
                if (tag.TagKindDefinition == "finish")
                    _dicTagKeyPath[tag.ParentPath].Value = value.Value; 

                tag.Value = value.Value;
            }

            tag.DataType = value.Value?.GetType().Name ?? "Unknown";
            tag.Timestamp = value.SourceTimestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
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
