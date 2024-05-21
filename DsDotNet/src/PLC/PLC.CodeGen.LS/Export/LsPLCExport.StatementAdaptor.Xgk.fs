namespace PLC.CodeGen.LS


open Engine.Core
open Dual.Common.Core.FS
open System

[<AutoOpen>]
module XgkTypeConvertorModule =
    type XgkTimerCounterStructResetCoil(tc:TimerCounterBaseStruct) =
        inherit TimerCounterBaseStruct(None, tc.Name, tc.DN, tc.PRE, tc.ACC, tc.RES, (tc :> IStorage).DsSystem)
        interface INamedExpressionizableTerminal with
            member x.StorageName = tc.Name


    /// {prefix}{Op}{suffix} 형태로 반환.  e.g "DADDU" : "{D}{ADD}{U}" => DWORD ADD UNSIGNED
    ///
    /// - prefix: "D" for DWORD, "R" for REAL, "L" for LONG REAL, "$" for STRING
    ///
    /// suffix: "U" for UNSIGNED
    let operatorToXgkFunctionName op (typ:Type) =
        let isComparison =
            match op with
            | (">" | ">=" | "<"  | "<="  | "==" | "!=" | "<>" )-> true
            | _ -> false

        let prefix =
            match typ with
            | _ when typ = typeof<byte> ->  // "S"       //"S" for short (1byte)
                failwith $"byte (sint) type operation {op} is not supported in XGK"     // byte 연산 지원 여부 확인 필요
            | _ when typ.IsOneOf(typeof<int32>, typeof<uint32>) -> "D"
            | _ when typ = typeof<single> -> "R"     //"R" for real
            | _ when typ = typeof<double> -> "L"     //"L" for long real
            | _ when typ = typeof<string> -> "$"     //"$" for string
            | _ when typ.IsOneOf(typeof<char>, typeof<int64>, typeof<uint64>) -> failwith "ERROR: type mismatch for XGK"
            | _ -> ""

        let unsigned =
            match typ with
            | _ when typ.IsOneOf(typeof<uint16>, typeof<uint32>) && op <> "MOV" -> "U"  // MOVE 는 "MOVU" 등이 없다.  size 만 중요하지 unsigned 여부는 중요하지 않다.
            | _ -> ""

        let opName =
            match op with
            | "+" -> "ADD"
            | "-" -> "SUB"
            | "*" -> "MUL"
            | "/" -> "DIV"
            | "MOV" -> "MOV"
            | "!=" -> "<>"
            | "==" -> "="
            | _ when isComparison -> op
            | _ -> failwithlog "ERROR"

        if isComparison then
            $"{unsigned}{prefix}{opName}"       // e.g "UD<="
        else
            $"{prefix}{opName}{unsigned}"


    /// exp 내에 포함된, {문장(statement)으로 추출 해야만 할 요소}를 newStatements 에 추가한다.
    /// 이 과정에서 추가할 필요가 있는 storate 는 newLocalStorages 에 추가한다.
    /// 반환 : exp, 추가된 storage, 추가된 statement
    ///
    /// e.g: XGK 의 경우, 함수를 지원하지 않으므로,
    ///     입력 exp: "2 + 3 > 4"
    ///     추가 statement : "tmp1 = 2 + 3"
    ///     추가 storage : tmp2
    ///     최종 exp: "tmp1 > 4"
    ///     반환 : exp, [tmp2], [tmp1 = 2 + 3]
    let exp2expXgk (prjParam: XgxProjectParams) (exp: IExpression, expStore:IStorage option) : IExpression * IStorage list * Statement list  =
        assert (prjParam.TargetType = XGK)
        let rec helper (nestLevel:int) (exp: IExpression, expStore:IStorage option) : IExpression * IStorage list * Statement list =
            match exp.FunctionName, exp.FunctionArguments with
            | Some fn, l::r::[] ->
                let lexpr, lstgs, lstmts = helper (nestLevel + 1) (l, None)
                let rexpr, rstgs, rstmts = helper (nestLevel + 1) (r, None)

                if fn.IsOneOf("!=", "==", "<>") && lexpr.DataType = typeof<bool> then
                    // XGK 에는 bit 의 비교 연산이 없다.  따라서, bool 타입의 비교 연산을 수행할 경우, 이를 OR, AND 로 변환한다.
                    let l, r, nl, nr = lexpr, rexpr, fbLogicalNot [lexpr], fbLogicalNot [rexpr]
                    let newExp =
                        match fn with
                        | ("!=" | "<>") -> fbLogicalOr([fbLogicalAnd [l; nr]; fbLogicalAnd [nl; r]])
                        | "==" -> fbLogicalOr([fbLogicalAnd [l; r]; fbLogicalAnd [nl; nr]])
                        | _ -> failwithlog "ERROR"
                    newExp, (lstgs @ rstgs), (lstmts @ rstmts)
                else
                    // XGK 에는 IEC Function 을 이용할 수 없으므로, 수식 내에 포함된 사칙 연산이나 비교 연산을 XGK function 으로 변환한다.
                    let newExp = exp.WithNewFunctionArguments [lexpr; rexpr]
                    let createTmpStorage =
                        fun () -> 
                            match expStore with
                            | Some stg -> stg
                            | None ->
                                let tmpNameHint = operatorToMnemonic fn
                                let tmpVar = createTypedXgxAutoVariable prjParam tmpNameHint exp.BoxedEvaluatedValue $"{exp.ToText()}"
                                tmpVar :> IStorage

                    match fn with
                    | ("+" | "-" | "*" | "/")
                    | (">" | ">=" | "<" | "<=" | "==" | "!=") ->
                        let stg = createTmpStorage()
                        let stmt = DuAssign(None, newExp, stg)
                        let varExp = stg.ToExpression()
                        varExp, (lstgs @ rstgs @ [ stg ]), (lstmts @ rstmts @ [ stmt ])
                    | _ ->
                        if lstgs.any() || rstgs.any() then
                            newExp, (lstgs @ rstgs), (lstmts @ rstmts)
                        else
                            exp, [], []
            | _ ->
                exp, [], []

        if exp.Terminal.IsSome then
            exp, [], []
        else
            helper 0 (exp, expStore)

    /// XGK 전용 Statement 확장
    let rec internal statement2XgkStatements (prjParam: XgxProjectParams) (newLocalStorages: XgxStorage) (statement: Statement) : Statement list =
        let newStatements =
            match statement with
            | DuAssign(condition, exp, target) ->
                let exp2, stgs, stmts = exp2expXgk prjParam (exp, Some target)
                newLocalStorages.AddRange(stgs)
                let duplicated =
                    option {
                        let! terminal = exp2.Terminal
                        let! variable = terminal.Variable
                        return variable = target
                    } |> Option.defaultValue false

                if stmts.any() && (exp = exp2 || duplicated) then
                    stmts
                else
                    let newStatement = DuAssign(condition, exp2, target)
                    stmts @ statement2XgxStatements prjParam newLocalStorages newStatement


            // e.g: XGK 에서 bool b3 = $nn1 > $nn2; 와 같은 선언의 처리.  다음과 같이 2개의 문장으로 분리한다.
            // bool b3;
            // b3 = $nn1 > $nn2;
            | DuVarDecl(exp, decl) when exp.Terminal.IsNone ->
                newLocalStorages.Add decl
                let stmt = DuAssign(Some systemOnRising, exp, decl)
                statement2XgkStatements prjParam newLocalStorages stmt

            | DuTimer tmr when tmr.ResetCondition.IsSome -> //|| tmr.RungInCondition.IsSome ->
                let reset = tmr.ResetCondition |> map (fun r -> DuAssign(None, tmr.ResetCondition.Value, new XgkTimerCounterStructResetCoil(tmr.Timer.TimerStruct)))

                // XGI timer 의 RST 조건을 XGK 에서는 Reset rung 으로 분리한다.
                let resetStatement = DuAssign(None, tmr.ResetCondition.Value, new XgkTimerCounterStructResetCoil(tmr.Timer.TimerStruct))
                [ statement; resetStatement ]

            | DuTimer _  -> [ statement ]

            | DuCounter ctr ->
                let statements = ResizeArray<Statement>([statement])
                // XGI counter 의 LD(Load) 조건을 XGK 에서는 Reset rung 으로 분리한다.
                let resetCoil = new XgkTimerCounterStructResetCoil(ctr.Counter.CounterStruct)
                let typ = ctr.Counter.Type
                match typ with
                | CTD -> DuAssign(None, ctr.LoadCondition.Value, resetCoil) |> statements.Add
                | (CTR|CTU|CTUD) -> DuAssign(None, ctr.ResetCondition.Value, resetCoil) |> statements.Add

                if typ = CTUD then
                    let mutable newStatement = statement
                    let mutable newCtr = ctr

                    // newStatementGenerator : fun () -> DuCounter({ ctr with UpCondition = Some ldVarExp })
                    let replaceComplexCondition (_ctr: CounterStatement) (cond:IExpression<bool>) (newStatementGenerator:IExpression<bool> -> Statement) =
                        let assignStatement, ldVar = cond.ToAssignStatementAndAuotVariable prjParam
                        statements.Add assignStatement
                        newLocalStorages.Add ldVar

                        let ldVarExp = ldVar.ToExpression() :?> IExpression<bool>
                        newStatement <- newStatementGenerator(ldVarExp)
                        match newStatement with
                        | DuCounter ctr -> newCtr <- ctr
                        | _ -> failwithlog "ERROR"

                        statements[0] <- newStatement


                    match newCtr.UpCondition with
                    | Some cond when cond.Terminal.IsNone ->
                        replaceComplexCondition newCtr cond (fun ldVarExp -> DuCounter({ newCtr with UpCondition = Some ldVarExp }))
                    | _ -> ()

                    match newCtr.DownCondition with
                    | Some cond when cond.Terminal.IsNone ->
                        replaceComplexCondition newCtr cond (fun ldVarExp -> DuCounter({ newCtr with DownCondition = Some ldVarExp }))
                    | _ -> ()

                    (* XGK CTUD 에서 load : 별도의 statement 롭 분리: ldcondition --- MOV PV C0001  *)
                    match newCtr.LoadCondition with
                    | Some cond ->
                        DuAction(DuCopy(cond, literal2expr(ctr.Counter.PRE.Value), ctr.Counter.CounterStruct)) |> statements.Add
                    | _ -> ()

                statements.ToFSharpList()

            | _ ->
                // 공용 처리
                statement2XgxStatements prjParam newLocalStorages statement

        newStatements |> List.ofSeq

