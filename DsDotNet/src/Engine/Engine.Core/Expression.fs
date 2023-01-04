namespace rec Engine.Core
open System
open System.Net.NetworkInformation

(*  expression: generic type <'T> 나 <_> 으로는 <obj> match 으로 간주됨
    Expression<'T> 객체에 대한 matching
    * :? Expression<int> as x -> 형태로 type 을 지정하면 matching 이 가능하다.
    * :? Expression<_> as x ->   형태로 type 을 지정하지 않으면, Expression<obj> 로 matching 시도해서 matching 이 불가능하다.
    * :? Expression<'T> as x ->  형태로 type 을 지정하지 않으면, Expression<obj> 로 matching 시도해서 matching 이 불가능하다.
    * matching 해서 수행해야 할 필요한 기능들은 non generic interface 인 IExpression 에 담아 두고, 이를 matching 한다.
*)

(*
    type TagMode =
        /// '-| |-' or '-( )-'
        | Normal
        /// '-|/|-' or '-(/)-'
        | Neg
        /// '-|P|-' or '-(P)-'
        | Pulse
        /// '-|N|-' or '-(N)-'
        | NPulse
        /// '-(S)-'
        | Set
        /// '-(R)-'
        | Reset
*)


[<AutoOpen>]
module ExpressionModule =

    type Terminal<'T when 'T:equality> =
        | DuTag of TagBase<'T>
        | DuVariable of VariableBase<'T>
        | DuLiteral of 'T

    type FunctionSpec<'T> = {
        FunctionBody: Arguments -> 'T
        Name        : string
        Arguments   : Arguments
    }


    type Expression<'T when 'T:equality> =
        | DuTerminal of Terminal<'T>
        | DuFunction of FunctionSpec<'T>  //FunctionBody:(Arguments -> 'T) * Name * Arguments
        interface IExpression<'T> with
            member x.DataType = x.DataType
            member x.EvaluatedValue = x.Evaluate()
            member x.BoxedEvaluatedValue = x.Evaluate() |> box
            member x.GetBoxedRawObject() = x.GetBoxedRawObject()
            member x.ToText(withParenthesys) = x.ToText(withParenthesys)
            member x.FunctionName = x.FunctionName
            member x.FunctionArguments = x.FunctionArguments
            member x.CollectStorages() = x.CollectStorages()
            member x.Flatten() = fwdFlattenExpression x

        member x.DataType = typedefof<'T>

    /// literal 'T 로부터 terminal Expression<'T> 생성
    let literal2expr (x:'T) =
        let t = x.GetType()
        if t.IsValueType || t = typedefof<string> then
            DuTerminal (DuLiteral x)
        else
            failwith "ERROR: Value Type Error.  only allowed for primitive type"

    /// Tag<'T> 로부터 Expression<'T> 생성
    let tag2expr (t: TagBase<'T>) = DuTerminal (DuTag t)

    /// Variable<'T> 로부터 Expression<'T> 생성
    let var2expr (t: VariableBase<'T>) = DuTerminal (DuVariable t)

    type Timer internal(typ:TimerType, timerStruct:TimerStruct) =

        let accumulator = new TickAccumulator(typ, timerStruct)

        member _.Type = typ
        member _.Name = timerStruct.Name
        member _.EN = timerStruct.EN
        member _.TT = timerStruct.TT
        member _.DN = timerStruct.DN
        member _.PRE = timerStruct.PRE
        member _.ACC = timerStruct.ACC
        member _.RES = timerStruct.RES

        member val InputEvaluateStatements:Statement list = [] with get, set
        interface IDisposable with
            member _.Dispose() = (accumulator :> IDisposable).Dispose()

    type Counter internal(typ:CounterType, counterStruct:CounterBaseStruct) =

        let accumulator = new CountAccumulator(typ, counterStruct)

        member _.Type = typ
        member _.Name = counterStruct.Name
        /// Count up
        member _.CU = counterStruct.CU
        /// Count down
        member _.CD = counterStruct.CD
        /// Underflow
        member _.UN = counterStruct.UN
        /// Overflow
        member _.OV = counterStruct.OV
        /// Done bit
        member _.DN = counterStruct.DN
        /// Preset
        member _.PRE = counterStruct.PRE
        /// Accumuated
        member _.ACC = counterStruct.ACC
        /// Reset
        member _.RES = counterStruct.RES

        member val InputEvaluateStatements:Statement list = [] with get, set
        interface IDisposable with
            member this.Dispose() = (accumulator :> IDisposable).Dispose()


    type TimerStatement = {
        Timer:Timer
        RungInCondition: IExpression<bool> option
        ResetCondition:  IExpression<bool> option
        /// Timer 생성시의 function name
        FunctionName:string
    }


    type CounterStatement = {
        Counter        : Counter
        UpCondition    : IExpression<bool> option
        DownCondition  : IExpression<bool> option
        ResetCondition : IExpression<bool> option
        // XGI only
        LoadCondition  : IExpression<bool> option
        /// Counter 생성시의 function name
        FunctionName   : string
    }


    /// Pulse coil '-(P)-' 생성 및 평가를 위한 구조
    type HistoryFlag() = 
        member val LastValue = false with get, set
    type RisingCoil = {
        Storage:IStorage
        HistoryFlag:HistoryFlag 
    } with
        interface IStorage with
            
            member x.Name with get() = $"RisingCoil.{x.Storage.Name}" and set(v) = failwith "ERROR"
            member x.DataType = typedefof<RisingCoil>
            member x.Value with get() = x.Storage.Value 
                            and set(v) = x.Storage.Value <- v

            member x.ToText() = $"ppulse(${x.Storage.Name})"    // positive pulse
            member x.ToBoxedExpression() = failwith "ERROR: not supported"

    /// Negative Pulse Coil '-(N)-' 생성 및 평가를 위한 구조
    type FallingCoil = {
        Storage:IStorage
        HistoryFlag:HistoryFlag 
    } with
        interface IStorage with
            
            member x.Name with get() = $"FallingCoil.{x.Storage.Name}" and set(v) = failwith "ERROR"
            member x.DataType = typedefof<FallingCoil>
            member x.Value with get() = x.Storage.Value 
                            and set(v) = x.Storage.Value <- v

            member x.ToText() = $"npulse(${x.Storage.Name})"    // negative pulse
            member x.ToBoxedExpression() = failwith "ERROR: not supported"

    type Statement =
        | DuAssign of expression:IExpression * target:IStorage
        /// 변수 선언.  PLC rung 생성시에는 관여되지 않는다.
        | DuVarDecl of expression:IExpression * variable:IStorage
        | DuTimer of TimerStatement
        | DuCounter of CounterStatement
        /// 주어진 condition 이 충족하면, source 를 target 으로 copy 수행하는 rung 생성
        | DuCopy of condition:IExpression<bool> * source:IExpression * target:IStorage


    type CommentedStatement = CommentedStatement of comment:string * statement:Statement
    let (|CommentAndStatement|) = function | CommentedStatement(x, y) -> x, y
    let commentAndStatement = (|CommentAndStatement|)
    let withNoComment statement = CommentedStatement("", statement)
    let withExpressionComment (append:string) (statement: Statement) =
        CommentedStatement($"{append}\t{statement.ToText()}", statement)


    let pulseDo(expr:IExpression, storage:IStorage, historyFlag:HistoryFlag, isRising:bool) = 
        historyFlag.LastValue <- (expr.BoxedEvaluatedValue |> unbox)
        if historyFlag.LastValue |> unbox = isRising //rising 경우 TRUE 변경시점 처리
        then storage.Value <- true   
             storage.Value <- false   
             //single 스켄방식이면 펄스조건 사용된 모든 Rung 처리후 Off 
             //이벤트 방식이면 단일 쓰레드이면 이벤트 끝난후 pulseDo Off 해서 상관없을듯  //이벤트 테스트 중 ahn

    type Statement with
        member x.Do() =
            match x with
            | DuAssign (expr, (:? RisingCoil as rc)) ->
                if expr.BoxedEvaluatedValue <> rc.HistoryFlag.LastValue //평가값 변경시
                then pulseDo (expr, rc.Storage, rc.HistoryFlag, true)
                   
            | DuAssign (expr, (:? FallingCoil as fc)) ->
                if expr.BoxedEvaluatedValue <> fc.HistoryFlag.LastValue //평가값 변경시
                then pulseDo (expr, fc.Storage, fc.HistoryFlag, false)

            | DuAssign (expr, target) ->
                assert(target.DataType = expr.DataType)
                target.Value <- expr.BoxedEvaluatedValue

            | DuVarDecl (expr, target) ->
                assert(target.DataType = expr.DataType)
                target.Value <- expr.BoxedEvaluatedValue

            | DuTimer timerStatement ->
                for s in timerStatement.Timer.InputEvaluateStatements do
                    s.Do()

            | DuCounter counterStatement ->
                for s in counterStatement.Counter.InputEvaluateStatements do
                    s.Do()

            | DuCopy (condition, source, target) ->
                if condition.EvaluatedValue then
                    target.Value <- source.BoxedEvaluatedValue

        member x.ToText() =
            match x with
            | DuAssign (expr, target) -> $"{target.ToText()} := {expr.ToText(false)}"
            | DuVarDecl (expr, var) -> $"{var.DataType.ToDsDataTypeString()} {var.Name} = {expr.ToText(false)}"
            | DuTimer timerStatement ->
                let ts, t = timerStatement, timerStatement.Timer
                let typ = t.Type.ToString()
                let functionName = ts.FunctionName  // e.g "createTON"
                let args = [    // [preset; rung-in-condition; (reset-condition)]
                    sprintf "%A" t.PRE.Value
                    match ts.RungInCondition with | Some c -> c.ToText(false) | None -> ()
                    match ts.ResetCondition  with | Some c -> c.ToText(false) | None -> () ]
                let args = String.Join(", ", args)
                $"{typ.ToLower()} {t.Name} = {functionName}({args})"

            | DuCounter counterStatement ->
                let cs, c = counterStatement, counterStatement.Counter
                let typ = c.Type.ToString()
                let functionName = cs.FunctionName  // e.g "createCTU"
                let args = [    // [preset; up-condition; (down-condition;) (reset-condition;) (accum;)]
                    sprintf "%A" c.PRE.Value
                    match cs.UpCondition    with | Some c -> c.ToText(false) | None -> ()
                    match cs.DownCondition  with | Some c -> c.ToText(false) | None -> ()
                    match cs.ResetCondition with | Some c -> c.ToText(false) | None -> ()
                    if c.ACC.Value <> 0us then
                        sprintf "%A" c.ACC.Value ]
                let args = String.Join(", ", args)
                $"{typ.ToLower()} {c.Name} = {functionName}({args})"
            | DuCopy (condition, source, target) ->
                $"copyIf({condition.ToText(false)}, {source.ToText(false)}, {target.ToText()})"

    type Terminal<'T when 'T:equality> with
        member x.TryGetStorage(): IStorage option =
            match x with
            | DuTag t -> Some t
            | DuVariable v -> Some v
            | DuLiteral l -> None

        member x.GetBoxedRawObject(): obj =
            match x with
            | DuTag t -> t |> box
            | DuVariable v -> v
            | DuLiteral v -> v |> box

        member x.Evaluate(): 'T =
            match x with
            | DuTag t -> t.Value
            | DuVariable v -> v.Value
            | DuLiteral v -> v

        member x.ToText() =
            match x with
            | DuTag t -> "$" + t.Name
            | DuVariable t -> "$" + t.Name
            | DuLiteral v -> sprintf "%A" v

    type Expression<'T when 'T:equality> with
        member x.GetBoxedRawObject() =  // return type:obj    return type 명시할 경우, 다음 compile error 발생:  error FS1198: 제네릭 멤버 'ToText'이(가) 이 프로그램 지점 전의 비균일 인스턴스화에 사용되었습니다. 이 멤버가 처음에 오도록 멤버들을 다시 정렬해 보세요. 또는, 인수 형식, 반환 형식 및 추가 제네릭 매개 변수와 제약 조건을 포함한 멤버의 전체 형식을 명시적으로 지정하세요.
            match x with
            | DuTerminal b -> b.GetBoxedRawObject()
            | DuFunction fs -> fs |> box

        member x.Evaluate(): 'T =
            match x with
            | DuTerminal b -> b.Evaluate()
            | DuFunction fs -> fs.FunctionBody fs.Arguments

        member x.FunctionName =
            match x with
            | DuTerminal _ -> None
            | DuFunction fs -> Some fs.Name

        member x.FunctionArguments:IExpression list =
            match x with
            | DuFunction fs -> fs.Arguments
            | DuTerminal _ -> []

        member x.ToText(withParenthesys:bool) =
            match x with
            | DuTerminal b -> b.ToText()
            | DuFunction fs ->
                let text = fwdSerializeFunctionNameAndBoxedArguments fs.Name fs.Arguments withParenthesys
                text

        member x.CollectStorages() : IStorage list =
            match x with
            | DuTerminal b -> b.TryGetStorage() |> Option.toList
            | DuFunction fs -> [ for arg in fs.Arguments do yield! arg.CollectStorages() ]


    type System.Type with
        member x.ToDsDataTypeString() =
            match x.Name with
            | "Single" -> "float32"
            | "Double" -> "float64"
            | "SByte"  -> "int8"
            | "Byte"   -> "uint8"
            | "Int16"  -> "int16"
            | "UInt16" -> "uint16"
            | "Int32"  -> "int32"
            | "UInt32" -> "uint32"
            | "Int64"  -> "int64"
            | "UInt64" -> "uint64"
            | "String" -> "string"
            | "Char"   -> "char"
            | "Boolean"-> "bool"
            | _  -> failwith "ERROR"





