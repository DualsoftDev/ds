namespace Engine.CodeGenPLC

open System.IO
open System.Linq
open System.Runtime.CompilerServices
open Engine.Core
open Dual.Common.Core.FS
open PLC.CodeGen.LSXGI
open PLC.CodeGen.Common
open System
open Engine.CodeGenCPU

[<AutoOpen>]
module ExportModule =
    let generateXmlXGI (system: DsSystem) globalStorages localStorages (pous: PouGen seq) existingLSISprj : string =
        let projName = system.Name
        
        let getXgiPOUParams (pouName: string) (taskName: string) (pouGens: PouGen seq) =
            let pouParams: XgiPOUParams =
                {
                  /// POU name.  "DsLogic"
                  POUName = pouName
                  /// POU container task name
                  TaskName = taskName
                  /// POU ladder 최상단의 comment
                  Comment = "DsLogic Automatically generate"
                  LocalStorages = localStorages
                  GlobalStorages = globalStorages
                  CommentedStatements = pouGens.Collect(fun p -> p.CommentedStatements()) |> Seq.toList }

            pouParams

        let usedByteIndices =
            let getBytes addr = 
                [  
                    match tryParseXGITag addr with
                    |Some tag -> 
                        if tag.DataType = PLCHwModel.DataType.Bit 
                        then 
                            yield tag.ByteOffset
                        else 
                            yield! [tag.ByteOffset..tag.DataType.GetByteLength()]
                    |None ->  failwithlog "ERROR"
                ]
            
           
            let usedAddresses =
                system.TagManager.Storages.Values
                |> Seq.filter (fun f -> not <| (f :? TimerCounterBaseStruct))
                |> Seq.filter (fun f -> f.Address <> null && f.Address <> "")
                |> Array.ofSeq

            //check if there is any duplicated address
            let duplicatedAddresses =
                usedAddresses
                |> Array.filter (fun f -> f.Address <> TextAddrEmpty)
                |> Array.groupBy (fun f -> f.Address)
                |> Array.filter (fun (_, vs) -> vs.Length > 1)

            // prints duplications
            if duplicatedAddresses.Length > 0 then
                let dupItems =
                    duplicatedAddresses
                    |> map (fun (address, vs) ->
                        let names = vs |> map (fun var -> var.Name) |> String.concat ", "
                        $"  {address}: {names}")
                    |> String.concat Environment.NewLine

                failwithlog
                    $"Total {duplicatedAddresses.Length} 중복주소 items:{Environment.NewLine}{dupItems}"

            let autoMemoryAllocationTags =
                system.TagManager.Storages.Values
                |> Seq.filter (fun f -> not <| (f :? TimerCounterBaseStruct))
                |> Seq.filter (fun f -> not <| String.IsNullOrEmpty(f.Address) && f.Address.StartsWith("%M"))
                |> Array.ofSeq

            autoMemoryAllocationTags
            |> map (fun f -> f.Name)
            |> String.concat ", "
            |> logDebug "Auto Memory Allocation Tags: %s"

            // generate used memory byte indices
            autoMemoryAllocationTags
            |> Seq.collect (fun f -> f.Address |> getBytes)
            |> Seq.distinct
            |> Seq.sort
            |> List.ofSeq

        logDebug "Used byte indices: %A" usedByteIndices

        let projParams: XgiProjectParams =
            { defaultXgiProjectParams with
                ProjectName = projName
                GlobalStorages = globalStorages
                ExistingLSISprj = existingLSISprj
                AppendExpressionTextToRungComment = false
                MemoryAllocatorSpec = AllocatorFunctions(createMemoryAllocator "M" (0, 640 * 1024) usedByteIndices) // 640K M memory 영역
                POUs =
                    [ yield pous.Where(fun f -> f.IsActive) |> getXgiPOUParams "Active" "Active"
                      yield pous.Where(fun f -> f.IsDevice) |> getXgiPOUParams "Devices" "Devices"
                      for p in pous.Where(fun f -> f.IsExternal) do
                          yield getXgiPOUParams (p.ToSystem().Name) (p.TaskName()) [ p ] ] }

        projParams.GenerateXmlString()

    let exportXMLforXGI (system: DsSystem, path: string, existingLSISprj) =
        RuntimeDS.Target <- XGI
        let globalStorage = new Storages()
        let localStorage = new Storages()
        let result = CpuLoaderExt.LoadStatements(system, globalStorage)
        // Create a list to hold commented statements
        let mutable css = []

        // Add commented statements from each CPU
        for cpu in result do
            css <- css @ cpu.CommentedStatements() |> List.ofSeq

        let usedTagNames = getTotalTags(css.Select(fun s->s.Statement)) |> Seq.map(fun t->t.Name, t) |> dict
        globalStorage.Iter(fun tagKV-> 

            if not (usedTagNames.ContainsKey(tagKV.Key)) 
               && tagKV.Value.DataType = typedefof<bool>  //bool 타입만 지우기 가능 타이머 카운터 살림
               && TagKindExt.GetVariableTagKind(tagKV.Value).IsNone //VariableTag 살림
            then globalStorage.Remove(tagKV.Key)|>ignore
            )

        let xml = generateXmlXGI system globalStorage localStorage result existingLSISprj
        let crlfXml = xml.Replace("\r\n", "\n").Replace("\n", "\r\n")
        File.WriteAllText(path, crlfXml)


    let exportTextforDS () = ()

    [<Extension>]
    type ExportModuleExt =
        [<Extension>]
        static member ExportXMLforXGI(system: DsSystem, path: string, tempLSISxml) =
            exportXMLforXGI (system, path, if tempLSISxml <> null then Some(tempLSISxml) else None)

