namespace rec Engine.Core

open System.IO
open System.Linq
open Newtonsoft.Json
open System.Collections.Generic
open System.Runtime.CompilerServices


[<AutoOpen>]
module ExportConfigsMoudle =
    
   
    type InterfaceConfig = {
        SystemName: string
        DsInterfaces: DsInterface[]
    }
    type DsInterface = {
        Id: int
        Work: string
        WorkType: string
        WorkInfo: string
        Address: string
        Device: string
        LibraryPath: string
        ParentFlow: string
        ParentCall: string
    }

    let private jsonSettings = JsonSerializerSettings()

    let LoadInterfaceConfig (path: string) =
        let json = File.ReadAllText(path)
        JsonConvert.DeserializeObject<InterfaceConfig>(json, jsonSettings)

    let SaveInterfaceConfig (path: string) (interfaceConfig:InterfaceConfig) =
        let json = JsonConvert.SerializeObject(interfaceConfig, Formatting.Indented, jsonSettings)
        File.WriteAllText(path, json)

    
    let GetDsInterfaces (sys: DsSystem) =
        let ifs = HashSet<DsInterface>()

        sys.GetVerticesHasJob()
           |> Seq.iter(fun v -> 
               let job = v.TargetJob
               job.DeviceDefs
               |> Seq.filter(fun dev -> dev.ApiItem.TX.Path3D.IsSome)
               |> Seq.iter(fun dev ->

                    let dataSync = 
                        {
                            Id = ifs.Count+1
                            Work = dev.ApiItem.TX.Name
                            WorkType = "Sync"
                            WorkInfo = dev.ApiItem.TX.Path3D.Value
                            Address = dev.ApiItem.TX.ActionSyncTag.Address
                            Device = dev.DeviceName
                            LibraryPath = sys.LoadedSystems.TryFindWithName(dev.DeviceName).Value.AbsoluteFilePath
                            ParentFlow = v.Parent.GetFlow().Name
                            ParentCall = v.Name
                        }
                    ifs.Add dataSync |> ignore

                    let dataStart = { dataSync with Id = ifs.Count+1; WorkType = "Start"; Address = dev.ApiItem.TX.ActionStartTag.Address }
                    ifs.Add dataStart |> ignore

                    let dataEnd   = { dataSync with Id = ifs.Count+1; WorkType = "End";   Address = dev.ApiItem.TX.ActionEndTag.Address }
                    ifs.Add dataEnd |> ignore
               )
           )

        ifs.ToArray()




[<AutoOpen>]
type ExportConfigsExt =

    [<Extension>] 
    static member ExportDSInterface (sys:DsSystem, exportPath:string) =
        let dsInterfaces = GetDsInterfaces(sys)
        let interfaceConfig = {SystemName = sys.Name; DsInterfaces = dsInterfaces}
        ExportConfigsMoudle.SaveInterfaceConfig exportPath interfaceConfig

