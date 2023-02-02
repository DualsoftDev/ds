namespace Engine.CodeGenCPU

open Engine.Core
open Engine.Common.FS
open System.Runtime.CompilerServices
open System.Linq
open PLC.CodeGen.LSXGI
open System.IO

[<AutoOpen>]
module ExportModule =
    let generateXmlXGI projName globalStorages localStorages (pous:PouGen seq) existingLSISprj: string =
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

        let projParams:XgiProjectParams = {
            defaultXgiProjectParams with
                ProjectName = projName
                GlobalStorages = globalStorages
                ExistingLSISprj = existingLSISprj
                AppendExpressionTextToRungComment = false
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
        let xml = generateXmlXGI system.Name globalStorage localStorage result existingLSISprj
        let crlfXml = xml.Replace("\r\n", "\n").Replace("\n", "\r\n")
        File.WriteAllText(path, crlfXml)


    let exportTextforDS() = ()

    [<Extension>]
    type ExportModuleExt =
        [<Extension>] static member ExportXMLforXGI (system:DsSystem, path:string, tempLSISxml) = exportXMLforXGI(system, path, tempLSISxml)
        [<Extension>] static member ExportXMLforPC  (system:DsSystem, path:string) = if system.Name = path then ()//test ahn

