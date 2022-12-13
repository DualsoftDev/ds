namespace Engine.CodeGenCPU

open System.Diagnostics
open System
open System.Text.RegularExpressions
open Engine.Core

[<AutoOpen>]
module  TagModule =
    // 호환성을 위한 임시 interface. 추후 정리
    type IPLCTag =
        inherit ITag
        abstract SetValue:obj -> unit
        abstract GetValue:unit -> obj

    /// PLC tag (PlcTag) class
    type PlcTag<'T> private (name, initValue:'T)  =
        inherit Tag<'T>(name, initValue)
        member val Address = "" with get, set
        static member Create(name:string, value:'T) = PlcTag(name, value)

        interface IPLCTag with
            member x.GetValue()  = x.GetValue()
            member x.SetValue(v) = x.SetValue(v)

        abstract GetValue:unit -> obj
        abstract SetValue:obj -> unit

        override x.GetValue()  = x.Value
        override x.SetValue(v) = x.Value <- v :?> 'T


    /// Monitor tag (PlanTag) class
    type DsBit (name, initValue:bool, memory:Memory, monitor:Monitor) =
        inherit Tag<bool>(name, initValue)

        interface IPLCTag with
            member x.GetValue()  = x.GetValue()
            member x.SetValue(v) = x.SetValue(v)

        abstract GetValue:unit -> obj
        abstract SetValue:obj -> unit

        override x.GetValue()  = memory.GetMonitorValue(monitor)
        override x.SetValue(v) =
            match monitor  with
            |Monitor.R|Monitor.G|Monitor.F| Monitor.H
                -> failwith "error Status4 read only"
            |_  -> memory.ChangeMonitor(monitor, Convert.ToBoolean(v))
                   x.Value  <- v :?> bool    //memory와 동기화


    //name[Index] 규격 ex : R203[3]
    type DsDotBit (name, initValue:bool, memory:Memory, index:int) =
        inherit Tag<bool>(name, initValue)
        interface IPLCTag with
            member x.GetValue()  = x.GetValue()
            member x.SetValue(v) = x.SetValue(v)

        abstract GetValue:unit -> obj
        abstract SetValue:obj -> unit

        override x.GetValue()  = memory.GetControlValue(index)
        override x.SetValue(v) = memory.SetControlValue(index, Convert.ToBoolean(v))
                                 x.Value  <- v :?> bool  //memory와 동기화

