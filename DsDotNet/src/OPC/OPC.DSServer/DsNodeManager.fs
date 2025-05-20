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
open System.Diagnostics
open DB.DuckDB


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


    let listExternalTagKinds = [
        TaskDevTag.actionIn|>int
        TaskDevTag.actionOut|>int
        VertexTag.motionStart|>int
        VertexTag.motionEnd|>int
        VertexTag.scriptStart|>int
        VertexTag.scriptEnd|>int
        VertexTag.scriptEnd|>int
        MonitorTag.UserTagType|>int
        ]


    let getTags(fqdn:FqdnObject) =
        fqdn.TagManager.Storages
            |> Seq.filter(fun tag -> tag.Value.Target.IsSome && tag.Value.Target.Value = fqdn)
            |> Seq.filter(fun tag -> not(listExternalTagKinds.Contains tag.Value.TagKind))
            |> Seq.map(fun tag -> tag.Value)


    let getIOTags(sys:DsSystem) =
        sys.TagManager.Storages.Values
            |> Seq.filter(fun tag -> tag.TagKind = (int)TaskDevTag.actionIn || tag.TagKind = (int)TaskDevTag.actionOut)

    let getMonitorTags(sys:DsSystem) =
        sys.TagManager.Storages.Values
            |> Seq.filter(fun tag -> tag.TagKind = (int)MonitorTag.UserTagType)

    let getActionTags(sys:DsSystem) =
        sys.TagManager.Storages.Values
            |> Seq.filter(fun tag -> tag.TagKind = (int)VertexTag.motionStart || tag.TagKind = (int)VertexTag.motionEnd)

    let getScriptTags(sys:DsSystem) =
        sys.TagManager.Storages.Values
            |> Seq.filter(fun tag -> tag.TagKind = (int)VertexTag.scriptStart || tag.TagKind = (int)VertexTag.scriptEnd)

type DsNodeManager(server: IServerInternal, configuration: ApplicationConfiguration, dsSys: DsSystem, mode:RuntimeMode) =
    inherit CustomNodeManager2(server, configuration, "ds")

    let logger = DuckDBWriter.LoggerPG(dsSys.Name)
    //start TagName, end Tag
    let _motionDic = dsSys |> getDsPlanInterfaces 
                           |> Seq.map(fun tag -> tag.MotionStartTag|>fst, tag.MotionEndTag|>fst) 
                           |> dict

    let _variables = Dictionary<string, BaseDataVariableState>()
    let _folders = Dictionary<string, FolderState>()
    let mutable _disposableTagDS: IDisposable option = None
    let mutable _dsStorages = dsSys.TagManager.Storages

    let handleWriteValue(
        _: ISystemContext, node: NodeState, _: NumericRange, 
        _: QualifiedName, value: byref<obj>, statusCode: byref<StatusCode>, timestamp: byref<DateTime>
    ) =
        try
            match node with
            | :? BaseVariableState as _variable ->
                // 클라이언트가 제공한 값을 노드에 설정 안함 (dsStorages SubscribeToDsTagEvents 여기서 처리함)
                //variable.Value <- value  
                //variable.Timestamp <- timestamp
                //variable.StatusCode <- statusCode

                if _dsStorages.ContainsKey(node.DisplayName.Text) then
                    let dsTag = _dsStorages[node.DisplayName.Text]
                    dsTag.BoxedValue <- value

                ServiceResult.Good
            | _ ->
                printfn "Unsupported node type for write operation."
                ServiceResult.Good
        with ex ->
            printfn "Exception in handleWriteValue: %s" ex.Message
            ServiceResult.Good

    let createVariable(folder: FolderState, name: string, address: string, tagKind: string , namespaceIndex: uint16, initialValue: Variant, typ: Type) =
        let variable = 
            new BaseDataVariableState(folder, 
                SymbolicName = name, 
                NodeId = NodeId(name, namespaceIndex),
                BrowseName = QualifiedName($"({tagKind}){name}", namespaceIndex),
                Description = address,
                DisplayName = name,
                DataType = mapToDataTypeId(typ),
                Value = initialValue.Value,
                AccessLevel = AccessLevels.CurrentReadOrWrite,
                UserAccessLevel = AccessLevels.CurrentReadOrWrite
                )

        variable.OnWriteValue <- NodeValueEventHandler(fun context node indexRange dataEncoding value statusCode timestamp ->
            handleWriteValue(context, node, indexRange, dataEncoding, &value, &statusCode, &timestamp)
        )

        variable.OnWriteValue <- NodeValueEventHandler(fun context node indexRange dataEncoding value statusCode timestamp ->

            let writeError = ServiceResult.Create(StatusCodes.BadUserAccessDenied, $"{variable.DisplayName.Text} read only")
            let handleWrite = handleWriteValue(context, node, indexRange, dataEncoding, &value, &statusCode, &timestamp)
            let isStg = _dsStorages.ContainsKey(node.DisplayName.Text)
            match mode with 
            | RuntimeMode.Control ->
                if isStg && _dsStorages[node.DisplayName.Text].IsMonitorStg() then
                    handleWrite
                else 
                    writeError
            | RuntimeMode.VirtualLogic ->
                if isStg && _dsStorages[node.DisplayName.Text].IsMotionEndStg() then
                    handleWrite
                else 
                    writeError  
            | RuntimeMode.VirtualPlant ->
                if isStg && _dsStorages[node.DisplayName.Text].IsActionOutStg() then
                    handleWrite
                else 
                    writeError  

            | RuntimeMode.Monitoring
            | RuntimeMode.Simulation -> writeError   

           
        )
        
        folder.AddChild(variable)
        variable

    let locker = obj()  // dsStorages locker 객체
    member this.ChangeDSStorage (stg:Storages) = 
                 lock locker (fun () ->
                _dsStorages <- stg
            )

    member private this.CreateOpcNodes (tags:IStorage seq) parentNode namespaceIndex= 

      
        // Create Variables for storages
        for tag in tags do
            if (tag.ObjValue.GetType().IsValueType || tag.ObjValue :? string)
              (*  && not(_variables.ContainsKey tag.Name)*) then
                //actionIn, actionOut 태그는 별도 처리
                let newVariable =
                    createVariable(
                        parentNode,
                        tag.Name,
                        tag.Address,
                        getTagKindName tag.TagKind,
                        namespaceIndex,
                        Variant(tag.ObjValue),
                        tag.ObjValue.GetType()
                    )
                _variables.Add (tag.Name, newVariable)
                this.AddPredefinedNode(this.SystemContext, newVariable)

    /// <summary>
    /// Create a new folder node and add it to the parent folder.
    /// </summary>
    member private this.CreateFolder(
        name: string, 
        displayName: string, 
        fqdnKind: string, 
        namespaceIndex: uint16, 
        parentFolder: FolderState option
    ) =
        let nameWithFqdn = if String.IsNullOrWhiteSpace(fqdnKind) then name else $"({fqdnKind}){name}"

        // Create the folder node
        let folder = 
            new FolderState(
                parentFolder |> Option.toObj, 
                SymbolicName = name, 
                NodeId = NodeId(name, namespaceIndex),
                BrowseName = QualifiedName(nameWithFqdn, namespaceIndex), 
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
        let rootNode = this.CreateFolder(DsText.TextOPCDSFolder, DsText.TextOPCDSFolder, "", nIndex, None)
        objectsFolder.Add(NodeStateReference(ReferenceTypeIds.Organizes, false, rootNode.NodeId))
        let rootTagNode = this.CreateFolder(DsText.TextOPCTagFolder,DsText.TextOPCTagFolder, "", nIndex, None)
        objectsFolder.Add(NodeStateReference(ReferenceTypeIds.Organizes, false, rootTagNode.NodeId))


        let nodeIO = this.CreateFolder("IO", "IO", "", nIndex, Some rootTagNode)
        this.CreateOpcNodes (getIOTags dsSys) nodeIO nIndex
        
        let nodeMONITOR = this.CreateFolder("MONITOR", "MONITOR", "", nIndex, Some rootTagNode)
        this.CreateOpcNodes (getMonitorTags dsSys) nodeMONITOR nIndex
        
        let nodeAction = this.CreateFolder("ACTION", "ACTION", "", nIndex, Some rootTagNode)
        this.CreateOpcNodes (getActionTags dsSys) nodeAction nIndex

        let nodeScript = this.CreateFolder("SCRIPT", "SCRIPT", "", nIndex, Some rootTagNode)
        this.CreateOpcNodes (getScriptTags dsSys) nodeScript nIndex
        
        let rootSysTagfolder = this.CreateFolder(dsSys.Name, $"{dsSys.Name}_System",  $"{dsSys.GetType().Name}", nIndex, Some rootNode)
        this.CreateOpcNodes (getTags dsSys) rootSysTagfolder nIndex

        // Create Tree Structure
        let processTreeLevels (rootNode: FolderState) (treeFlows: DsTreeNode) =
            // 큐를 사용하여 단계별로 처리
            let queue = Queue<(FolderState * DsTreeNode)>()

            // 초기 루트 노드를 큐에 추가
            for flowTree in treeFlows.Children do
                queue.Enqueue((rootNode, flowTree))

            while queue.Count > 0 do
                // 현재 레벨의 노드를 처리
                let parentNode, treeNode = queue.Dequeue()
                if treeNode.Node.FqdnObject.IsSome
                then
                    // 현재 노드에 해당하는 폴더 생성
                    let target = treeNode.Node.FqdnObject
                    let isJob = target.IsSome && (target.Value :? Job)
                    let isTaskDev = target.IsSome && (target.Value :? TaskDev)
                    let folderName = $"[{treeNode.Node.FqdnObject.Value.QualifiedName}]"

                    //OP, CMD   FqdnObject 없는 요소도 추후 추가 필요 ?
                    if  _folders.ContainsKey folderName then
                        printfn "Folder already exists: %s" treeNode.Node.Name
                    else 
                        let folder = 
                            if isJob
                            then
                                parentNode
                            else 
                                let folderDisplayName = $"[{treeNode.Node.Name}]"
                                let fqdnName = 
                                    if isTaskDev 
                                    then
                                        let inTag =  (target.Value :?> TaskDev).InTag
                                        let outTag=  (target.Value :?> TaskDev).OutTag
                                        let tagIONames = String.Join(";", [inTag; outTag]
                                                            .Where(fun f->f.IsNonNull())
                                                            .Select(fun f->f.Name))
                                        if tagIONames = "" then $"TaskDev" else $"TaskDev;{tagIONames}"
                                    else 
                                        $"{treeNode.Node.FqdnObject.Value.GetType().Name}"
                                  
                                this.CreateFolder(folderName, folderDisplayName, fqdnName, nIndex, Some parentNode)

                        // 태그가 있는 경우 OPC 노드 생성
                        if target.IsSome && not(isJob) && not(isTaskDev) then
                            let tags = getTags target.Value
                            match mode with 
                            | RuntimeMode.Control -> 
                                this.CreateOpcNodes tags folder nIndex    
                            | _->
                                //calcTimeoutDetected는 제외 자동으로 OPC Server에서 처리
                                let tags = tags |> Seq.filter(fun tag -> not(tag.IsControlErrorStg())) 
                                this.CreateOpcNodes tags folder nIndex    

                        printfn "Adding Folder: %s under Parent: %s" treeNode.Node.Name parentNode.BrowseName.Name

                        // 자식 노드를 큐에 추가
                        for child in treeNode.Children do
                            queue.Enqueue(folder, child)
#if DEBUG
        //[dsSys]@dsSys.GetRecursiveLoadedSystems()  //디버그 모드에서만 사용된 시스템 전부 모니터 사용
        [dsSys]
#else 
        [dsSys]
#endif
        |> Seq.iter (fun sys ->
            let treeFlows = DsPropertyTreeExt.GetPropertyTreeFromSystem(sys)
            processTreeLevels rootNode treeFlows 
        )


        this.processTextAdd rootNode dsSys

        // Subscribe to DS tag events
        this.SubscribeToDsTagEvents()


        let sendStat() =
            _variables.Iter(fun kv ->
                let tagKind = _dsStorages[kv.Key].TagKind |> int
                if tagKind = int VertexTag.calcAverage  ||
                   tagKind = int VertexTag.calcStandardDeviation
                then 
                    kv.Value.Value <- _dsStorages[kv.Key].BoxedValue;
                    kv.Value.Timestamp <- DateTime.UtcNow   
                    kv.Value.ClearChangeMasks(this.SystemContext, false)    
            )      
                            

        /// 5초마다 통계데이터 전송 클라이언트 
        let startClientMonitor () =
            let timer = new System.Timers.Timer(1000.0)
            timer.Elapsed.Add(fun _ -> sendStat())
            timer.AutoReset <- true
            timer.Start()

        DsTimeAnalysisMoudle.initUpdateStat dsSys
        startClientMonitor()

    /// JSON 데이터를 추가하는 함수
    member private this.processTextAdd(rootNode: FolderState)(dsSys: DsSystem) =
        let dsData = dsSys.ToDsText(false, false)
        let graphData = dsSys.ToJsonGraph()
        let dsVariable =
            createVariable(rootNode, $"{dsSys.Name}.ds", "Metadata", "",
                this.NamespaceIndexes.[0], Variant(dsData), typeof<string>
            )
        let graphVariable =
            createVariable(rootNode, $"{dsSys.Name}.json", "Metadata","",
                this.NamespaceIndexes.[0], Variant(graphData), typeof<string>
            )

        let totalOpcItemFolder=
            createVariable(rootNode, $"folderCount", "Count","",
                this.NamespaceIndexes.[0], Variant(_folders.Count), typeof<string>
            )
        let totalOpcItemVariable =
            createVariable(rootNode, $"variableCount", "Count","",
                this.NamespaceIndexes.[0], Variant(_variables.Count), typeof<string>
            )


        let heartBit =
            createVariable(rootNode, "HeartBit",  $"HeartBit", "",
                this.NamespaceIndexes.[0], Variant(false), typeof<bool>
            )
        this.AddPredefinedNode(this.SystemContext, heartBit)
      
        // 타이머 설정
        let timer = new System.Timers.Timer(1000.0) // 1초 간격으로 타이머 실행
        timer.Elapsed.Add(fun _ ->
            heartBit.Value <- not (Convert.ToBoolean(heartBit.Value))
            heartBit.Timestamp <- DateTime.UtcNow
            heartBit.ClearChangeMasks(this.SystemContext, false)
        )
        timer.Start()


        //logger.SetParameter ("TIME", DateTime.Now)


        // NodeState로 형변환 후 AddPredefinedNode 호출
        this.AddPredefinedNode(this.SystemContext, dsVariable)
        this.AddPredefinedNode(this.SystemContext, graphVariable)
        this.AddPredefinedNode(this.SystemContext, totalOpcItemFolder)
        this.AddPredefinedNode(this.SystemContext, totalOpcItemVariable)
    

    member private this.SubscribeToDsTagEvents() =
        if _disposableTagDS.IsNone 
        then 
            _disposableTagDS <- 
                Some(
                    ValueSubject.Subscribe(fun (sys, stg, value) ->
                        
                        if  dsSys = (sys:?>DsSystem) then   // active만 처리
                            if stg.IsVertexOpcDataTag() then
                                handleCalcTag (stg) |> ignore 

                            if mode <> RuntimeMode.Control && TagKindExt.IsNeedSaveDBLogForDSPilot stg then //Control 아니면 DB 로깅  
                                logger.LogTagChange (stg.Name, value)
                            else 
                                //Control 모드일때는 DB 로깅 하지 않음
                                () //test ahn 리테인 처리 필요

                        if _variables.ContainsKey(stg.Name) then
                            let variable = _variables[stg.Name]
                            variable.Value <- value
                            variable.Timestamp <- DateTime.UtcNow
                            variable.ClearChangeMasks(this.SystemContext, false)    

                            //opc 모션 motionStart  신호가 꺼지면 자동으로  motionEnd OFF 처리
                            if _motionDic.ContainsKey(stg.Name) && not(Convert.ToBoolean(value))
                            then
                                let endTagName = _motionDic.[stg.Name]
                                _dsStorages[endTagName].BoxedValue <- false

                        //async { ...
                        //} |> Async.Start // 비동기로 처리 하면 빠른 신호는 Client 까지 신호 안감
                    )
                )   
