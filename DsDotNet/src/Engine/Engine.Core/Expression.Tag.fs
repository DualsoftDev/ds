namespace Engine.Core

open System
open System.Runtime.CompilerServices
open Engine.Common.FS

[<AutoOpen>]
module TagModule =

    type TagBase<'T when 'T:equality> with
        member x.Expr = var2expr x

    [<AbstractClass>]
    type Tag<'T when 'T:equality> (name, initValue:'T) =
        inherit TagBase<'T>(name, initValue)
        override x.ToBoxedExpression() = var2expr x

    /// Variable for WINDOWS platform
    type Variable<'T when 'T:equality> (name, initValue:'T) =
        inherit VariableBase<'T>(name, initValue)
        override x.ToBoxedExpression() = var2expr x

    /// plc / pc / 다른 runtime platform 지원가능한 물리 TAG
    type PlcTag<'T when 'T:equality> (name, address:string, initValue:'T) =
        inherit Tag<'T>(name, initValue)
        interface ITagWithAddress with
            member x.Address = x.Address
        member val Address = address with get, set

    /// PlanTag 나의 시스템 내부 TAG
    type PlanTag<'T when 'T:equality> (name, initValue:'T) =
        inherit PlcTag<'T>(name, "", initValue)
        member val Vertex:Vertex option = None with get, set

    /// ActionTag 다른 시스템 연결 TAG
    type ActionTag<'T when 'T:equality> (name, address, initValue:'T) =
        inherit PlcTag<'T>(name, address, initValue)



    // 다음 컴파일 에러 회피하기 위한 boxing
    // error FS0030: 값 제한이 있습니다. 값 'fwdCreateVariableWithValue'은(는) 제네릭 형식    val mutable fwdCreateVariableWithValue: (string -> '_a -> IVariable)을(를) 가지는 것으로 유추되었습니다.    'fwdCreateVariableWithValue'에 대한 인수를 명시적으로 만들거나, 제네릭 요소로 만들지 않으려는 경우 형식 주석을 추가하세요.
    type BoxedObjectHolder = { Object:obj }

    let createWindowsVariableWithTypeAndValue (typ:System.Type) (name:string) (boxedValue:BoxedObjectHolder): IVariable =
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

    let createWindowsVariableWithType (typ:System.Type) (name:string) : IVariable =
        verify (Runtime.Target = WINDOWS)
        let value = { Object = typeDefaultValue typ }
        createWindowsVariableWithTypeAndValue typ name value


    let mutable fwdCreateVariableWithType = createWindowsVariableWithType
    let mutable fwdCreateVariableWithTypeAndValue = createWindowsVariableWithTypeAndValue
