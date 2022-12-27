namespace Engine.Core

open System

[<AutoOpen>]
module  TagModule =

    type TagFlag =
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
    | Counter       // Ring Counter 
    | TimerTx       // Timer Command Delay
    | TimerRx       // Timer Observe Delay
    | ET            // End Tag
    | RT            // Reset Tag
    | ST            // Start Tag
    | EP            // End Port
    | RP            // Reset Port
    | SP            // Start Port
    | EF            // End Force
    | RF            // Reset Force
    | SF            // Start Force

    [<AbstractClass>]
    type Tag<'T when 'T:equality> (name, initValue:'T)  =
        inherit TagBase<'T>(name, initValue)
        override x.ToBoxedExpression() = tag2expr x
        member x.Expr = tag2expr x

    type Variable<'T when 'T:equality> (name, initValue:'T)  =
        inherit VariableBase<'T>(name, initValue)
        override x.ToBoxedExpression() = var2expr x

    /// PLC action tag (PlcTag) class
    type PlcTag<'T when 'T:equality> (name, address:string, initValue:'T)  =
        inherit Tag<'T>(name, initValue)
        interface ITagWithAddress with
            member x.Address = x.Address        

        member val Address = address with get, set

      /// Ds 일반 plan tag : going relay에 사용중
    type DsTag<'T when 'T:equality> (name, initValue:'T)  =
        inherit Tag<'T>(name, initValue)

    /// DsBit tag (PlanTag) class
    type DsBit (name, initValue:bool, v:Vertex, tagFlag:TagFlag) =
        inherit Tag<bool>(name, initValue)
        member x.NotifyStatus() =
             if x.Value then
                 match tagFlag with
                 | R -> ChangeStatusEvent (v, Ready)
                 | G -> ChangeStatusEvent (v, Going)
                 | F -> ChangeStatusEvent (v, Finish)
                 | H -> ChangeStatusEvent (v, Homing)
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
