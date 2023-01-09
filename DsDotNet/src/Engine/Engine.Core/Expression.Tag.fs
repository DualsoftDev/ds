namespace Engine.Core

open System
open System.Runtime.CompilerServices

[<AutoOpen>]
module  TagModule =

    type BitFlag =
    | R             // Ready Status
    | G             // Going  Status
    | F             // Finish Status
    | H             // Homing  Status
    | Origin        // Origin Monitor
    | Pause         // Pause Monitor
    | ErrorTx       // Error Tx Monitor
    | ErrorRx       // Error Rx Monitor
    | RelayReal     // Real Init Relay
    | RelayCall     // Call Done Relay
    | RelayGoing    // Going Relay
    | Pulse         // Start Pulse
    | ET            // End Tag
    | RT            // Reset Tag
    | ST            // Start Tag
    | EP            // End Port
    | RP            // Reset Port
    | SP            // Start Port
    | EF            // End Force
    | RF            // Reset Force
    | SF            // Start Force

    type TimerFlag =
    | TimerOnDely   // Timer OnDely
    | TimeOut       // Timer TimeOut
    
    type CounterFlag =
    | CountRing     // Ring Counter


    [<AbstractClass>]
    type Tag<'T when 'T:equality> (name, initValue:'T) =
        inherit TagBase<'T>(name, initValue)
        override x.ToBoxedExpression() = tag2expr x
        member x.Expr = tag2expr x

    type Variable<'T when 'T:equality> (name, initValue:'T) =
        inherit VariableBase<'T>(name, initValue)
        override x.ToBoxedExpression() = var2expr x

    /// PLC action tag (PlcTag) class
    type PlcTag<'T when 'T:equality> (name, address:string, initValue:'T) =
        inherit Tag<'T>(name, initValue)
        interface ITagWithAddress with
            member x.Address = x.Address

        member val Address = address with get, set

      /// Ds Plan tag : system bit, eop, mop 등등.. 사용중
    type DsTag<'T when 'T:equality> (name, initValue:'T) =
        inherit Tag<'T>(name, initValue)

    /// Ds Plan tag with Vertex class
    type DsBit (name, initValue:bool, v:Vertex, tagFlag:BitFlag) =
        inherit Tag<bool>(name, initValue)
        member x.TagFlag = tagFlag
        member x.Vertex = v

    /// DsTimer tag with Vertex class
    type DsTimer (name, initValue:bool, v:Vertex, tagFlag:TimerFlag, ts:TimerStruct) =
        inherit Tag<bool>(name, initValue)
        member x.TagFlag = tagFlag
        member x.Vertex = v
        member x.TimerStruct = ts
        member x.DN = ts.DN :?> Tag<bool>

    /// DsCounter tag with Vertex class
    type DsCounter (name, initValue:bool, v:Vertex, tagFlag:CounterFlag, cs:CTRStruct) =
        inherit Tag<bool>(name, initValue)
        member x.TagFlag = tagFlag
        member x.Vertex = v
        member x.CTRStruct = cs
        member x.DN = cs.DN

    
    [<Extension>]
    type ExpressionExt =
        [<Extension>] 
        static member NotifyStatus (x:DsBit) =
            if x.Value then
                match x.TagFlag with
                | R -> ChangeStatusEvent (x.Vertex, Ready)
                | G -> ChangeStatusEvent (x.Vertex, Going)
                | F -> ChangeStatusEvent (x.Vertex, Finish)
                | H -> ChangeStatusEvent (x.Vertex, Homing)
                | _->()

    //bitFlag
    //[<Flags>]
    //type MemoryFlag =
    //| E         = 1   //End
    //| R         = 2   //Reset
    //| S         = 4   //Start
    //| Relay     = 8   //Init Start Relay    (Real) ; Child Done (Call)
    //| Origin    = 16  //Children StartPoint
    //| Pause     = 32
    //| ErrorTx   = 64   //error bit1
    //| ErrorRx   = 128  //error bit2

    //let LowNibble = 15    //xxxx0000
    //let HiNibble  = 240   //0000xxxx
