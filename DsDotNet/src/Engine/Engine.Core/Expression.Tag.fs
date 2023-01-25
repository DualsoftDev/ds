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

    [<Obsolete("<ahn> PLCTag 에서 address 가 None or empty string 인 경우가 존재할 수 있는지 체크")>]
    /// plc / pc / 다른 runtime platform 지원가능한 물리 TAG
    type Tag<'T when 'T:equality> (param:TagCreationParams<'T>) =
        inherit TagBase<'T>(param)
        let address = param.Address |? ""   // todo: <ahn> address None 과 "" 구분 처리.  `|? ""` 없이 동작해야..
        new(name, address:string, initValue:'T) =
            let param = {Name = name; Address = Some address; Comment = None; Value = initValue}
            Tag<'T>(param)

        interface ITagWithAddress with
            member x.Address = x.Address
        member val Address = address with get, set
        override x.ToBoxedExpression() = var2expr x

    /// PlanTag 나의 시스템 내부 TAG
    type PlanTag<'T when 'T:equality> (param:TagCreationParams<'T>) =
        inherit Tag<'T>(param)
        member val Vertex:Vertex option = None with get, set

    /// ActionTag 다른 시스템 연결 TAG
    type ActionTag<'T when 'T:equality> (param:TagCreationParams<'T>) =
        inherit Tag<'T>(param)



    // 다음 컴파일 에러 회피하기 위한 boxing
    // error FS0030: 값 제한이 있습니다. 값 'fwdCreateVariableWithValue'은(는) 제네릭 형식    val mutable fwdCreateVariableWithValue: (string -> '_a -> IVariable)을(를) 가지는 것으로 유추되었습니다.    'fwdCreateVariableWithValue'에 대한 인수를 명시적으로 만들거나, 제네릭 요소로 만들지 않으려는 경우 형식 주석을 추가하세요.
    type BoxedObjectHolder = { Object:obj }

    let createWindowsVariableWithTypeAndValue (typ:System.Type) (name:string) (boxedValue:BoxedObjectHolder): IVariable =
        verify (Runtime.Target = WINDOWS)
        let v = boxedValue.Object
        let createParam () = {Name=name; Value=unbox v; Comment=None; Address=None;}
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
