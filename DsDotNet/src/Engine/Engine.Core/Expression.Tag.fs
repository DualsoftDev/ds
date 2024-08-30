namespace Engine.Core

open Dual.Common.Core.FS
open System.Linq

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

    type IMemberVariable = interface end
    /// Timer, Counter 등의 structure 내의 변수.  PLC 로 내릴 때, 실제 변수를 생성하지는 않지만, 참조는 가능해야 한다.  e.g myTimer1.EN
    type MemberVariable<'T when 'T:equality> (param:StorageCreationParams<'T>) =
        inherit Variable<'T>(param)
        interface IMemberVariable


    type IPlanVar = interface end
    /// PlanVar 나의 시스템 내부의 global variable
    type PlanVar<'T when 'T:equality> (param:StorageCreationParams<'T>) =
        inherit Variable<'T>(param)
        interface IPlanVar



    // 다음 컴파일 에러 회피하기 위한 boxing
    // error FS0030: 값 제한이 있습니다. 값 'fwdCreateVariableWithValue'은(는) 제네릭 형식    val mutable fwdCreateVariableWithValue: (string -> '_a -> IVariable)을(를) 가지는 것으로 유추되었습니다.    'fwdCreateVariableWithValue'에 대한 인수를 명시적으로 만들거나, 제네릭 요소로 만들지 않으려는 경우 형식 주석을 추가하세요.
    type BoxedObjectHolder = { Object:obj }

    let createVariable (name:string) (boxedValue:BoxedObjectHolder) (comment:string option) : IVariable =
        let v = boxedValue.Object
        let createParam () =
            {
                defaultStorageCreationParams(unbox v) (VariableTag.PcUserVariable|>int) with
                    Name=name; Comment=comment}
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

    let createVariableByType (name:string) (dataType:DsDataType.DataType) : IVariable =
        let defaultValue = DsDataType.typeDefaultValue (dataType.ToType())
        createVariable name { Object = defaultValue } None


    let createTagByBoxedValue (name:string)  (boxedValue:BoxedObjectHolder) tagKind address sys fqdn: ITag =
        let v = boxedValue.Object
        let createParam () = {defaultStorageCreationParams(unbox v) tagKind with Name=name; Address= Some address; System=sys; Target = Some fqdn}
        match v.GetType().Name with
        | BOOL   -> new Tag<bool>   (createParam())
        | CHAR   -> new Tag<char>   (createParam())
        | FLOAT32-> new Tag<single> (createParam())
        | FLOAT64-> new Tag<double> (createParam())
        | INT16  -> new Tag<int16>  (createParam())
        | INT32  -> new Tag<int32>  (createParam())
        | INT64  -> new Tag<int64>  (createParam())
        | INT8   -> new Tag<int8>   (createParam())
        | STRING -> new Tag<string> (createParam())
        | UINT16 -> new Tag<uint16> (createParam())
        | UINT32 -> new Tag<uint32> (createParam())
        | UINT64 -> new Tag<uint64> (createParam())
        | UINT8  -> new Tag<uint8>  (createParam())
        | _  -> failwithlog "ERROR"

    type Statement with

        member x.GetTargetStorages() =
            match x with
            | DuAssign (_, _expr, target) -> [ target ]
            | DuVarDecl (_expr, var) -> [ var ]
            | DuTimer timerStatement -> [timerStatement.Timer.DN ]
            | DuCounter counterStatement -> [counterStatement.Counter.DN ]
            //| DuAction (DuCopy (_condition, _source, target)) -> [ target ]
            | DuAction (DuCopyUdt { Storages=storages; UdtDecl=udtDecl; Target=target}) ->
                udtDecl.Members |> map (fun m -> storages[$"{target}.{m.Name}"] )

            | DuPLCFunction { Output=target } -> [target]
            | (DuUdtDecl _ | DuUdtDef _) -> failwith "Unsupported.  Should not be called for these statements"
            | (DuLambdaDecl _ | DuProcDecl _ | DuProcCall _) ->
                failwith "ERROR: Not yet implemented"       // 추후 subroutine 사용시, 필요에 따라 세부 구현

        member x.GetSourceStorages() =
            match x with
            | DuAssign (condi, expr, _target) ->
                [
                    if condi.IsSome then
                        yield! condi.Value.CollectStorages()
                    else
                        yield! expr.CollectStorages()
                ]
            | DuVarDecl (expr, _var) -> expr.CollectStorages()
            | DuTimer timerStatement ->
                [ for s in timerStatement.Timer.InputEvaluateStatements do
                    yield! s.GetSourceStorages() ]
            | DuCounter counterStatement ->
                [ for s in counterStatement.Counter.InputEvaluateStatements do
                    yield! s.GetSourceStorages() ]
            //| DuAction (DuCopy (condition, _source, _target)) ->
            //    condition.CollectStorages()
            | DuAction (DuCopyUdt { Storages=_storages; UdtDecl=_udtDecl; Condition=condition; Source=_source}) ->
                [
                    yield! condition.CollectStorages()
                ]
            | DuPLCFunction {Condition = condi; Arguments=_args} ->
                [
                    //if condi.IsSome then  //예외 발생시에 확인 필요?
                    yield! condi.Value.CollectStorages()
                ]
            | (DuUdtDecl _ | DuUdtDef _) -> failwith "Unsupported.  Should not be called for these statements"
            | (DuLambdaDecl _ | DuProcDecl _ | DuProcCall _) -> failwith "ERROR: Not yet implemented"




    let getTargetStorages (x:Statement) = x.GetTargetStorages() |> List.toSeq
    let getSourceStorages (x:Statement) = x.GetSourceStorages() |> List.toSeq

    let getTotalTags(statements:Statement seq) =
        [ for s in statements do
            yield! s.GetSourceStorages()
            yield! s.GetTargetStorages()
        ].Distinct()

    let getRungMap (statements: Statement seq) =
        let totalTags = getTotalTags statements

        // Dictionary를 사용하여 소스를 태그별로 그룹화
        let dicSource =
            statements
            |> Seq.collect (fun s -> s.GetSourceStorages() |> Seq.map (fun source -> source, s))
            |> Seq.groupBy fst
            |> Map.ofSeq

        // 태그별로 관련된 문장을 추출하여 맵에 추가
        let map =
            totalTags
            |> Seq.map (fun tag ->
                let statementsWithTag =
                    match dicSource.TryFind tag with
                    | Some sts -> sts |> Seq.map snd
                    | None -> Seq.empty
                tag, statementsWithTag)
            |> Tuple.toReadOnlyDictionary

        forceTrace $"--- getRungMap  statements: {statements.Count()} usedTags: {dicSource.Count} totalTags:{totalTags.Count()}"

        map
