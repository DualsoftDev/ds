namespace Engine.Core

open System.Linq
open System.Diagnostics
open System.Collections.Generic

open Engine.Common.FS

[<AutoOpen>]
module ExpressionForwardDeclModule =
    type IValue<'T> =
        inherit IValue
        abstract Value: 'T with get, set

    type IStorage<'T> =
        inherit IStorage
        inherit IValue<'T>
    type Storages = Dictionary<string, IStorage>

    type ITag = inherit IStorage
    type ITag<'T> =
        inherit ITag
        inherit IStorage<'T>

    type IBridgeTag =
        inherit ITag
        abstract Address:string

    type IVariable = inherit IStorage
    type IVariable<'T> =
        inherit IVariable
        inherit IStorage<'T>

    (* PLC generation module 용 *)
    type IFlatExpression = interface end


    /// 이름을 갖는 terminal expression 이 될 수 있는 객체.  Tag, Variable (Literal 은 제외)
    type INamedExpressionizableTerminal =
        inherit IExpressionizableTerminal
        abstract StorageName:string


    // <kwak> IExpression<'T> vs IExpression : 강제 변환
    /// Expression<'T> 을 boxed 에서 접근하기 위한 최소의 interface
    type IExpression =
        abstract DataType : System.Type
        //abstract ExpressionType : ExpressionType
        abstract BoxedEvaluatedValue : obj
        /// Tag<'T> 나 Variable<'T> 객체 boxed 로 반환
        abstract GetBoxedRawObject: unit -> obj
        /// withParenthesys: terminal 일 경우는 무시되고, Function 일 경우에만 적용됨
        abstract ToText : withParenthesys:bool -> string
        /// Function expression 인 경우 function name 반환.  terminal 이면 none
        abstract FunctionName: string option
        /// Function expression 인 경우 function args 반환.  terminal 이거나 argument 없으면 empty list 반환
        abstract FunctionArguments: IExpression list
        /// Function arguments 목록만 치환된 새로운 expression 반환
        abstract WithNewFunctionArguments: IExpression list -> IExpression

        abstract Terminal: ITerminal option

        /// Function expression 에 사용된 IStorage 항목들을 반환
        abstract CollectStorages: unit -> IStorage list
        abstract Flatten: unit -> IFlatExpression

    type IExpression<'T when 'T:equality> =
        inherit IExpression
        abstract EvaluatedValue : 'T

[<AutoOpen>]
module rec ExpressionPrologModule =

    module ExpressionPrologSubModule =
        let expectN (n:int) (xs:'a seq) = if xs.Count() <> n then failwith $"Wrong number of arguments: expect {n}"
        let expect1 xs = expectN 1 xs; xs.First()
        let expect2 xs = expectN 2 xs; Array.ofSeq xs
        let expectGteN (n:int) (xs:'a seq) =
            if xs.Count() < n then failwith $"Wrong number of arguments: expect at least {n} arguments"

        let (|Float64|_|) (x:obj) =
            match x with
            | :? bool as b -> Some (if b then 1.0 else 0.0)     // toInt(false) 등에서의 casting 허용 위해 필요
            | :? byte   as n -> Some (double n)
            | :? double as n -> Some (double n)
            | :? int16  as n -> Some (double n)
            | :? int32  as n -> Some (double n)
            | :? int64  as n -> Some (double n)
            | :? sbyte  as n -> Some (double n)
            | :? single as n -> Some (double n)
            | :? uint16 as n -> Some (double n)
            | :? uint32 as n -> Some (double n)
            | :? uint64 as n -> Some (double n)
            | _ ->
                logWarn $"Cannot convert {x} to double"
                None

        let (|Float32|_|) (x:obj) =
            match x with
            | :? bool as b -> Some (if b then 1.f else 0.f)
            | :? byte   as n -> Some (float32 n)
            | :? double as n -> Some (float32 n)
            | :? int16  as n -> Some (float32 n)
            | :? int32  as n -> Some (float32 n)
            | :? int64  as n -> Some (float32 n)
            | :? sbyte  as n -> Some (float32 n)
            | :? single as n -> Some (float32 n)
            | :? uint16 as n -> Some (float32 n)
            | :? uint32 as n -> Some (float32 n)
            | :? uint64 as n -> Some (float32 n)
            | _ ->
                logWarn $"Cannot convert {x} to float"
                None

        let (|Byte|_|) (x:obj) =
            match x with
            | :? bool as b -> Some (if b then 1uy else 0uy)
            | :? byte   as n -> Some (byte n)
            | :? double as n -> Some (byte n)
            | :? int16  as n -> Some (byte n)
            | :? int32  as n -> Some (byte n)
            | :? int64  as n -> Some (byte n)
            | :? sbyte  as n -> Some (byte n)
            | :? single as n -> Some (byte n)
            | :? uint16 as n -> Some (byte n)
            | :? uint32 as n -> Some (byte n)
            | :? uint64 as n -> Some (byte n)
            | _ ->
                logWarn $"Cannot convert {x} to byte"
                None

        let (|SByte|_|) (x:obj) =
            match x with
            | :? bool as b -> Some (if b then 1y else 0y)
            | :? byte   as n -> Some (sbyte n)
            | :? double as n -> Some (sbyte n)
            | :? int16  as n -> Some (sbyte n)
            | :? int32  as n -> Some (sbyte n)
            | :? int64  as n -> Some (sbyte n)
            | :? sbyte  as n -> Some (sbyte n)
            | :? single as n -> Some (sbyte n)
            | :? uint16 as n -> Some (sbyte n)
            | :? uint32 as n -> Some (sbyte n)
            | :? uint64 as n -> Some (sbyte n)
            | _ ->
                logWarn $"Cannot convert {x} to sbyte"
                None

        let (|Int16|_|) (x:obj) =
            match x with
            | :? bool as b -> Some (if b then 1s else 0s)
            | :? byte   as n -> Some (int16 n)
            | :? double as n -> Some (int16 n)
            | :? int16  as n -> Some (int16 n)
            | :? int32  as n -> Some (int16 n)
            | :? int64  as n -> Some (int16 n)
            | :? sbyte  as n -> Some (int16 n)
            | :? single as n -> Some (int16 n)
            | :? uint16 as n -> Some (int16 n)
            | :? uint32 as n -> Some (int16 n)
            | :? uint64 as n -> Some (int16 n)
            | _ ->
                logWarn $"Cannot convert {x} to int16"
                None

        let (|UInt16|_|) (x:obj) =
            match x with
            | :? bool as b -> Some (if b then 1us else 0us)
            | :? byte   as n -> Some (uint16 n)
            | :? double as n -> Some (uint16 n)
            | :? int16  as n -> Some (uint16 n)
            | :? int32  as n -> Some (uint16 n)
            | :? int64  as n -> Some (uint16 n)
            | :? sbyte  as n -> Some (uint16 n)
            | :? single as n -> Some (uint16 n)
            | :? uint16 as n -> Some (uint16 n)
            | :? uint32 as n -> Some (uint16 n)
            | :? uint64 as n -> Some (uint16 n)
            | _ ->
                logWarn $"Cannot convert {x} to uint16"
                None

        let (|Int32|_|) (x:obj) =
            match x with
            | :? bool as b -> Some (if b then 1 else 0)     // toInt(false) 등에서의 casting 허용 위해 필요
            | :? byte   as n -> Some (int32 n)
            | :? double as n -> Some (int32 n)
            | :? int16  as n -> Some (int32 n)
            | :? int32  as n -> Some (int32 n)
            | :? int64  as n -> Some (int32 n)
            | :? sbyte  as n -> Some (int32 n)
            | :? single as n -> Some (int32 n)
            | :? uint16 as n -> Some (int32 n)
            | :? uint32 as n -> Some (int32 n)
            | :? uint64 as n -> Some (int32 n)
            | _ ->
                logWarn $"Cannot convert {x} to int32"
                None

        let (|UInt32|_|) (x:obj) =
            match x with
            | :? bool as b -> Some (if b then 1u else 0u)
            | :? byte   as n -> Some (uint32 n)
            | :? double as n -> Some (uint32 n)
            | :? int16  as n -> Some (uint32 n)
            | :? int32  as n -> Some (uint32 n)
            | :? int64  as n -> Some (uint32 n)
            | :? sbyte  as n -> Some (uint32 n)
            | :? single as n -> Some (uint32 n)
            | :? uint16 as n -> Some (uint32 n)
            | :? uint32 as n -> Some (uint32 n)
            | :? uint64 as n -> Some (uint32 n)
            | _ ->
                logWarn $"Cannot convert {x} to uint32"
                None

        let (|Int64|_|) (x:obj) =
            match x with
            | :? bool as b -> Some (if b then 1L else 0L)
            | :? byte   as n -> Some (int64 n)
            | :? double as n -> Some (int64 n)
            | :? int16  as n -> Some (int64 n)
            | :? int32  as n -> Some (int64 n)
            | :? int64  as n -> Some (int64 n)
            | :? sbyte  as n -> Some (int64 n)
            | :? single as n -> Some (int64 n)
            | :? uint16 as n -> Some (int64 n)
            | :? uint32 as n -> Some (int64 n)
            | :? uint64 as n -> Some (int64 n)
            | _ ->
                logWarn $"Cannot convert {x} to int64"
                None

        let (|UInt64|_|) (x:obj) =
            match x with
            | :? bool as b -> Some (if b then 1UL else 0UL)
            | :? byte   as n -> Some (uint64 n)
            | :? double as n -> Some (uint64 n)
            | :? int16  as n -> Some (uint64 n)
            | :? int32  as n -> Some (uint64 n)
            | :? int64  as n -> Some (uint64 n)
            | :? sbyte  as n -> Some (uint64 n)
            | :? single as n -> Some (uint64 n)
            | :? uint16 as n -> Some (uint64 n)
            | :? uint32 as n -> Some (uint64 n)
            | :? uint64 as n -> Some (uint64 n)
            | _ ->
                logWarn $"Cannot convert {x} to uint64"
                None


        let (|Bool|_|) (x:obj) =
            match x with
            | :? bool as b -> Some b
            | :? single as n -> Some (n <> 0.f)
            | :? double as n -> Some (n <> 0.0)
            | Int32 n -> Some (n <> 0)      (* int32 로 변환 가능한 모든 numeric type 포함 *)
            | _ -> None  // bool casting 실패

        let toBool    x = (|Bool|_|)    x |> Option.get
        let toFloat32 x = (|Float32|_|) x |> Option.get
        let toFloat64 x = (|Float64|_|) x |> Option.get
        let toInt16   x = (|Int16|_|)   x |> Option.get
        let toInt32   x = (|Int32|_|)   x |> Option.get
        let toInt64   x = (|Int64|_|)   x |> Option.get
        let toInt8    x = (|SByte|_|)   x |> Option.get
        let toUInt16  x = (|UInt16|_|)  x |> Option.get
        let toUInt32  x = (|UInt32|_|)  x |> Option.get
        let toUInt64  x = (|UInt64|_|)  x |> Option.get
        let toUInt8   x = (|Byte|_|)    x |> Option.get

        let isEqual (x:obj) (y:obj) =
            match x, y with
            | (:? bool as x), (:? bool as y) -> x = y
            | (:? string as a), (:? string as b) -> a = b
            | Float64 x, Float64 y -> x = y     // double 로 환산가능한 숫자만 비교하면 모든 type 의 숫자 비교는 OK
            | _ ->
                failwithlog "ERROR"
                false

    [<AbstractClass>]
    [<DebuggerDisplay("{Name}")>]
    type TypedValueStorage<'T when 'T:equality>(name, initValue:'T, ?comment) =
        let mutable value = initValue
        let comment = comment |? ""
        member _.Name: string = name
        member x.Value
            with get() = value
            and set(v) =
                if value <> v then
                    value <- v //cpu 단위로 이벤트 필요 ahn
                    ValueSubject.OnNext(x :> IStorage)

        member val Comment: string = comment with get, set

        interface IStorage with
            member x.DataType = typedefof<'T>
            member x.Comment with get() = x.Comment and set(v) = x.Comment <- v
            member x.BoxedValue with get() = x.Value and set(v) = x.Value <- (v :?> 'T)
            member x.ObjValue = x.Value :> obj
            member x.ToBoxedExpression() = x.ToBoxedExpression()

        interface IStorage<'T> with
            member x.Value with get() = x.Value and set(v) = x.Value <- v

        interface INamed with
            member x.Name with get() = x.Name and set(v) = failwithlog "ERROR: not supported"

        interface IText with
            member x.ToText() = x.ToText()

        interface IExpressionizableTerminal

        abstract ToText: unit -> string
        abstract ToBoxedExpression : unit -> obj    /// IExpression<'T> 의 boxed 형태의 expression 생성

    [<AbstractClass>]
    type TagBase<'T when 'T:equality>(name, initValue:'T, ?comment) =
        inherit TypedValueStorage<'T>(name, initValue, comment |? "")

        interface ITag<'T>
        interface INamedExpressionizableTerminal with
            member x.StorageName = name
        override x.ToText() = "$" + name

    [<AbstractClass>]
    type VariableBase<'T when 'T:equality>(name, initValue:'T, ?comment) =
        inherit TypedValueStorage<'T>(name, initValue, comment |? "")

        interface IVariable<'T>
        override x.ToText() = "$" + name

    type ILiteralHolder =
        abstract ToTextWithoutTypeSuffix: unit -> string

    type LiteralHolder<'T when 'T:equality>(literalValue:'T) =
        member _.Value = literalValue
        interface IExpressionizableTerminal with
            member x.ToText() = sprintf "%A" x.Value
        interface ILiteralHolder with
            member x.ToTextWithoutTypeSuffix() = $"{x.Value}"

    type Arg       = IExpression
    type Arguments = IExpression list
    type Args      = Arguments

    /// 모든 args 의 data type 이 동일한지 여부 반환
    let isAllExpressionSameType(args:Args) =
        args |> Seq.distinctBy(fun a -> a.DataType) |> Seq.length = 1
    let verifyAllExpressionSameType = isAllExpressionSameType >> verifyM "Type mismatch"
    let isThisOperatorRequireAllArgumentsSameType: (string -> bool)  =
        let hash =
            [   "+" ; "-" ; "*" ; "/" ; "%"
                ">" ; ">=" ; "<" ; "<=" ; "=" ; "!="
                "&&" ; "||"
                "&" ; "|" ; "&&&" ; "|||"
                "add"; "sub"; "mul"; "div"
                "gt"; "gte"; "lt"; "lte"
                "equal"; "notEqual"; "and"; "or"
            ] |> HashSet<string>
        fun (name:string) -> hash.Contains (name)
    let verifyArgumentsTypes operator args =
        if isThisOperatorRequireAllArgumentsSameType operator && not <| isAllExpressionSameType args then
            failwith $"Type mismatch for operator={operator}"


[<AutoOpen>]
module ExpressionPrologModule2 =
    let mutable internal fwdSerializeFunctionNameAndBoxedArguments =
        let dummy (functionName:string) (args:Args) (withParenthesys:bool): string =
            failwithlog "Should be reimplemented."
        dummy

    let mutable fwdCreateBoolEndoTag     = let dummy (tagName:string) (initValue:bool)   : TagBase<bool>   = failwithlog "Should be reimplemented." in dummy
    let mutable fwdCreateUShortEndoTag   = let dummy (tagName:string) (initValue:uint16) : TagBase<uint16> = failwithlog "Should be reimplemented." in dummy
    let mutable fwdFlattenExpression = let dummy (expr:IExpression)                  : IFlatExpression = failwithlog "Should be reimplemented." in dummy


