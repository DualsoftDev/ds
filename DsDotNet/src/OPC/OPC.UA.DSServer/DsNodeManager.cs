using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using Dual.Common.Core;
using Engine.Core;
using Opc.Ua;
using Opc.Ua.Server;
using static Engine.Core.Interface;
using static Engine.Core.TagKindModule;

namespace OPC.UA.DSServer
{
    public class DsNodeManager : CustomNodeManager2
    {
        private readonly Dictionary<string, BaseDataVariableState> _variables = new();
        private readonly Storages _dsStorages;
        private IDisposable? _disposableTagDS;

        public DsNodeManager(IServerInternal server, ApplicationConfiguration configuration, Storages dsStorages)
            : base(server, configuration, "https://dualsoft.com//ds")
        {
            _dsStorages = dsStorages;
        }

        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            var namespaceIndex = NamespaceIndexes[0];
            var folderNode = CreateFolder("Dualsoft", "Dualsoft", namespaceIndex, externalReferences);

            foreach (var (key, value) in _dsStorages)
            {
                if (!value.ObjValue.GetType().IsValueType) continue;

                var variable = CreateVariable(
                    folderNode,
                    key,
                    key,
                    namespaceIndex,
                    new Variant(value.ObjValue),
                    value.ObjValue.GetType());

                _variables.Add(key, variable);
            }

            SubscribeToTagEvents();
        }

        private FolderState CreateFolder(
            string name,
            string displayName,
            ushort namespaceIndex,
            IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            var folder = new FolderState(null)
            {
                SymbolicName = name,
                NodeId = new NodeId(name, namespaceIndex),
                BrowseName = new QualifiedName(name, namespaceIndex),
                DisplayName = displayName,
                TypeDefinitionId = ObjectTypeIds.FolderType,
                EventNotifier = EventNotifiers.SubscribeToEvents
            };

            folder.AddReference(ReferenceTypeIds.Organizes, true, ObjectIds.ObjectsFolder);

            if (!externalReferences.ContainsKey(ObjectIds.ObjectsFolder))
            {
                externalReferences[ObjectIds.ObjectsFolder] = new List<IReference>();
            }

            externalReferences[ObjectIds.ObjectsFolder].Add(
                new NodeStateReference(ReferenceTypeIds.Organizes, false, folder.NodeId));

            AddPredefinedNode(SystemContext, folder);
            return folder;
        }

        private BaseDataVariableState CreateVariable(
            FolderState folder,
            string name,
            string displayName,
            ushort namespaceIndex,
            Variant initialValue,
            Type type)
        {
            var variable = new BaseDataVariableState(folder)
            {
                SymbolicName = name,
                NodeId = new NodeId(name, namespaceIndex),
                BrowseName = new QualifiedName(name, namespaceIndex),
                Description = displayName,
                DisplayName = displayName,
                DataType = MapToDataTypeId(type),
                Value = initialValue.Value,
                AccessLevel = AccessLevels.CurrentReadOrWrite,
                UserAccessLevel = AccessLevels.CurrentReadOrWrite
            };
            variable.OnWriteValue = HandleWriteValue;

            folder.AddChild(variable);
            AddPredefinedNode(SystemContext, variable);
            return variable;
        }

        private ServiceResult HandleWriteValue(
               ISystemContext context,
               NodeState node,
               NumericRange indexRange,
               QualifiedName dataEncoding,
               ref object value,
               ref StatusCode statusCode,
               ref DateTime timestamp)
        {
            Console.WriteLine($"Write Value: {value}, Node: {node.BrowseName}");

            if(_dsStorages.ContainsKey(node.BrowseName.Name))
            {
                _dsStorages[node.BrowseName.Name].BoxedValue = value;
                Console.WriteLine($"DS Tag '{node.BrowseName.Name}' updated to: {value}");
            }
            else
            {
                Console.WriteLine($"DS Tag '{node.BrowseName.Name}' not found!");
            }

            timestamp = DateTime.UtcNow;
            return ServiceResult.Good;
        }

        private static NodeId MapToDataTypeId(Type type) => type switch
        {
            { } t when t == typeof(bool) => DataTypeIds.Boolean,
            { } t when t == typeof(char) => DataTypeIds.String,
            { } t when t == typeof(float) => DataTypeIds.Float,
            { } t when t == typeof(double) => DataTypeIds.Double,
            { } t when t == typeof(short) => DataTypeIds.Int16,
            { } t when t == typeof(int) => DataTypeIds.Int32,
            { } t when t == typeof(long) => DataTypeIds.Int64,
            { } t when t == typeof(sbyte) => DataTypeIds.SByte,
            { } t when t == typeof(string) => DataTypeIds.String,
            { } t when t == typeof(ushort) => DataTypeIds.UInt16,
            { } t when t == typeof(uint) => DataTypeIds.UInt32,
            { } t when t == typeof(ulong) => DataTypeIds.UInt64,
            { } t when t == typeof(byte) => DataTypeIds.Byte,
            _ => DataTypeIds.Boolean
        };
        private void SubscribeToTagEvents()
        {
            _disposableTagDS ??= TagEventSubject.Subscribe(evt =>
            {
                var tag = TagKindExt.GetStorage(evt);

                var variable = _variables[tag.Name];
                variable.Value = tag.ObjValue;
                variable.Timestamp = DateTime.UtcNow;
                variable.ClearChangeMasks(SystemContext, false);
            }); ;
        }
    }
}
