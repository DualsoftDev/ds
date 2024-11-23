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
            x.FqdnObject <- Some sys

    type PropertyReal() =
        inherit PropertyBase()

        let mutable finished = false
        let mutable noTransData = false
        let mutable motion = getNull<string>()
        let mutable script = getNull<string>()
        let mutable repeatCount = Nullable()
        let mutable avg = Nullable()
        let mutable std = Nullable()

        new(x: Real) as this = PropertyReal() then this.UpdateProperty(x)

        member x.Finished
            with get() = finished
            and set(v) = x.UpdateField(&finished, v)

        member x.NoTransData
            with get() = noTransData
            and set(v) = x.UpdateField(&noTransData, v)

        member x.Motion
            with get() = motion
            and set(v) = x.UpdateField(&motion, v)

        member x.Script
            with get() = script
            and set(v) = x.UpdateField(&script, v)

        member x.RepeatCount
            with get() = repeatCount
            and set(v) = x.UpdateField(&repeatCount, v)

        member x.AVG
            with get() = avg
            and set(v) = x.UpdateField(&avg, v)

        member x.STD
            with get() = std
            and set(v) = x.UpdateField(&std, v)

        member private x.UpdateProperty(real: Real) =
            x.Name <- real.Name
            x.FqdnObject <- Some real
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
        member private x.UpdateProperty(job: Job) =
            x.Name <- job.DequotedQualifiedName
            x.FqdnObject <- Some job

    and PropertyTaskDev() =
        inherit PropertyBase()
        new(x: TaskDev) as this = PropertyTaskDev() then this.UpdateProperty(x)
        member val In =  getNull<PropertyTaskDevParam>() with get, set
        member val Out =  getNull<PropertyTaskDevParam>() with get, set
        
        member private x.UpdateProperty(taskDev: TaskDev) =
            x.Name <- taskDev.FullName
            x.FqdnObject <- Some taskDev
            x.In <- PropertyTaskDevParam(taskDev.TaskDevParamIO.InParam)
            x.Out <- PropertyTaskDevParam(taskDev.TaskDevParamIO.OutParam)

    and PropertyCall() as this =
        inherit PropertyBase()
        let safetyConditions = ExpandableBindingList<string>()
        let autoPreConditions = ExpandableBindingList<string>()
        
        // Properties
        let mutable disabled = false
        do
            safetyConditions.ListChanged.Add(fun _ -> this.OnPropertyChanged((nameof this.SafetyConditions)))
            autoPreConditions.ListChanged.Add(fun _ -> this.OnPropertyChanged((nameof this.AutoPreConditions)))

        new(x: Call) as this = PropertyCall() then this.UpdateProperty(x)

        member x.Disabled
            with get() = disabled
            and set(v) = x.UpdateField(&disabled, v)

        member val ValueParamIO =  getNull<PropertyValueParamIO>() with get, set

        member x.SafetyConditions = safetyConditions
        member x.AutoPreConditions = autoPreConditions
        // Method to update properties
        member private x.UpdateProperty(call: Call) =
            x.Name <- call.Name
            x.FqdnObject <- Some call
            x.Disabled <- call.Disabled
            x.ValueParamIO <- PropertyValueParamIO(call.ValueParamIO)
            call.SafetyConditions.Select(fun f ->  f.GetCall().DequotedQualifiedName ).Iter (x.SafetyConditions.Add)
            call.AutoPreConditions.Select(fun f -> f.GetCall().DequotedQualifiedName).Iter (x.AutoPreConditions.Add)
        
    and PropertyValueParamIO() =
        inherit PropertyBase()

        new(x: ValueParamIO) as this = PropertyValueParamIO() then this.UpdateProperty(x)
        member val In =  getNull<PropertyValueParam>() with get, set
        member val Out =  getNull<PropertyValueParam>() with get, set
        member private x.UpdateProperty(vp: ValueParamIO) =
            x.In <- PropertyValueParam(vp.In)
            x.Out <- PropertyValueParam(vp.Out)

    and PropertyValueParam() =
        inherit PropertyBase()
        let mutable targetValue = getNull<string>()
        let mutable max = getNull<string>()
        let mutable min = getNull<string>()
        let mutable isInclusiveMax = Nullable<bool>()

        new(x: ValueParam) as this = PropertyValueParam() then this.UpdateProperty(x)

        member x.TargetValue
            with get() = targetValue
            and set(v) = x.UpdateField(&targetValue, v)

        member x.Max
            with get() = max
            and set(v) = x.UpdateField(&max, v)

        member x.Min
            with get() = min
            and set(v) = x.UpdateField(&min, v)

        member x.IsInclusiveMax
            with get() = isInclusiveMax
            and set(v) = x.UpdateField(&isInclusiveMax, v)

        member private x.UpdateProperty(vp: ValueParam) =
            x.TargetValue <- vp.TargetValueText
            x.Max <- vp.MaxText
            x.Min <- vp.MinText
            x.IsInclusiveMax <- vp.IsInclusiveMax

    and PropertyFlow() =
        inherit PropertyBase()
        new(x: Flow) as this = PropertyFlow() then this.UpdateProperty(x)

        member private x.UpdateProperty(flow: Flow) =
            x.Name <- flow.Name
            x.FqdnObject <- Some flow

    and PropertyAlias() =
        inherit PropertyBase()
        let mutable targetName = getNull<string>()
        new(x: Alias) as this = PropertyAlias() then this.UpdateProperty(x)

        member x.TargetName
            with get() = targetName
            and set(v) = x.UpdateField(&targetName, v)

        member private x.UpdateProperty(alias: Alias) =
            x.Name <- alias.Name
            x.FqdnObject <- Some alias
            x.TargetName <- alias.TargetWrapper.GetTarget().DequotedQualifiedName

    and PropertyApiItem() =
        inherit PropertyBase()
        let mutable txName = ""
        let mutable rxName = ""
        new(x: ApiItem) as this = PropertyApiItem() then this.UpdateProperty(x)

        member x.TxName
            with get() = txName
            and set(v) = x.UpdateField(&txName, v)

        member x.RxName
            with get() = rxName
            and set(v) = x.UpdateField(&rxName, v)

        member private x.UpdateProperty(apiItem: ApiItem) =
            x.Name <- apiItem.Name
            x.TxName <- apiItem.TX.DequotedQualifiedName
            x.RxName <- apiItem.RX.DequotedQualifiedName

    and PropertyOperatorFunction() =
        inherit PropertyBase()
        let mutable operatorCode = ""
        new(x: OperatorFunction) as this = PropertyOperatorFunction() then this.UpdateProperty(x)

        member x.OperatorCode
            with get() = operatorCode
            and set(v) = x.UpdateField(&operatorCode, v)

        member private x.UpdateProperty(opFunc: OperatorFunction) =
            x.Name <- opFunc.Name
            x.OperatorCode <- opFunc.OperatorCode

    and PropertyCommandFunction() =
        inherit PropertyBase()
        let mutable commandCode = ""
        new(x: CommandFunction) as this = PropertyCommandFunction() then this.UpdateProperty(x)

        member x.CommandCode
            with get() = commandCode
            and set(v) = x.UpdateField(&commandCode, v)

        member private x.UpdateProperty(cmdFunc: CommandFunction) =
            x.Name <- cmdFunc.Name
            x.CommandCode <- cmdFunc.CommandCode

    and PropertyHwSystemDef() =
        inherit PropertyBase()
        let mutable settingFlows = ""
        let mutable inAddress = getNull<string>()
        let mutable outAddress = getNull<string>()
        let mutable hwDefType = ""
        let mutable inParam = getNull<PropertyTaskDevParam>()
        let mutable outParam = getNull<PropertyTaskDevParam>()

        new(x: HwSystemDef) as this = PropertyHwSystemDef() then this.UpdateProperty(x)

        member x.SettingFlows
            with get() = settingFlows
            and set(v) = x.UpdateField(&settingFlows, v)

        member x.InAddress
            with get() = inAddress
            and set(v) = x.UpdateField(&inAddress, v)

        member x.OutAddress
            with get() = outAddress
            and set(v) = x.UpdateField(&outAddress, v)

        member x.HwDefType
            with get() = hwDefType
            and set(v) = x.UpdateField(&hwDefType, v)

        member x.InParam
            with get() = inParam
            and set(v) = x.UpdateField(&inParam, v)

        member x.OutParam
            with get() = outParam
            and set(v) = x.UpdateField(&outParam, v)

        member private x.UpdateProperty(hwDef: HwSystemDef) =
            x.Name <- hwDef.Name
            x.FqdnObject <- Some hwDef
            x.SettingFlows <- String.Join("; ", hwDef.SettingFlows |> Seq.map (fun f -> f.Name))
            x.InAddress <- hwDef.InAddress
            x.OutAddress <- hwDef.OutAddress
            x.HwDefType <- hwDef.GetType().Name
            x.InParam <- PropertyTaskDevParam(hwDef.TaskDevParamIO.InParam)
            x.OutParam <- PropertyTaskDevParam(hwDef.TaskDevParamIO.OutParam)
       
    type PropertyTaskDevParam(address: string, dataType: string, symbol: string) =
        inherit PropertyBase()
        let mutable address = address
        let mutable dataType = dataType
        let mutable symbol = symbol

        new() = PropertyTaskDevParam("", "", "")
        new(x: TaskDevParam) as this = PropertyTaskDevParam() then this.UpdateProperty(x)
        
        member x.Address
            with get() = address
            and set(v) = x.UpdateField(&address, v)

        member x.DataType
            with get() = dataType
            and set(v) = x.UpdateField(&dataType, v)

        member x.Symbol
            with get() = symbol
            and set(v) = x.UpdateField(&symbol, v)

        member private x.UpdateProperty(tdp: TaskDevParam) =
            x.Address <- tdp.Address
            x.DataType <- tdp.DataType.ToText()
            x.Symbol <- tdp.Symbol


    type PropertyModelConfig()  =
        inherit PropertyBase()

        let mutable dsFilePath = ""
        let mutable hwIP = ""
        let mutable runtimePackage = RuntimePackage.PC
        let mutable hwDriver = ""
        let mutable runtimeMotionMode = RuntimeMotionMode.MotionAsync
        let mutable timeSimutionMode = TimeSimutionMode.TimeX1
        let mutable timeoutCall = 0u

        new(x: ModelConfig) as this =
            PropertyModelConfig()
            then this.UpdateProperty(x)

        member private x.UpdateProperty(config: ModelConfig) =
            x.Name <- "Model Config"
            x.UpdateField(&dsFilePath, config.DsFilePath, nameof dsFilePath)
            x.UpdateField(&hwIP, config.HwIP, nameof hwIP)
            x.UpdateField(&runtimePackage, config.RuntimePackage, nameof runtimePackage)
            x.UpdateField(&hwDriver, config.HwDriver, nameof hwDriver)
            x.UpdateField(&runtimeMotionMode, config.RuntimeMotionMode, nameof runtimeMotionMode)
            x.UpdateField(&timeSimutionMode, config.TimeSimutionMode, nameof timeSimutionMode)
            x.UpdateField(&timeoutCall, config.TimeoutCall, nameof timeoutCall)

        member x.DsFilePath
            with get() = dsFilePath
            and set(v) = x.UpdateField(&dsFilePath, v, nameof x.DsFilePath)

        member x.HwIP
            with get() = hwIP
            and set(v) = x.UpdateField(&hwIP, v, nameof x.HwIP)

        member x.RuntimePackage
            with get() = runtimePackage
            and set(v) = x.UpdateField(&runtimePackage, v, nameof x.RuntimePackage)

        member x.HwDriver
            with get() = hwDriver
            and set(v) = x.UpdateField(&hwDriver, v, nameof x.HwDriver)

        member x.RuntimeMotionMode
            with get() = runtimeMotionMode
            and set(v) = x.UpdateField(&runtimeMotionMode, v, nameof x.RuntimeMotionMode)

        member x.TimeSimutionMode
            with get() = timeSimutionMode
            and set(v) = x.UpdateField(&timeSimutionMode, v, nameof x.TimeSimutionMode)

        member x.TimeoutCall
            with get() = timeoutCall
            and set(v) = x.UpdateField(&timeoutCall, v, nameof x.TimeoutCall)
