namespace Engine.Cpu

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
    | LowNibble = 15    //xxxx0000
    | HiNibble  = 255   //0000xxxx

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
    type Memory(m: Byte) =
        let mutable value: Byte = m
        interface IData
        new() = Memory(0uy)
        member private x.getValue(flag:MemoryFlag) = 
                        (value &&& (byte)flag) = (byte)flag
        member private x.setValue(flag:MemoryFlag, v:bool) = 
                        if v 
                        then value <- value ||| (byte)flag
                        else value <- value &&& ~~~((byte)flag)

        member x.Value      with get() = value and set(v:Byte) = value <- v
        member x.Relay      = x.getValue(MemoryFlag.Relay)
        member x.RelayOn()  = x.setValue(MemoryFlag.Relay, true) 
        member x.RelayOff() = x.setValue(MemoryFlag.Relay, false)

        member x.Start      = x.getValue(MemoryFlag.S)
        member x.StartOn()  = x.setValue(MemoryFlag.S, true) 
        member x.StartOff() = x.setValue(MemoryFlag.S, false)

        member x.Reset      = x.getValue(MemoryFlag.R)
        member x.ResetOn()  = x.setValue(MemoryFlag.R, true) 
        member x.ResetOff() = x.setValue(MemoryFlag.R, false)

        member x.End      = x.getValue(MemoryFlag.E)
        member x.EndOn()  = x.setValue(MemoryFlag.E, true) 
        member x.EndOff() = x.setValue(MemoryFlag.E, false)

        //status4 DS RGFH 상태
        member x.Status = 
            let lowNibble = value &&& (MemoryFlag.LowNibble |> byte)
            //Start = 1 Reset = 2 End = 4
            match lowNibble with  
            |0uy|2uy|6uy -> Monitor.R
            |4uy         -> Monitor.G
            |1uy|5uy     -> Monitor.F
            |3uy|7uy     -> Monitor.H
            |_ ->  failwith "error"
                               
        //Origin, Stop, ErrorTx, ErrorRx  상태
        member x.Origin  = x.getValue(MemoryFlag.Origin)
        member x.Pause   = x.getValue(MemoryFlag.Pause)
        member x.ErrorTx = x.getValue(MemoryFlag.ErrorTx)
        member x.ErrorRx = x.getValue(MemoryFlag.ErrorRx)
      
