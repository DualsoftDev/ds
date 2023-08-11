namespace Engine.Core

open Dual.Common.Core.FS

[<AutoOpen>]
module TagModule =

    type TypedValueStorage<'T when 'T:equality> with
        member x.Expr = var2expr x


    /// Variable for WINDOWS platform
    type Variable<'T when 'T:equality> (param:StorageCreationParams<'T>) =
        inherit VariableBase<'T>(param)
        interface INamedExpressionizableTerminal with
            member x.StorageName = param.Name
        override x.ToBoxedExpression() = var2expr x

    /// 시스템 간 연결용 tag.  Address 필수
    type Tag<'T when 'T:equality> (param:StorageCreationParams<'T>) =
        inherit TagBase<'T>(param)
        override x.ToBoxedExpression() = var2expr x

    /// Timer, Counter 등의 structure 내의 변수.  PLC 로 내릴 때, 실제 변수를 생성하지는 않지만, 참조는 가능해야 한다.  e.g myTimer1.EN
    type MemberVariable<'T when 'T:equality> (param:StorageCreationParams<'T>) =
        inherit Variable<'T>(param)


    /// PlanVar 나의 시스템 내부의 global variable
    type PlanVar<'T when 'T:equality> (param:StorageCreationParams<'T>) =
        inherit Variable<'T>(param)



    // 다음 컴파일 에러 회피하기 위한 boxing
    // error FS0030: 값 제한이 있습니다. 값 'fwdCreateVariableWithValue'은(는) 제네릭 형식    val mutable fwdCreateVariableWithValue: (string -> '_a -> IVariable)을(를) 가지는 것으로 유추되었습니다.    'fwdCreateVariableWithValue'에 대한 인수를 명시적으로 만들거나, 제네릭 요소로 만들지 않으려는 경우 형식 주석을 추가하세요.
    type BoxedObjectHolder = { Object:obj }

    let createVariable (name:string) (boxedValue:BoxedObjectHolder) : IVariable =
        let v = boxedValue.Object
        let createParam () = {defaultStorageCreationParams(unbox v) with Name=name; }
        match v.GetType().Name with
        | BOOL   -> new Variable<bool>   (createParam())
        | CHAR   -> new Variable<char>   (createParam())
        | FLOAT32-> new Variable<single> (createParam())
        | FLOAT64-> new Variable<double> (createParam())
        | INT16  -> new Variable<int16>  (createParam())
        | INT32  -> new Variable<int32>  (createParam())
        | INT64  -> new Variable<int64>  (createParam())
        | INT8   -> new Variable<int8>   (createParam())
        | STRING -> new Variable<string> (createParam())
        | UINT16 -> new Variable<uint16> (createParam())
        | UINT32 -> new Variable<uint32> (createParam())
        | UINT64 -> new Variable<uint64> (createParam())
        | UINT8  -> new Variable<uint8>  (createParam())
        | _  -> failwithlog "ERROR"

