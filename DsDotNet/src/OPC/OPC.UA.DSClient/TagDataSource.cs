using System;
using System.Data;
using Opc.Ua;
using Opc.Ua.Client;

namespace OPC.UA.DSClient.Winform
{
    public class TagDataSource
    {
        private readonly Session m_session;
        private Subscription m_subscription;
        private readonly DataTable m_tagDataTable;

        public TagDataSource(Session session)
        {
            m_session = session ?? throw new ArgumentNullException(nameof(session));
            m_tagDataTable = InitializeTagDataTable();
        }

        public DataTable TagDataTable => m_tagDataTable;

        private DataTable InitializeTagDataTable()
        {
            var dataTable = new DataTable();
            dataTable.Columns.Add("Folder Path", typeof(string)); // Added for folder hierarchy
            dataTable.Columns.Add("Tag Name", typeof(string));
            dataTable.Columns.Add("Value", typeof(string));
            dataTable.Columns.Add("Data Type", typeof(string));
            dataTable.Columns.Add("Timestamp", typeof(DateTime));
            return dataTable;
        }

        public void UpdateTagData()
        {
            if (m_session == null || !m_session.Connected)
                return;

            m_tagDataTable.Clear();

            // Find the Dualsoft folder NodeId
            NodeId dualsoftNodeId = FindDualsoftFolderNodeId();
            if (dualsoftNodeId == null)
            {
                throw new InvalidOperationException("Dualsoft folder not found.");
            }

            // Recursively browse and add variables
            BrowseAndAddVariables(dualsoftNodeId, "Dualsoft");
        }

        private void BrowseAndAddVariables(NodeId parentNodeId, string currentPath)
        {
            ReferenceDescriptionCollection references;
            byte[] continuationPoint;

            m_session.Browse(
                null,
                null,
                parentNodeId,
                0u,
                BrowseDirection.Forward,
                ReferenceTypeIds.HierarchicalReferences,
                true,
                (uint)NodeClass.Object | (uint)NodeClass.Variable,
                out continuationPoint,
                out references);

            foreach (var reference in references)
            {
                var nodeId = ExpandedNodeId.ToNodeId(reference.NodeId, m_session.NamespaceUris);
                var nodePath = $"{currentPath}/{reference.DisplayName.Text}";

                if (reference.NodeClass == NodeClass.Object)
                {
                    // Recursively browse children with updated path
                    BrowseAndAddVariables(nodeId, nodePath);
                }
                else if (reference.NodeClass == NodeClass.Variable)
                {
                    // Add to subscription and DataTable
                    AddMonitoredItem(reference.DisplayName.Text, nodeId);
                    var value = m_session.ReadValue(nodeId);
                    if (value != null)
                    {
                        m_tagDataTable.Rows.Add(nodePath, reference.DisplayName.Text, value.Value, value.Value?.GetType().Name, value.SourceTimestamp);
                    }
                }
            }
        }

        private void AddMonitoredItem(string displayName, NodeId nodeId)
        {
            if (m_subscription == null)
            {
                m_subscription = new Subscription(m_session.DefaultSubscription)
                {
                    DisplayName = "DualsoftSubscription",
                    PublishingInterval = 1000,
                };
                m_session.AddSubscription(m_subscription);
                m_subscription.Create();
            }

            var monitoredItem = new MonitoredItem(m_subscription.DefaultItem)
            {
                DisplayName = displayName,
                StartNodeId = nodeId,
                AttributeId = Attributes.Value,
                SamplingInterval = 1000,
            };
            monitoredItem.Notification += MonitoredItem_Notification;

            m_subscription.AddItem(monitoredItem);
            m_subscription.ApplyChanges();
        }
        private void MonitoredItem_Notification(MonitoredItem item, MonitoredItemNotificationEventArgs e)
        {
            foreach (var value in item.DequeueValues())
            {
                // UI 스레드에서 데이터 업데이트 강제
                if (Form.ActiveForm?.InvokeRequired ?? false)
                {
                    Form.ActiveForm?.Invoke(new Action(() => UpdateDataTable(item, value)));
                }
                else
                {
                    UpdateDataTable(item, value);
                }
            }
        }

        private void UpdateDataTable(MonitoredItem item, DataValue value)
        {
            // DataTable 업데이트
            var tagName = item.DisplayName;
            var timestamp = value.SourceTimestamp;

            // 기존 Row 업데이트
            var row = m_tagDataTable.Select($"[Tag Name] = '{tagName}'").FirstOrDefault();
            if (row != null)
            {
                row["Value"] = value.Value;
                row["Timestamp"] = timestamp;
            }
            else
            {
                // Row가 없으면 새 Row 추가
                m_tagDataTable.Rows.Add(tagName, value.Value, value.Value?.GetType().Name ?? "Unknown", timestamp);
            }
        }

        private NodeId FindDualsoftFolderNodeId()
        {
            ReferenceDescriptionCollection references;
            byte[] continuationPoint;

            m_session.Browse(
                null,
                null,
                ObjectIds.ObjectsFolder,
                0u,
                BrowseDirection.Forward,
                ReferenceTypeIds.HierarchicalReferences,
                true,
                (uint)NodeClass.Object,
                out continuationPoint,
                out references);

            foreach (var reference in references)
            {
                if (reference.DisplayName.Text == "Dualsoft")
                {
                    return ExpandedNodeId.ToNodeId(reference.NodeId, m_session.NamespaceUris);
                }
            }

            return null;
        }
    }
}
