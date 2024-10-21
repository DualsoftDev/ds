namespace rec Engine.Core

open System.IO
open System.Linq
open Newtonsoft.Json
open System.Collections.Generic
open System.Runtime.CompilerServices
open Dual.Common.Core.FS
open Dual.Common.Base.FS


[<AutoOpen>]
module ExportConfigsMoudle =

    type InterfacePlanSimpleConfig = {
        Motions: DsSimplePlanInterface[]
    }
    type DsSimplePlanInterface = { MotionName:string }

    type InterfaceConfig = {
        SystemName: string
        HwName: string //LS-XGI-IO, LS-XGK-IO, PC
        HwIP: string //PlatformIP
        DsPlanInterfaces: DsPlanInterface[]
        DsActionInterfaces: DsActionInterface[]
    }

    type DsPlanInterface = {
        Id: int
        Work: string
        WorkInfo: string

        ///storage name, address
        ScriptStartTag: string*string
        ScriptEndTag: string*string
        ///storage name, address
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
        Name : string
        Address : string
        DataType : string //bool. int32, float
        DeviceType : string //input, output, memory
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

        sys.GetTaskDevsCall() |> distinctBy (fun (td, _c) -> td)
        |> Seq.filter(fun (dev,_)-> dev.ApiItem.RX.Motion.IsSome) //RX 기준으로 모션 처리한다.
        |> Seq.iter(fun (dev,v) ->
            let real = dev.ApiItem.RX
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
                    Action = dev.ApiItem.Name
                    LibraryPath = sys.LoadedSystems.TryFindWithName(dev.DeviceName).Value.RelativeFilePath
                    Motion = dev.FullName
                }
            ifs.Add dataSync |> ignore
        )

        ifs.ToArray()

    let getDsActionInterfaces (sys: DsSystem) =
        let ifs = HashSet<DsActionInterface>()

        sys.GetTaskDevsCall().DistinctBy(fun (td, _c) -> td)
        |> Seq.iter(fun (dev,_) ->

            if dev.InTag.IsNonNull()
            then
                let dataIn =
                    {
                        Id = ifs.Count
                        Name = dev.InTag.Name
                        Address = dev.InTag.Address
                        DataType = dev.InTag.DataType.ToDsDataTypeString()
                        DeviceType = "input"
                    }
                ifs.Add dataIn |> ignore

            if dev.OutTag.IsNonNull()
            then
                let dataOut =
                    {
                        Id = ifs.Count
                        Name = dev.OutTag.Name
                        Address = dev.OutTag.Address
                        DataType = dev.OutTag.DataType.ToDsDataTypeString()
                        DeviceType = "output"
                    }

                ifs.Add dataOut |> ignore
        )

        ifs.ToArray()

    let getDsInterfaceConfig (sys: DsSystem, hwName:string, hwIP:string) =
        {
            DsPlanInterfaces = getDsPlanInterfaces sys
            DsActionInterfaces = getDsActionInterfaces sys
            HwName  = hwName
            HwIP = hwIP
            SystemName = sys.Name
        }

[<AutoOpen>]
type ExportConfigsExt =

    [<Extension>]
    static member ExportDSInterface (sys:DsSystem, exportPath:string, hwName:string, hwIP:string) =
        let interfaceConfig = getDsInterfaceConfig (sys, hwName, hwIP)
        saveInterfaceConfig exportPath interfaceConfig

        let dsSimpleInterfaces =
            interfaceConfig.DsPlanInterfaces
                .Select(fun f-> {MotionName =  f.Motion}).ToArray()

        let interfaceSimpleConifg = {Motions =dsSimpleInterfaces}
        let exportSimplePath =  PathManager.changeExtension (DsFile(exportPath)) "dsConfigMoiton"
        saveInterfaceSimpleConfig exportSimplePath interfaceSimpleConifg

    [<Extension>]
    static member LoadInterfaceConfig (path:string) = loadInterfaceConfig path
    [<Extension>]
    static member getSimplePlanInterface (jsonText:string) = JsonConvert.DeserializeObject<DsSimplePlanInterface>(jsonText)
