namespace Engine.Core

open System
open Engine.Common.FS

[<AutoOpen>]
module TagModule =

    type TagBase<'T when 'T:equality> with
        member x.Expr = var2expr x


    /// Variable for WINDOWS platform
    type Variable<'T when 'T:equality> (param:TagCreationParams<'T>) =
        inherit VariableBase<'T>(param)
        override x.ToBoxedExpression() = var2expr x

    let createParam name add comm v  = {Name=name; Comment=comm; Address=add; Value= v; System = Runtime.System}

    /// 시스템 간 연결용 tag.  Address 필수
    type BridgeTag<'T when 'T:equality> (param:TagCreationParams<'T>) =
        inherit TagBase<'T>(param)
        //address 없는 auto tag plcTag생성
        new(name, initValue:'T)
            = BridgeTag<'T>(createParam name None           None initValue)
        //address 있는 주소tag plcTag생성
        new(name, address:string, initValue:'T)
            = BridgeTag<'T>(createParam name (Some address) None initValue)

        interface ITagWithAddress with
            member x.Address = x.Address
        ///xgi tag 생성시 주소가 _A_이면 자동주소 //auto tag 예약 문자 _A_
        member val Address = ("_A_", param.Address) ||> Option.defaultValue
        override x.ToBoxedExpression() = var2expr x

    type Tag<'T when 'T:equality> = BridgeTag<'T>
    type ActionTag<'T when 'T:equality> = BridgeTag<'T>

    /// PlanTag 나의 시스템 내부 TAG
    type PlanTag<'T when 'T:equality> (param:TagCreationParams<'T>) =
        inherit BridgeTag<'T>(param)
        member val Vertex:Vertex option = None with get, set



    // 다음 컴파일 에러 회피하기 위한 boxing
    // error FS0030: 값 제한이 있습니다. 값 'fwdCreateVariableWithValue'은(는) 제네릭 형식    val mutable fwdCreateVariableWithValue: (string -> '_a -> IVariable)을(를) 가지는 것으로 유추되었습니다.    'fwdCreateVariableWithValue'에 대한 인수를 명시적으로 만들거나, 제네릭 요소로 만들지 않으려는 경우 형식 주석을 추가하세요.
    type BoxedObjectHolder = { Object:obj }

    let createWindowsVariableWithTypeAndValue (typ:System.Type) (name:string) (boxedValue:BoxedObjectHolder) : IVariable =
        verify (Runtime.Target = WINDOWS)
        let v = boxedValue.Object
        let createParam () = {Name=name; Value=unbox v; Comment=None; Address=None; System = Runtime.System}
        match typ.Name with
        | BOOL   -> new Variable<bool>   (createParam())
        | UINT8  -> new Variable<uint8>  (createParam())
        | CHAR   -> new Variable<char>   (createParam())
        | FLOAT64-> new Variable<double> (createParam())
        | INT16  -> new Variable<int16>  (createParam())
        | INT32  -> new Variable<int32>  (createParam())
        | INT64  -> new Variable<int64>  (createParam())
        | INT8   -> new Variable<int8>   (createParam())
        | FLOAT32-> new Variable<single> (createParam())
        | STRING -> new Variable<string> (createParam())
        | UINT16 -> new Variable<uint16> (createParam())
        | UINT32 -> new Variable<uint32> (createParam())
        | UINT64 -> new Variable<uint64> (createParam())
        | _  -> failwithlog "ERROR"

    let createWindowsVariableWithType (typ:System.Type) (name:string) : IVariable =
        verify (Runtime.Target = WINDOWS)
        let value = { Object = typeDefaultValue typ }
        createWindowsVariableWithTypeAndValue typ name value


    let mutable fwdCreateVariableWithType = createWindowsVariableWithType
    let mutable fwdCreateVariableWithTypeAndValue = createWindowsVariableWithTypeAndValue
