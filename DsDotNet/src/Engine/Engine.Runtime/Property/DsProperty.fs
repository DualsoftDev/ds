namespace Engine.Runtime

open System
open System.IO
open System.Linq
open System.Collections
open Newtonsoft.Json
open System.Runtime.CompilerServices
open Engine.Core
open Dual.Common.Core.FS
open System.ComponentModel


[<AutoOpen>]
module rec DsPropertyModule =


    type PropertySystem() =
        inherit PropertyBase()
        new(x: DsSystem) as this = PropertySystem() then this.UpdateProperty(x)

        member private x.UpdateProperty(sys: DsSystem) =
            x.Name <- sys.Name

    and PropertyReal() =
        inherit PropertyBase()
        new(x: Real) as this = PropertyReal() then this.UpdateProperty(x)

        member val Finished = false with get, set
        member val NoTransData = false with get, set
        member val Motion = getNull<string>() with get, set
        member val Script = getNull<string>() with get, set
        member val RepeatCount = Nullable() with get, set
        member val AVG = Nullable() with get, set
        member val STD = Nullable() with get, set

        member private x.UpdateProperty(real: Real) =
            x.Name <- real.Name
            x.Finished <- real.Finished
            x.NoTransData <- real.NoTransData
            x.Motion <- toNull real.Motion
            x.Script <- toNull real.Script
            x.RepeatCount <- toNullable real.RepeatCount
            x.AVG <- toNullable real.DsTime.AVG
            x.STD <- toNullable real.DsTime.STD

    and PropertyJob() =
        inherit PropertyBase()
        new(x: Job) as this = PropertyJob() then this.UpdateProperty(x)

        member val JobAction = JobTypeAction.ActionNormal with get, set
        member val JobSensing = JobTypeSensing.SensingNormal with get, set
        member val TimeOut = Nullable() with get, set
        member val DelayCheck = Nullable() with get, set

        member private x.UpdateProperty(job: Job) =
            x.Name <- job.DequotedQualifiedName
            x.JobAction <- job.JobParam.JobAction
            x.JobSensing <- job.JobParam.JobSensing
            x.TimeOut <- toNullable job.JobTime.TimeOut    
            x.DelayCheck <- toNullable job.JobTime.DelayCheck
    
    // Custom class to be displayed in PropertyGrid with expandable collections
    and PropertyCall()  =
        inherit PropertyBase()
        let safetyConditions = ObservableBindingList<string>()
        let autoPreConditions = ObservableBindingList<string>()

        new(x: Call) as this = PropertyCall() then this.UpdateProperty(x)
        // Properties
        member val Disabled = false with get, set

        member x.SafetyConditions = safetyConditions
        member x.AutoPreConditions = autoPreConditions

        // Method to update properties
        member private x.UpdateProperty(call: Call) =
            x.Name <- call.Name
            x.Disabled <- call.Disabled
            call.SafetyConditions.Select(fun f ->  f.GetJob().DequotedQualifiedName ).Iter (x.SafetyConditions.Add)
            call.AutoPreConditions.Select(fun f -> f.GetJob().DequotedQualifiedName).Iter (x.AutoPreConditions.Add)
        

    and PropertyTaskDev() =
        inherit PropertyBase()
        new(x: TaskDev) as this = PropertyTaskDev() then this.UpdateProperty(x)
        member x.TaskDevName = x.Name

        member private x.UpdateProperty(taskDev: TaskDev) =
            x.Name <- taskDev.FullName

    and PropertyApiParam() =
        inherit PropertyBase()
        new(x: ApiParam) as this = PropertyApiParam() then this.UpdateProperty(x)
        [<TypeConverter(typeof<ExpandableObjectConverter>)>]
        member val InParam = getNull<PropertyTaskDevParam>() with get, set
        [<TypeConverter(typeof<ExpandableObjectConverter>)>]
        member val OutParam = getNull<PropertyTaskDevParam>() with get, set

        member private x.UpdateProperty(apiParam: ApiParam) =
            x.Name <- apiParam.ApiItem.QualifiedName
            x.InParam  <-  match apiParam.TaskDevParamIO.InParam  with |Some x -> PropertyTaskDevParam(x) | _ -> getNull<PropertyTaskDevParam>()
            x.OutParam <-  match apiParam.TaskDevParamIO.OutParam with |Some x -> PropertyTaskDevParam(x) | _ -> getNull<PropertyTaskDevParam>()

    and PropertyFlow() =
        inherit PropertyBase()
        new(x: Flow) as this = PropertyFlow() then this.UpdateProperty(x)

        member x.UpdateProperty(flow: Flow) =
            x.Name <- flow.Name

    and PropertyAlias() =
        inherit PropertyBase()
        new(x: Alias) as this = PropertyAlias() then this.UpdateProperty(x)

        member val TargetName = null with get, set

        member private x.UpdateProperty(alias: Alias) =
            x.Name <- alias.Name
            x.TargetName <- alias.TargetWrapper.GetTarget().DequotedQualifiedName

    and PropertyApiItem() =
        inherit PropertyBase()
        new(x: ApiItem) as this = PropertyApiItem() then this.UpdateProperty(x)

        member val TxName = "" with get, set
        member val RxName = "" with get, set

        member private x.UpdateProperty(apiItem: ApiItem) =
            x.Name <- apiItem.Name
            x.TxName <- apiItem.TX.DequotedQualifiedName
            x.RxName <- apiItem.RX.DequotedQualifiedName

    and PropertyOperatorFunction() =
        inherit PropertyBase()
        new(x: OperatorFunction) as this = PropertyOperatorFunction() then this.UpdateProperty(x)

        member val OperatorCode = "" with get, set

        member private x.UpdateProperty(opFunc: OperatorFunction) =
            x.Name <- opFunc.Name
            x.OperatorCode <- opFunc.OperatorCode

    and PropertyCommandFunction() =
        inherit PropertyBase()
        new(x: CommandFunction) as this = PropertyCommandFunction() then this.UpdateProperty(x)

        member val CommandCode = "" with get, set

        member private x.UpdateProperty(cmdFunc: CommandFunction) =
            x.Name <- cmdFunc.Name
            x.CommandCode <- cmdFunc.CommandCode

    and PropertyHwSystemDef() =
        inherit PropertyBase()
        new(x: HwSystemDef) as this = PropertyHwSystemDef() then this.UpdateProperty(x)

        member val SettingFlows = "" with get, set
        member val InAddress = null with get, set
        member val OutAddress = null with get, set
        member val HwDefType = "" with get, set
        member val InParam = getNull<PropertyTaskDevParam>() with get, set
        member val OutParam = getNull<PropertyTaskDevParam>() with get, set

        member private x.UpdateProperty(hwDef: HwSystemDef) =
            x.Name <- hwDef.Name
            x.SettingFlows <- String.Join("; ", hwDef.SettingFlows |> Seq.map (fun f -> f.Name))
            x.InAddress <- hwDef.InAddress
            x.OutAddress <- hwDef.OutAddress
            x.HwDefType <- hwDef.GetType().Name
            x.InParam  <-  match hwDef.TaskDevParamIO.InParam  with |Some x -> PropertyTaskDevParam(x) | _ -> getNull<PropertyTaskDevParam>()
            x.OutParam <-  match hwDef.TaskDevParamIO.OutParam with |Some x -> PropertyTaskDevParam(x) | _ -> getNull<PropertyTaskDevParam>()
       
    type PropertyTaskDevParam(symbolName: string, dataType: string, valueText: string) =
        inherit PropertyBase()
        new() = PropertyTaskDevParam("", "", "")
        new(x: TaskDevParam) as this = PropertyTaskDevParam() then this.UpdateProperty(x)
        
        member val SymbolName = symbolName with get, set
        member val DataType = dataType with get, set
        member val ValueText = valueText with get, set

        member x.UpdateProperty(tdp: TaskDevParam) =
            x.DataType <- tdp.DataType.ToText()
            x.SymbolName <- tdp.GetSymbolName()
            x.ValueText <- tdp.ValueParam.ToText()
            