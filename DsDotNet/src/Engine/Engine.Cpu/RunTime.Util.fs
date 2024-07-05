namespace Engine.Cpu

open Engine.Core
open Dual.Common.Core.FS
open System
open System.Linq
open System.Reactive.Linq
open System.Collections.Generic
open Engine.CodeGenCPU

[<AutoOpen>]
module internal RunTimeUtil =


    
    let notifyPreExcute ( x:IStorage) =
        x.GetTagInfo() |> Option.iter(fun t -> t.OnChanged())
        
    //let  notifyPostExcute ( x:IStorage) =
    //    x.GetTagInfo() |> Option.iter(fun t -> 
    //            match t with
    //            | EventVertex (tag,_,kind) ->
    //                    match kind with
    //                    | VertexTag.forceOn
    //                    | VertexTag.forceOff 
    //                    | VertexTag.forceStart 
    //                    | VertexTag.forceReset -> tag.BoxedValue <- false 
    //                    | _-> ()
    //            | _ -> ())
        
        

    ///시뮬레이션 비트 ON
    let cpuSimOn(sys:DsSystem) =
        let simTag = (sys.TagManager :?> SystemManager).GetSystemTag(SystemTag.sim) 
        simTag.BoxedValue <- true
   
    ///HMI Reset
    let syncReset(system:DsSystem ) =
        let stgs = system.TagManager.Storages
        let stgs = stgs.Where(fun w-> w.Value.TagKind <> (int)SystemTag._ON)

        for tag in stgs do
            let stg = tag.Value
            match stg with
            | :? TimerCounterBaseStruct as tc ->
                tc.ResetStruct()  // 타이머 카운터 리셋
            | _ ->
                stg.BoxedValue <- textToDataType(stg.DataType.Name).DefaultValue()

