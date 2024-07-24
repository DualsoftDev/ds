namespace rec Engine.Core
open System
open System.Diagnostics
open Dual.Common.Core.FS
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
    let private unsupported() = failwithlog "ERROR: not supported"
    

    [<DebuggerDisplay("{ToText()}")>]
    type Terminal<'T when 'T:equality> =
        | DuVariable of TypedValueStorage<'T>
        | DuLiteral of LiteralHolder<'T>        // Literal 도 IExpressionizableTerminal 구현해야 하므로, 'T 대신 LiteralHolder<'T> 사용
        interface ITerminal with
            member x.Variable = match x with | DuVariable variable -> Some variable | _ -> None
            member x.Literal  = match x with | DuLiteral literal   -> Some literal  | _ -> None
        interface IExpression<'T> with
            member x.DataType = typeof<'T>
            member x.BoxedEvaluatedValue = match x with | DuVariable v -> v.Value | DuLiteral l -> l.Value |> box
            member x.GetBoxedRawObject() = (x :> IExpression).BoxedEvaluatedValue
            member x.ToText() = (x :> IExpression).ToText(false)
            member x.ToText(_withParenthesis) = match x with | DuVariable v -> v.ToText() | DuLiteral l -> l.Value.ToString()
            member x.CollectStorages() = match x with | DuVariable v -> [v] | _ -> []
            member x.Flatten() = fwdFlattenExpression x
            member x.IsEqual y = match x with | DuVariable v -> v.Value = unbox y.BoxedEvaluatedValue | DuLiteral l -> l.Value = unbox y.BoxedEvaluatedValue

            member x.FunctionName = None
            member x.FunctionSpec = None
            member x.FunctionArguments = []
            member x.WithNewFunctionArguments _args = failwithlog "ERROR"
            member x.Terminal = Some x
            member x.EvaluatedValue = x.Evaluate()

    [<DebuggerDisplay("{ToText(true)}")>]
    type Expression<'T when 'T:equality> =
        | DuTerminal of Terminal<'T>
        | DuFunction of FunctionSpec<'T>  //FunctionBody:(Arguments -> 'T) * Name * Arguments
        interface IExpression<'T> with
            member x.DataType = x.DataType 
            member x.EvaluatedValue = x.Evaluate()
            member x.BoxedEvaluatedValue = x.Evaluate() |> box
            member x.GetBoxedRawObject() = x.GetBoxedRawObject()
            member x.ToText() = x.ToText(false)
            member x.ToText(withParenthesis) = x.ToText(withParenthesis)
            member x.CollectStorages() = x.CollectStorages()
            member x.Flatten() = fwdFlattenExpression x
            member x.IsEqual y = x.IsEqual y

            (* 'T type DU 를 접근하기 위한 members *)
            member x.FunctionName = x.FunctionName
            member x.FunctionArguments = x.FunctionArguments
            member x.FunctionSpec = x.FunctionSpec
            member x.Terminal = match x with | DuTerminal t -> Some t | _ -> None
            member x.WithNewFunctionArguments(args) =
                match x with
                | DuFunction fs -> DuFunction {fs with Arguments = args }
                | _ -> failwithlog "ERROR"

        member x.DataType =
               match x with
                | DuTerminal t ->
                    match t with
                    | DuLiteral literal -> literal.Value.GetType()
                    | _ -> typedefof<'T>
                | _ -> typedefof<'T>
        member x.FunctionSpec =
            match x with
            | DuFunction fs -> Some (fs :> IFunctionSpec)
            | _ -> None

        /// expression 의 type 이 동일한 경우 ToString() 결과가 같으면 동일한 것으로 간주
        /// type 이 다르면 항상 false 반환
        member x.IsEqual (y:IExpression) =
            if x.GetType() = y.GetType() then
                x.ToText() = y.ToText()
            else
                false

    /// literal 'T 로부터 terminal Expression<'T> 생성
    let literal2expr (x:'T) =
        let t = x.GetType()
        if t.IsValueType || t = typedefof<string> then
            DuTerminal (DuLiteral ({Value = (x|> unbox)}:LiteralHolder<'T>))
        else
            failwithlog "ERROR: Value Type Error.  only allowed for primitive type"

    let any2expr (value:obj) : IExpression =
        let inline toExpr v = literal2expr v :> IExpression
        match value with
        | :? bool   as v -> toExpr v
        | :? byte   as v -> toExpr v
        | :? double as v -> toExpr v
        | :? int16  as v -> toExpr v
        | :? int32  as v -> toExpr v
        | :? int64  as v -> toExpr v
        | :? sbyte  as v -> toExpr v
        | :? single as v -> toExpr v
        | :? uint16 as v -> toExpr v
        | :? uint32 as v -> toExpr v
        | :? uint64 as v -> toExpr v
        | _ ->
            failwith "ERROR"

    /// Tag<'T> or Variable<'T> 로부터 Expression<'T> 생성
    let var2expr (t: TypedValueStorage<'T>): Expression<'T> = DuTerminal (DuVariable t)

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

        ///// XGK 에서는 사용하는 timer 의 timer resolution 을 곱해서 실제 preset 값을 계산해야 한다.
        //member val XgkTimerResolution = 1.0 with get, set
        ///// XGK 에서 사전 설정된 timer resolution 을 고려해서 실제 preset 값을 계산
        //member x.CalculateXgkTimerPreset() = int ( (float timerStruct.PRE.Value) / x.XgkTimerResolution)

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
        /// Accumulated
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

    type CounterStatement with
        /// CounterStatement 의 UpCondition 또는 DownCondition 반환
        member x.GetUpOrDownCondition() : IExpression<bool> = [x.DownCondition; x.UpCondition] |> List.choose id |> List.exactlyOne


    type IParserData =
        /// UDT 구조체 멤버 값 복사.  source 및 target 이 string 으로 주어진다. (e.g "people[0]", "hong")
        /// PC 버젼에서 UDT 변수 복사에 대한 실제 실행문.
        abstract member CopyUdt: UdtDecl * string * string -> unit

    type CopyUdtStatement = {
        Storages:Storages
        UdtDecl:UdtDecl
        Condition:IExpression<bool>
        Source:string
        Target:string
    }

    type ActionStatement =
        | DuCopy of condition:IExpression<bool> * source:IExpression * target:IStorage
        | DuCopyUdt of CopyUdtStatement

   
    type FunctionParameters = {
        /// Enable bit
        Condition:IExpression<bool> option

        FunctionName:string
        Arguments:Arguments
        OriginalExpression:IExpression
        /// Function output store target
        Output:IStorage
    }

    // e.g struct Person { string name; int age; };
    type UdtDecl = {
        TypeName:string
        Members:TypeDecl list
    }

    // e.g Person people[10];
    type UdtDef = {
        TypeName:string
        VarName:string
        /// Array 가 아닌 경우, 1 의 값을 가짐.  array 인 경우 1보다 큰 값.  array index 는 0 부터 시작
        ArraySize:int
    }


    type ProcDecl = {
        ProcName:string
        Arguments:TypeDecl list
        Bodies:StatementContainer
    }

    type ProcCall = {
        ProcDecl:ProcDecl
        ActuralPrameters:Arguments
    }

    type Statement =
        /// 변수 선언.  e.g "int a = $pi + 3;"  초기값 처리에 주의
        ///
        /// XGI : 선언한 변수의 초기값으로 설정.  Rung 생성에는 관여되지 않음
        /// XGK : _1ON 을 조건으로 한 대입문으로 rung 생성.  (XGK 에서는 변수 초기값이 지원되지 않음)
        ///
        /// Ladder 생성 시점에는 DuVarDecl statement 는 존재하지 않는다.  변수 선언 혹은 assign 문으로 사전에 변환된다.
        | DuVarDecl of expression:IExpression * variable:IStorage

        /// User Defined Type (structure) 선언.  e.g "struct Person { string name; int age; };"
        | DuUdtDecl of UdtDecl

        | DuLambdaDecl of LambdaDecl
        | DuProcDecl of ProcDecl
        | DuProcCall of ProcCall

        /// UDT instances 정의.  e.g "Person peopole[10];"
        | DuUdtDef of UdtDef

        /// 대입문.  e.g "$a = $b + 3;"
        ///
        /// condition 조건이 만족할 경우, expression 을 target 에 대입.  condition None 인 경우, _ON 의 의미.
        | DuAssign of condition:IExpression<bool> option * expression:IExpression * target:IStorage

        | DuTimer   of TimerStatement
        | DuCounter of CounterStatement
        | DuAction  of ActionStatement

        /// PLC function (비교, 사칙연산, Move) 을 호출하는 statement
        ///
        /// - 주로 XGI 에서 사용.  XGK 에서는 zipAndExpression 와 Statement.ToStatements() 에서 사용.  Statement.ToStatements() 사용은 DuAction(DuCopy...) 부분인데, 대체 가능함.
        | DuPLCFunction of FunctionParameters

    /// 추가 가능한 Statement container
    type StatementContainer = ResizeArray<Statement>

    type CommentedStatement =
        | CommentedStatement of comment:string * statement:Statement
        member x.Statement = match x with | CommentedStatement (_c, s) -> s
        member x.TargetName =
            match x.Statement with
            | DuAssign (_, _expression, target) -> target.Name
            | DuVarDecl (_expression,variable) -> variable.Name
            | DuTimer (t:TimerStatement) -> t.Timer.Name
            | DuCounter (c:CounterStatement) -> c.Counter.Name
            | DuAction (a:ActionStatement) ->
               match a with
               | DuCopy (_condition:IExpression<bool>, _source:IExpression,target:IStorage)-> target.Name
               | DuCopyUdt { Target = target } -> target
            | DuPLCFunction { FunctionName = fn } -> fn
            | (DuUdtDecl _ | DuUdtDef _) -> failwith "Unsupported.  Should not be called for these statements"
            | (DuLambdaDecl _ | DuProcDecl _ | DuProcCall _) ->
                failwith "ERROR: Not yet implemented"       // 추후 subroutine 사용시, 필요에 따라 세부 구현

        member x.TargetValue =
            match x.Statement with
            | DuAssign (_, _expression, target) -> target.BoxedValue
            | DuVarDecl (_expression,variable) -> variable.BoxedValue
            | DuTimer (t:TimerStatement) -> t.Timer.DN.Value
            | DuCounter (c:CounterStatement) -> c.Counter.DN.Value
            | DuAction (a:ActionStatement) ->
                match a with
                | DuCopy (_condition:IExpression<bool>, _source:IExpression,target:IStorage)-> target.BoxedValue
                | DuCopyUdt _ -> failwith "ERROR: Invalid value reference"
            | DuPLCFunction { OriginalExpression = exp } ->  exp.BoxedEvaluatedValue
            | (DuUdtDecl _ | DuUdtDef _) -> failwith "Unsupported.  Should not be called for these statements"
            | (DuLambdaDecl _ | DuProcDecl _ | DuProcCall _) ->
                failwith "ERROR: Not yet implemented"       // 추후 subroutine 사용시, 필요에 따라 세부 구현

    let (|CommentAndStatement|) = function | CommentedStatement(x, y) -> x, y
    let commentAndStatement = (|CommentAndStatement|)
    let withNoComment statement = CommentedStatement("", statement)
    let withExpressionComment (append:string) (statement: Statement) =
        CommentedStatement(append, statement)

    /// UDT 구조체 멤버 값 복사.  source 및 target 이 string 으로 주어진다. (e.g "people[0]", "hong")
    /// PC 버젼에서는 UDT 변수 복사에 대한 실제 실행문.
    let copyUdt (storages:Storages) (decl:UdtDecl) (source:string) (target:string): unit =
        for m in decl.Members do
            let s, t = $"{source}.{m.Name}", $"{target}.{m.Name}"
            storages[t].BoxedValue <- storages[s].BoxedValue

    type Statement with
        member x.Do() =
            match x with
            | DuAssign (_, expr, target) ->
                if target.DataType <> expr.DataType then
                    failwith $"ERROR: {target.Name} Type mismatch in assignment statement"
                target.BoxedValue <- expr.BoxedEvaluatedValue

            | DuVarDecl (expr, target) ->
                if target.DataType <> expr.DataType then
                    failwith $"ERROR: {target.Name} Type mismatch in assignment statement"
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

            | DuAction (DuCopyUdt { Storages=storages; UdtDecl=udtDecl; Condition=condition; Source=source; Target=target}) ->
                if condition.EvaluatedValue then
                    // 구조체 멤버 복사
                    copyUdt storages udtDecl source target

            | (DuUdtDecl _ | DuUdtDef _ | DuLambdaDecl _ | DuProcDecl _) -> ()  // OK: Noting todo

            | DuProcCall _ ->
                failwithlog "ERROR: Procedure call not yet implemented."

            | DuPLCFunction _ ->
                failwithlog "ERROR"

        member x.ToText() =
            match x with
            | DuAssign (_condition, expr, target) -> $"{target.ToText()} = {expr.ToText()};"    // todo: condition 을 totext 에 포함할지 여부
            | DuVarDecl (expr, var) -> $"{var.DataType.ToDsDataTypeString()} {var.Name} = {expr.ToText()};"
            | DuTimer timerStatement ->
                let ts, t = timerStatement, timerStatement.Timer
                let typ = t.Type.ToString()
                let functionName = ts.FunctionName  // e.g "createTON"
                let args = [    // [preset; rung-in-condition; (reset-condition)]
                    sprintf "%A" t.PRE.Value
                    match ts.RungInCondition with | Some c -> c.ToText() | None -> ()
                    match ts.ResetCondition  with | Some c -> c.ToText() | None -> () ]
                let args = String.Join(", ", args)
                $"{typ.ToLower()} {t.Name} = {functionName}({args});"

            | DuCounter counterStatement ->
                let cs, c = counterStatement, counterStatement.Counter
                let typ = c.Type.ToString()
                let functionName = cs.FunctionName  // e.g "createCTU"
                let args = [    // [preset; up-condition; (down-condition;) (reset-condition;) (accum;)]
                    sprintf "%A" c.PRE.Value
                    match cs.UpCondition    with | Some c -> c.ToText() | None -> ()
                    match cs.DownCondition  with | Some c -> c.ToText() | None -> ()
                    match cs.ResetCondition with | Some c -> c.ToText() | None -> ()
                    if c.ACC.Value <> 0u then
                        sprintf "%A" c.ACC.Value ]
                let args = String.Join(", ", args)
                $"{typ.ToLower()} {c.Name} = {functionName}({args});"
            | DuAction (DuCopy (condition, source, target)) ->
                $"copyIf({condition.ToText()}, {source.ToText()}, {target.ToText()});"

            | DuAction (DuCopyUdt { Condition=condition; Source=source; Target=target}) ->
                $"copyStructIf({condition.ToText()}, {source}, {target})"

            | DuUdtDecl _ -> sprintf "%A" x
            | DuUdtDef _ -> sprintf "%A;" x
            | DuLambdaDecl { Prototype=proto; Arguments=args;Body = exp} ->
                let argLists = args |> map (fun arg -> $"{arg.Type.Name} {arg.Name}") |> joinWith ", "
                $"{proto.Type.Name} {proto.Name}({argLists} => {exp.ToText()})"

            | DuPLCFunction {FunctionName = fn; Arguments=fargs; Output=output } ->
                let args = fargs |> map (fun arg -> arg.ToText()) |> joinWith ", "
                $"PLCFunction {output.ToText()} = {fn}({args})"
            | _ ->
                failwithlog "ERROR"

        member x.IsDuCaseVarDecl() = match x with | DuVarDecl _ -> true | _ -> false

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
            | DuLiteral v -> literal2Text v.Value

    type Expression<'T when 'T:equality> with
        member x.GetBoxedRawObject() =  // return type:obj    return type 명시할 경우, 다음 compile error 발생:  error FS1198: 제네릭 멤버 'ToText'이(가) 이 프로그램 지점 전의 비균일 인스턴스화에 사용되었습니다. 이 멤버가 처음에 오도록 멤버들을 다시 정렬해 보세요. 또는, 인수 형식, 반환 형식 및 추가 제네릭 매개 변수와 제약 조건을 포함한 멤버의 전체 형식을 명시적으로 지정하세요.
            match x with
            | DuTerminal b -> b.GetBoxedRawObject()
            | DuFunction fs -> fs |> box

        member x.Evaluate(): 'T =
            match x with
            | DuTerminal b -> b.Evaluate()
            | DuFunction fs ->
                match fs.LambdaApplication with
                | Some la ->
                    // Lambda 식 평가는, formal parameter 에 해당하는 값에 actual parameter 값을 저장한 후 수행
                    // e.g "int sum(int a, int b) => $a + $b;  $all = sum(1, 2);"
                    //      => _local_sum_a = 1, _local_+sum_b = 2 의 값을 storage 에 저장 한 후 sum 함수 실행
                    let ld = la.LambdaDecl
                    let funName = ld.Prototype.Name
                    assert (la.Arguments.Length = ld.Arguments.Length)
                    for i in [0..la.Arguments.Length-1] do
                        let declVarName = ld.Arguments[i].Name   // a
                        let encryptedFormalParamName = getFormalParameterName funName declVarName    // _local_sum_a
                        let actualParamValue = la.Arguments.[i].BoxedEvaluatedValue
                        la.Storages[encryptedFormalParamName].BoxedValue <- actualParamValue
                | None ->
                    ()

                fs.FunctionBody fs.Arguments

        member x.FunctionName =
            match x with
            | DuTerminal _ -> None
            | DuFunction fs -> Some fs.Name

        member x.FunctionArguments:IExpression list =
            match x with
            | DuFunction fs -> fs.Arguments
            | DuTerminal _ -> []

        member x.ToText(?withParenthesis:bool) =
            let withParenthesis = withParenthesis |? false
            match x with
            | DuTerminal b -> b.ToText()
            | DuFunction fs ->
                let text = fwdSerializeFunctionNameAndBoxedArguments fs.Name fs.Arguments fs.LambdaApplication withParenthesis
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
            | INT8    -> "int8"
            | INT16   -> "int16"
            | INT32   -> "int32"
            | INT64   -> "int64"
            | UINT8   -> "uint8"
            | UINT16  -> "int16"
            | UINT32  -> "uint32"
            | UINT64  -> "uint64"
            | STRING  -> "string"
            | _  -> failwithlog "ERROR"


    type IStorage with
        member x.ToExpression():IExpression =
            match x.DataType.Name with
            | BOOL    -> DuTerminal (DuVariable (x :?> TypedValueStorage<bool>  )) :> IExpression
            | CHAR    -> DuTerminal (DuVariable (x :?> TypedValueStorage<char>  )) :> IExpression
            | FLOAT32 -> DuTerminal (DuVariable (x :?> TypedValueStorage<single>)) :> IExpression
            | FLOAT64 -> DuTerminal (DuVariable (x :?> TypedValueStorage<double>)) :> IExpression
            | INT8    -> DuTerminal (DuVariable (x :?> TypedValueStorage<int8>  )) :> IExpression
            | INT16   -> DuTerminal (DuVariable (x :?> TypedValueStorage<int16> )) :> IExpression
            | INT32   -> DuTerminal (DuVariable (x :?> TypedValueStorage<int32> )) :> IExpression
            | INT64   -> DuTerminal (DuVariable (x :?> TypedValueStorage<int64> )) :> IExpression
            | UINT8   -> DuTerminal (DuVariable (x :?> TypedValueStorage<uint8> )) :> IExpression
            | UINT16  -> DuTerminal (DuVariable (x :?> TypedValueStorage<uint16>)) :> IExpression
            | UINT32  -> DuTerminal (DuVariable (x :?> TypedValueStorage<uint32>)) :> IExpression
            | UINT64  -> DuTerminal (DuVariable (x :?> TypedValueStorage<uint64>)) :> IExpression
            | STRING  -> DuTerminal (DuVariable (x :?> TypedValueStorage<string>)) :> IExpression
            | _       -> failwithlog "ERROR"

