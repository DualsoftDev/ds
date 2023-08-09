namespace rec Engine.Core
open System
open System.Diagnostics
open Engine.Common.FS
open ExpressionModule

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

    [<DebuggerDisplay("{ToText()}")>]
    type Terminal<'T when 'T:equality> =
        | DuVariable of TypedValueStorage<'T>
        | DuLiteral of LiteralHolder<'T>        // Literal 도 IExpressionizableTerminal 구현해야 하므로, 'T 대신 LiteralHolder<'T> 사용
        interface ITerminal with
            member x.Variable = match x with | DuVariable variable -> Some variable | _ -> None
            member x.Literal  = match x with | DuLiteral literal   -> Some literal  | _ -> None

    type IFunctionSpec =
        abstract Name: string
        abstract Arguments: Arguments

    type FunctionSpec<'T> = {
        FunctionBody: Arguments -> 'T
        Name        : string
        Arguments   : Arguments
    }
    with
        interface IFunctionSpec with
            member x.Name      = x.Name
            member x.Arguments = x.Arguments

    [<DebuggerDisplay("{ToText(true)}")>]
    type Expression<'T when 'T:equality> =
        | DuTerminal of Terminal<'T>
        | DuFunction of FunctionSpec<'T>  //FunctionBody:(Arguments -> 'T) * Name * Arguments
        interface IExpression<'T> with
            member x.DataType = x.DataType
            member x.EvaluatedValue = x.Evaluate()
            member x.BoxedEvaluatedValue = x.Evaluate() |> box
            member x.GetBoxedRawObject() = x.GetBoxedRawObject()
            member x.ToText(withParenthesys) = x.ToText(withParenthesys)
            member x.CollectStorages() = x.CollectStorages()
            member x.Flatten() = fwdFlattenExpression x
            member x.IsEqual y = x.IsEqual y

            (* 'T type DU 를 접근하기 위한 members *)
            member x.FunctionName = x.FunctionName
            member x.FunctionArguments = x.FunctionArguments
            member x.Terminal = match x with | DuTerminal t -> Some t | _ -> None
            member x.WithNewFunctionArguments(args) =
                match x with
                | DuFunction fs -> DuFunction {fs with Arguments = args }
                | _ -> failwithlog "ERROR"

        member x.DataType = typedefof<'T>
        /// expression 의 type 이 동일한 경우 ToString() 결과가 같으면 동일한 것으로 간주
        /// type 이 다르면 항상 false 반환
        member x.IsEqual (y:IExpression) =
            if x.GetType() = y.GetType() then
                x.ToText(false) = y.ToText(false)
            else
                false

    /// literal 'T 로부터 terminal Expression<'T> 생성
    let literal2expr (x:'T) =
        let t = x.GetType()
        if t.IsValueType || t = typedefof<string> then
            DuTerminal (DuLiteral ({Value=x}:LiteralHolder<_>))
        else
            failwithlog "ERROR: Value Type Error.  only allowed for primitive type"

    /// Tag<'T> or Variable<'T> 로부터 Expression<'T> 생성
    let var2expr (t: TypedValueStorage<'T>):Expression<'T> = DuTerminal (DuVariable t)

    [<RequireQualifiedAccess>]
    module Expression =
        let True  = literal2expr true
        let False = literal2expr false
        let Zero  = literal2expr 0

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
        member _.TimerStruct = timerStruct

        member val InputEvaluateStatements:Statement list = [] with get, set
        interface IDisposable with
            member _.Dispose() = (accumulator :> IDisposable).Dispose()

    type Counter internal(typ:CounterType, counterStruct:CounterBaseStruct) =

        let accumulator = new CountAccumulator(typ, counterStruct)

        member _.Type = typ
        member _.CounterStruct = counterStruct
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

    type ActionStatement =
        | DuCopy of condition:IExpression<bool> * source:IExpression * target:IStorage

    /// Pulse coil '-(P)-' 생성 및 평가를 위한 구조
    type HistoryFlag() =
        member val LastValue = false with get, set

    let private unsupported() = failwithlog "ERROR: not supported"
    type RisingCoil = {
        Storage:IStorage
        HistoryFlag:HistoryFlag
        System:ISystem
    } with
        interface IStorage with

            member x.DsSystem = x.System
            member x.Target = None
            member x.TagKind = -1
            member x.Name with get() = $"RisingCoil.{x.Storage.Name}" and set(_v) = failwithlog "ERROR"
            member _.Address with get() = unsupported() and set(_v) = unsupported()
            member _.DataType = typedefof<RisingCoil>
            member x.IsGlobal with get() = x.Storage.IsGlobal and set(v) = x.Storage.IsGlobal <- v
            member _.Comment with get() = "" and set(_v) = failwithlog "ERROR"
            member x.BoxedValue with get() = x.Storage.BoxedValue
                                and set(v) = x.Storage.BoxedValue <- v
            member x.ObjValue = x.Storage.BoxedValue

            member x.ToText() = $"ppulse(${x.Storage.Name})"    // positive pulse
            member _.ToBoxedExpression() = failwithlog "ERROR: not supported"

    /// Negative Pulse Coil '-(N)-' 생성 및 평가를 위한 구조
    type FallingCoil = {
        Storage:IStorage
        HistoryFlag:HistoryFlag
        System:ISystem
    } with
        interface IStorage with

            member x.DsSystem = x.System
            member x.Target = None
            member x.TagKind = -1
            member x.Name with get() = $"FallingCoil.{x.Storage.Name}" and set(_v) = failwithlog "ERROR"
            member _.Address with get() = unsupported() and set(_v) = unsupported()
            member _.DataType = typedefof<FallingCoil>
            member x.IsGlobal with get() = x.Storage.IsGlobal and set(v) = x.Storage.IsGlobal <- v
            member _.Comment with get() = "" and set(_v) = failwithlog "ERROR"
            member x.BoxedValue with get() = x.Storage.BoxedValue
                                and set(v) = x.Storage.BoxedValue <- v

            member x.ObjValue = x.Storage.BoxedValue
            member x.ToText() = $"npulse(${x.Storage.Name})"    // negative pulse
            member _.ToBoxedExpression() = failwithlog "ERROR: not supported"


    type FunctionParameters = {
        FunctionName:string
        Arguments:Arguments
        /// Function output store target
        Output:INamedExpressionizableTerminal
    }

    type Statement =
        | DuAssign of expression:IExpression * target:IStorage
        /// 변수 선언.  PLC rung 생성시에는 관여되지 않는다.
        | DuVarDecl of expression:IExpression * variable:IStorage

        | DuTimer   of TimerStatement
        | DuCounter of CounterStatement
        | DuAction  of ActionStatement

        | DuAugmentedPLCFunction of FunctionParameters


    type CommentedStatement =
        | CommentedStatement of comment:string * statement:Statement
        member x.Statement = match x with | CommentedStatement (_c, s) -> s
        member x.TargetName =
             match x.Statement with
             | DuAssign (_expression, target) -> target.Name
             | DuVarDecl (_expression,variable) -> variable.Name
             | DuTimer (t:TimerStatement) -> t.Timer.Name
             | DuCounter (c:CounterStatement) -> c.Counter.Name
             | DuAction (a:ActionStatement) ->
                match a with
                | DuCopy (_condition:IExpression<bool>, _source:IExpression,target:IStorage)-> target.Name
             | DuAugmentedPLCFunction (_f:FunctionParameters) ->  _f.FunctionName  // Function은 항상 false 함수에 따른다.

        member x.TargetValue    =
            match x.Statement with
            | DuAssign (_expression, target) -> target.BoxedValue
            | DuVarDecl (_expression,variable) -> variable.BoxedValue
            | DuTimer (t:TimerStatement) -> t.Timer.DN.Value
            | DuCounter (c:CounterStatement) -> c.Counter.DN.Value
            | DuAction (a:ActionStatement) ->
                match a with
                | DuCopy (_condition:IExpression<bool>, _source:IExpression,target:IStorage)-> target.BoxedValue
            | DuAugmentedPLCFunction (_f:FunctionParameters) ->  false  // Function은 항상 false 함수에 따른다.

    let (|CommentAndStatement|) = function | CommentedStatement(x, y) -> x, y
    let commentAndStatement = (|CommentAndStatement|)
    let withNoComment statement = CommentedStatement("", statement)
    let withExpressionComment (append:string) (statement: Statement) =
        CommentedStatement(append, statement)


    let pulseDo(expr:IExpression, storage:IStorage, historyFlag:HistoryFlag, isRising:bool) =
        if expr.BoxedEvaluatedValue <> historyFlag.LastValue //평가값 변경시
        then
            historyFlag.LastValue <- (expr.BoxedEvaluatedValue |> unbox)
            if historyFlag.LastValue |> unbox = isRising //rising 경우 TRUE 변경시점 처리
            then
                storage.BoxedValue <- true
                storage.BoxedValue <- false
                //single 스켄방식이면 펄스조건 사용된 모든 Rung 처리후 Off

    type Statement with
        member x.Do() =
            match x with
            | DuAssign (expr, (:? RisingCoil as rc)) ->
                pulseDo (expr, rc.Storage, rc.HistoryFlag, true)

            | DuAssign (expr, (:? FallingCoil as fc)) ->
                pulseDo (expr, fc.Storage, fc.HistoryFlag, false)

            | DuAssign (expr, target) ->
                assert(target.DataType = expr.DataType)
                target.BoxedValue <- expr.BoxedEvaluatedValue

            | DuVarDecl (expr, target) ->
                assert(target.DataType = expr.DataType)
                target.BoxedValue <- expr.BoxedEvaluatedValue

            | DuTimer timerStatement ->
                for s in timerStatement.Timer.InputEvaluateStatements do
                    s.Do()

            | DuCounter counterStatement ->
                for s in counterStatement.Counter.InputEvaluateStatements do
                    s.Do()

            | DuAction (DuCopy (condition, source, target)) ->
                if condition.EvaluatedValue then
                    target.BoxedValue <- source.BoxedEvaluatedValue
            | DuAugmentedPLCFunction _ ->
                failwithlog "ERROR"

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
            | DuAction (DuCopy (condition, source, target)) ->
                $"copyIf({condition.ToText(false)}, {source.ToText(false)}, {target.ToText()})"
            | DuAugmentedPLCFunction _ ->
                failwithlog "ERROR"


    type Terminal<'T when 'T:equality> with
        member x.TryGetStorage(): IStorage option =
            match x with
            | DuVariable v -> Some v
            | DuLiteral _ -> None

        member x.GetBoxedRawObject(): obj =
            match x with
            | DuVariable v -> v
            | DuLiteral v -> v |> box

        member x.Evaluate(): 'T =
            match x with
            | DuVariable v -> v.Value
            | DuLiteral v -> v.Value

        member x.Name =
            match x with
            | DuVariable t -> t.Name
            | DuLiteral _ -> failwithlog "ERROR"

        member x.ToText() =
            match x with
            | DuVariable t -> "$" + t.Name
            | DuLiteral v -> sprintf "%A" v.Value

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

        member x.ToText(?withParenthesys:bool) =
            let withParenthesys = withParenthesys |? false
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
            | BOOL    -> "bool"
            | CHAR    -> "char"
            | FLOAT32 -> "float32"
            | FLOAT64 -> "float64"
            | INT16   -> "uint16"
            | INT32   -> "int32"
            | INT64   -> "int64"
            | INT8    -> "int8"
            | STRING  -> "string"
            | UINT16  -> "int16"
            | UINT32  -> "uint32"
            | UINT64  -> "uint64"
            | UINT8   -> "uint8"
            | _  -> failwithlog "ERROR"


    type IStorage with
        member x.ToExpression():IExpression =
            match x.DataType.Name with
            | BOOL    -> DuTerminal (DuVariable (x :?> TypedValueStorage<bool>  )) :> IExpression
            | CHAR    -> DuTerminal (DuVariable (x :?> TypedValueStorage<char>  )) :> IExpression
            | FLOAT32 -> DuTerminal (DuVariable (x :?> TypedValueStorage<single>)) :> IExpression
            | FLOAT64 -> DuTerminal (DuVariable (x :?> TypedValueStorage<double>)) :> IExpression
            | INT16   -> DuTerminal (DuVariable (x :?> TypedValueStorage<int16> )) :> IExpression
            | INT32   -> DuTerminal (DuVariable (x :?> TypedValueStorage<int32> )) :> IExpression
            | INT64   -> DuTerminal (DuVariable (x :?> TypedValueStorage<int64> )) :> IExpression
            | INT8    -> DuTerminal (DuVariable (x :?> TypedValueStorage<int8>  )) :> IExpression
            | STRING  -> DuTerminal (DuVariable (x :?> TypedValueStorage<string>)) :> IExpression
            | UINT16  -> DuTerminal (DuVariable (x :?> TypedValueStorage<uint16>)) :> IExpression
            | UINT32  -> DuTerminal (DuVariable (x :?> TypedValueStorage<uint32>)) :> IExpression
            | UINT64  -> DuTerminal (DuVariable (x :?> TypedValueStorage<uint64>)) :> IExpression
            | UINT8   -> DuTerminal (DuVariable (x :?> TypedValueStorage<uint8> )) :> IExpression
            | _       -> failwithlog "ERROR"

