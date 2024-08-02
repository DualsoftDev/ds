namespace rec Engine.Core

open System.IO
open System.Linq
open Newtonsoft.Json
open System.Collections.Generic
open System.Runtime.CompilerServices
open Dual.Common.Core.FS


[<AutoOpen>]
module ExportConfigsMoudle =

    type InterfacePlanSimpleConfig = {
        Motions: DsSimplePlanInterface[]
    }
    type DsSimplePlanInterface = { MotionName:string }

    type InterfaceConfig = {
        SystemName: string
        DsPlanInterfaces: DsPlanInterface[]
        DsActionInterfaces: DsActionInterface[]
    }
    
    type DsPlanInterface = {
        Id: int
        Work: string
        WorkInfo: string
   
        ScriptStartTag: string*string
        ScriptEndTag: string*string

        MotionStartTag: string*string
        MotionEndTag: string*string

        Station: string
        Device: string
        Action: string
        LibraryPath: string
        Motion: string
    }
    with 
        member x.ToJson() = JsonConvert.SerializeObject(x, Formatting.Indented)
        member x.ToJsonSimpleFormat() = JsonConvert.SerializeObject({MotionName =  x.Motion}, Formatting.Indented)
    
    type DsActionInterface = {
        Id: int
        InputTag: string*string
        OutputTag: string*string
    }
    with 
        member x.ToJson() = JsonConvert.SerializeObject(x, Formatting.Indented)
    

    let private jsonSettings = JsonSerializerSettings()

    let loadInterfaceConfig (path: string) =
        let json = File.ReadAllText(path)
        JsonConvert.DeserializeObject<InterfaceConfig>(json, jsonSettings)

    let saveInterfaceConfig (path: string) (interfaceConfig:InterfaceConfig) =
        let json = JsonConvert.SerializeObject(interfaceConfig, Formatting.Indented, jsonSettings)
        File.WriteAllText(path, json)

    let saveInterfaceSimpleConfig (path: string) (interfaceSimpleConfig:InterfacePlanSimpleConfig) =
        let json = JsonConvert.SerializeObject(interfaceSimpleConfig, Formatting.Indented, jsonSettings)
        File.WriteAllText(path, json)

    let getDsPlanInterfaces (sys: DsSystem) =
        let ifs = HashSet<DsPlanInterface>()

        sys.GetTaskDevsCall().DistinctBy(fun (td, _c) -> td)
        |> Seq.filter(fun (dev,_)-> dev.FirstApi.RX.Motion.IsSome) //RX 기준으로 모션 처리한다.
        |> Seq.iter(fun (dev,v) ->  
            let real = dev.FirstApi.RX
            let dataSync = 
                {
                    Id = ifs.Count
                    Work = real.Name
                    WorkInfo = real.Motion.Value
                    ScriptStartTag = real.ScriptStartTag.Name, real.ScriptStartTag.Address
                    ScriptEndTag =   real.ScriptEndTag.Name  , real.ScriptEndTag.Address
                    MotionStartTag = real.MotionStartTag.Name, real.MotionStartTag.Address
                    MotionEndTag =   real.MotionEndTag.Name  , real.MotionEndTag.Address
                    Station = v.Parent.GetFlow().Name
                    Device = dev.DeviceName
                    Action = dev.FirstApi.Name
                    LibraryPath = sys.LoadedSystems.TryFindWithName(dev.DeviceName).Value.RelativeFilePath
                    Motion = dev.GetApiStgName(v.TargetJob)
                }
            ifs.Add dataSync |> ignore
        )

        ifs.ToArray()

    let getDsActionInterfaces (sys: DsSystem) =
        let ifs = HashSet<DsActionInterface>()

        sys.GetTaskDevsCall().DistinctBy(fun (td, _c) -> td)
        |> Seq.iter(fun (dev,_) ->  
            let dataSync = 
                {
                    Id = ifs.Count
                    InputTag = if dev.InTag.IsNonNull() then dev.InTag.Name, dev.InTag.Address else "", ""
                    OutputTag = if dev.OutTag.IsNonNull() then dev.OutTag.Name, dev.OutTag.Address else "", ""
                }
            ifs.Add dataSync |> ignore
        )

        ifs.ToArray()

    let getDsInterfaceConfig (sys: DsSystem) =
        { 
            DsPlanInterfaces = getDsPlanInterfaces sys
            DsActionInterfaces = getDsActionInterfaces sys
            SystemName = sys.Name
        }

[<AutoOpen>]
type ExportConfigsExt =

    [<Extension>] 
    static member ExportDSInterface (sys:DsSystem, exportPath:string) =
        let pIFs = getDsPlanInterfaces(sys)
        let aIFs = getDsActionInterfaces(sys)
        let interfaceConfig = {SystemName = sys.Name; DsPlanInterfaces = pIFs; DsActionInterfaces = aIFs}
        saveInterfaceConfig exportPath interfaceConfig

        let dsSimpleInterfaces =
            pIFs
                .Select(fun f-> {MotionName =  f.Motion}).ToArray() 

        let interfaceSimpleConifg = {Motions =dsSimpleInterfaces}
        let exportSimplePath =  PathManager.changeExtension (DsFile(exportPath)) "dsConfigMoiton"
        saveInterfaceSimpleConfig exportSimplePath interfaceSimpleConifg

    [<Extension>] 
    static member LoadInterfaceConfig (path:string) = loadInterfaceConfig path
