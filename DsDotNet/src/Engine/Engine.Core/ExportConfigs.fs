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
        WorkInfo: string
   
        ScriptStartTag: string
        ScriptEndTag: string

        MotionStartTag: string
        MotionEndTag: string

        Station: string
        Device: string
        Action: string
        LibraryPath: string
        Motion: string
    }
    
    type InterfaceSimpleConfig = {
        MotionSync: (int*string)[]
    }

    let private jsonSettings = JsonSerializerSettings()

    let LoadInterfaceConfig (path: string) =
        let json = File.ReadAllText(path)
        JsonConvert.DeserializeObject<InterfaceConfig>(json, jsonSettings)

    let SaveInterfaceConfig (path: string) (interfaceConfig:InterfaceConfig) =
        let json = JsonConvert.SerializeObject(interfaceConfig, Formatting.Indented, jsonSettings)
        File.WriteAllText(path, json)

    let SaveInterfaceSimpleConfig (path: string) (interfaceSimpleConfig:InterfaceSimpleConfig) =
        let json = JsonConvert.SerializeObject(interfaceSimpleConfig, Formatting.Indented, jsonSettings)
        File.WriteAllText(path, json)

    let GetDsInterfaces (sys: DsSystem) =
        let ifs = HashSet<DsInterface>()

        sys.GetVerticesHasJob()
           |> Seq.iter(fun v -> 
               v.TargetJob.DeviceDefs
               |> Seq.filter(fun dev -> dev.ApiItem.TX.Motion.IsSome)
               |> Seq.iter(fun dev ->

                    let dataSync = 
                        {
                            Id = ifs.Count+1
                            Work = dev.ApiItem.TX.Name
                            WorkInfo = dev.ApiItem.TX.Motion.Value
                            ScriptStartTag = dev.ApiItem.TX.ScriptStartTag.Address
                            ScriptEndTag = dev.ApiItem.TX.ScriptEndTag.Address
                            MotionStartTag = dev.ApiItem.TX.MotionStartTag.Address
                            MotionEndTag = dev.ApiItem.TX.MotionEndTag.Address
                            Station = v.Parent.GetFlow().Name
                            Device = dev.DeviceName
                            Action = dev.ApiItem.Name
                            LibraryPath = sys.LoadedSystems.TryFindWithName(dev.DeviceName).Value.RelativeFilePath
                            Motion = dev.ApiStgName
                        }
                    ifs.Add dataSync |> ignore

                    //let dataEnd   = { dataSync with Id = ifs.Count+1; WorkType = "End";}
                    //ifs.Add dataEnd |> ignore
               )
           )

        ifs.ToArray()



[<AutoOpen>]
type ExportConfigsExt =

    [<Extension>] 
    static member ExportDSInterface (sys:DsSystem, exportPath:string) =
        let dsInterfaces = GetDsInterfaces(sys)
        let interfaceConfig = {SystemName = sys.Name; DsInterfaces = dsInterfaces}
        SaveInterfaceConfig exportPath interfaceConfig

        let dsSimpleInterfaces = dsInterfaces
                                    .Select(fun f-> f.Id, f.Motion).ToArray() 

        let interfaceSimpleConifg = {MotionSync = dsSimpleInterfaces}
        let exportSimplePath =  PathManager.changeExtension (DsFile(exportPath)) "dsConfigMoiton"
        SaveInterfaceSimpleConfig exportSimplePath interfaceSimpleConifg

