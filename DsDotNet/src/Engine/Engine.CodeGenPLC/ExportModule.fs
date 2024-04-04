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
    [<Obsolete("getBytes 이거 수정 필요!!!!")>]
    let generateXmlXGX (plcType:RuntimeTargetType) (system: DsSystem) globalStorages localStorages (pous: PouGen seq) existingLSISprj : string =
        let projName = system.Name
        
        let getXgxPOUParams (pouName: string) (taskName: string) (pouGens: PouGen seq) =
            let pouParams: XgxPOUParams =
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
            let tryParseXgxTag =
                match plcType with
                | XGI -> tryParseXGITag
                | XGK -> tryParseXGKTag
                | _ -> failwithlog "Not supported plc type"

            let getBytes addr = 
                [  
                    match tryParseXgxTag addr with
                    |Some tag -> 
                        if tag.DataType = PLCHwModel.DataType.Bit 
                        then 
                            yield tag.ByteOffset
                        else 
                            yield! [tag.ByteOffset..tag.DataType.GetByteLength()]
                    |None ->
                        yield 0        // todo: 이거 삭제하고 아래 fail uncomment
                        //failwithlog "ERROR"
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

        let projParams: XgxProjectParams =
            { defaultXgxProjectParams with
                ProjectName = projName
                GlobalStorages = globalStorages
                ExistingLSISprj = existingLSISprj
                AppendExpressionTextToRungComment = false
                MemoryAllocatorSpec = AllocatorFunctions(createMemoryAllocator "M" (0, 640 * 1024) usedByteIndices) // 640K M memory 영역
                POUs =
                    [ yield pous.Where(fun f -> f.IsActive) |> getXgxPOUParams "Active" "Active"
                      yield pous.Where(fun f -> f.IsDevice) |> getXgxPOUParams "Devices" "Devices"
                      for p in pous.Where(fun f -> f.IsExternal) do
                          yield getXgxPOUParams (p.ToSystem().Name) (p.TaskName()) [ p ] ] }

        projParams.GenerateXmlString()

    let exportXMLforLSPLC (plcType:RuntimeTargetType, system: DsSystem, path: string, existingLSISprj) =
        assert(plcType.IsOneOf(XGI, XGK))
        RuntimeDS.Target <- plcType
        let globalStorage = new Storages()
        let localStorage = new Storages()
        let pous = CpuLoaderExt.LoadStatements(system, globalStorage)
        // Create a list to hold <C>ommented <S>tatement<S>
        let mutable css = []

        // Add commented statements from each CPU
        for cpu in pous do
            css <- css @ cpu.CommentedStatements() |> List.ofSeq

        let usedTagNames = getTotalTags(css.Select(fun s->s.Statement)) |> Seq.map(fun t->t.Name, t) |> dict
        globalStorage.Iter(fun tagKV-> 

            if not (usedTagNames.ContainsKey(tagKV.Key)) 
               && tagKV.Value.DataType = typedefof<bool>  //bool 타입만 지우기 가능 타이머 카운터 살림
               && TagKindExt.GetVariableTagKind(tagKV.Value).IsNone //VariableTag 살림
            then globalStorage.Remove(tagKV.Key)|>ignore
            )

        let xml = generateXmlXGX plcType system globalStorage localStorage pous existingLSISprj
        let crlfXml = xml.Replace("\r\n", "\n").Replace("\n", "\r\n")
        File.WriteAllText(path, crlfXml)


    let exportTextforDS () = ()

[<Extension>]
type ExportModuleExt =
    [<Extension>]
    static member ExportXMLforXGI(system: DsSystem, path: string, tempLSISxml:string) =
        let existingLSISprj = if not(tempLSISxml.IsNullOrEmpty()) then Some(tempLSISxml) else None
        exportXMLforLSPLC (XGI, system, path, existingLSISprj)

    [<Extension>]
    static member ExportXMLforXGK(system: DsSystem, path: string, tempLSISxml:string) =
        let existingLSISprj = if not(tempLSISxml.IsNullOrEmpty()) then Some(tempLSISxml) else None
        exportXMLforLSPLC (XGK, system, path, existingLSISprj)

