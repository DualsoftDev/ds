namespace Engine.Runtime

open System
open System.IO
open System.Linq
open System.Runtime.CompilerServices
open Newtonsoft.Json
open Microsoft.FSharp.Core
open Dual.Common.Core.FS
open Engine.Core
open System.ComponentModel

[<AutoOpen>]
module DsPropertySubModule =

    type TaskDevParamSub(symbolName: string, dataType: string, valueText: string) =
        new() = TaskDevParamSub("", "", "")
        new(x: TaskDevParam) as this = TaskDevParamSub() then this.UpdateProperty(x)
        
        override x.ToString() =  "TaskDevParamSub"
         
        member val SymbolName = symbolName with get, set
        member val DataType = dataType with get, set
        member val ValueText = valueText with get, set

        member x.UpdateProperty(tdp: TaskDevParam) =
            x.DataType <- tdp.DataType.ToText()
            x.SymbolName <- tdp.GetSymbolName()
            x.ValueText <- tdp.ValueParam.ToText()
