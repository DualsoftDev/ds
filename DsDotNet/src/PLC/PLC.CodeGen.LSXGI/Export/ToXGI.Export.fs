namespace PLC.CodeGen.LSXGI

open System.Linq

open Engine.Common.FS
open Engine.Core
open PLC.CodeGen.LSXGI

open PLC.CodeGen.Common

[<AutoOpen>]
module LsXGI =

    /// (조건=coil) seq 로부터 rung xml 들의 string 을 생성
    let internal generateRungs (prologComment:string) (commentedStatements:CommentedXgiStatements seq) : XmlOutput =
        let xmlRung (expr:FlatExpression option) xgiCommand y : RungGenerationInfo =
            let {Coordinate=c; Xml=xml} = rung (0, y) expr xgiCommand
            let yy = c / 1024
            { Xmls = [$"\t<Rung BlockMask={dq}0{dq}>\r\n{xml}\t</Rung>"]; Y = yy}

        let mutable rgi:RungGenerationInfo = {Xmls = []; Y = 0}

        // Prolog 설명문
        if prologComment.NonNullAny() then
            let xml = getCommentRung rgi.Y prologComment
            rgi <- rgi.Add(xml)

        let simpleRung (expr:IExpression) (target:IStorage) =
            let coil =
                match target with
                | :? RisingCoil as rc -> COMPulseCoil(rc.Storage :?> INamedExpressionizableTerminal)
                | :? FallingCoil as fc -> COMNPulseCoil(fc.Storage :?> INamedExpressionizableTerminal)
                | _ -> COMCoil(target :?> INamedExpressionizableTerminal)
            let flatExpr = expr.Flatten() :?> FlatExpression
            let command = CoilCmd(coil)
            let rgiSub = xmlRung (Some flatExpr) (Some command) rgi.Y
            //rgi <- {Xmls = rgiSub.Xmls @ rgi.Xmls; Y = rgi.Y + rgiSub.Y}
            rgi <- {Xmls = rgiSub.Xmls @ rgi.Xmls; Y = rgiSub.Y}

        // Rung 별로 생성
        for CommentAndXgiStatements(cmt, stmts) in commentedStatements do

            // 다중 라인 설명문을 하나의 설명문 rung 에..
            if cmt.NonNullAny() then
                let xml = getCommentRung rgi.Y cmt
                rgi <- rgi.Add(xml)
            for stmt in stmts do
                match stmt with
                | DuAssign (expr, target) -> simpleRung expr target
                | DuAugmentedPLCFunction ({FunctionName = ("&&"|"||") as op; Arguments = args; Output=output }) ->
                    let psedoFunction (args:Args):bool = failwithlog "THIS IS PSEUDO FUNCTION.  SHOULD NOT BE EVALUATED!!!!"
                    let expr = DuFunction { FunctionBody=psedoFunction; Name=op; Arguments=args }
                    simpleRung expr (output :?> IStorage)


                // <kwak> <timer>
                | Statement.DuTimer timerStatement ->
                    let command = FunctionBlockCmd(TimerMode(timerStatement))
                    let rgiSub = xmlRung None (Some command) rgi.Y
                    rgi <- {Xmls = rgiSub.Xmls @ rgi.Xmls; Y = rgi.Y + rgiSub.Y}

                | Statement.DuCounter counterStatement ->
                    let command = FunctionBlockCmd(CounterMode(counterStatement))
                    let rgiSub = xmlRung None (Some command) rgi.Y
                    rgi <- {Xmls = rgiSub.Xmls @ rgi.Xmls; Y = rgi.Y + rgiSub.Y}

                | DuAugmentedPLCFunction ({FunctionName = (">"|">="|"<"|"<="|"="|"!=") as op; Arguments = args; Output=output }) ->
                    let fn = operatorToXgiFunctionName op
                    let command = PredicateCmd(Compare(fn, output, args))
                    let rgiSub = xmlRung None (Some command) rgi.Y
                    rgi <- {Xmls = rgiSub.Xmls @ rgi.Xmls; Y = (*rgi.Y +*) 1+rgiSub.Y}

                | DuAugmentedPLCFunction ({FunctionName = ("+"|"-"|"*"|"/") as op; Arguments = args; Output=output }) ->
                    let fn = operatorToXgiFunctionName op
                    let command = FunctionCmd(Arithmatic(fn, output, args))
                    let rgiSub = xmlRung None (Some command) rgi.Y
                    rgi <- {Xmls = rgiSub.Xmls @ rgi.Xmls; Y = (*rgi.Y +*) 1+rgiSub.Y}
                | DuAugmentedPLCFunction ({FunctionName = XgiConstants.FunctionNameMove as func; Arguments = args; Output=output }) ->
                    let condition = args[0] :?> IExpression<bool>
                    let source = args[1]
                    let target = output :?> IStorage
                    let command = ActionCmd(Move(condition, source, target))
                    let rgiSub = xmlRung None (Some command) rgi.Y
                    rgi <- {Xmls = rgiSub.Xmls @ rgi.Xmls; Y = (*rgi.Y +*) 1+rgiSub.Y}
                | _ ->
                    failwithlog "Not yet"

        let rungEnd = generateEnd (rgi.Y + 1)
        rgi <- rgi.Add(rungEnd)
        rgi.Xmls |> List.rev |> String.concat "\r\n"

    let internal generateXgiXmlFromStatement
        (prologComment:string) (commentedStatements:CommentedXgiStatements seq)
        (xgiSymbols:XgiSymbol seq) (existingLSISprj:string option)
      =
        let symbolInfos = xgiSymbolsToSymbolInfos xgiSymbols

        /// Symbol table 정의 XML 문자열
        let symbolsLocalXml = XGITag.generateLocalSymbolsXml symbolInfos

        let globalSym = [
            for s in symbolInfos do
                if not (s.Device.IsNullOrEmpty()) then
                    XGITag.copyLocal2GlobalSymbol s
        ]

        let symbolsGlobalXml = XGITag.generateGlobalSymbolsXml globalSym

        let rungsXml = generateRungs prologComment commentedStatements

        logInfo "Finished generating PLC code."
        wrapWithXml rungsXml symbolsLocalXml symbolsGlobalXml existingLSISprj


    let internal commentedStatementsToCommentedXgiStatements
        (storages:IStorage seq)
        (commentedStatements:CommentedStatement list)
        : IStorage list * CommentedXgiStatements list
      =
        (* Timer 및 Counter 의 Rung In Condition 을 제외한 부수의 조건들이 직접 tag 가 아닌 condition expression 으로
            존재하는 경우, condition 들을 임시 tag 에 assign 하는 rung 으로 분리해서 저장.
            => 새로운 임시 tag 와 새로운 임시 tag 에 저장하기 위한 rung 들이 추가된다.
        *)

        let newCommentedStatements = ResizeArray<CommentedXgiStatements>()
        let newStorages = ResizeArray<IStorage>(storages)
        for cmtSt in commentedStatements do
            let xgiCmtStmts = commentedStatement2CommentedXgiStatements newStorages cmtSt
            let (CommentAndXgiStatements(comment_, xgiStatements)) = xgiCmtStmts
            if xgiStatements.Any() then
                newCommentedStatements.Add xgiCmtStmts
        newStorages.ToFSharpList(), newCommentedStatements.ToFSharpList()

    let generateXml (storages:Storages) (commentedStatements:CommentedStatement list) : string =
        match Runtime.Target with
        | XGI -> ()
        | _ -> failwith $"ERROR: Require XGI Runtime target.  Current runtime target = {Runtime.Target}"

        let prologComment = "DS Logic for XGI"

        let existingLSISprj = None

        let newStorages, newCommentedStatements = commentedStatementsToCommentedXgiStatements storages.Values commentedStatements

        let xgiSymbols = storagesToXgiSymbol newStorages

        let xml = generateXgiXmlFromStatement prologComment newCommentedStatements xgiSymbols existingLSISprj
        xml
