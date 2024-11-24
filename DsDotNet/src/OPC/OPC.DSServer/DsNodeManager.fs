namespace OPC.DSServer

open System
open System.Collections.Generic
open Opc.Ua
open Opc.Ua.Server
open System.Reactive.Subjects
open Engine.Core.Interface
open Engine.Core
open Engine.Core.TagKindModule
open Engine.Runtime

[<AutoOpen>]
module DsNodeManagerExt =
    
    let mapToDataTypeId(typ: Type) =
        match typ with
        | t when t = typeof<bool> -> DataTypeIds.Boolean
        | t when t = typeof<char> -> DataTypeIds.String
        | t when t = typeof<float> -> DataTypeIds.Float
        | t when t = typeof<double> -> DataTypeIds.Double
        | t when t = typeof<int16> -> DataTypeIds.Int16
        | t when t = typeof<int32> -> DataTypeIds.Int32
        | t when t = typeof<int64> -> DataTypeIds.Int64
        | t when t = typeof<sbyte> -> DataTypeIds.SByte
        | t when t = typeof<string> -> DataTypeIds.String
        | t when t = typeof<uint16> -> DataTypeIds.UInt16
        | t when t = typeof<uint32> -> DataTypeIds.UInt32
        | t when t = typeof<uint64> -> DataTypeIds.UInt64
        | t when t = typeof<byte> -> DataTypeIds.Byte
        | _ -> DataTypeIds.Boolean


    let getTags(fqdn:FqdnObject) =
        fqdn.TagManager.Storages
            |> Seq.filter(fun tag -> tag.Value.Target.IsSome && tag.Value.Target.Value = fqdn)
            |> Seq.map(fun tag -> tag.Value)
    

type DsNodeManager(server: IServerInternal, configuration: ApplicationConfiguration, dsSys: DsSystem) =
    inherit CustomNodeManager2(server, configuration, "https://dualsoft.com//ds")

    let _variables = Dictionary<string, BaseDataVariableState>()
    let mutable _disposableTagDS: IDisposable option = None
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
    

    let createVariable(folder: FolderState, name: string, description: string, namespaceIndex: uint16, initialValue: Variant, typ: Type) =
        let variable = 
            new BaseDataVariableState(folder, SymbolicName = name, NodeId = NodeId(name, namespaceIndex),
                                  BrowseName = QualifiedName($"({description}){name}", namespaceIndex), Description = description,
                                  DisplayName = name, DataType = mapToDataTypeId(typ),
                                  Value = initialValue.Value, AccessLevel = AccessLevels.CurrentReadOrWrite,
                                  UserAccessLevel = AccessLevels.CurrentReadOrWrite)
        
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

        // Add the folder to the address space

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

        //let rec addTreeNodes (parentNode: FolderState) (treeNode: DsTreeNode) =
        //    // Create a folder for the current tree node under the parentNode
        //    let target = treeNode.Node.FqdnObject
        //    let isJob = target.IsSome && (target.Value :? Job)
        //    let folder = 
        //        if not(isJob)
        //        then
        //            let folderName = $"[{treeNode.Node.Name}]"
        //            this.CreateFolder(folderName, folderName, nIndex, Some parentNode)
        //        else 
        //            parentNode
            
        //    if target.IsSome && not(isJob) then
        //        let tags = getTags target.Value
        //        this.CreateOpcNodes tags folder nIndex

        //    printfn "Adding Folder: %s under Parent: %s" treeNode.Node.Name parentNode.BrowseName.Name

        //    // Recursively process child nodes
        //    for child in treeNode.Children do
        //        addTreeNodes folder child
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

                let folder = 
                    if isJob
                    then
                        parentNode
                    else 
                        let folderDisplayName = $"[{treeNode.Node.Name}]"
                        let folderName = $"[{treeNode.Node.FqdnObject.Value.QualifiedName}]"
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

        // Subscribe to tag events
        this.SubscribeToTagEvents()


    member private this.SubscribeToTagEvents() =
        if _disposableTagDS.IsNone 
        then 
            _disposableTagDS <- 
                Some(
                    TagEventSubject.Subscribe(fun evt ->
                        let tag = TagKindExt.GetStorage(evt)
                        if _variables.ContainsKey(tag.Name) then
                            let variable = _variables[tag.Name]
                            variable.Value <- tag.ObjValue
                            variable.Timestamp <- DateTime.Now
                            variable.ClearChangeMasks(this.SystemContext, false)
                    )
                ) 