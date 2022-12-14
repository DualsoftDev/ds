namespace Engine.CodeGenCPU

open System.Diagnostics
open System
open System.Text.RegularExpressions
open Engine.Core

[<AutoOpen>]
module  TagModule =
    

    /// PLC tag (PlcTag) class
    type PlcTag<'T> (name, initValue:'T)  =
        inherit Tag<'T>(name, initValue)
        member val Address = "" with get, set

    /// TagFlag tag (PlanTag) class
    type DsBit (name, initValue:bool, m:Memory, tagFlag:TagFlag, controlIndex:int) =
        inherit Tag<bool>(name, initValue)
        //Segment 모니터 전용 bit 생성 (controlIndex(-1))
        new (n, iv, memory, tagFlag) = DsBit (n, iv, memory, tagFlag, -1)
        //Segment 컨트롤 전용 bit 생성 (controlIndex(0 or 1 or 2 or 3))
        new (n, iv, memory, index) =   DsBit (n, iv, memory, getControlFlag index, index)

        member x.GetValue()  =
            if tagFlag.IsControl
                then m.GetControlValue(controlIndex)
                else m.GetMonitorValue(tagFlag)

        member x.SetValue(v:bool) = 
            if x.Value <> v
            then 
                if tagFlag.IsControl
                    then m.SetControlValue(controlIndex, v)
                    else m.SetMonitorValue(tagFlag, v)
                x.Value  <- v  //memory와 동기화
