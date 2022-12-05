namespace Engine.Obsolete.CpuUnit

open System.Collections.Concurrent
open System.Diagnostics
open System
open Engine.Core
open System.Collections

[<AutoOpen>]
module MemoryModule =
    
    //bitFlag
    [<Flags>] 
    type MemoryFlag =
    | E         = 1   //End
    | R         = 2   //Reset
    | S         = 4   //Start
    | Relay     = 8   //Init Start Relay    (Real) ; Child Done (Call)
    | Origin    = 16  //Children StartPoint 
    | Pause     = 32   
    | ErrorTx   = 64   //error bit1
    | ErrorRx   = 128  //error bit2
    
    let LowNibble = 15    //xxxx0000
    let HiNibble  = 240   //0000xxxx

    let  [<Literal>] EndIndex        = 0
    let  [<Literal>] ResetIndex      = 1
    let  [<Literal>] StartIndex      = 2
    let  [<Literal>] RelayIndex      = 3

    type Monitor =
    | R
    | G
    | F
    | H
    | Origin 
    | Pause 
    | ErrorTx 
    | ErrorRx 

    [<DebuggerDisplay("{Status}")>]
    type Memory(m:byte) =
        let mutable value = m
        interface IData
        member internal x.getValue(flag:MemoryFlag) = 
                        (value &&& (byte)flag) = (byte)flag
        member internal x.setValue(flag:MemoryFlag, v:bool) = 
                        if v 
                        then value <- value ||| (byte)flag
                        else value <- value &&& ~~~((byte)flag)

        member x.Value      with get() = value and set(v:Byte) = value <- v
        member x.Change(flag:MemoryFlag, v:bool)  = x.setValue(flag, v) 

        //status4 DS RGFH 상태
        member x.Status = 
            let lowNibble = value &&& (LowNibble |> byte)
            //Start = 1 Reset = 2 End = 4
            match lowNibble with  
            |0uy|2uy|6uy|8uy|10uy|14uy -> Monitor.R
            |4uy|12uy                  -> Monitor.G
            |1uy|5uy|9uy|13uy          -> Monitor.F
            |3uy|7uy|11uy|15uy         -> Monitor.H
            |_ ->  failwith "error"
          
        member x.GetControlValue(index:int)   = 
            match index with 
            | EndIndex   -> x.getValue(MemoryFlag.E) 
            | ResetIndex -> x.getValue(MemoryFlag.R)  
            | StartIndex -> x.getValue(MemoryFlag.S)  
            | RelayIndex -> x.getValue(MemoryFlag.Relay)  
            |_ -> failwith "error"
       
        member x.SetControlValue(index:int, v:bool)   = 
            match index with 
            | EndIndex ->   x.Change(MemoryFlag.E, v)
            | ResetIndex -> x.Change(MemoryFlag.R, v)
            | StartIndex -> x.Change(MemoryFlag.S, v)
            | RelayIndex -> x.Change(MemoryFlag.Relay, v)
            |_ -> failwith "error"

        member x.GetMonitorValue(monitor:Monitor)   = 
            match monitor  with 
            |Monitor.R|Monitor.G|Monitor.F| Monitor.H  
                              -> x.Status = monitor 
            |Monitor.Origin   -> x.getValue(MemoryFlag.Origin) 
            |Monitor.Pause    -> x.getValue(MemoryFlag.Pause) 
            |Monitor.ErrorTx  -> x.getValue(MemoryFlag.ErrorTx) 
            |Monitor.ErrorRx  -> x.getValue(MemoryFlag.ErrorRx) 

        //Origin, Stop, ErrorTx, ErrorRx  변경
        member x.ChangeMonitor(monitor:Monitor, v:bool)   = 
            match monitor  with 
            |Monitor.R|Monitor.G|Monitor.F| Monitor.H  
                              -> failwith "error Status4 read only"
            |Monitor.Origin   -> x.setValue(MemoryFlag.Origin,v) 
            |Monitor.Pause    -> x.setValue(MemoryFlag.Pause,v) 
            |Monitor.ErrorTx  -> x.setValue(MemoryFlag.ErrorTx,v) 
            |Monitor.ErrorRx  -> x.setValue(MemoryFlag.ErrorRx,v) 
