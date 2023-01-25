namespace Engine.Core

open System.Diagnostics
open System.Collections.Generic

open Engine.Common.FS

[<AutoOpen>]

module TagVariableModule =
    type TagCreationParams<'T when 'T:equality> = {
        Name: string
        Value: 'T
        Address: string option
        Comment: string option
    }

    let defaultTagCreationParam = {
        Name = ""
        Value = false
        Address = None
        Comment = None
    }


    [<AbstractClass>]
    [<DebuggerDisplay("{Name}")>]
    type TypedValueStorage<'T when 'T:equality>(param:TagCreationParams<'T>) =
        let {Name=name; Value=initValue; Comment=comment; } = param
        let mutable value = initValue
        let comment = comment |? ""
        member _.Name: string = name
        member x.Value
            with get() = value
            and set(v) =
                if value <> v then
                    value <- v //cpu 단위로 이벤트 필요 ahn
                    ValueSubject.OnNext(x :> IStorage, v)

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
    type TagBase<'T when 'T:equality>(param:TagCreationParams<'T>) =
        inherit TypedValueStorage<'T>(param)
        let {Name=name; } = param

        interface ITag<'T>
        interface INamedExpressionizableTerminal with
            member x.StorageName = name
        override x.ToText() = "$" + name

    [<AbstractClass>]
    type VariableBase<'T when 'T:equality>(param:TagCreationParams<'T>) =
        inherit TypedValueStorage<'T>(param)
        let {Name=name; Value=initValue; Comment=comment; } = param

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



[<AutoOpen>]
module ExpressionPrologModule =
    type Arg       = IExpression
    type Arguments = IExpression list
    type Args      = Arguments

    let mutable internal fwdSerializeFunctionNameAndBoxedArguments =
        let dummy (functionName:string) (args:Args) (withParenthesys:bool): string =
            failwithlog "Should be reimplemented."
        dummy

    let mutable fwdCreateBoolTag     = let dummy (tagName:string) (initValue:bool)   : TagBase<bool>   = failwithlog "Should be reimplemented." in dummy
    let mutable fwdCreateUShortTag   = let dummy (tagName:string) (initValue:uint16) : TagBase<uint16> = failwithlog "Should be reimplemented." in dummy
    let mutable fwdFlattenExpression = let dummy (expr:IExpression)                  : IFlatExpression = failwithlog "Should be reimplemented." in dummy




