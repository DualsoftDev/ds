namespace Engine.Cpu

open System.Diagnostics
open System
open System.Text.RegularExpressions
open Engine.Core

[<AutoOpen>]
module  TagModule =

    /// PLC tag (PlcTag) class
    type PlcTag<'T> private (name, initValue:'T)  =
        inherit Tag<'T>(name, initValue)
        member val Address = "" with get, set
        static member Create(name:string, value:'T) = PlcTag(name, value)

        override x.GetValue()  = x.Value
        override x.SetValue(v) = x.Value <- v :?> 'T


    /// Monitor tag (PlanTag) class
    type DsBit<'T> (name, initValue:'T, memory:Memory, monitor:Monitor) =
        inherit Tag<'T>(name, initValue)

        override x.GetValue() = memory.GetMonitorValue(monitor)
        override x.SetValue(v) =
            match monitor  with
            |Monitor.R|Monitor.G|Monitor.F| Monitor.H
                -> failwith "error Status4 read only"
            |_  -> memory.ChangeMonitor(monitor, Convert.ToBoolean(v))
                   x.Value  <- v :?> 'T    //memory와 동기화


    //name[Index] 규격 ex : R203[3]
    type DsDotBit<'T> (name, initValue:'T, memory:Memory) =
        inherit Tag<'T>(name, initValue)
        let mutable index:int=  getIndex(name)//대괄호 안에 내용의 index 가져오기

        override x.GetValue()  = memory.GetControlValue(index)
        override x.SetValue(v) = memory.SetControlValue(index, Convert.ToBoolean(v))
                                 x.Value  <- v :?> 'T  //memory와 동기화

