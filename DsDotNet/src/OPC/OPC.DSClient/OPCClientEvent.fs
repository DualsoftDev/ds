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
    let opcClientManager = OPCClientManager(sys)
    let opcClient = OPCDsClient()

    do
        opcClient.ConnectionReady.Add(fun _ ->
            opcClientManager.DisposeTags()  
            let tags = opcClientManager.LoadTags(opcClient.Session.Value)

            tags |> Seq.iter (fun tag ->
                tag.AddHandler(fun _ _ ->  
                        match TagKindExt.GetTagInfo(tag.DsStorage) with
                        | Some dstag -> tagOPCEventSubject.OnNext(dstag)
                        | None -> ()
                )
            )
        )
        opcClient.InitializeOPC("opc.tcp://localhost:2747", 3000);


    member _.TagOPCEventSubject = tagOPCEventSubject
