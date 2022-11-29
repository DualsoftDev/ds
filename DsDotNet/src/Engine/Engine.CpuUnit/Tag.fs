namespace Engine.Cpu

open System.Diagnostics
open System
open System.Text.RegularExpressions
open Engine.Core

[<AutoOpen>]
module  TagModule = 

    [<AbstractClass>]
    [<DebuggerDisplay("{Name}")>]
    type Tag<'T>(name, initValue:'T) =
        let mutable value  = initValue 
        interface ITag with
            member _.ToText() = $"({name}={(value.ToString())})"
            member _.Name: string = name
            member _.Value  with get()      = value
                            and  set(v:obj) = value <- (v |> unbox)
        member x.ToText() = (x :> ITag).ToText()
        member x.Name     = (x :> ITag).Name
        member x.Value    = (x :> ITag).Value 

        //memory bit masking 처리를 위해 일반 PlcTag와 DsMemory 구별 구현
        abstract SetValue:obj -> unit
        abstract GetValue:unit -> obj

    /// PLC tag (PlcTag) class
    type PlcTag<'T> private (name, initValue:'T)  =
        inherit Tag<'T>(name, initValue)
        member val Address = "" with get, set
        static member Create(name:string, value:'T) = PlcTag(name, value)
         
        override x.GetValue()  = (x :> ITag).Value
        override x.SetValue(v) = (x :> ITag).Value <- v


    /// Monitor tag (PlanTag) class
    type DsBit<'T> (name, initValue:'T, memory:Memory, monitor:Monitor) =
        inherit Tag<'T>(name, initValue)
        let mutable memory:Memory = memory

        override x.GetValue() = memory.GetMonitorValue(monitor) 
        override x.SetValue(v) = 
            match monitor  with 
            |Monitor.R|Monitor.G|Monitor.F| Monitor.H  
                -> failwith "error Status4 read only"
            |_  -> memory.ChangeMonitor(monitor, Convert.ToBoolean(v)) 
                   (x:> ITag).Value  <- v    //memory와 동기화


    //name[Index] 규격 ex : R203[3]  
    type DsDotBit<'T> (name, initValue:'T, memory:Memory) =
        inherit Tag<'T>(name, initValue)
        let mutable index:int=  getIndex(name)//대괄호 안에 내용의 index 가져오기
        let mutable memory:Memory = memory
       
        override x.GetValue()  = memory.GetControlValue(index)
        override x.SetValue(v) = memory.SetControlValue(index, Convert.ToBoolean(v))
                                 (x:> ITag).Value  <- v  //memory와 동기화
                                 
