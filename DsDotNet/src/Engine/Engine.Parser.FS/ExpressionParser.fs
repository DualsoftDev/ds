namespace Engine.Parser.FS

open System.Linq
open System.Collections.Generic

open Antlr4.Runtime
open Engine.Common.FS
open Engine.Core
open type exprParser
open Antlr4.Runtime.Tree

[<AutoOpen>]
module rec ExpressionParser =
    let private createParser(text:string) =
        let inputStream = new AntlrInputStream(text)
        let lexer = exprLexer (inputStream)
        let tokenStream = CommonTokenStream(lexer)
        let parser = exprParser (tokenStream)

        let listener_lexer = new ErrorListener<int>(true)
        let listener_parser = new ErrorListener<IToken>(true)
        lexer.AddErrorListener(listener_lexer)
        parser.AddErrorListener(listener_parser)
        parser

    let createExpression
        (storages:Storages)
        (ctx:ExprContext) : IExpression =

        let rec helper(ctx:ExprContext) : IExpression =
            let text = ctx.GetText()
            let expr =
                match ctx with
                | :? FunctionCallExprContext as exp ->  // functionName '(' arguments? ')'
                    tracefn $"FunctionCall: {text}"
                    let funName = exp.TryFindFirstChild<FunctionNameContext>().Value.GetText()
                    let args =
                        [
                            match exp.TryFindFirstChild<ExprListContext>() with
                            | Some exprListCtx ->
                                for exprCtx in exprListCtx.children.OfType<ExprContext>() do
                                    helper exprCtx
                            | None ->
                                ()
                        ]
                    createCustomFunctionExpression funName args

                | :? CastingExprContext as exp ->   // '(' type ')' expr
                    tracefn $"Casting: {text}"
                    let castName = exp.TryFindFirstChild<TypeContext>().Value.GetText()
                    let exprCtx = exp.TryFindFirstChild<ExprContext>().Value
                    let expr = helper exprCtx
                    createCustomFunctionExpression castName [expr]

                |(  :? BinaryExprMultiplicativeContext
                  | :? BinaryExprAdditiveContext
                  | :? BinaryExprBitwiseShiftContext
                  | :? BinaryExprRelationalContext
                  | :? BinaryExprEqualityContext
                  | :? BinaryExprBitwiseAndContext
                  | :? BinaryExprBitwiseXorContext
                  | :? BinaryExprBitwiseOrContext
                  | :? BinaryExprLogicalAndContext
                  | :? BinaryExprLogicalOrContext) ->
                    tracefn $"Binary: {text}"
                    match ctx.children.ToFSharpList() with
                    | left::op::right::[] ->
                        let expL = helper(left :?> ExprContext)
                        let expR = helper(right :?> ExprContext)
                        let op = op.GetText()
                        createBinaryExpression expL op expR
                    | _ ->
                        failwith "ERROR"


                | :? UnaryExprContext as exp ->
                    tracefn $"Unary: {text}"
                    match exp.children.ToFSharpList() with
                    | op::opnd::[] ->
                        let exp = helper(opnd :?> ExprContext)
                        let op = op.GetText()
                        createUnaryExpression op exp
                    | _ ->
                        failwith "ERROR"

                | :? TerminalExprContext as terminalExp ->
                    tracefn $"Terminal: {text}"
                    assert(terminalExp.ChildCount = 1)
                    let terminal = terminalExp.children[0].GetChild(0)
                    match terminal with
                    | :? LiteralContext as exp ->
                        assert(exp.ChildCount = 1)
                        match exp.children[0] with
                        | :? LiteralSbyteContext  -> text.Replace("y" , "")  |> System.SByte.Parse  |> literal2expr |> iexpr
                        | :? LiteralByteContext   -> text.Replace("uy", "")  |> System.Byte.Parse   |> literal2expr |> iexpr
                        | :? LiteralInt16Context  -> text.Replace("s" , "")  |> System.Int16.Parse  |> literal2expr |> iexpr
                        | :? LiteralUint16Context -> text.Replace("us", "")  |> System.UInt16.Parse |> literal2expr |> iexpr
                        | :? LiteralInt32Context  -> text                    |> System.Int32.Parse  |> literal2expr |> iexpr
                        | :? LiteralUint32Context -> text.Replace("u" , "")  |> System.UInt32.Parse |> literal2expr |> iexpr
                        | :? LiteralInt64Context  -> text.Replace("L" , "")  |> System.Int64.Parse  |> literal2expr |> iexpr
                        | :? LiteralUint64Context -> text.Replace("UL", "")  |> System.UInt64.Parse |> literal2expr |> iexpr
                        | :? LiteralSingleContext -> text.Replace("f" , "")  |> System.Single.Parse |> literal2expr |> iexpr
                        | :? LiteralDoubleContext -> text                    |> System.Double.Parse |> literal2expr |> iexpr
                        | :? LiteralBoolContext   -> text                    |> System.Boolean.Parse|> literal2expr |> iexpr
                        | :? LiteralStringContext -> text                    |> deQuoteOnDemand     |> literal2expr |> iexpr
                        | :? LiteralCharContext   ->
                            // text : "'a'" 의 형태
                            let dq, sq = "\"", "'"
                            let xxx = text |> unwrapString dq dq |> unwrapString sq sq
                            text |> unwrapString dq dq |> unwrapString sq sq |> System.Char.Parse   |> literal2expr |> iexpr
                        | _ -> failwith "ERROR"
                    | :? TagContext as texp ->
                        failwith "Obsoleted.  Why not Storage???"   // todo : remove
                        //iexpr <| tag (storages[text])
                    | :? StorageContext as sctx ->
                        let storage =
                            option {
                                let! storageCtx = sctx.TryFindFirstChild<StorageNameContext>()
                                let name = storageCtx.GetText()
                                return! storages.TryFind(name)
                            }
                        match storage with
                        | Some strg -> strg.ToBoxedExpression() :?> IExpression
                        | None -> failwith $"Failed to find variable/tag name in {sctx.GetText()}"
                    | _ ->
                        failwith "ERROR"

                | :? ArrayReferenceExprContext as exp ->
                    tracefn $"ArrayReference: {text}"
                    failwith "Not yet"

                | :? ParenthesysExprContext as exp ->
                    tracefn $"Parenthesys: {text}"
                    let exp = exp.TryFindFirstChild<ExprContext>().Value
                    helper exp

                | _ ->
                    failwith "Not yet"

            expr

        helper ctx

    let parseExpression (storages:Storages) (text:string) =
        try
            let parser = createParser (text)
            let ctx = parser.expr()

            createExpression storages ctx
        with exn ->
            failwith $"Failed to parse Expression: {text}\r\n{exn}" // Just warning.  하나의 이름에 '.' 을 포함하는 경우.  e.g "#seg.testMe!!!"


    let private getFirstChildExpressionContext (ctx:ParserRuleContext) : ExprContext = ctx.children.OfType<ExprContext>().First()

    let private (|UnitValue|_|) (x:IExpression) =
        match x.BoxedEvaluatedValue with
        | :? CountUnitType as value -> Some value
        | _ -> None
    let private (|BoolExp|) (x:IExpression) = x :?> IExpression<bool>

    let private parseCounterStatement (storages:Storages) (ctx:CounterDeclContext) : Statement =
        let typ = ctx.Descendants<CounterTypeContext>().First().GetText().ToUpper() |> DU.fromString<CounterType>
        let fail() = failwith $"Counter declaration error: {ctx.GetOriginalText()}"
        match typ with
        | Some typ ->
            let exp = createExpression storages (getFirstChildExpressionContext ctx)
            let exp = exp :?> Expression<Counter>
            let name = ctx.Descendants<StorageNameContext>().First().GetText()
            match exp  with
            | DuFunction { Name=functionName; Arguments=args } ->     // functionName = "createCTU"
                let preset, rungInCondtion =
                    match args with
                    | (UnitValue preset)::(BoolExp rungInCondtion)::_ -> preset, rungInCondtion
                    | _ -> failwith "ERROR"

                let tcParams={Storages=storages; Name=name; Preset=preset; RungInCondition=rungInCondtion; FunctionName=functionName}

                match typ, functionName, args with
                | CTU, "createWinCTU", _::_::[] ->
                    CounterStatement.CreateCTU(tcParams)
                | CTU, "createXgiCTU", _::_::(BoolExp resetCondition)::[] ->
                    CounterStatement.CreateCTU(tcParams, resetCondition)

                | CTD, "createWinCTD", _::_::[] ->
                    CounterStatement.CreateCTD(tcParams)
                | CTD, "createXgiCTD", _::_::(BoolExp resetCondition)::[] ->
                    CounterStatement.CreateXgiCTD(tcParams, resetCondition)

                | CTUD, "createWinCTUD", _::_::(BoolExp countDownCondition)::(BoolExp resetCondition)::[] ->
                    CounterStatement.CreateCTUD(tcParams, countDownCondition, resetCondition)
                | CTUD, "createXgiCTUD", _::_::(BoolExp countDownCondition)::(BoolExp resetCondition)::(BoolExp ldCondition)::[] ->
                    CounterStatement.CreateXgiCTUD(tcParams, countDownCondition, resetCondition, ldCondition)

                //| CTUD, "createCTUD", _::_::(BoolExp countDownCondition)::(UnitValue accum)::[] ->
                //    CounterStatement.CreateCTUD(tcParams, countDownCondition, accum)
                //| CTUD, "createCTUD", _::_::(BoolExp countDownCondition)::(BoolExp resetCondition)::(UnitValue accum)::[] ->
                //    CounterStatement.CreateCTUD(tcParams, countDownCondition, resetCondition, accum)

                | CTR, ("createCTR" | "createWinCTR" | "createXgiCTR"), _::_::(BoolExp resetCondition)::[] ->
                    CounterStatement.CreateXgiCTR(tcParams, resetCondition)

                | _ -> fail()

            | _ -> fail()
        | None -> fail()

    let private parseTimerStatement (storages:Storages) (ctx:TimerDeclContext) : Statement =
        let typ = ctx.Descendants<TimerTypeContext>().First().GetText().ToUpper() |> DU.fromString<TimerType>
        let fail() = failwith $"Timer declaration error: {ctx.GetText()}"
        match typ with
        | Some typ ->
            let exp = createExpression storages (getFirstChildExpressionContext ctx)
            let exp = exp :?> Expression<Timer>
            let name = ctx.Descendants<StorageNameContext>().First().GetText()
            match exp  with
            | DuFunction { Name=functionName; Arguments=args } ->     // functionName = "createTON"
                let preset, rungInCondtion =
                    match args with
                    | (UnitValue preset)::(BoolExp rungInCondtion)::_ -> preset, rungInCondtion
                    | _ -> failwith "ERROR"

                let tcParams={Storages=storages; Name=name; Preset=preset; RungInCondition=rungInCondtion; FunctionName=functionName}
                match typ, functionName, args with
                | TON, ("createXgiTON" | "createWinTON"), _::_::[] ->
                    TimerStatement.CreateTON(tcParams)
                | TOF, ("createXgiTOF" | "createWinTOF"), _::_::[] ->
                    TimerStatement.CreateTOF(tcParams)
                | RTO, ("createXgiRTO" | "createWinRTO"), _::_::[] ->
                    TimerStatement.CreateRTO(tcParams)
                //| TON, "createTON", _::_::(BoolExp resetCondition)::[] ->
                //    TimerStatement.CreateTON(tcParams, resetCondition)
                //| TOF, "createTOF", _::_::(BoolExp resetCondition)::[] ->
                //    TimerStatement.CreateTOF(tcParams, resetCondition)
                | RTO, ("createXgiRTO" | "createWinRTO"), _::_::(BoolExp resetCondition)::[] ->
                    TimerStatement.CreateRTO(tcParams, resetCondition)
                | _ -> fail()
            | _ -> fail()
        | None -> fail()


    let tryCreateStatement (storages:Storages) (ctx:StatementContext) : Statement option =
        assert(ctx.ChildCount = 1)
        let storageName = ctx.Descendants<StorageNameContext>().First().GetText()

        let optStatement =
            match ctx.children[0] with
            | :? VarDeclContext as varDeclCtx ->
                let exp = createExpression storages (getFirstChildExpressionContext varDeclCtx)
                let declType = ctx.Descendants<TypeContext>().First().GetText() |> System.Type.FromString
                if storages.ContainsKey storageName then
                    failwith $"ERROR: Duplicated variable declaration {storageName}"

                match exp.FunctionName with
                | Some functionName when functionName = "createTag" ->
                    match exp.FunctionArguments with
                    | tagAddress::tagValue::[] ->
                        if tagValue.DataType <> declType then failwith $"ERROR: Type mismatch in {varDeclCtx.GetOriginalText()}"
                        let addr = tagAddress.BoxedEvaluatedValue :?> string
                        let tag = declType.CreateTag(storageName, addr, tagValue.BoxedEvaluatedValue)
                        storages.Add(storageName, tag)
                    | _ -> failwith $"ERROR: Tag declaration error in {varDeclCtx.GetOriginalText()}"
                    None
                | _ ->
                    if exp.DataType <> declType then
                        failwith $"ERROR: Type mismatch in variable declaration {ctx.GetText()}"
                    let variable = declType.CreateVariable(storageName, exp.BoxedEvaluatedValue)
                    storages.Add(storageName, variable)
                    Some <| DuVarDecl (exp, variable)

            | :? AssignContext as assignCtx ->
                if not <| storages.ContainsKey storageName then
                    failwith $"ERROR: Failed to assign into non existing storage {storageName}"
                let storage = storages[storageName]
                let createExp ctx = createExpression storages (getFirstChildExpressionContext ctx)
                match assignCtx.children.ToFSharpList() with
                | (:? RisingAssignContext as ctx)::[] ->
                    let risingCoil:RisingCoil = {Storage = storage; HistoryFlag = HistoryFlag()}
                    Some <| DuAssign (createExp ctx, risingCoil)
                | (:? FallingAssignContext as ctx)::[] ->
                    let fallingCoil:FallingCoil = {Storage = storage; HistoryFlag = HistoryFlag()}
                    Some <| DuAssign (createExp ctx, fallingCoil)
                | (:? NormalAssignContext as ctx)::[] -> Some <| DuAssign (createExp ctx, storage)
                | _ -> failwith "ERROR"
            | :? CounterDeclContext as counterDeclCtx -> Some <| parseCounterStatement storages counterDeclCtx
            | :? TimerDeclContext as timerDeclCtx -> Some <| parseTimerStatement storages timerDeclCtx
            | :? CopyStatementContext as copyStatementCtx ->
                let expr ctx = ctx |> getFirstChildExpressionContext |> createExpression storages
                let condition = copyStatementCtx.Descendants<CopyConditionContext>().First() |> expr :?> IExpression<bool>
                let source = copyStatementCtx.Descendants<CopySourceContext>().First() |> expr
                let target = copyStatementCtx.Descendants<CopyTargetContext>().First().GetText()
                assert(target.StartsWith("$"))
                let target = storages[target.Replace("$", "")]
                Some <| DuAction (DuCopy(condition, source, target))
            | _ ->
                failwith "ERROR: Not yet statement"

        optStatement.Iter(fun st -> st.Do())
        optStatement

    let tryParseStatement (storages:Storages) (text:string) : Statement option =
        try
            let parser = createParser (text)
            let ctx = parser.statement()

            tryCreateStatement storages ctx
        with exn ->
            failwith $"Failed to parse Statement: {text}\r\n{exn}"


    let parseCode (storages:Storages) (text:string) : Statement list =
        try
            let parser = createParser (text)

            let children = parser.toplevels().children
            let topLevels =
                [
                    for t in children do
                        match t with
                        | :? ToplevelContext as ctx -> ctx
                        | :? ITerminalNode as semicolon when semicolon.GetText() = ";" -> ()
                        | _ -> failwith "ERROR"
                ]


            [
                for t in topLevels do
                    let text = t.GetOriginalText()
                    tracefn $"Toplevel: {text}"
                    assert(t.ChildCount = 1)

                    match t.children[0] with
                    | :? StatementContext as stmt -> tryCreateStatement storages stmt
                    | _ ->
                        failwith $"ERROR: {text}: expect statements"
            ] |> List.choose id
        with exn ->
            failwith $"Failed to parse code: {text}\r\n{exn}"


    type System.Type with
        member x.CreateVariable(name:string, boxedValue:obj) = fwdCreateVariableWithTypeAndValue name x ({Object = boxedValue}:BoxedObjectHolder)
        member x.CreateVariable(name:string) = fwdCreateVariableWithType name x

        member x.CreateTag(name:string, address:string, boxedValue:obj) : IStorage =
            let v = boxedValue
            match x.Name with
            | "Single" -> new PlcTag<single>(name, address, v :?> single)
            | "Double" -> new PlcTag<double>(name, address, v :?> double)
            | "SByte"  -> new PlcTag<int8>  (name, address, v :?> int8)
            | "Byte"   -> new PlcTag<uint8> (name, address, v :?> uint8)
            | "Int16"  -> new PlcTag<int16> (name, address, v :?> int16)
            | "UInt16" -> new PlcTag<uint16>(name, address, v :?> uint16)
            | "Int32"  -> new PlcTag<int32> (name, address, v :?> int32)
            | "UInt32" -> new PlcTag<uint32>(name, address, v :?> uint32)
            | "Int64"  -> new PlcTag<int64> (name, address, v :?> int64)
            | "UInt64" -> new PlcTag<uint64>(name, address, v :?> uint64)
            | "Boolean"-> new PlcTag<bool>  (name, address, v :?> bool)
            | "String" -> new PlcTag<string>(name, address, v :?> string)
            | "Char"   -> new PlcTag<char>  (name, address, v :?> char)
            | _  -> failwith "ERROR"

        member x.CreateTag(name:string, address:string) : IStorage =
            match x.Name with
            | "Single" -> new PlcTag<single>(name, address, 0.0f)
            | "Double" -> new PlcTag<double>(name, address, 0.0)
            | "SByte"  -> new PlcTag<int8>  (name, address, 0y)
            | "Byte"   -> new PlcTag<uint8> (name, address, 0uy)
            | "Int16"  -> new PlcTag<int16> (name, address, 0s)
            | "UInt16" -> new PlcTag<uint16>(name, address, 0us)
            | "Int32"  -> new PlcTag<int32> (name, address, 0)
            | "UInt32" -> new PlcTag<uint32>(name, address, 0u)
            | "Int64"  -> new PlcTag<int64> (name, address, 0L)
            | "UInt64" -> new PlcTag<uint64>(name, address, 0UL)
            | "Boolean"-> new PlcTag<bool>  (name, address, false)
            | "String" -> new PlcTag<string>(name, address, "")
            | "Char"   -> new PlcTag<char>  (name, address, ' ')
            | _  -> failwith "ERROR"


        static member FromString(typeName:string) : System.Type =
            match typeName.ToLower() with
            | ("float32" | "single") -> typedefof<single>
            | ("float64" | "double") -> typedefof<double>
            | ("int8"    | "sbyte")  -> typedefof<int8>
            | ("uint8"   | "byte")   -> typedefof<uint8>
            | ("int16"   | "short")  -> typedefof<int16>
            | ("uint16"  | "ushort") -> typedefof<uint16>
            | ("int32"   | "int" )   -> typedefof<int32>
            | ("uint32"  | "uint")   -> typedefof<uint32>
            | ("int64"   | "long")   -> typedefof<int64>
            | ("uint64"  | "ulong")  -> typedefof<uint64>
            | ("bool"    | "boolean")-> typedefof<bool>
            | "string"               -> typedefof<string>
            | "char"                 -> typedefof<char>
            | _  -> failwith "ERROR"
