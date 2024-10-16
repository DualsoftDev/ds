namespace Engine.Runtime

open System
open System.IO
open System.Linq
open Newtonsoft.Json
open System.Collections.Generic
open System.Runtime.CompilerServices
open Engine.Core
open Dual.Common.Core.FS
open System.ComponentModel

[<AutoOpen>]
module DsPropertyModule =

    type PropertyBase() =
        new(name: string) as this = PropertyBase()  then this.UpdateProperty(name)
        [<Browsable(false)>] // 속성 창에서 숨기기
        member val Name = getNull<string>() with get, set
        member x._ClassType = x.GetType().Name

        member x.UpdateProperty(name: string) =
            x.Name <- name

    type PropertySystem() =
        inherit PropertyBase()
        new(x: DsSystem) as this = PropertySystem() then this.UpdateProperty(x)

        member x.UpdateProperty(sys: DsSystem) =
            x.Name <- sys.Name

    type PropertyReal() =
        inherit PropertyBase()
        new(x: Real) as this = PropertyReal() then this.UpdateProperty(x)

        member val Finished = false with get, set
        member val NoTransData = false with get, set
        member val Motion = getNull<string>() with get, set
        member val Script = getNull<string>() with get, set
        member val RepeatCount = Nullable() with get, set
        member val AVG = Nullable() with get, set
        member val STD = Nullable() with get, set

        member x.UpdateProperty(real: Real) =
            x.Name <- real.Name
            x.Finished <- real.Finished
            x.NoTransData <- real.NoTransData
            x.Motion <- toNull real.Motion
            x.Script <- toNull real.Script
            x.RepeatCount <- toNullable real.RepeatCount
            x.AVG <- toNullable real.DsTime.AVG
            x.STD <- toNullable real.DsTime.STD

    type PropertyCall() =
        inherit PropertyBase()
        new(x: Call) as this = PropertyCall() then this.UpdateProperty(x)

        member val Disabled = false with get, set
        member val SafetyConditions = ResizeArray<string>() with get, set
        member val AutoPreConditions = ResizeArray<string>() with get, set

        member x.UpdateProperty(call: Call) =
            x.Name <- call.Name
            x.Disabled <- call.Disabled
            x.SafetyConditions <- call.SafetyConditions.Select(fun f->f.GetJob().DequotedQualifiedName).ToResizeArray()
            x.AutoPreConditions <- call.AutoPreConditions.Select(fun f->f.GetJob().DequotedQualifiedName).ToResizeArray()

    type PropertyTaskDev() =
        inherit PropertyBase()
        new(x: TaskDev) as this = PropertyTaskDev() then this.UpdateProperty(x)
        member x.TaskDevName = x.Name

        member x.UpdateProperty(taskDev: TaskDev) =
            x.Name <- taskDev.FullName

    type PropertyApiParam() =
        inherit PropertyBase()
        new(x: ApiParam) as this = PropertyApiParam() then this.UpdateProperty(x)
        [<TypeConverter(typeof<ExpandableObjectConverter>)>]
        member val InParam = getNull<TaskDevParamSub>() with get, set
        [<TypeConverter(typeof<ExpandableObjectConverter>)>]
        member val OutParam = getNull<TaskDevParamSub>() with get, set

        member x.UpdateProperty(apiParam: ApiParam) =
            x.Name <- apiParam.ApiItem.QualifiedName
            x.InParam  <-  match apiParam.TaskDevParamIO.InParam  with |Some x -> TaskDevParamSub(x) | _ -> getNull<TaskDevParamSub>()
            x.OutParam <-  match apiParam.TaskDevParamIO.OutParam with |Some x -> TaskDevParamSub(x) | _ -> getNull<TaskDevParamSub>()


    type PropertyJob() =
        inherit PropertyBase()
        new(x: Job) as this = PropertyJob() then this.UpdateProperty(x)

        member val JobAction = JobTypeAction.ActionNormal with get, set
        member val JobSensing = JobTypeSensing.SensingNormal with get, set
        member val TimeOut = Nullable() with get, set
        member val DelayCheck = Nullable() with get, set

        member x.UpdateProperty(job: Job) =
            x.Name <- job.DequotedQualifiedName
            x.JobAction <- job.JobParam.JobAction
            x.JobSensing <- job.JobParam.JobSensing
            x.TimeOut <- toNullable job.JobTime.TimeOut    
            x.DelayCheck <- toNullable job.JobTime.DelayCheck

    type PropertyFlow() =
        inherit PropertyBase()
        new(x: Flow) as this = PropertyFlow() then this.UpdateProperty(x)

        member x.UpdateProperty(flow: Flow) =
            x.Name <- flow.Name

    type PropertyAlias() =
        inherit PropertyBase()
        new(x: Alias) as this = PropertyAlias() then this.UpdateProperty(x)

        member val TargetName = null with get, set
        member val IsExFlowReal = false with get, set

        member x.UpdateProperty(alias: Alias) =
            x.Name <- alias.Name
            x.TargetName <- alias.TargetWrapper.GetTarget().DequotedQualifiedName

    type PropertyApiItem() =
        inherit PropertyBase()
        new(x: ApiItem) as this = PropertyApiItem() then this.UpdateProperty(x)

        member val TxName = "" with get, set
        member val RxName = "" with get, set

        member x.UpdateProperty(apiItem: ApiItem) =
            x.Name <- apiItem.Name
            x.TxName <- apiItem.TX.DequotedQualifiedName
            x.RxName <- apiItem.RX.DequotedQualifiedName

    type PropertyOperatorFunction() =
        inherit PropertyBase()
        new(x: OperatorFunction) as this = PropertyOperatorFunction() then this.UpdateProperty(x)

        member val OperatorCode = "" with get, set

        member x.UpdateProperty(opFunc: OperatorFunction) =
            x.Name <- opFunc.Name
            x.OperatorCode <- opFunc.OperatorCode

    type PropertyCommandFunction() =
        inherit PropertyBase()
        new(x: CommandFunction) as this = PropertyCommandFunction() then this.UpdateProperty(x)

        member val CommandCode = "" with get, set

        member x.UpdateProperty(cmdFunc: CommandFunction) =
            x.Name <- cmdFunc.Name
            x.CommandCode <- cmdFunc.CommandCode

    type PropertyHwSystemDef() =
        inherit PropertyBase()
        new(x: HwSystemDef) as this = PropertyHwSystemDef() then this.UpdateProperty(x)

        member val SettingFlows = "" with get, set
        member val InAddress = null with get, set
        member val OutAddress = null with get, set
        member val HwDefType = "" with get, set
        [<TypeConverter(typeof<ExpandableObjectConverter>)>]
        member val InParam = getNull<TaskDevParamSub>() with get, set
        [<TypeConverter(typeof<ExpandableObjectConverter>)>]
        member val OutParam = getNull<TaskDevParamSub>() with get, set

        member x.UpdateProperty(hwDef: HwSystemDef) =
            x.Name <- hwDef.Name
            x.SettingFlows <- String.Join("; ", hwDef.SettingFlows |> Seq.map (fun f -> f.Name))
            x.InAddress <- hwDef.InAddress
            x.OutAddress <- hwDef.OutAddress
            x.HwDefType <- hwDef.GetType().Name
            x.InParam  <-  match hwDef.TaskDevParamIO.InParam  with |Some x -> TaskDevParamSub(x) | _ -> getNull<TaskDevParamSub>()
            x.OutParam <-  match hwDef.TaskDevParamIO.OutParam with |Some x -> TaskDevParamSub(x) | _ -> getNull<TaskDevParamSub>()



[<Extension>]
type DsPropertyExt =

    [<Extension>]
    static member ExportPropertyToJson(path: string, data: obj) =
        let settings = JsonSerializerSettings()
        settings.Formatting <- Formatting.Indented
        settings.TypeNameHandling <- TypeNameHandling.Auto
        let json = JsonConvert.SerializeObject(data, settings)
        File.WriteAllText(path, json)

    [<Extension>]
    static member ImportPropertyFromJson<'T>(path: string) : 'T =
        if File.Exists(path) then
            let json = File.ReadAllText(path)
            let settings = JsonSerializerSettings()
            settings.TypeNameHandling <- TypeNameHandling.Auto
            JsonConvert.DeserializeObject<'T>(json, settings)
        else
            failwith "File not found or invalid path"
