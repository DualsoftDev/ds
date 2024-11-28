using DevExpress.Diagram.Core.Native;
using DevExpress.Mvvm.Native;
using DevExpress.XtraEditors;
using DevExpress.XtraRichEdit.Import.Html;
using Opc.Ua;
using Opc.Ua.Client;
using OPC.DSClient.WinForm.UserControl;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using static OPC.DSClient.WinForm.OpcTagManager;
using Subscription = Opc.Ua.Client.Subscription;

namespace OPC.DSClient.WinForm
{


    public class OpcTagManager
    {
        public BindingList<OpcDsTag> OpcTags { get; } = new();
        public List<OpcDsTag> OpcFolderTags { get; } = new();
        public string OpcDsText { get; set; } = string.Empty;
        public string OpcJsonText { get; set; } = string.Empty;

        public DsSystemJson DsSystemJson { get; set; } = new();

        Dictionary<string, OpcDsTag> _dicTagKeyPath = new();
        Dictionary<string, DsJsonBase> _dicDsJson = new();

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
            if (rootNodeId == null)
                throw new InvalidOperationException("Root Node not found.");

            // 1. folderCount 및 variableCount 값을 먼저 설정
            SetGlobalCounts(session, rootNodeId);

            // 2. 나머지 태그 처리
            BrowseAndAddVariables(session, rootNodeId, "Dualsoft");

            // 딕셔너리 생성
            _dicTagKeyPath = OpcTags.Concat(OpcFolderTags)
                .ToDictionary(tag => tag.Path);
            // 딕셔너리 생성
            _dicDsJson = DsSystemJsonUtils.GetAllDsJsons(DsSystemJson)
                .ToDictionary(dsJson => dsJson.Name);

            DsSystemJsonUtils.UpdateOpcDSTags(this);
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
        private void SetGlobalCounts(Session session, NodeId parentNodeId)
        {


            session.Browse(
                null, null, parentNodeId, 0u, BrowseDirection.Forward,
                ReferenceTypeIds.HierarchicalReferences, true,
                (uint)NodeClass.Object | (uint)NodeClass.Variable, out _, out var references);

            foreach (var reference in references)
            {
                var nodeId = ExpandedNodeId.ToNodeId(reference.NodeId, session.NamespaceUris);
                var name = reference.DisplayName.Text;

                if (name == "folderCount")
                {
                    var initialValue = session.ReadValue(nodeId);
                    Global.FolderCount = initialValue.Value is int count ? count : 0;
                }
                else if (name == "variableCount")
                {
                    var initialValue = session.ReadValue(nodeId);
                    Global.VariableCount = initialValue.Value is int count ? count : 0;
                }

                if (name.EndsWith(".ds"))
                { // text데이터는 초기 값을 읽기
                    var initialValue = session.ReadValue(nodeId);
                    OpcDsText = initialValue.Value?.ToString() ?? string.Empty;
                }
                if (name.EndsWith(".json"))
                { // text데이터는 초기 값을 읽기
                    var initialValue = session.ReadValue(nodeId);
                    OpcJsonText = initialValue.Value?.ToString() ?? string.Empty;
                    DsSystemJson = DsSystemJsonUtils.LoadJson(OpcJsonText);
                }

                //  모두 설정되면 중단
                if (Global.FolderCount > 0 && Global.VariableCount > 0
                    && OpcDsText.Length > 0 && OpcJsonText.Length > 0)
                    break;
            }
        }

        private void AddMonitoredItem(Session session, OpcDsTag tag, NodeId nodeId)
        {
            try
            {
                Global.OpcProcessCount++;

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
            // 공통 데이터 업데이트
            UpdateTagCommonInfo(tag, value);

            // `.ds` 및 `.json` 태그 처리
            if (HandleSpecialTags(tag, value.Value?.ToString()))
                return;

            // 부모 태그 및 JSON 데이터 매핑 처리
            if (_dicTagKeyPath.TryGetValue(tag.ParentPath, out var parent))
            {
                UpdateParentValues(tag, value, parent);

                if (_dicDsJson.TryGetValue(parent.QualifiedName, out var dsJson))
                {
                    UpdateDsJsonValues(tag, value, dsJson);
                }
            }
        }

        /// <summary>
        /// 태그의 공통 정보 업데이트
        /// </summary>
        private void UpdateTagCommonInfo(OpcDsTag tag, DataValue value)
        {
            tag.Value = value.Value;
            tag.DataType = value.Value?.GetType().Name ?? "Unknown";
            tag.Timestamp = value.SourceTimestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }

        /// <summary>
        /// `.ds` 및 `.json` 태그 처리
        /// </summary>
        /// <returns>해당 태그를 처리했는지 여부</returns>
        private bool HandleSpecialTags(OpcDsTag tag, string? tagValue)
        {
            if (tag.Name.EndsWith(".ds"))
            {
                OpcDsText = tagValue ?? string.Empty;
                return true;
            }

            if (tag.Name.EndsWith(".json"))
            {
                OpcJsonText = tagValue ?? string.Empty;
                DsSystemJson = DsSystemJsonUtils.LoadJson(OpcJsonText);
                return true;
            }

            return false;
        }


        public TagKindOpc? GetTagKindEnum(string tagKindDefinition)
        {
            // 문자열을 TagKindText enum으로 변환
            return Enum.TryParse<TagKindOpc>(tagKindDefinition, true, out var result) ? result : null;
        }

        private void UpdateParentValues(OpcDsTag tag, DataValue value, OpcDsTag parent)
        {
      
            var actionMap = new Dictionary<TagKindOpc, Action>
            {
                { TagKindOpc.ActionIn, () => parent.Value = value.Value },
                { TagKindOpc.Finish, () => parent.Value = value.Value },
                { TagKindOpc.CalcCount, () => parent.Count = Convert.ToInt32(value.Value) },
                { TagKindOpc.CalcActiveDuration, () => parent.ActiveDuration = Convert.ToUInt32(value.Value) },
                { TagKindOpc.CalcWaitingDuration, () => parent.WaitingDuration = Convert.ToUInt32(value.Value) },
                { TagKindOpc.CalcMovingDuration, () =>
                                    {
                                    parent.MovingDuration = Convert.ToUInt32(value.Value);
                                    parent.MovingTimes.Add(tag.MovingDuration);
                                    // 100개 초과 시 가장 오래된 항목 제거
                                    if (parent.MovingTimes.Count > 100)
                                        parent.MovingTimes.RemoveAt(0);
                                }
                },
                { TagKindOpc.CalcAverage, () => parent.MovingAVG = Convert.ToSingle(value.Value) },
                { TagKindOpc.CalcStandardDeviation, () => parent.MovingSTD = Convert.ToSingle(value.Value) }
            };

            var tagKindEnum = GetTagKindEnum(tag.TagKindDefinition);

            if (tagKindEnum.HasValue && actionMap.TryGetValue(tagKindEnum.Value, out var action))
            {
                action();
            }
        }
        private void UpdateDsJsonValues(OpcDsTag tag, DataValue value, DsJsonBase dsJson)
        {
            if (dsJson is VertexJson vertexJson && dsJson.Type == "Call")
            {
                var actionCallMap = new Dictionary<TagKindOpc, Action>
                {
                    { TagKindOpc.CalcCount, () => UpdateTaskDevs(vertexJson, dev => dev.Count = Convert.ToInt32(value.Value)) },
                    { TagKindOpc.CalcActiveDuration, () => UpdateTaskDevs(vertexJson, dev => dev.ActiveDuration = Convert.ToUInt32(value.Value)) },
                    { TagKindOpc.CalcWaitingDuration, () => UpdateTaskDevs(vertexJson, dev => dev.WaitingDuration = Convert.ToUInt32(value.Value)) },
                    { TagKindOpc.CalcMovingDuration, () => UpdateTaskDevs(vertexJson, dev =>
                        {
                            dev.MovingDuration = Convert.ToUInt32(value.Value);
                            dev.MovingTimes.Add(dev.MovingDuration);

                            // 100개 초과 시 가장 오래된 항목 제거
                            if (dev.MovingTimes.Count > 100)
                                dev.MovingTimes.RemoveAt(0);
                        }) },
                    { TagKindOpc.CalcAverage, () => UpdateTaskDevs(vertexJson, dev => dev.MovingAVG = Convert.ToSingle(value.Value)) },
                    { TagKindOpc.CalcStandardDeviation, () => UpdateTaskDevs(vertexJson, dev => dev.MovingSTD = Convert.ToSingle(value.Value)) }
                };

                // TagKindDefinition을 TagKindText enum으로 변환
                var tagKindEnum = GetTagKindEnum(tag.TagKindDefinition);

                if (tagKindEnum.HasValue && actionCallMap.TryGetValue(tagKindEnum.Value, out var actionCall))
                {
                    actionCall();
                }
            }
        }



        /// <summary>
        /// VertexJson의 모든 TaskDevs에 대해 작업 수행
        /// </summary>
        private void UpdateTaskDevs(VertexJson vertexJson, Action<OpcDsTag> updateAction)
        {
            vertexJson.TaskDevs
                .SelectMany(dev => dev.SubOpcDsTags)
                .ForEach(updateAction);
        }


        private Subscription CreateDefaultSubscription(Session session)
        {
            try
            {
                var subscription = new Subscription
                {
                    DisplayName = "OpcTagSubscription",
                    PublishingInterval = 500
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
