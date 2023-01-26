namespace Engine.Core

open System
open Engine.Common.FS

[<AutoOpen>]
module TagModule =

    type TypedValueStorage<'T when 'T:equality> with
        member x.Expr = var2expr x


    /// Variable for WINDOWS platform
    type Variable<'T when 'T:equality> (param:TagCreationParams<'T>) =
        inherit VariableBase<'T>(param)
        override x.ToBoxedExpression() = var2expr x

    let createParam name add comm v  = {Name=name; Comment=comm; Address=add; Value= v; System = Runtime.System}

    /// 시스템 간 연결용 tag.  Address 필수
    type BridgeTag<'T when 'T:equality> (param:TagCreationParams<'T>) =
        inherit TagBase<'T>(param)

        interface IBridgeTag with
            member x.Address = x.Address
        member val Address = param.Address.Value
        override x.ToBoxedExpression() = var2expr x

    /// 시스템 내(endo-)의 tag.  Address 불필요
    type EndoTag<'T when 'T:equality> (param:TagCreationParams<'T>) =
        inherit TagBase<'T>(param)
        override x.ToBoxedExpression() = var2expr x


    /// PlanVar 나의 시스템 내부 variable
    type PlanVar<'T when 'T:equality> (param:TagCreationParams<'T>) =
        inherit Variable<'T>(param)
        member val Vertex:Vertex option = None with get, set



    // 다음 컴파일 에러 회피하기 위한 boxing
    // error FS0030: 값 제한이 있습니다. 값 'fwdCreateVariableWithValue'은(는) 제네릭 형식    val mutable fwdCreateVariableWithValue: (string -> '_a -> IVariable)을(를) 가지는 것으로 유추되었습니다.    'fwdCreateVariableWithValue'에 대한 인수를 명시적으로 만들거나, 제네릭 요소로 만들지 않으려는 경우 형식 주석을 추가하세요.
    type BoxedObjectHolder = { Object:obj }

    let createWindowsVariableWithTypeAndValue (name:string) (boxedValue:BoxedObjectHolder) : IVariable =
        verify (Runtime.Target = WINDOWS)
        let v = boxedValue.Object
        let createParam (v) = {Name=name; Value=v; Comment=None; Address=None; System = Runtime.System}
        match v.GetType().Name with
        | BOOL   -> new Variable<bool>   (createParam(unbox v))
        | CHAR   -> new Variable<char>   (createParam(unbox v))
        | FLOAT32-> new Variable<single> (createParam(unbox v))
        | FLOAT64-> new Variable<double> (createParam(unbox v))
        | INT16  -> new Variable<int16>  (createParam(unbox v))
        | INT32  -> new Variable<int32>  (createParam(unbox v))
        | INT64  -> new Variable<int64>  (createParam(unbox v))
        | INT8   -> new Variable<int8>   (createParam(unbox v))
        | STRING -> new Variable<string> (createParam(unbox v))
        | UINT16 -> new Variable<uint16> (createParam(unbox v))
        | UINT32 -> new Variable<uint32> (createParam(unbox v))
        | UINT64 -> new Variable<uint64> (createParam(unbox v))
        | UINT8  -> new Variable<uint8>  (createParam(unbox v))
        | _  -> failwithlog "ERROR"

    let mutable fwdCreateVariableWithTypeAndValue = createWindowsVariableWithTypeAndValue
