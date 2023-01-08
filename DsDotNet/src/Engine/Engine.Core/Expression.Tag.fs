namespace Engine.Core

open System
open Engine.Common.FS

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

      /// Ds 일반 plan tag : going relay에 사용중
    type DsTag<'T when 'T:equality> (name, initValue:'T) =
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



    let private createVariableWithTypeOnWindows (name:string) (typ:System.Type): IVariable =
        verify (RuntimeTarget = WINDOWS)
        match typ.Name with
        | "Single" -> new Variable<single>(name, 0.0f)
        | "Double" -> new Variable<double>(name, 0.0)
        | "SByte"  -> new Variable<int8>  (name, 0y)
        | "Byte"   -> new Variable<uint8> (name, 0uy)
        | "Int16"  -> new Variable<int16> (name, 0s)
        | "UInt16" -> new Variable<uint16>(name, 0us)
        | "Int32"  -> new Variable<int32> (name, 0)
        | "UInt32" -> new Variable<uint32>(name, 0u)
        | "Int64"  -> new Variable<int64> (name, 0L)
        | "UInt64" -> new Variable<uint64>(name, 0UL)
        | "Boolean"-> new Variable<bool>  (name, false)
        | "String" -> new Variable<string>(name, "")
        | "Char"   -> new Variable<char>  (name, ' ')
        | _  -> failwith "ERROR"

    // error FS0030: 값 제한이 있습니다. 값 'fwdCreateVariableWithValue'은(는) 제네릭 형식    val mutable fwdCreateVariableWithValue: (string -> '_a -> IVariable)을(를) 가지는 것으로 유추되었습니다.    'fwdCreateVariableWithValue'에 대한 인수를 명시적으로 만들거나, 제네릭 요소로 만들지 않으려는 경우 형식 주석을 추가하세요.
    type BoxedObjectHolder = { Object:obj }

    let private createVariableWithTypeAndValueOnWindows (name:string) (typ:System.Type) (boxedValue:BoxedObjectHolder): IVariable =
        verify (RuntimeTarget = WINDOWS)
        let v = boxedValue.Object
        match typ.Name with
        | "Single" -> new Variable<single>(name, v :?> single)
        | "Double" -> new Variable<double>(name, v :?> double)
        | "SByte"  -> new Variable<int8>  (name, v :?> int8)
        | "Byte"   -> new Variable<uint8> (name, v :?> uint8)
        | "Int16"  -> new Variable<int16> (name, v :?> int16)
        | "UInt16" -> new Variable<uint16>(name, v :?> uint16)
        | "Int32"  -> new Variable<int32> (name, v :?> int32)
        | "UInt32" -> new Variable<uint32>(name, v :?> uint32)
        | "Int64"  -> new Variable<int64> (name, v :?> int64)
        | "UInt64" -> new Variable<uint64>(name, v :?> uint64)
        | "Boolean"-> new Variable<bool>  (name, v :?> bool)
        | "String" -> new Variable<string>(name, v :?> string)
        | "Char"   -> new Variable<char>  (name, v :?> char)
        | _  -> failwith "ERROR"


    let mutable fwdCreateVariableWithType = createVariableWithTypeOnWindows
    let mutable fwdCreateVariableWithTypeAndValue = createVariableWithTypeAndValueOnWindows
