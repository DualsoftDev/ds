namespace Engine.Core

open System.Diagnostics
open Dual.Common.Core.FS
open System

[<AutoOpen>]
module TagVariableModule =
    type StorageCreationParams<'T when 'T:equality> = {
        Name: string
        Value: 'T
        Address: string option
        Comment: string option
        System: ISystem
        Target: IQualifiedNamed option
        TagKind: int
        IsGlobal: bool
    }

    let defaultStorageCreationParams value tagKind= {
        Name = ""
        Value = value
        Address = None
        Comment = None
        System = RuntimeDS.System
        Target = if tagKind > -1 then Some(RuntimeDS.System:?>IQualifiedNamed) else None
        TagKind = tagKind
        IsGlobal = false
    }


    [<AbstractClass>]
    [<DebuggerDisplay("{Name}({Value})")>]
    type TypedValueStorage<'T when 'T:equality>(param:StorageCreationParams<'T>) =
        do
            ()  // just for debug breakpoint

        let {Name=name; Value=initValue; Address=address; Comment=comment; IsGlobal=isGlobal } = param
        let mutable address = if address.IsSome then address.Value else TextAddrEmpty
        let mutable value = initValue
        let mutable tagChanged = false
        let comment = comment |? ""
        member _.Name: string = name
        member val IsGlobal = isGlobal with get, set
        member x.Value
            with get() = value
            and set(v) =
                if value <> v then
                    value <- v
                    tagChanged <- true
                 //모델 단위로 Subscribe 에서 자신 system만 처리 (system간 TAG 링크 때문에)
                    onValueChanged((x:>IStorage).DsSystem, x, v)
                 //기존 시스템 단위로
                 //   (x:>  IStorage).DsSystem.ValueChangeSubject.OnNext(x :> IStorage, v)
        member val Comment: string = comment with get, set
        member val Address = address with get, set

        interface IStorage with
            member x.DsSystem = param.System
            member x.Target = param.Target
            member x.TagKind = param.TagKind
            member x.TagChanged  with get() = tagChanged and set(v) = tagChanged <- v
            member x.DataType = typedefof<'T>
            member x.IsGlobal with get() = x.IsGlobal and set(v) = x.IsGlobal <- v
            member x.Comment with get() = x.Comment and set(v) = x.Comment <- v
            member x.BoxedValue with get() = x.Value and set(v) = x.Value <- (v :?> 'T)
            member x.ObjValue = x.Value :> obj
            /// null 인 경우, memory 주소를 할당하지 않는다.   "" 인 경우, memory 주소를 할당한다.   다른 정상 문자열이 있으면 그대로 둔다.
            member x.Address with get() = x.Address and set(v) = x.Address <- v
            member x.ToBoxedExpression() = x.ToBoxedExpression()
            member x.CompareTo(other) = String.Compare(x.Name, (other:?>IStorage).Name) 
                        
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
        abstract ToText : unit -> string
        abstract ToTextWithoutTypeSuffix: unit -> string

    type LiteralHolder<'T when 'T:equality> = { Value: 'T }
        with
            interface IExpressionizableTerminal with
                member x.ToText() = sprintf "%A" x.Value
            interface ILiteralHolder with
                member x.ToTextWithoutTypeSuffix() = $"{x.Value}"
                member x.ToText() = sprintf "%A" x.Value
            interface IValue<'T> with
                member x.Value with get() = x.Value and set(_v) = failwithlog "ERROR: unsupported."
                member x.ObjValue = box x.Value


[<AutoOpen>]
module ExpressionPrologModule =
    type Arg       = IExpression
    type Arguments = IExpression list
    type Args      = Arguments

    let mutable internal fwdSerializeFunctionNameAndBoxedArguments =
        let dummy (_functionName:string) (_args:Args) (_withParenthesis:bool): string =
            failwithlog "Should be reimplemented."
        dummy

    let mutable fwdCreateBoolMemberVariable   = let dummy (_tagName:string) (_initValue:bool)  (_tagKind:int)  : VariableBase<bool>   = failwithlog "Should be reimplemented." in dummy
    let mutable fwdCreateUShortMemberVariable = let dummy (_tagName:string) (_initValue:uint16) (_tagKind:int) : VariableBase<uint16> = failwithlog "Should be reimplemented." in dummy
    let mutable fwdCreateUInt32MemberVariable = let dummy (_tagName:string) (_initValue:uint32) (_tagKind:int) : VariableBase<uint32> = failwithlog "Should be reimplemented." in dummy
    let mutable fwdFlattenExpression          = let dummy (_expr:IExpression)                   : IFlatExpression      = failwithlog "Should be reimplemented." in dummy

    let clearVarBoolsOnDemand(varbools:VariableBase<bool> seq) =
        varbools
        |> filter (isItNull >> not)
        |> iter(fun vb -> vb.Value <- false)


