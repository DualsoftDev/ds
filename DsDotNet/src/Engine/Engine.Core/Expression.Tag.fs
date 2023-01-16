namespace Engine.Core

open System
open System.Runtime.CompilerServices
open Engine.Common.FS

[<AutoOpen>]
module TagModule =

    type BitFlag =
    | R                 // Ready Status
    | G                 // Going  Status
    | F                 // Finish Status
    | H                 // Homing  Status
    | Origin            // Origin Monitor
    | Pause             // Pause Monitor
    | ErrorTx           // Error Tx Monitor
    | ErrorRx           // Error Rx Monitor
    | RealOriginAction  // Real Origin Action
    | RelayReal         // Real Init Relay
    | RelayCall         // Call Done Relay
    | RelayGoing        // Going Relay
    | Pulse             // Start Pulse
    | ET                // End Tag
    | RT                // Reset Tag
    | ST                // Start Tag
    | EP                // End Port
    | RP                // Reset Port
    | SP                // Start Port
    | EF                // End Force
    | RF                // Reset Force
    | SF                // Start Force

    type TimerFlag =
    | TimerOnDely   // Timer OnDely
    | TimeOut       // Timer TimeOut

    type CounterFlag =
    | CountRing     // Ring Counter


    [<AbstractClass>]
    type Tag<'T when 'T:equality> (name, initValue:'T) =
        inherit TagBase<'T>(name, initValue)
        override x.ToBoxedExpression() = var2expr x
        member x.Expr = var2expr x

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


    let typeDefaultValue (typ:System.Type) =
        match typ.Name with
        | "Boolean"-> box false
        | "Byte"   -> box 0uy
        | "Char"   -> box ' '
        | "Double" -> box 0.0
        | "Int16"  -> box 0s
        | "Int32"  -> box 0
        | "Int64"  -> box 0L
        | "SByte"  -> box 0y
        | "Single" -> box 0.0f
        | "String" -> box ""
        | "UInt16" -> box 0us
        | "UInt32" -> box 0u
        | "UInt64" -> box 0UL
        | _  -> failwithlog "ERROR"


    // error FS0030: 값 제한이 있습니다. 값 'fwdCreateVariableWithValue'은(는) 제네릭 형식    val mutable fwdCreateVariableWithValue: (string -> '_a -> IVariable)을(를) 가지는 것으로 유추되었습니다.    'fwdCreateVariableWithValue'에 대한 인수를 명시적으로 만들거나, 제네릭 요소로 만들지 않으려는 경우 형식 주석을 추가하세요.
    type BoxedObjectHolder = { Object:obj }

    let createVariableWithTypeAndValueOnWindows (typ:System.Type) (name:string) (boxedValue:BoxedObjectHolder): IVariable =
        verify (Runtime.Target = WINDOWS)
        let v = boxedValue.Object
        match typ.Name with
        | "Boolean"-> new Variable<bool>  (name, unbox v)
        | "Byte"   -> new Variable<uint8> (name, unbox v)
        | "Char"   -> new Variable<char>  (name, unbox v)
        | "Double" -> new Variable<double>(name, unbox v)
        | "Int16"  -> new Variable<int16> (name, unbox v)
        | "Int32"  -> new Variable<int32> (name, unbox v)
        | "Int64"  -> new Variable<int64> (name, unbox v)
        | "SByte"  -> new Variable<int8>  (name, unbox v)
        | "Single" -> new Variable<single>(name, unbox v)
        | "String" -> new Variable<string>(name, unbox v)
        | "UInt16" -> new Variable<uint16>(name, unbox v)
        | "UInt32" -> new Variable<uint32>(name, unbox v)
        | "UInt64" -> new Variable<uint64>(name, unbox v)
        | _  -> failwithlog "ERROR"

    let createVariableWithTypeOnWindows (typ:System.Type) (name:string) : IVariable =
        verify (Runtime.Target = WINDOWS)
        let value = { Object = typeDefaultValue typ }
        createVariableWithTypeAndValueOnWindows typ name value


    let mutable fwdCreateVariableWithType = createVariableWithTypeOnWindows
    let mutable fwdCreateVariableWithTypeAndValue = createVariableWithTypeAndValueOnWindows
