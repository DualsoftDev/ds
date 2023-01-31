namespace Engine.Core

open System.Diagnostics
open System.Collections.Generic

open Engine.Common.FS

[<AutoOpen>]

module TagVariableModule =
    type StorageCreationParams<'T when 'T:equality> = {
        Name: string
        Value: 'T
        /// 1. None 이면 자동으로 주소를 할당하지 않음
        /// 2. "" 이면 자동으로 주소를 할당
        /// 3. 그외의 문자열이면 그것 자체의 주소를 사용
        Address: string option
        Comment: string option
        System: ISystem
        IsGlobal: bool
    }
    let defaultStorageCreationParams(value) = {
        Name = ""
        Value = value
        Address = None
        Comment = None
        System = Runtime.System
        IsGlobal = false
    }



    [<AbstractClass>]
    [<DebuggerDisplay("{Name}")>]
    type TypedValueStorage<'T when 'T:equality>(param:StorageCreationParams<'T>) =
        let {Name=name; Value=initValue; Comment=comment; IsGlobal=isGlobal } = param
        let mutable value = initValue
        let comment = comment |? ""
        member _.Name: string = name
        member _.IsGlobal = isGlobal
        member x.Value
            with get() = value
            and set(v) =
                if value <> v then
                    value <- v
                    (x:>  IStorage).DsSystem.ValueChangeSubject.OnNext(x :> IStorage, v)
        member val Comment: string = comment with get, set
        member val Address = param.Address.Value with get, set

        interface IStorage with
            member x.DsSystem = param.System
            member x.DataType = typedefof<'T>
            member x.IsGlobal = isGlobal
            member x.Comment with get() = x.Comment and set(v) = x.Comment <- v
            member x.BoxedValue with get() = x.Value and set(v) = x.Value <- (v :?> 'T)
            member x.ObjValue = x.Value :> obj
            member x.Address with get() = x.Address and set(v) = x.Address <- v
            member x.ToBoxedExpression() = x.ToBoxedExpression()

        interface IStorage<'T> with
            member x.Value with get() = x.Value and set(v) = x.Value <- v

        interface INamed with
            member x.Name with get() = x.Name and set(_v) = failwithlog "ERROR: not supported"

        interface IText with
            member x.ToText() = x.ToText()

        interface INamedExpressionizableTerminal with
            member x.StorageName = name

        abstract ToText: unit -> string
        /// IExpression<'T> 의 boxed 형태의 expression 생성
        abstract ToBoxedExpression : unit -> obj

    /// PLC 기준 tag 로 생성되어야 하는 것들.  e.g Counter, Timer 구조의 멤버 변수 포함 (EN, CU, CD, ..)
    /// Tag<'T> 는 address 를 갖는 tag
    [<AbstractClass>]
    type TagBase<'T when 'T:equality>(param:StorageCreationParams<'T>) =
        inherit TypedValueStorage<'T>(param)
        let {Name=name; } = param

        interface ITag<'T>
        override x.ToText() = "$" + name

    [<AbstractClass>]
    type VariableBase<'T when 'T:equality>(param:StorageCreationParams<'T>) =
        inherit TypedValueStorage<'T>(param)

        interface IVariable<'T>
        override x.ToText() = "$" + param.Name

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
        let dummy (_functionName:string) (_args:Args) (_withParenthesys:bool): string =
            failwithlog "Should be reimplemented."
        dummy

    let mutable fwdCreateBoolMemberVariable   = let dummy (_tagName:string) (_initValue:bool)   : VariableBase<bool>   = failwithlog "Should be reimplemented." in dummy
    let mutable fwdCreateUShortMemberVariable = let dummy (_tagName:string) (_initValue:uint16) : VariableBase<uint16> = failwithlog "Should be reimplemented." in dummy
    let mutable fwdFlattenExpression          = let dummy (_expr:IExpression)                   : IFlatExpression      = failwithlog "Should be reimplemented." in dummy




