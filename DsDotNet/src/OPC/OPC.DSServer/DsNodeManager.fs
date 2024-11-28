namespace OPC.DSServer

open System
open System.Collections.Generic
open System.Linq
open Opc.Ua
open Opc.Ua.Server
open System.Reactive.Subjects
open Engine.Core.Interface
open Engine.Core
open Engine.Core.TagKindModule
open Engine.Runtime
open Dual.Common.Core.FS


[<AutoOpen>]
module DsNodeManagerExt =
    
    let mapToDataTypeId(typ: Type) =
        match typ with
        | t when t = typeof<bool> -> DataTypeIds.Boolean
        | t when t = typeof<char> -> DataTypeIds.String
        | t when t = typeof<float> -> DataTypeIds.Double
        | t when t = typeof<float32> -> DataTypeIds.Float
        | t when t = typeof<int16> -> DataTypeIds.Int16
        | t when t = typeof<int32> -> DataTypeIds.Int32
        | t when t = typeof<int64> -> DataTypeIds.Int64
        | t when t = typeof<sbyte> -> DataTypeIds.SByte
        | t when t = typeof<string> -> DataTypeIds.String
        | t when t = typeof<uint16> -> DataTypeIds.UInt16
        | t when t = typeof<uint32> -> DataTypeIds.UInt32
        | t when t = typeof<uint64> -> DataTypeIds.UInt64
        | t when t = typeof<byte> -> DataTypeIds.Byte
        | _ -> failwithf "Unsupported data type"


    let getTags(fqdn:FqdnObject) =
        fqdn.TagManager.Storages
            |> Seq.filter(fun tag -> tag.Value.Target.IsSome && tag.Value.Target.Value = fqdn)
            |> Seq.map(fun tag -> tag.Value)
    

type DsNodeManager(server: IServerInternal, configuration: ApplicationConfiguration, dsSys: DsSystem) =
    inherit CustomNodeManager2(server, configuration, "https://dualsoft.com//ds")
    let _variables = Dictionary<string, BaseDataVariableState>()
    let _folders = Dictionary<string, FolderState>()
    let mutable _disposableTagDS: IDisposable option = None
    let mutable opcItemCnt:int = 0
    let dsStorages = dsSys.TagManager.Storages

    let handleWriteValue(
        _: ISystemContext, node: NodeState, _: NumericRange, 
        _: QualifiedName, value: byref<obj>, statusCode: byref<StatusCode>, timestamp: byref<DateTime>
    ) =
        printfn "Write Value: %A, Node: %s" value node.BrowseName.Name

        if dsStorages.ContainsKey(node.BrowseName.Name) then
            dsStorages[node.BrowseName.Name].BoxedValue <- value
            printfn "DS Tag '%s' updated to: %A" node.BrowseName.Name value
        else
            printfn "DS Tag '%s' not found!" node.BrowseName.Name

        timestamp <- DateTime.UtcNow
        ServiceResult.Good
    

    let createVariable(folder: FolderState, name: string, tagKind: string , namespaceIndex: uint16, initialValue: Variant, typ: Type) =
        let variable = 
            new BaseDataVariableState(folder, 
                SymbolicName = name, 
                NodeId = NodeId(name, namespaceIndex),
                BrowseName = QualifiedName($"({tagKind}){name}", namespaceIndex),
                Description = "",
                DisplayName = name,
                DataType = mapToDataTypeId(typ),
                Value = initialValue.Value,
                AccessLevel = AccessLevels.CurrentReadOrWrite,
                UserAccessLevel = AccessLevels.CurrentReadOrWrite
                )

              

        variable.OnWriteValue <- NodeValueEventHandler(fun context node indexRange dataEncoding value statusCode timestamp ->
            handleWriteValue(context, node, indexRange, dataEncoding, &value, &statusCode, &timestamp)
        )
        
        folder.AddChild(variable)
        variable


    member private this.CreateOpcNodes (tags:IStorage seq) parentNode namespaceIndex= 

      
        // Create Variables for storages
        for tag in tags do
            if tag.ObjValue.GetType().IsValueType 
                && not(_variables.ContainsKey tag.Name) then
                let variable =
                    createVariable(
                        parentNode,
                        tag.Name,
                        getTagKindName tag.TagKind,
                        namespaceIndex,
                        Variant(tag.ObjValue),
                        tag.ObjValue.GetType()
                    )
                this.AddPredefinedNode(this.SystemContext, variable)
                _variables.Add(tag.Name, variable)

    /// <summary>
    /// Create a new folder node and add it to the parent folder.
    /// </summary>
    member private this.CreateFolder(
        name: string, 
        displayName: string, 
        namespaceIndex: uint16, 
        parentFolder: FolderState option
    ) =
        // Create the folder node
        let folder = 
            new FolderState(
                parentFolder |> Option.toObj, 
                SymbolicName = name, 
                NodeId = NodeId(name, namespaceIndex),
                BrowseName = QualifiedName(name, namespaceIndex), 
                DisplayName = displayName,
                TypeDefinitionId = ObjectTypeIds.FolderType, 
                EventNotifier = EventNotifiers.SubscribeToEvents
            )

        // Add the folder as a child of the parent folder
        match parentFolder with
        | Some parent -> parent.AddChild(folder)
        | None -> () // Root folder does not have a parent

        _folders.Add(name, folder)
        this.AddPredefinedNode(this.SystemContext, folder)
        folder

    override this.CreateAddressSpace(externalReferences: IDictionary<NodeId, IList<IReference>>) =
        let nIndex = this.NamespaceIndexes[0]
        // Create the Objects folder if it does not exist
        let objectsFolder =
            if not (externalReferences.ContainsKey(ObjectIds.ObjectsFolder)) then
                let references = List<IReference>()
                externalReferences[ObjectIds.ObjectsFolder] <- references
                references
            else
                externalReferences[ObjectIds.ObjectsFolder] |> List<IReference>

        // Dualsoft root folder under Objects
        let rootNode = this.CreateFolder("Dualsoft", "Dualsoft", nIndex, None)
        objectsFolder.Add(NodeStateReference(ReferenceTypeIds.Organizes, false, rootNode.NodeId))
        
        let rootTagfolder = this.CreateFolder(dsSys.Name, $"{dsSys.Name} tags",  nIndex, Some rootNode)

        let sysTags = getTags dsSys
        this.CreateOpcNodes sysTags rootTagfolder nIndex

        // Create Tree Structure
        let treeFlows = DsPropertyTreeExt.GetPropertyTreeFromSystem(dsSys)
        let processTreeLevels (rootNode: FolderState) (treeFlows: DsTreeNode) =
            // 큐를 사용하여 단계별로 처리
            let queue = Queue<(FolderState * DsTreeNode)>()

            // 초기 루트 노드를 큐에 추가
            for flowTree in treeFlows.Children do
                queue.Enqueue((rootNode, flowTree))

            while queue.Count > 0 do
                // 현재 레벨의 노드를 처리
                let parentNode, treeNode = queue.Dequeue()

                // 현재 노드에 해당하는 폴더 생성
                let target = treeNode.Node.FqdnObject
                let isJob = target.IsSome && (target.Value :? Job)
                let folderName = $"[{treeNode.Node.FqdnObject.Value.QualifiedName}]"

                if _folders.ContainsKey folderName then
                    printfn "Folder already exists: %s" treeNode.Node.Name
                else 
                    let folder = 
                        if isJob
                        then
                            parentNode
                        else 
                            let folderDisplayName = $"[{treeNode.Node.Name}]"
                            this.CreateFolder(folderName, folderDisplayName, nIndex, Some parentNode)

                    // 태그가 있는 경우 OPC 노드 생성
                    if target.IsSome && not (isJob) then
                        let tags = getTags target.Value
                        this.CreateOpcNodes tags folder nIndex

                    printfn "Adding Folder: %s under Parent: %s" treeNode.Node.Name parentNode.BrowseName.Name

                    // 자식 노드를 큐에 추가
                    for child in treeNode.Children do
                        queue.Enqueue((folder, child))

        processTreeLevels rootNode treeFlows 
        this.processTextAdd rootNode dsSys

        // Subscribe to tag events
        this.SubscribeToTagEvents()

    /// JSON 데이터를 추가하는 함수
    member private this.processTextAdd(rootNode: FolderState)(dsSys: DsSystem) =
        let dsData = dsSys.ToDsText(false, false)
        let graphData = dsSys.ToJsonGraph()
        let dsVariable =
            createVariable(rootNode, $"{dsSys.Name}.ds", "Metadata",
                this.NamespaceIndexes.[0], Variant(dsData), typeof<string>
            )
        let graphVariable =
            createVariable(rootNode, $"{dsSys.Name}.json", "Metadata",
                this.NamespaceIndexes.[0], Variant(graphData), typeof<string>
            )

        let totalOpcItemFolder=
            createVariable(rootNode, $"folderCount", "Count",
                this.NamespaceIndexes.[0], Variant(_folders.Count), typeof<string>
            )
        let totalOpcItemVariable =
            createVariable(rootNode, $"variableCount", "Count",
                this.NamespaceIndexes.[0], Variant(_variables.Count), typeof<string>
            )

        // NodeState로 형변환 후 AddPredefinedNode 호출
        this.AddPredefinedNode(this.SystemContext, dsVariable)
        this.AddPredefinedNode(this.SystemContext, graphVariable)
        this.AddPredefinedNode(this.SystemContext, totalOpcItemFolder)
        this.AddPredefinedNode(this.SystemContext, totalOpcItemVariable)

    member private this.SubscribeToTagEvents() =
        if _disposableTagDS.IsNone 
        then 
            _disposableTagDS <- 
                Some(
                    ValueSubject.Subscribe(fun (sys, stg, value) ->
                        if dsSys = (sys:?>DsSystem) // active만 처리
                        then
                            if stg.IsVertexOpcDataTag() then 
                                handleCalcTag (stg) |> ignore

                            if _variables.ContainsKey(stg.Name) then
                                let variable = _variables[stg.Name]
                                variable.Value <- value
                                variable.Timestamp <- DateTime.Now
                                variable.ClearChangeMasks(this.SystemContext, false)    
                    )
                )