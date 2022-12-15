namespace Engine.Core

open System.Diagnostics
open System
open System.Text.RegularExpressions

[<AutoOpen>]
module  TagModule =


    /// PLC action tag (PlcTag) class
    type PlcTag<'T when 'T:equality> (name, initValue:'T)  =
        inherit Tag<'T>(name, initValue)
        member val Address = "" with get, set
        // <ahn> override x.NotifyValueChanged() = ChangeValueEvent x

      /// Ds 일반 plan tag : going relay에 사용중
    type DsTag<'T when 'T:equality> (name, initValue:'T)  =
        inherit Tag<'T>(name, initValue)
        // <ahn> override x.NotifyValueChanged() = ChangeValueEvent x

    /// DsBit tag (PlanTag) class
    type DsBit (name, initValue:bool, v:Vertex, m:Memory, tagFlag:TagFlag, controlIndex:int) =
        inherit Tag<bool>(name, initValue)
        //Segment 모니터 전용 bit 생성 (controlIndex(-1))
        new (n, iv, vertex, memory, tagFlag) = DsBit (n, iv, vertex, memory, tagFlag, -1)
        //Segment 컨트롤 전용 bit 생성 (controlIndex(0 or 1 or 2 or 3))
        new (n, iv, vertex, memory, index) =   DsBit (n, iv, vertex, memory, getControlFlag index, index)

        // <ahn>
        //override x.NotifyValueChanged() =
        //    ChangeValueEvent x
        //    //if tagFlag.IsControl
        //    //then ChangeStatusEvent (v, m.Status)

        member x.GetValue()  =
            if tagFlag.IsControl
                then m.GetControlValue(controlIndex)
                else m.GetMonitorValue(tagFlag)

        member x.SetValue(v:bool) =
            if tagFlag.IsControl
                then m.SetControlValue(controlIndex, v)
                else m.SetMonitorValue(tagFlag, v)
            x.Value  <- v  //memory와 동기화
