namespace Engine.CodeGenCPU

open System.IO
open System.Linq
open System.Runtime.CompilerServices
open Engine.Core
open Engine.Common.FS
open PLC.CodeGen.LSXGI
open PLC.CodeGen.Common
open System

[<AutoOpen>]
module ExportModule =
    let generateXmlXGI (system:DsSystem) globalStorages localStorages (pous:PouGen seq) existingLSISprj: string =
        let projName = system.Name
        let getXgiPOUParams (pouName:string) (taskName:string) (pouGens:PouGen seq) =
            let pouParams:XgiPOUParams = {
                /// POU name.  "DsLogic"
                POUName = pouName
                /// POU container task name
                TaskName = taskName
                        /// POU ladder 최상단의 comment
                Comment = $"DsLogic Automatically generate"
                LocalStorages = localStorages
                GlobalStorages = globalStorages
                CommentedStatements = pouGens.Collect(fun p->p.CommentedStatements()) |> Seq.toList
            }
            pouParams

        let usedByteIndices =
            let getBytes addr =
                [ match addr with
                    | RegexPattern @"^%M([BWDL])(\d+)\.\d+$" [ AddressConvert.DataTypePattern dataType; Int32Pattern off2; ] ->
                        let l = dataType.GetByteLength()
                        yield l * off2 + 1
                    | RegexPattern @"^%M([BWDL])(\d+)$" [ AddressConvert.DataTypePattern dataType; Int32Pattern off2; ] ->
                        let l = dataType.GetByteLength()
                        let s = l * off2;
                        yield! [s .. s+l]
                    | RegexPattern @"^%MX(\d+)$" [ Int32Pattern bitoffset; ] ->
                        yield bitoffset/8
                    | _ ->
                        failwith "ERROR"
                ]

            let usedAddresses =
                system.TagManager.Storages.Values
                |> Seq.filter(fun f -> not <| (f :? TimerCounterBaseStruct))
                |> Seq.filter(fun f -> f.Address <> null && f.Address <> "")
                |> Array.ofSeq

            // check if there is any duplicated address
            let duplicatedAddresses =
                usedAddresses
                |> Array.groupBy(fun f -> f.Address)
                |> Array.filter(fun (address, vs) -> vs.Length > 1)

            // prints duplications
            if duplicatedAddresses.Length > 0 then
                let dupItems =
                    duplicatedAddresses
                    |> map (fun (address, vs) ->
                        let names = vs |> map(fun var -> var.Name) |> String.concat ", "
                        $"  {address}: {names}")
                    |> String.concat Environment.NewLine

                failwithlog $"Total {duplicatedAddresses.Length} Duplicated address items:{Environment.NewLine}{dupItems}"

            let autoMemoryAllocationTags =
                system.TagManager.Storages.Values
                |> Seq.filter(fun f -> not <| (f :? TimerCounterBaseStruct))
                |> Seq.filter(fun f-> not <| String.IsNullOrEmpty(f.Address) && f.Address.StartsWith("%M"))
                |> Array.ofSeq

            autoMemoryAllocationTags |> map (fun f -> f.Name) |> String.concat ", " |> logDebug "Auto Memory Allocation Tags: %s"

            // generate used memory byte indices
            autoMemoryAllocationTags
            |> Seq.collect(fun f -> f.Address |> getBytes)
            |> Seq.distinct
            |> Seq.sort
            |> List.ofSeq

        logDebug "Used byte indices: %A" usedByteIndices

        let projParams:XgiProjectParams = {
            defaultXgiProjectParams with
                ProjectName = projName
                GlobalStorages = globalStorages
                ExistingLSISprj = existingLSISprj
                AppendExpressionTextToRungComment = false
                MemoryAllocatorSpec = AllocatorFunctions (createMemoryAllocator "M" (0, 640*1024) usedByteIndices)    // 640K M memory 영역
                POUs = [
                          yield pous.Where(fun f->f.IsActive) |> getXgiPOUParams "Active" "Active"
                          yield pous.Where(fun f->f.IsDevice) |> getXgiPOUParams "Devices" "Devices"
                          for p in pous.Where(fun f->f.IsExternal) do
                             yield getXgiPOUParams (p.ToSystem().Name) (p.TaskName()) [p]
                ]
        }

        projParams.GenerateXmlString()

    let exportXMLforXGI(system:DsSystem, path:string, existingLSISprj) =
        Runtime.Target <- XGI
        let globalStorage = new Storages()
        let localStorage =  new Storages()
        let result = Cpu.LoadStatements(system, globalStorage)
        let xml = generateXmlXGI system globalStorage localStorage result existingLSISprj
        let crlfXml = xml.Replace("\r\n", "\n").Replace("\n", "\r\n")
        File.WriteAllText(path, crlfXml)


    let exportTextforDS() = ()

    [<Extension>]
    type ExportModuleExt =
        [<Extension>] static member ExportXMLforXGI (system:DsSystem, path:string, tempLSISxml) = exportXMLforXGI(system, path, tempLSISxml)
        [<Extension>] static member ExportXMLforPC  (system:DsSystem, path:string) = if system.Name = path then ()//test ahn

