namespace OPC.DSClient

open System
open System.Collections.Generic
open System.ComponentModel
open System.Reactive.Subjects
open Opc.Ua
open Opc.Ua.Client
open Engine.Core

type OPCClientEvent(sys: DsSystem) =
    let tagOPCEventSubject = new Subject<TagEvent>()
    let opcClient = OPCDsClient()

    do
        let opcClientManager = OPCClientManager(sys)
        opcClient.ConnectionReady.Add(fun _ ->
            let tags = opcClientManager.LoadTags(opcClient.Session.Value)

            tags |> Seq.iter (fun tag ->
                tag.PropertyChanged.AddHandler(fun _ _ ->  
                        match TagKindExt.GetTagInfo(tag.DsStorage) with
                        | Some dstag -> tagOPCEventSubject.OnNext(dstag)
                        | None -> ()
                )
            )
        )
        opcClient.InitializeOPC("opc.tcp://localhost:2747", 3000);


    member _.TagOPCEventSubject = tagOPCEventSubject
