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

    let loadInterfaceConfig (path: string) =
        let json = File.ReadAllText(path)
        JsonConvert.DeserializeObject<InterfaceConfig>(json, jsonSettings)

    let saveInterfaceConfig (path: string) (interfaceConfig:InterfaceConfig) =
        let json = JsonConvert.SerializeObject(interfaceConfig, Formatting.Indented, jsonSettings)
        File.WriteAllText(path, json)

    let saveInterfaceSimpleConfig (path: string) (interfaceSimpleConfig:InterfaceSimpleConfig) =
        let json = JsonConvert.SerializeObject(interfaceSimpleConfig, Formatting.Indented, jsonSettings)
        File.WriteAllText(path, json)

    let getDsInterfaces (sys: DsSystem) =
        let ifs = HashSet<DsInterface>()

        sys.GetDevicesCall()
           |> Seq.filter(fun (dev,_)-> dev.ApiItem.RX.Motion.IsSome) //RX 기준으로 모션 처리한다.
           |> Seq.iter(fun (dev,v) ->

                    let dataSync = 
                        {
                            Id = ifs.Count
                            Work = dev.ApiItem.RX.Name
                            WorkInfo = dev.ApiItem.RX.Motion.Value
                            ScriptStartTag = dev.ApiItem.RX.ScriptStartTag.Address
                            ScriptEndTag = dev.ApiItem.RX.ScriptEndTag.Address
                            MotionStartTag = dev.ApiItem.RX.MotionStartTag.Address
                            MotionEndTag = dev.ApiItem.RX.MotionEndTag.Address
                            Station = v.Parent.GetFlow().Name
                            Device = dev.DeviceName
                            Action = dev.ApiItem.Name
                            LibraryPath = sys.LoadedSystems.TryFindWithName(dev.DeviceName).Value.RelativeFilePath
                            Motion = dev.ApiStgName
                        }
                    ifs.Add dataSync |> ignore
               )
           

        ifs.ToArray()



[<AutoOpen>]
type ExportConfigsExt =

    [<Extension>] 
    static member ExportDSInterface (sys:DsSystem, exportPath:string) =
        let dsInterfaces = getDsInterfaces(sys)
        let interfaceConfig = {SystemName = sys.Name; DsInterfaces = dsInterfaces}
        saveInterfaceConfig exportPath interfaceConfig

        let dsSimpleInterfaces = dsInterfaces
                                    .Select(fun f-> f.Id, f.Motion).ToArray() 

        let interfaceSimpleConifg = {MotionSync = dsSimpleInterfaces}
        let exportSimplePath =  PathManager.changeExtension (DsFile(exportPath)) "dsConfigMoiton"
        saveInterfaceSimpleConfig exportSimplePath interfaceSimpleConifg

    [<Extension>] 
    static member LoadInterfaceConfig (path:string) = loadInterfaceConfig path
