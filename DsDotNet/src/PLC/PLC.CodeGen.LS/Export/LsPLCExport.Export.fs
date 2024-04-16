namespace PLC.CodeGen.LS

open System.Linq
open System.Xml

open Dual.Common.Core.FS
open Engine.Core
open PLC.CodeGen.LS
open PLC.CodeGen.Common

[<AutoOpen>]
module XgxXmlGeneratorModule =
    /// Program 부분 Xml string 반환: <Program Task="taskName" ..>pouName
    let createXmlStringProgram taskName pouName (mainScan:string option) =
        sprintf
            """
			<Program Task="%s" Version="256" LocalVariable="1" Kind="0" InstanceName="" Comment="" FindProgram="1" FindVar="1" Encrytption="">%s
                <Body>
					<LDRoutine>
						<OnlineUploadData Compressed="1" dt:dt="bin.base64" xmlns:dt="urn:schemas-microsoft-com:datatypes">QlpoOTFBWSZTWY5iHkIAAA3eAOAQQAEwAAYEEQAAAaAAMQAACvKMj1MnqSRSSVXekyB44y38
XckU4UJCOYh5CA==</OnlineUploadData>
					</LDRoutine>
				</Body>
				<RungTable></RungTable>
			</Program>"""
            (if mainScan.IsSome then mainScan.Value else taskName)
            pouName

    /// Task 부분 Xml string 반환: <Task Version=..>taskNameName
    let createXmlStringTask taskName kind priority index device=
        sprintf
            """
            <Task Version="257" Type="0" Attribute="2" Kind="%d" Priority="%d" TaskIndex="%d"
                Device="%d" DeviceType="0" WordValue="0" WordCondition="0" BitCondition="0">%s</Task>"""
            kind
            priority
            index
            device
            taskName



[<AutoOpen>]
module XgiExportModule =

    /// (조건=coil) seq 로부터 rung xml 들의 string 을 생성
    let internal generateRungs (prjParam: XgxProjectParams) (prologComment: string) (commentedStatements: CommentedXgxStatements seq) : XmlOutput =
        let xmlRung (expr: FlatExpression option) xgiCommand y : RungGenerationInfo =
            let { Coordinate = c; Xml = xml } = rung prjParam (0, y) expr xgiCommand
            let yy = c / 1024

            { Xmls = [ $"\t<Rung BlockMask={dq}0{dq}>\r\n{xml}\t</Rung>" ]
              Y = yy }

        let mutable rgi: RungGenerationInfo = { Xmls = []; Y = 0 }

        // Prolog 설명문
        if prologComment.NonNullAny() then
            let xml = getCommentRungXml rgi.Y prologComment
            rgi <- rgi.Add(xml)

        let simpleRung (expr: IExpression) (target: IStorage) =

            let getXgkTerminalString(terminalExp:IExpression) =
                match terminalExp.Terminal with
                | Some t ->
                    match t.Variable, t.Literal with
                    | Some v, None -> v.Name
                    | None, Some (:? ILiteralHolder as lh) -> lh.ToTextWithoutTypeSuffix()
                    | _ -> failwith "ERROR: Unknown terminal literal case."
                | _ -> failwith "ERROR: Not a Terminal"

            match prjParam.TargetType, expr.FunctionName, expr.FunctionArguments with
            | XGK, Some funName, l::r::[] when funName.IsOneOf("+", "-", "*", "/", ">", ">=", "<", "<=", "=", "<>") ->
            
                let op = operatorToXgkFunctionName funName |> escapeXml
                let ls, rs = getXgkTerminalString l, getXgkTerminalString r
                let drawXgkFb, paramFunc =
                    if funName.IsOneOf("+", "-", "*", "/") then
                        drawXgkFBRight, $"Param={dq}{op},{ls},{rs},{target.Name}{dq}"
                    elif funName.IsOneOf(">", ">=", "<", "<=", "=", "<>") then
                        drawXgkFBLeft, $"Param={dq}{op},{ls},{rs}{dq}"
                    else
                        failwithlog $"ERROR: {funName}"

                let xmls = drawXgkFb (0, rgi.Y) paramFunc target.Name
                rgi <-
                    {   Xmls = [xmls] @ rgi.Xmls
                        Y = rgi.Y + 1 }
            | _ ->

                let coil =
                    match target with
                    | _ -> COMCoil(target :?> INamedExpressionizableTerminal)

                let flatExpr = expr.Flatten() :?> FlatExpression
                let command = CoilCmd(coil)
                let rgiSub = xmlRung (Some flatExpr) (Some command) rgi.Y
                //rgi <- {Xmls = rgiSub.Xmls @ rgi.Xmls; Y = rgi.Y + rgiSub.Y}
                rgi <-
                    { Xmls = rgiSub.Xmls @ rgi.Xmls
                      Y = rgiSub.Y }

        // Rung 별로 생성
        for CommentAndXgxStatements(cmt, stmts) in commentedStatements do

            // 다중 라인 설명문을 하나의 설명문 rung 에..
            if cmt.NonNullAny() then
                let xml =
                    let rungCounter = prjParam.RungCounter.Value()
                    getCommentRungXml rgi.Y $"[{rungCounter}] {cmt}"
                rgi <- rgi.Add(xml)

            for stmt in stmts do
                match stmt with
                | DuAssign(expr, target) -> simpleRung expr target
                | DuAugmentedPLCFunction({ FunctionName = ("&&" | "||") as op
                                           Arguments = args
                                           Output = output }) ->
                    let psedoFunction (_args: Args) : bool =
                        failwithlog "THIS IS PSEUDO FUNCTION.  SHOULD NOT BE EVALUATED!!!!"

                    let expr =
                        DuFunction
                            { FunctionBody = psedoFunction
                              Name = op
                              Arguments = args }

                    simpleRung expr (output :?> IStorage)


                // <kwak> <timer>
                | Statement.DuTimer timerStatement ->
                    let command = FunctionBlockCmd(TimerMode(timerStatement))
                    let rgiSub = xmlRung None (Some command) rgi.Y

                    rgi <-
                        { Xmls = rgiSub.Xmls @ rgi.Xmls
                          Y = 1 + rgiSub.Y }

                | Statement.DuCounter counterStatement ->
                    let command = FunctionBlockCmd(CounterMode(counterStatement))
                    let rgiSub = xmlRung None (Some command) rgi.Y

                    rgi <-
                        { Xmls = rgiSub.Xmls @ rgi.Xmls
                          Y = 1 + rgiSub.Y }

                | DuAugmentedPLCFunction({ FunctionName = (">" | ">=" | "<" | "<=" | "=" | "!=") as op
                                           Arguments = args
                                           Output = output }) ->
                    let fn = operatorToXgiFunctionName op
                    let command = PredicateCmd(Compare(fn, output, args))
                    let rgiSub = xmlRung None (Some command) rgi.Y

                    rgi <-
                        { Xmls = rgiSub.Xmls @ rgi.Xmls
                          Y = 1 + rgiSub.Y }

                | DuAugmentedPLCFunction({ FunctionName = ("+" | "-" | "*" | "/") as op
                                           Arguments = args
                                           Output = output }) ->
                    let fn = operatorToXgiFunctionName op
                    let command = FunctionCmd(Arithmatic(fn, output, args))
                    let rgiSub = xmlRung None (Some command) rgi.Y

                    rgi <-
                        { Xmls = rgiSub.Xmls @ rgi.Xmls
                          Y = 1 + rgiSub.Y }
                | DuAugmentedPLCFunction({ FunctionName = XgiConstants.FunctionNameMove as _func
                                           Arguments = args
                                           Output = output }) ->
                    let condition = args[0] :?> IExpression<bool>
                    let source = args[1]
                    let target = output :?> IStorage
                    let command = ActionCmd(Move(condition, source, target))
                    let rgiSub = xmlRung None (Some command) rgi.Y

                    rgi <-
                        { Xmls = rgiSub.Xmls @ rgi.Xmls
                          Y = 1 + rgiSub.Y }
                | _ -> failwithlog "Not yet"

        let rungEnd = generateEndXml (rgi.Y + 1)
        rgi <- rgi.Add(rungEnd)
        rgi.Xmls |> List.rev |> String.concat "\r\n"

    let internal getGlobalTagSkipSysTag(xs:IStorage seq) = 
                    xs |> filter(fun stg-> not(stg.GetSystemTagKind().IsSome && stg.Name.StartsWith("_")))

    /// [S] -> [XS]
    let internal commentedStatementsToCommentedXgxStatements
        (prjParam: XgxProjectParams)
        (localStorages: IStorage seq)
        (commentedStatements: CommentedStatement list)
      : IStorage list * CommentedXgxStatements list =
        (* Timer 및 Counter 의 Rung In Condition 을 제외한 부수의 조건들이 직접 tag 가 아닌 condition expression 으로
            존재하는 경우, condition 들을 임시 tag 에 assign 하는 rung 으로 분리해서 저장.
            => 새로운 임시 tag 와 새로운 임시 tag 에 저장하기 위한 rung 들이 추가된다.
        *)

        let newCommentedStatements = ResizeArray<CommentedXgxStatements>()
        let newLocalStorages = ResizeArray<IStorage>(localStorages)

        for cmtSt in commentedStatements do
            let xgxCmtStmts =
                statement2Statements prjParam newLocalStorages cmtSt

            let (CommentAndXgxStatements(_comment, xgxStatements)) = xgxCmtStmts

            if xgxStatements.Any() then
                newCommentedStatements.Add xgxCmtStmts

        newLocalStorages.ToFSharpList(), newCommentedStatements.ToFSharpList()

        
    type XgxPOUParams with

        member x.GenerateXmlString(prjParam: XgxProjectParams, scanName:string option) = x.GenerateXmlNode(prjParam, scanName).OuterXml

        /// POU 단위로 xml rung 생성
        member x.GenerateXmlNode(prjParam: XgxProjectParams, scanName:string option) : XmlNode =
            let { TaskName = taskName
                  POUName = pouName
                  Comment = prologComment
                  GlobalStorages = globalStorages
                  LocalStorages = localStorages
                  CommentedStatements = commentedStatements } =
                x

            let newLocalStorages, commentedXgiStatements =
                commentedStatementsToCommentedXgxStatements prjParam localStorages.Values commentedStatements

            let globalStoragesRefereces =
                [
                  // POU 에 사용된 모든 storages (global + local 모두 포함)
                  let allUsedStorages =
                      [ for cstmt in commentedStatements do
                            yield! cstmt.CollectStorages() ]
                      |> List.distinct

                  yield! newLocalStorages.Where(fun s -> s.IsGlobal)

                  for stg in allUsedStorages.Except(newLocalStorages) do
                      (* 'Timer1.Q' 등의 symbol 이 사용되었으면, Timer1 을 global storage 의 reference 로 간주하고, 이를 local var 에 external 로 등록한다. *)
                      match stg.Name with
                      | RegexPattern @"(^[^\.]+)\.(.*)$" [ structName; _tail ] ->
                          if globalStorages.ContainsKey structName then
                              yield globalStorages[structName]
                          else
                              logWarn $"Unknown struct name {structName}"
                      | _ -> yield stg ]
                |> distinct
                |> getGlobalTagSkipSysTag
                |> Seq.sortBy (fun stg -> stg.Name)

            (* storage 참조 무결성 체크 *)
            do
                for ref in globalStoragesRefereces do
                    let name = ref.Name
                    let inGlobal = globalStorages.ContainsKey name
                    let inLocal = localStorages.ContainsKey name

                    if not (inGlobal || inLocal) then
                        failwithf "Storage '%s' is not defined" name

            let newLocalStorages = newLocalStorages.Except(globalStoragesRefereces)

            let localStoragesXml =
                storagesToLocalXml prjParam newLocalStorages globalStoragesRefereces

            (*
             * Rung 생성
             *)
            let rungsXml = generateRungs prjParam prologComment commentedXgiStatements

            /// POU/Programs/Program
            let programTemplate = createXmlStringProgram taskName pouName scanName |> DualXmlNode.ofString

            //let programTemplate = DsXml.adoptChild programs programTemplate

            /// LDRoutine 위치 : Rung 삽입 위치
            let posiLdRoutine = programTemplate.GetXmlNode "Body/LDRoutine"
            let onlineUploadData = posiLdRoutine.FirstChild
            (*
             * Rung 삽입
             *)
            let rungsXml = $"<Rungs>{rungsXml}</Rungs>" |> DualXmlNode.ofString

            for r in rungsXml.GetChildrenNodes() do
                onlineUploadData.InsertBefore r |> ignore

            (*
             * Local variables 삽입 - 동일 코드 중복.  수정시 동일하게 변경 필요
             *)
            let programBody = posiLdRoutine.ParentNode
            let localSymbols = localStoragesXml |> DualXmlNode.ofString
            programBody.InsertAfter localSymbols |> ignore

            programTemplate


    and XgxProjectParams with

        member private x.GetTemplateXmlDoc() =
            x.ExistingLSISprj |> Option.map DualXmlDocument.loadFromFile
            |? getTemplateXgxXmlDoc x.TargetType

        member x.GenerateXmlString() = x.GenerateXmlDocument().OuterXml

        member x.GenerateXmlDocument() : XmlDocument =
            let { ProjectName = projName
                  TargetType = targetType
                  ProjectComment = projComment
                  GlobalStorages = globalStorages
                  EnableXmlComment = enableXmlComment
                  POUs = pous } =
                x

            // todo : 사전에 처리 되었어야...
            for g in globalStorages.Values do
                g.IsGlobal <- true

            EnableXmlComment <- enableXmlComment
            let xdoc = x.GetTemplateXmlDoc()
            let programs = xdoc.SelectNodes("//POU/Programs/Program")
            
            let existingTaskPous =
                    [ for p in programs do
                          let taskName = p.GetAttribute("Task")
                          let pouName = p.FirstChild.OuterXml
                          taskName, pouName ]


            (* validation : POU 중복 이름 체크 *)
            do
                let newTaskPous = [ for p in pous -> p.TaskName, p.POUName ]

                let duplicated =
                    existingTaskPous @ newTaskPous
                    |> List.groupBy id
                    |> List.filter (fun (_, v) -> v.Length > 1)

                if duplicated.Length > 0 then
                    failwithf "ERROR: Duplicated POU name : %A" duplicated

            (* project name/comment 변경 *)
            do
                let xnProj = xdoc.SelectSingleNode("//Project")
                xnProj.FirstChild.InnerText <- projName

                if projComment.NonNullAny() then
                    let xe = (xnProj :?> XmlElement)
                    xe.SetAttribute("Comment", projComment)

            (* xn = Xml Node *)
            
            (* Tasks/Task 삽입 *)
            do
                let xnTasks = xdoc.SelectSingleNode("//Configurations/Configuration/Tasks")
                let pous = pous |> List.distinctBy (fun pou -> pou.TaskName)

                for i, pou in pous.Indexed() do
                    let index = max (i - 1) 0
                    let kind = if i = 0 then 0 else 2 //0:스캔프로그램Task 2:user Task
                    let priority = kind
                    let device = if kind =0 then 0 else 10  //정주기 10msec 디바이스항목으로 저장
                    if kind = 2 //user task 만 삽입 (스캔프로그램Task는 template에 항상 있음)
                    then
                        createXmlStringTask pou.TaskName kind priority index device
                        |> DualXmlNode.ofString
                        |> xnTasks.AdoptChild
                        |> ignore
             

            (* Global variables 삽입 *)
            do
                let xPathGlobalVar = getXPathGlobalVariable targetType
                //let xnGlobalVar = xdoc.GetXmlNodeTheGlobalVariable(targetType)
                //let xnGlobalVarSymbols = xnGlobalVar.GetXmlNode "Symbols"
                let xnGlobalSymbols = xdoc.GetXmlNodes($"{xPathGlobalVar}/Symbols/Symbol") |> List.ofSeq

                let countExistingGlobal = xnGlobalSymbols.Length

                let existingGlobalSymbols = xnGlobalSymbols |> map xmlSymbolNodeToSymbolInfo

                (* existing global name 과 신규 global name 충돌 check *)
                do
                    let existingGlobalNames = existingGlobalSymbols |> map (name >> String.toUpper)
                    let currentGlobalNames = globalStorages.Keys |> map String.toUpper

                    match existingGlobalNames.Intersect(currentGlobalNames) |> Seq.tryHead with
                    | Some duplicated -> failwith $"ERROR: Duplicated global variable name : {duplicated}"
                    | _ -> ()

                (* I, Q 영역의 existing global address 와 신규 global address 충돌 check *)
                do
                    let standardize (addrs: string seq) =
                        addrs
                        |> filter notNullAny
                        |> map standardizeAddress
                        |> filter (function
                            | RegexPattern @"^%[IQ]" _ -> true
                            | _ -> false)

                    let existingGlobalAddresses = existingGlobalSymbols |> map address |> standardize

                    let currentGlobalAddresses =
                        globalStorages.Values
                        |> filter (fun s -> s :? ITag || s :? IVariable)
                        |> map address
                        |> standardize

                    match existingGlobalAddresses.Intersect(currentGlobalAddresses) |> Seq.tryHead with
                    | Some duplicated -> failwith $"ERROR: Duplicated address usage : {duplicated}"
                    | _ -> ()

                // symbolsGlobal = "<GlobalVariable Count="1493"> <Symbols> <Symbol> ... </Symbol> ... <Symbol> ... </Symbol>
                let globalStoragesSortedByAllocSize =
                    globalStorages.Values
                    |> getGlobalTagSkipSysTag
                    |> Seq.sortByDescending (fun t ->
                        if t :? TimerCounterBaseStruct 
                            || isNull t.Address 
                            || TextAddrEmpty <> t.Address
                        then
                            0
                        else // t.Address 가  "_"(TextAddrEmpty) 인 경우에만 자동으로 채운다. (null 은 아님)
                            t.DataType.GetBitSize())
                    |> Array.ofSeq

                let globalStoragesXmlNode =
                    storagesToGlobalXml x globalStoragesSortedByAllocSize |> DualXmlNode.ofString

                let numNewGlobals =
                    globalStoragesXmlNode.Attributes.["Count"].Value |> System.Int32.Parse
                // timer, counter 등을 고려했을 때는 numNewGlobals <> globalStorages.Count 일 수 있다.

                let xnGlobalVarSymbols = xdoc.GetXmlNode($"{xPathGlobalVar}/Symbols")
                let xnCountConainer =
                    match targetType with
                    | XGI -> xdoc.GetXmlNode xPathGlobalVar
                    | XGK -> xnGlobalVarSymbols
                    | _ -> failwithlog $"Unknown Target: {targetType}"

                xnCountConainer.Attributes.["Count"].Value <- sprintf "%d" (countExistingGlobal + numNewGlobals)

                globalStoragesXmlNode.SelectNodes(".//Symbols/Symbol").ToEnumerables()
                |> iter (xnGlobalVarSymbols.AdoptChild >> ignore)


            (* POU program 삽입 *)
            do
                let xnPrograms = xdoc.SelectSingleNode("//POU/Programs")
                let mainScanName =
                    if existingTaskPous.any() then
                        existingTaskPous.First() |> fst
                    else 
                        let task = xdoc.SelectNodes("//Tasks/Task").ToEnumerables().First() 
                        task.FirstChild.OuterXml 


                for i, pou in pous.Indexed() do //i = 0 은 메인 스캔 프로그램
                    let mainScan =   if i = 0 then Some(mainScanName) else None
                    // POU 단위로 xml rung 생성
                    pou.GenerateXmlNode(x, mainScan)
                    |> xnPrograms.AdoptChild
                    |> ignore

            if targetType = XGK then
                xdoc.MovePOULocalSymbolsToGlobal(targetType)

            xdoc.Check targetType
