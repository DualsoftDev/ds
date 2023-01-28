namespace Engine.CodeGenCPU

open Engine.Core
open Engine.Common.FS
open System.Runtime.CompilerServices
open System.Linq
open PLC.CodeGen.LSXGI
open System.IO

[<AutoOpen>]
module ExportModule =
    let generateXmlXGI projName globalStorages localStorages (pous:PouGen seq): string =
        let getXgiPOUParams (pouGen:PouGen) =
            let pouParams:XgiPOUParams = {
                /// POU name.  "DsLogic"
                POUName = pouGen.ToSystem().Name
                /// POU container task name
                TaskName = pouGen.TaskName()
                        /// POU ladder 최상단의 comment
                Comment = "DS Logic for XGI"
                LocalStorages = localStorages
                GlobalStorages = globalStorages
                CommentedStatements = pouGen.CommentedStatements()
            }
            pouParams

        let projParams:XgiProjectParams = {
            defaultXgiProjectParams with
                ProjectName = projName
                GlobalStorages = globalStorages
                POUs = pous.Select(getXgiPOUParams) |> Seq.toList
        }

        projParams.GenerateXmlString()

    let exportXMLforXGI(system:DsSystem, path:string) =
        Runtime.Target <- XGI
        let globalStorage = Storages()
        let localStorage = Storages()
        let result = Cpu.LoadStatements(system, globalStorage)
        let xml = generateXmlXGI system.Name globalStorage localStorage result
        let crlfXml = xml.Replace("\r\n", "\n").Replace("\n", "\r\n")
        File.WriteAllText($@"{path}.xml", crlfXml)


    let exportTextforDS() = ()

    [<Extension>]
    type ExportModuleExt =
        [<Extension>] static member ExportXMLforXGI (system:DsSystem, path:string) = exportXMLforXGI(system, path)

