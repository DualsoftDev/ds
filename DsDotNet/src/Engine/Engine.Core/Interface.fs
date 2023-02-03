// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open System.Collections.Generic
open Engine.Common.FS
open System.Reactive.Subjects
open System

[<AutoOpen>]
module Interface =


    type IVertex = interface end

    type INamed  =
         abstract Name:string with get, set

    type IText  =
         abstract ToText:unit -> string

    //type IRenameable =
    //    inherit INamed
    //    abstract Name:string with set

    type IQualifiedNamed =
        inherit INamed
        abstract QualifiedName:string with get
        abstract NameComponents:string[] with get

    type IValue =
        abstract ObjValue: obj with get        // 'BoxedValue' 사용시 이름 충돌

    /// Expression 의 Terminal 이 될 수 있는 subclass: Tag<'T>, Variable<'T>
    type IStorage =
        inherit IValue
        inherit INamed
        inherit IText
        abstract Address:string with get, set
        abstract DsSystem: ISystem
        abstract Target: IQualifiedNamed option
        abstract TagKind: int
        abstract BoxedValue: obj with get, set
        abstract DataType : System.Type
        abstract IsGlobal : bool with get, set
        abstract Comment: string with get, set
        abstract ToBoxedExpression : unit -> obj    /// IExpression<'T> 의 boxed 형태의 expression 생성

    and ISystem =
        abstract ValueChangeSubject : Subject<IStorage*obj>

    /// terminal expression 이 될 수 있는 객체.  Tag, Variable, Literal.  IExpression 은 아님
    type IExpressionizableTerminal =
        inherit IText

    type ITerminal =
        abstract Variable:IStorage option
        abstract Literal:IExpressionizableTerminal option

    type Storages() =
        inherit Dictionary<string, IStorage>(StringComparer.OrdinalIgnoreCase)

    type ITagManager =
        abstract Target: IQualifiedNamed
        abstract Storages: Storages

    /// x.Address 을 반환
    let inline address x = ( ^T: (member Address:string) x )

