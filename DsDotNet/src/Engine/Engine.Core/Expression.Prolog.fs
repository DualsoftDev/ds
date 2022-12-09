namespace Engine.Core
open System
open System.Linq
open Engine.Common.FS
open System.Diagnostics

[<AutoOpen>]
module rec ExpressionPrologModule =
    module ExpressionPrologSubModule =
        let expectN (n:int) (xs:'a seq) = if xs.Count() <> n then failwith $"Wrong number of arguments: expect {n}"
        let expect1 xs = expectN 1 xs; xs.First()
        let expect2 xs = expectN 2 xs; Array.ofSeq xs
        let expectGteN (n:int) (xs:'a seq) =
            if xs.Count() < n then failwith $"Wrong number of arguments: expect at least {n} arguments"

        let (|Double|_|) (x:obj) =
            match x with
            | :? single as n -> Some (double n)
            | :? double as n -> Some (double n)
            | :? sbyte  as n -> Some (double n)
            | :? byte   as n -> Some (double n)
            | :? int16  as n -> Some (double n)
            | :? uint16 as n -> Some (double n)
            | :? int32  as n -> Some (double n)
            | :? uint32 as n -> Some (double n)
            | :? int64  as n -> Some (double n)
            | :? uint64 as n -> Some (double n)
            | _ ->
                logWarn $"Cannot convert {x} to double"
                None

        let (|Float|_|) (x:obj) =
            match x with
            | :? single as n -> Some (float n)
            | :? double as n -> Some (float n)
            | :? sbyte  as n -> Some (float n)
            | :? byte   as n -> Some (float n)
            | :? int16  as n -> Some (float n)
            | :? uint16 as n -> Some (float n)
            | :? int32  as n -> Some (float n)
            | :? uint32 as n -> Some (float n)
            | :? int64  as n -> Some (float n)
            | :? uint64 as n -> Some (float n)
            | _ ->
                logWarn $"Cannot convert {x} to float"
                None

        let (|Byte|_|) (x:obj) =
            match x with
            | :? single as n -> Some (byte n)
            | :? double as n -> Some (byte n)
            | :? sbyte  as n -> Some (byte n)
            | :? byte   as n -> Some (byte n)
            | :? int16  as n -> Some (byte n)
            | :? uint16 as n -> Some (byte n)
            | :? int32  as n -> Some (byte n)
            | :? uint32 as n -> Some (byte n)
            | :? int64  as n -> Some (byte n)
            | :? uint64 as n -> Some (byte n)
            | _ ->
                logWarn $"Cannot convert {x} to byte"
                None

        let (|SByte|_|) (x:obj) =
            match x with
            | :? single as n -> Some (sbyte n)
            | :? double as n -> Some (sbyte n)
            | :? sbyte  as n -> Some (sbyte n)
            | :? byte   as n -> Some (sbyte n)
            | :? int16  as n -> Some (sbyte n)
            | :? uint16 as n -> Some (sbyte n)
            | :? int32  as n -> Some (sbyte n)
            | :? uint32 as n -> Some (sbyte n)
            | :? int64  as n -> Some (sbyte n)
            | :? uint64 as n -> Some (sbyte n)
            | _ ->
                logWarn $"Cannot convert {x} to sbyte"
                None

        let (|Int16|_|) (x:obj) =
            match x with
            | :? single as n -> Some (int16 n)
            | :? double as n -> Some (int16 n)
            | :? sbyte  as n -> Some (int16 n)
            | :? byte   as n -> Some (int16 n)
            | :? int16  as n -> Some (int16 n)
            | :? uint16 as n -> Some (int16 n)
            | :? int32  as n -> Some (int16 n)
            | :? uint32 as n -> Some (int16 n)
            | :? int64  as n -> Some (int16 n)
            | :? uint64 as n -> Some (int16 n)
            | _ ->
                logWarn $"Cannot convert {x} to int16"
                None

        let (|UInt16|_|) (x:obj) =
            match x with
            | :? single as n -> Some (uint16 n)
            | :? double as n -> Some (uint16 n)
            | :? sbyte  as n -> Some (uint16 n)
            | :? byte   as n -> Some (uint16 n)
            | :? int16  as n -> Some (uint16 n)
            | :? uint16 as n -> Some (uint16 n)
            | :? int32  as n -> Some (uint16 n)
            | :? uint32 as n -> Some (uint16 n)
            | :? int64  as n -> Some (uint16 n)
            | :? uint64 as n -> Some (uint16 n)
            | _ ->
                logWarn $"Cannot convert {x} to uint16"
                None

        let (|Int32|_|) (x:obj) =
            match x with
            | :? single as n -> Some (int32 n)
            | :? double as n -> Some (int32 n)
            | :? sbyte  as n -> Some (int32 n)
            | :? byte   as n -> Some (int32 n)
            | :? int16  as n -> Some (int32 n)
            | :? uint16 as n -> Some (int32 n)
            | :? int32  as n -> Some (int32 n)
            | :? uint32 as n -> Some (int32 n)
            | :? int64  as n -> Some (int32 n)
            | :? uint64 as n -> Some (int32 n)
            | _ ->
                logWarn $"Cannot convert {x} to int32"
                None

        let (|UInt32|_|) (x:obj) =
            match x with
            | :? single as n -> Some (uint32 n)
            | :? double as n -> Some (uint32 n)
            | :? sbyte  as n -> Some (uint32 n)
            | :? byte   as n -> Some (uint32 n)
            | :? int16  as n -> Some (uint32 n)
            | :? uint16 as n -> Some (uint32 n)
            | :? int32  as n -> Some (uint32 n)
            | :? uint32 as n -> Some (uint32 n)
            | :? int64  as n -> Some (uint32 n)
            | :? uint64 as n -> Some (uint32 n)
            | _ ->
                logWarn $"Cannot convert {x} to uint32"
                None

        let (|Int64|_|) (x:obj) =
            match x with
            | :? single as n -> Some (int64 n)
            | :? double as n -> Some (int64 n)
            | :? sbyte  as n -> Some (int64 n)
            | :? byte   as n -> Some (int64 n)
            | :? int16  as n -> Some (int64 n)
            | :? uint16 as n -> Some (int64 n)
            | :? int32  as n -> Some (int64 n)
            | :? uint32 as n -> Some (int64 n)
            | :? int64  as n -> Some (int64 n)
            | :? uint64 as n -> Some (int64 n)
            | _ ->
                logWarn $"Cannot convert {x} to int64"
                None

        let (|UInt64|_|) (x:obj) =
            match x with
            | :? single as n -> Some (uint64 n)
            | :? double as n -> Some (uint64 n)
            | :? sbyte  as n -> Some (uint64 n)
            | :? byte   as n -> Some (uint64 n)
            | :? int16  as n -> Some (uint64 n)
            | :? uint16 as n -> Some (uint64 n)
            | :? int32  as n -> Some (uint64 n)
            | :? uint32 as n -> Some (uint64 n)
            | :? int64  as n -> Some (uint64 n)
            | :? uint64 as n -> Some (uint64 n)
            | _ ->
                logWarn $"Cannot convert {x} to uint64"
                None


        let (|Bool|_|) (x:obj) =
            match x with
            | :? bool as b -> Some b
            | Int32 n when n = 0 -> Some false      (* int32 로 변환 가능한 모든 numeric type 포함 *)
            | Int32 _            -> Some true
            | _ -> None  // bool casting 실패

        let toBool   x = (|Bool|_|)    x |> Option.get
        let toDouble x = (|Double|_|)  x |> Option.get
        let toSingle x = (|Float|_|)   x |> Option.get
        let toUInt8  x = (|Byte|_|)    x |> Option.get
        let toInt8   x = (|SByte|_|)   x |> Option.get
        let toInt16  x = (|Int16|_|)   x |> Option.get
        let toUInt16 x = (|UInt16|_|)  x |> Option.get
        let toInt32  x = (|Int32|_|)   x |> Option.get
        let toUInt32 x = (|UInt32|_|)  x |> Option.get
        let toInt64  x = (|Int64|_|)   x |> Option.get
        let toUInt64 x = (|UInt64|_|)  x |> Option.get

        let isEqual (x:obj) (y:obj) =
            match x, y with
            | Double x, Double y -> x = y
            | (:? string as a), (:? string as b) -> a = b
            | _ -> false


    /// Expression 의 Terminal 이 될 수 있는 subclass: Tag<'T>, StorageVariable<'T>
    type IStorage =
        inherit INamed
        inherit IText
        abstract Value: obj with get, set
        abstract DataType : System.Type

    type IStorage<'T> =
        inherit IStorage
        abstract Value: 'T with get, set

    type ITag = inherit IStorage
    type IVariable = inherit IStorage

    /// Expression<'T> 을 boxed 에서 접근하기 위한 최소의 interface
    type IExpression =
        abstract DataType : System.Type
        //abstract ExpressionType : ExpressionType
        abstract BoxedEvaluatedValue : obj
        /// Tag<'T> 나 Variable<'T> 객체 boxed 로 반환
        abstract GetBoxedRawObject: unit -> obj
        /// withParenthesys: terminal 일 경우는 무시되고, Function 일 경우에만 적용됨
        abstract ToText : withParenthesys:bool -> string


    [<AbstractClass>]
    [<DebuggerDisplay("{Name}")>]
    type TypedValueStorage<'T>(name, initValue:'T) =
        member _.Name: string = name
        member val Value = initValue with get, set

        interface IStorage with
            member x.DataType = typedefof<'T>
            member x.Value with get() = x.Value and set(v) = x.Value <- v :?> 'T
            member x.ToText() = x.ToText()

        interface IStorage<'T> with
            member x.Value with get() = x.Value and set(v) = x.Value <- v
        interface INamed with
            member x.Name with get() = x.Name and set(v) = failwith "ERROR: not supported"

        abstract ToText: unit -> string




    [<AbstractClass>]
    type Tag<'T>(name, initValue:'T) =
        inherit TypedValueStorage<'T>(name, initValue)

        interface ITag
        override x.ToText() = "%" + name

    // todo: 임시 이름... 추후 Variable로
    type StorageVariable<'T>(name, initValue:'T) =
        inherit TypedValueStorage<'T>(name, initValue)

        interface IVariable
        override x.ToText() = "$" + name

    type Arg       = IExpression
    type Arguments = IExpression list
    type Args      = Arguments

    /// 모든 args 의 data type 이 동일한지 여부 반환
    let isAllExpressionSameType(args:Args) =
        args |> Seq.distinctBy(fun a -> a.DataType) |> Seq.length = 1
    let verifyAllExpressionSameType = isAllExpressionSameType >> verifyM "Type mismatch"

[<AutoOpen>]
module ExpressionPrologModule2 =
    let private dummySerializeFunctionNameAndBoxedArguments (functionName:string) (args:Args) (withParenthesys:bool): string =
        failwith "Should be reimplemented."
    let mutable internal fwdSerializeFunctionNameAndBoxedArguments = dummySerializeFunctionNameAndBoxedArguments
