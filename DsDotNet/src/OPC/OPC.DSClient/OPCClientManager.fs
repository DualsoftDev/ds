namespace OPC.DSClient

open System
open System.Collections.Generic
open Opc.Ua
open Opc.Ua.Client
open System.Text.RegularExpressions
open System.ComponentModel
open Engine.Core

type OPCClientManager(dsSys:DsSystem) =
    let opcClientTags = Dictionary<string, OPCClientTag>()
    let addMonitoredItemEvent, addVariablesEvent = Event<unit>(), Event<unit>()

    [<CLIEvent>] member _.AddMonitoredItemEvent = addMonitoredItemEvent.Publish
    [<CLIEvent>] member _.AddVariablesEvent = addVariablesEvent.Publish
    member _.DsSystem = dsSys
    
    member this.LoadTags(session: Session) =
        if session = null || not session.Connected then invalidOp "Session is not connected."

        let browseAndProcess name =
            this.FindNodeIdByName(session, ObjectIds.ObjectsFolder, name)
            |> Option.iter (fun nodeId -> this.BrowseAndAddVariables(session, nodeId, name))


        browseAndProcess "Dualsoft"
        opcClientTags.Values |> Seq.iter (fun tag -> this.AddMonitoredItem(session, tag))
        opcClientTags.Values

    member private this.FindNodeIdByName(session: Session, parentNodeId: NodeId, name: string) =
        session.Browse(null, null, parentNodeId, 0u, BrowseDirection.Forward,
                       ReferenceTypeIds.HierarchicalReferences, true,
                       uint32 NodeClass.Object, ref null)
        |> snd
        |> Seq.cast<ReferenceDescription>
        |> Seq.tryFind (fun ref -> ref.DisplayName.Text = name)
        |> Option.map (fun ref -> ExpandedNodeId.ToNodeId(ref.NodeId, session.NamespaceUris))

    member private this.AddMonitoredItem(session: Session, tag: OPCClientTag) =
        try
            if session.DefaultSubscription.MonitoredItemCount = 0u then
                session.AddSubscription(session.DefaultSubscription) |> ignore
                session.DefaultSubscription.PublishingInterval <- 1
                session.DefaultSubscription.Create()

            let monitoredItem = MonitoredItem(session.DefaultSubscription.DefaultItem, DisplayName = tag.Name, StartNodeId = tag.NodeId, AttributeId = Attributes.Value)
            monitoredItem.add_Notification(
                MonitoredItemNotificationEventHandler(fun item _ ->
                    item.MonitoringMode <- MonitoringMode.Reporting
                    item.SamplingInterval <- 1
                    item.DequeueValues() |> Seq.iter (fun value ->
                        tag.Value <- value.Value
                        tag.Timestamp <- TimeZoneInfo.ConvertTime(value.SourceTimestamp, TimeZoneInfo.Utc, TimeZoneInfo.Local)
                    )
                )
            )

            addMonitoredItemEvent.Trigger()
            session.DefaultSubscription.AddItem(monitoredItem)
            session.DefaultSubscription.ApplyChanges()

            printfn "Monitored item added for tag: %s" tag.Name
        with
        | :? ServiceResultException as ex ->
            printfn "Error adding monitored item: %s" ex.Message
            raise ex

    member private this.BrowseAndAddVariables(session: Session, parentNodeId: NodeId, currentPath: string) =
        session.Browse(null, null, parentNodeId, 0u, BrowseDirection.Forward,
                       ReferenceTypeIds.HierarchicalReferences, true,
                       uint32 NodeClass.Object ||| uint32 NodeClass.Variable, ref null)
        |> snd
        |> Seq.cast<ReferenceDescription>
        |> Seq.iter (fun reference ->
            let nodeId = ExpandedNodeId.ToNodeId(reference.NodeId, session.NamespaceUris)
            let browseName, name = reference.BrowseName.Name, reference.DisplayName.Text.Trim('[', ']')
            let nodePath, isFolder = $"{currentPath}/{name}", reference.TypeDefinition.Equals(ObjectTypeIds.FolderType)

            let tagKind, _fqdnType, qualifiedName =
                match Regex.Match(browseName, @"\(([^)]*)\)") with
                | matchResult when matchResult.Success ->
                    let extractedValue = matchResult.Groups.[1].Value
                    let remainingText = Regex.Replace(browseName, @"\([^)]*\)", "").Trim('[', ']')
                    if isFolder then "", extractedValue, remainingText else extractedValue, "", remainingText
                | _ -> "", "", browseName.Trim()

            if  (isFolder || tryParseOPCClientTagKind tagKind |> Option.isSome)
                then
                let initValue = 
                    if isFolder then box "N/A"
                    else 
                        session.ReadValue(nodeId).Value

             

                if isFolder then 
                    this.BrowseAndAddVariables(session, nodeId, nodePath)
                else
                    let tag = OPCClientTag(dsSys.TagManager.Storages[qualifiedName])
                
                    tag.Value <- initValue
                    tag.Timestamp<- DateTime.MinValue
                    tag.NodeId <- nodeId;
                
                    addVariablesEvent.Trigger()
                    opcClientTags.Add(tag.Name, tag) |> ignore
        )
