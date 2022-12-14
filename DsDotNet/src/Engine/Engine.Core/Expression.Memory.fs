namespace Engine.Core

open System.Collections.Concurrent
open System.Diagnostics
open System
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

    type TagFlag =
    | R        
    | G        
    | F        
    | H        
    | Origin   
    | Pause    
    | ErrorTx  
    | ErrorRx  
    | End      
    | Reset    
    | Start    
    | Relay    
        member x.IsControl = 
            match x with
            | End| Reset| Start| Relay -> true
            | _ -> false
        member x.IsTagFlag = x.IsControl |> not

    let getControlFlag(index:int) = 
            match index with
            |EndIndex   -> TagFlag.End
            |ResetIndex -> TagFlag.Reset
            |StartIndex -> TagFlag.Start
            |RelayIndex -> TagFlag.Relay
            |_ -> failwith "Error"

    [<DebuggerDisplay("{Status}")>]
    type Memory(m:byte) =
        let mutable value = m
        member internal x.getValue(flag:MemoryFlag) = 
                        (value &&& (byte)flag) = (byte)flag
        member internal x.setValue(flag:MemoryFlag, v:bool) = 
                        if v 
                        then value <- value ||| (byte)flag
                        else value <- value &&& ~~~((byte)flag)

        member x.Value      with get() = value and set(v:Byte) = value <- v
        member x.Change(flag:MemoryFlag, v:bool)  = x.setValue(flag, v) 
        
        //-------------------------
        //  Status   ST  RT  ET  RE
        //-------------------------
        //    R      x   -   x   -
        //           o   o   x   -
        //    G      o   x   x   -
        //    F      -   x   o   -
        //    H      -   o   o   -
        //-------------------------
        //- 'o' : ON, 'x' : Off, '-' 는 don't care
        //status4 DS RGFH 상태
        member x.Status = 
            let lowNibble = value &&& (LowNibble |> byte)
            //Start = 4 Reset = 2 End = 0
            match lowNibble with  
            |0uy|2uy|6uy|8uy|10uy|14uy -> Status4.Ready
            |4uy|12uy                  -> Status4.Going
            |1uy|5uy|9uy|13uy          -> Status4.Finish
            |3uy|7uy|11uy|15uy         -> Status4.Homing
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

        member x.GetMonitorValue(tagFlag:TagFlag)   = 
            match tagFlag  with 
            |TagFlag.R -> x.Status = Ready 
            |TagFlag.G -> x.Status = Going 
            |TagFlag.F -> x.Status = Finish 
            |TagFlag.H -> x.Status = Homing 
            |TagFlag.Origin   -> x.getValue(MemoryFlag.Origin) 
            |TagFlag.Pause    -> x.getValue(MemoryFlag.Pause) 
            |TagFlag.ErrorTx  -> x.getValue(MemoryFlag.ErrorTx) 
            |TagFlag.ErrorRx  -> x.getValue(MemoryFlag.ErrorRx) 
            |_  -> failwith $"Error {tagFlag} is not TagFlag type"

        //Origin, Stop, ErrorTx, ErrorRx  변경
        member x.SetMonitorValue(tagFlag:TagFlag, v:bool)   = 
            match tagFlag  with 
            |TagFlag.Origin   -> x.setValue(MemoryFlag.Origin,v) 
            |TagFlag.Pause    -> x.setValue(MemoryFlag.Pause,v) 
            |TagFlag.ErrorTx  -> x.setValue(MemoryFlag.ErrorTx,v) 
            |TagFlag.ErrorRx  -> x.setValue(MemoryFlag.ErrorRx,v) 
            |TagFlag.R|TagFlag.G|TagFlag.F| TagFlag.H 
                -> failwith $"Error {tagFlag} can't set value" 
            |_  -> failwith $"Error {tagFlag} is not TagFlag type"
