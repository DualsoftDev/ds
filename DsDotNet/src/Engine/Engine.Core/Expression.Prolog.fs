namespace Engine.Core
open System
open System.Linq
open System.Runtime.CompilerServices
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
            | :? double as a -> Some a
            | :? int as a -> Some (double a)
            | :? single as a -> Some (double a)
            | _ -> None
        let (|Float|_|) (x:obj) =
            match x with
            | :? double as a -> Some (float a)
            | :? int as a -> Some (float a)
            | :? single as a -> Some (float a)
            | _ -> None

        let (|Byte|_|) (x:obj) =
            match x with
            | :? byte as n -> Some n
            | :? int as n -> Some (byte n)
            | :? uint as n -> Some (byte n)
            | :? double as n -> Some (byte n)
            | _ -> None
        let (|SByte|_|) (x:obj) =
            match x with
            | :? sbyte as n -> Some n
            | :? int as n -> Some (sbyte n)
            | :? uint as n -> Some (sbyte n)
            | :? double as n -> Some (sbyte n)
            | _ -> None

        let (|Int16|_|) (x:obj) =
            match x with
            | :? int16 as n -> Some n
            | :? int as n -> Some (int16 n)
            | :? uint as n -> Some (int16 n)
            | :? double as n -> Some (int16 n)
            | _ -> None

        let (|UInt16|_|) (x:obj) =
            match x with
            | :? uint16 as n -> Some n
            | :? int as n -> Some (uint16 n)
            | :? uint as n -> Some (uint16 n)
            | :? double as n -> Some (uint16 n)
            | _ -> None
        let (|Int32|_|) (x:obj) =
            match x with
            | :? int32 as n -> Some n
            | :? uint as n -> Some (int n)
            | :? double as n -> Some (int n)
            | _ -> None
        let (|UInt32|_|) (x:obj) =
            match x with
            | :? uint32 as n -> Some n
            | :? uint16 as n -> Some (uint32 n)
            | :? int as n -> Some (uint32 n)
            | :? double as n -> Some (uint32 n)
            | _ -> None
        let (|Bool|_|) (x:obj) =
            match x with
            | :? bool as n -> Some n
            | Int32 n when n <> 0 -> Some true
            | _ -> None

        let (|PLCTag|_|) (x:obj) =
            match x with
            | :? ITag as t -> Some t
            | _ -> None

        let toBool   x = (|Bool|_|)    x |> Option.get
        let toDouble x = (|Double|_|)  x |> Option.get
        let toFloat  x = (|Float|_|)   x |> Option.get
        let toByte   x = (|Byte|_|)    x |> Option.get
        let toSByte  x = (|SByte|_|)   x |> Option.get
        let toInt    x = (|Int32|_|)   x |> Option.get
        let toInt16  x = (|Int16|_|)   x |> Option.get
        let toUInt16 x = (|UInt16|_|)  x |> Option.get
        let toUInt32 x = (|UInt32|_|)  x |> Option.get
        let toTag    x = (|PLCTag|_|)  x |> Option.get
        let toString (x:obj) = Convert.ToString x

        let isEqual (x:obj) (y:obj) =
            match x, y with
            | Double x, Double y -> x = y
            | (:? string as a), (:? string as b) -> a = b
            | _ -> false

        [<Extension>] // type SeqExt =
        type SeqExt =
            [<Extension>] static member ExpectGteN(xs:'a seq, n) = expectGteN n xs; xs
            [<Extension>] static member Expect1(xs:'a seq) = expect1 xs
            [<Extension>] static member Expect2(xs:'a seq) = expect2 xs

    ///// Expression<'T> 로 생성할 수 있는 interface
    //type IExpressionCreatable    =
    //    abstract CreateBoxedExpression: unit -> obj        // Terminal<'T>

    type ExpressionType =
        | ExpTypeFunction
        | ExpTypeVariable
        | ExpTypeTag
        | ExpTypeLiteral

    /// Expression 의 Terminal 이 될 수 있는 subclass: Tag<'T>, StorageVariable<'T>
    type IStorage =
        inherit INamed
        inherit IText
        abstract Value: obj with get, set
    type IStorage<'T> =
        inherit IStorage
        abstract Value: 'T with get, set

    type ITag = inherit IStorage
    type IVariable = inherit IStorage

    /// Expression<'T> 을 boxed 에서 접근하기 위한 최소의 interface
    type IExpression =
        abstract DataType : System.Type
        abstract ExpressionType : ExpressionType
        abstract BoxedEvaluatedValue : obj
        /// Tag<'T> 나 Variable<'T> 객체 boxed 로 반환
        abstract GetBoxedRawObject: unit -> obj
        abstract ToText : withParenthesys:bool -> string


    [<AbstractClass>]
    [<DebuggerDisplay("{Name}")>]
    type TypedValueStorage<'T>(name, initValue:'T) =
        member _.Name: string = name
        member val Value = initValue with get, set

        interface IStorage with
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

    type Arguments = IExpression list
    type Args      = Arguments


[<AutoOpen>]
module ExpressionPrologModule2 =
    let private dummySerializeFunctionNameAndBoxedArguments (functionName:string) (args:Args) (withParenthesys:bool): string =
        failwith "Should be reimplemented."
    let mutable internal fwdSerializeFunctionNameAndBoxedArguments = dummySerializeFunctionNameAndBoxedArguments
