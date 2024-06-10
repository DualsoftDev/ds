namespace PLC.CodeGen.LS

open System.Linq
open System.Xml

open Dual.Common.Core.FS
open Engine.Core
open PLC.CodeGen.LS
open PLC.CodeGen.Common
open System
open PLC.CodeGen.Common.K


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
        $"""<Task Version="257" Type="0" Attribute="2" Kind="{kind}" Priority="{priority}" TaskIndex="{index}"
                Device="{device}" DeviceType="0" WordValue="0" WordCondition="0" BitCondition="0">{taskName}</Task>"""


[<AutoOpen>]
module XgiExportModule =
    type ITerminal with
        member x.ToTextWithoutTypeSuffix() =
            match x.Literal with
            | Some( :? ILiteralHolder as lh) -> lh.ToTextWithoutTypeSuffix()
            | _ -> x.GetContact()

    /// (조건=coil) seq 로부터 rung xml 들의 string 을 생성
    let internal generateRungs (prjParam: XgxProjectParams) (prologComment: string) (commentedStatements: CommentedStatements seq) : XmlOutput =
        let isXgi, isXgk = prjParam.TargetType = XGI, prjParam.TargetType = XGK

        let rgiCommandRung (condition: FlatExpression option) (xgiCommand:CommandTypes) (y:int) : RungGenerationInfo =
            let { Coordinate = c; Xml = xml } = rxiRung prjParam (0, y) condition xgiCommand
            let yy = c / 1024

            {   Xmls = [ wrapWithRung xml ]
                NextRungY = yy }

        let mutable rgi: RungGenerationInfo = { Xmls = []; NextRungY = 0 }

        // Prolog 설명문
        if prologComment.NonNullAny() then
            let xml = getCommentRungXml rgi.NextRungY prologComment
            rgi <- rgi.AddSingleLineXml(xml)

        let simpleRung (*(condition:IExpression)*) (expr: IExpression) (target: IStorage) : unit =
            match prjParam.TargetType, expr.FunctionName, expr.FunctionArguments with
            | XGK, Some funName, l::r::[] when funName.IsOneOf(arithmaticOperators @ comparisonOperators) ->
            
                let op = operatorToXgkFunctionName funName l.DataType |> escapeXml
                let ls, rs = l.GetTerminalString(prjParam) , r.GetTerminalString(prjParam)
                let xmls:XmlOutput =
                    let xy = (0, rgi.NextRungY)
                    let targetContact = if target.Address.IsNullOrEmpty() then target.Name else target.Address
                    if funName.IsOneOf(arithmaticOperators) then
                        let param = $"Param={dq}{op},{ls},{rs},{targetContact}{dq}"        // XGK 에서는 직접변수를 사용
                        xmlXgkFBRight xy param
                    elif funName.IsOneOf(comparisonOperators) then
                        let param = $"Param={dq}{op},{ls},{rs}{dq}"
                        xmlXgkFBLeft xy param targetContact
                    else
                        failwithlog $"ERROR: {funName}"


                rgi <-
                    {   Xmls = xmls::rgi.Xmls
                        NextRungY = rgi.NextRungY + 1 }
            | _ ->

                let coil =
                    match target with
                    | :? XgkTimerCounterStructResetCoil as rc ->
                        COMResetCoil(rc)
                    | _ -> COMCoil(target :?> INamedExpressionizableTerminal)

                let flatExpr = expr.Flatten() :?> FlatExpression
                let command = CoilCmd(coil)
                let rgiSub = rgiCommandRung (Some flatExpr) command rgi.NextRungY
                //rgi <- {Xmls = rgiSub.Xmls @ rgi.Xmls; Y = rgi.Y + rgiSub.Y}
                rgi <-
                    {   Xmls = rgiSub.Xmls @ rgi.Xmls
                        NextRungY = rgiSub.NextRungY }

        /// XGK 용 MOV : MOV,S,D
        let moveCmdRungXgk (condition:IExpression<bool>) (source: IExpression) (destination: IStorage) : unit =
            // test case : XGK "Add 10 items test"
            if prjParam.TargetType <> XGK then
                failwithlog "Something wrong!"

            let srcTerminal: ITerminal = source.Terminal.Value
            let x, y = 0, rgi.NextRungY

            //xmlRung 활용 필요
            let flatCondition = condition.Flatten() :?> FlatExpression


            if destination.DataType = typeof<bool> then
                match tryParseXGKTag destination.Address with
                | Some ( {
                    Tag = _tag
                    Device = device
                    DataType = _datatType
                    BitOffset = totalBitOffset
                    }) ->
                        let dh = sprintf "%A%04d%s" device (totalBitOffset / 16) (if totalBitOffset % 16 >= 8 then "8" else "0")  // destination head : destination 의 word
                        let offset = totalBitOffset % 8 // destination 이 속한 word 내에서의 bit offset
                        let mSet = 1uy <<< offset                       // OR mask 를 통해 해당 bit set 하기 위한 용도
                        let mClear = Byte.MaxValue - (1uy <<< offset) // AND mask 를 통해 해당 bit clear 하기 위한 용도
                        let printBinary (n:byte) = Convert.ToString(int n, 2).PadLeft(8, '0')
                        tracefn $"Dh: {dh}, Offset={offset}, mSet=0b{printBinary mSet}, mClear=0b{printBinary mClear}"

                        let flatten (exp: IExpression) = exp.Flatten() :?> FlatExpression
                        
                        let condWithTrue, condWithFalse =
                            match source with
                            | :? Expression<bool> as DuTerminal(DuLiteral lh) when lh.Value  ->
                                Some (flatten condition), None
                            | :? Expression<bool> as DuTerminal(DuLiteral lh) when not lh.Value  ->
                                None, Some (flatten condition)
                            | _ ->
                                let t = fbLogicalAnd([condition; source]) |> flatten
                                let f = fbLogicalAnd([condition; source.NegateBool() ]) |> flatten
                                Some t, Some f

                        if condWithTrue.IsSome then
                            let cmd =
                                let param = $"Param={dq}BOR,{dh},{mSet},{dh},1{dq}"         // Byte OR
                                XgkParamCmd(param, 5)
                            let rgiSub = rgiCommandRung condWithTrue cmd rgi.NextRungY
                            rgi <- {    Xmls = rgiSub.Xmls @ rgi.Xmls
                                        NextRungY = 1 + rgiSub.NextRungY }
                        if condWithFalse.IsSome then
                            let cmd =
                                let param = $"Param={dq}BAND,{dh},{mClear},{dh},1{dq}"      // Byte AND
                                XgkParamCmd(param, 5)
                            let rgiSub = rgiCommandRung condWithFalse cmd rgi.NextRungY
                            rgi <- {    Xmls = rgiSub.Xmls @ rgi.Xmls
                                        NextRungY = 1 + rgiSub.NextRungY }
                        //let rgiSub =
                        //    let cmd =
                        //        let param = $"Param={dq}BOR,{dh},{mSet},{dh},1{dq}"
                        //        XgkParamCmd(param, 5)
                        //    let sourceTrueCondition = fbLogicalAnd([condition; source]) |> flatten
                        //    rgiXmlRung (Some sourceTrueCondition) (Some cmd) rgi.NextRungY
                        //rgi <- {    Xmls = rgiSub.Xmls @ rgi.Xmls
                        //            NextRungY = 1 + rgiSub.NextRungY }

                        //let rgiSub =
                        //    let cmd =
                        //        let param = $"Param={dq}BAND,{dh},{mClear},{dh},1{dq}"
                        //        XgkParamCmd(param, 5)
                        //    let sourceFalseCondition = fbLogicalAnd([condition; fbLogicalNot [source]]) |> flatten
                        //    rgiXmlRung (Some sourceFalseCondition) (Some cmd) rgi.NextRungY
                        //rgi <- {    Xmls = rgiSub.Xmls @ rgi.Xmls
                        //            NextRungY = 1 + rgiSub.NextRungY }


                | _ ->
                    failwith "ERROR: XGK Tag parsing error"
            else
                let cmd = ActionCmd(Move(condition, srcTerminal :?> IExpression, destination))
                let blockXml = rxiRung prjParam (x, y) (Some flatCondition) cmd
                let xml = wrapWithRung blockXml.Xml
                rgi <- {   Xmls = xml::rgi.Xmls
                           NextRungY = rgi.NextRungY + blockXml.SpanY + 1 }

        // Rung 별로 생성
        for CommentAndStatements(cmt, stmts) in commentedStatements do

            // 다중 라인 설명문을 하나의 설명문 rung 에..
            if cmt.NonNullAny() then
                let xml =
                    let rungCounter = prjParam.RungCounter()
                    getCommentRungXml rgi.NextRungY $"[{rungCounter}] {cmt}"
                rgi <- rgi.AddSingleLineXml(xml)

            for stmt in stmts do
                match stmt with
                | DuAssign(condition, expr, target) when isXgk || expr.DataType <> typeof<bool> ->
                    let cond =
                        match condition with
                        | Some c -> c
                        | None -> Expression.True
                    // bool type 이 아닌 경우 ladder 에 의한 assign 이 불가능하므로, MOV/XGK or MOVE/XGI 를 사용한다.
                    if isXgi then
                        let command = ActionCmd(Move(cond, expr, target))
                        //let command =
                        //    match expr.Terminal, expr.FunctionName with
                        //    | Some _t, None -> ActionCmd(Move(cond, expr, target))
                        //    | None, Some fn -> FunctionCmd(Arithmetic(operatorToXgiFunctionName fn, target :?> INamedExpressionizableTerminal, expr.FunctionArguments))
                        //    | _ -> failwithlog "ERROR"
                        let rgiSub = rgiCommandRung None command rgi.NextRungY

                        rgi <-
                            {   Xmls = rgiSub.Xmls @ rgi.Xmls
                                NextRungY = 1 + rgiSub.NextRungY }
                    else
                        match condition, expr.Terminal with
                        | _, Some _ when expr.DataType <> typeof<bool> ->
                            moveCmdRungXgk cond expr target
                        | Some _, Some _ -> //when expr.DataType <> typeof<bool> ->
                            moveCmdRungXgk cond expr target
                        | _ ->
                            assert(condition.IsNone)
                            simpleRung expr target

                | DuAssign(condition, expr, target) ->
                    assert(condition.IsNone)
                    simpleRung expr target


                | DuPLCFunction({
                        FunctionName = ("&&" | "||") as _op
                        Arguments = args
                        OriginalExpression = originalExpr
                        Output = output }) ->
                    let expr = originalExpr.WithNewFunctionArguments args
                    simpleRung expr output

                | DuPLCFunction({
                        FunctionName = XgiConstants.FunctionNameMove as _op
                        Arguments = [ :? IExpression<bool> as condition; :? IExpression<bool> as source]
                        OriginalExpression = _originalExpr
                        Output = destination }) when isXgk && source.DataType = typeof<bool> ->
                    
                    moveCmdRungXgk condition source destination
                    //let rgiSub = rgiXgkBoolTypeCopyIfRungs condition source.Terminal.Value destination
                    //rgi <-
                    //    { Xmls = rgiSub.Xmls @ rgi.Xmls
                    //      NextRungY = 1 + rgiSub.NextRungY }

                // <kwak> <timer>
                | Statement.DuTimer timerStatement ->
                    let command = FunctionBlockCmd(TimerMode(timerStatement))
                    let rgiSub = rgiCommandRung None command rgi.NextRungY

                    rgi <-
                        {   Xmls = rgiSub.Xmls @ rgi.Xmls
                            NextRungY = 1 + rgiSub.NextRungY }

                | Statement.DuCounter counterStatement ->
                    let command = FunctionBlockCmd(CounterMode(counterStatement))
                    let rgiSub = rgiCommandRung None command rgi.NextRungY

                    rgi <-
                        {   Xmls = rgiSub.Xmls @ rgi.Xmls
                            NextRungY = 1 + rgiSub.NextRungY }

                | DuPLCFunction({
                        FunctionName = (">"|">="|"<"|"<="|"=="|"!="|"<>") as op
                        Arguments = args
                        Output = output }) ->
                    let fn = operatorToXgiFunctionName op
                    let command = PredicateCmd(Compare(fn, (output :?> INamedExpressionizableTerminal), args))
                    let rgiSub = rgiCommandRung None command rgi.NextRungY

                    rgi <-
                        {   Xmls = rgiSub.Xmls @ rgi.Xmls
                            NextRungY = 1 + rgiSub.NextRungY }

                | DuPLCFunction({
                        FunctionName = ("+"|"-"|"*"|"/") as op
                        Arguments = args
                        Output = output }) ->
                    let fn = operatorToXgiFunctionName op
                    let command = FunctionCmd(Arithmetic(fn, (output :?> INamedExpressionizableTerminal), args))
                    let rgiSub = rgiCommandRung None command rgi.NextRungY

                    rgi <-
                        {   Xmls = rgiSub.Xmls @ rgi.Xmls
                            NextRungY = 1 + rgiSub.NextRungY }

                | DuPLCFunction({
                        FunctionName = XgiConstants.FunctionNameMove as _func
                        Arguments = args
                        Output = output }) ->
                    let condition = args[0] :?> IExpression<bool>
                    let source = args[1]
                    let command = ActionCmd(Move(condition, source, output))
                    let rgiSub = rgiCommandRung None command rgi.NextRungY

                    rgi <-
                        {   Xmls = rgiSub.Xmls @ rgi.Xmls
                            NextRungY = 1 + rgiSub.NextRungY }

                | DuAction (DuCopyUdt (udtDecl, condition, source, target)) when isXgi ->
                    for m in udtDecl.Members do
                        let s, t = $"{source}.{m.Name}", $"{target}.{m.Name}"
                        let s, t = prjParam.GlobalStorages[s], prjParam.GlobalStorages[t]

                        let command = ActionCmd(Move(condition, s.ToExpression(), t))
                        let rgiSub = rgiCommandRung None command rgi.NextRungY

                        rgi <-
                            {   Xmls = rgiSub.Xmls @ rgi.Xmls
                                NextRungY = 1 + rgiSub.NextRungY }

                | DuAction(DuCopy(condition, source, target)) when isXgk ->
                    moveCmdRungXgk condition source target

                | _ -> failwithlog "Not yet"

        let rungEnd = generateEndXml (rgi.NextRungY + 1)
        rgi <- rgi.AddSingleLineXml(rungEnd)
        rgi.Xmls |> List.rev |> String.concat "\r\n"

    let internal getGlobalTagSkipSysTag(xs:IStorage seq) = 
                    xs |> filter(fun stg-> not(stg.GetSystemTagKind().IsSome && stg.Name.StartsWith("_")))

    /// [S] -> [XS]
    let internal css2Css
        (prjParam: XgxProjectParams)
        (localStorages: IStorage seq)
        (commentedStatements: CommentedStatement list)
      : IStorage list * CommentedStatements list =
        (* Timer 및 Counter 의 Rung In Condition 을 제외한 부수의 조건들이 직접 tag 가 아닌 condition expression 으로
            존재하는 경우, condition 들을 임시 tag 에 assign 하는 rung 으로 분리해서 저장.
            => 새로운 임시 tag 와 새로운 임시 tag 에 저장하기 위한 rung 들이 추가된다.
        *)

        let newCommentedStatements = ResizeArray<CommentedStatements>()
        let newLocalStorages = XgxStorage(localStorages)

        for cmtSt in commentedStatements do
            let xgxCmtStmts:CommentedStatements = cmtSt.ToCommentedStatements(prjParam, newLocalStorages)

            let (CommentAndStatements(_comment, xgxStatements)) = xgxCmtStmts

            if xgxStatements.Any() then
                newCommentedStatements.Add xgxCmtStmts

        newLocalStorages.ToFSharpList(), newCommentedStatements.ToFSharpList()

    type XgxProjectParamsProperties with
        /// Project XML 문서로부터 필요한 정보를 추출
        member x.FillPropertiesFromXmlDocument(prjParam:XgxProjectParams, xdoc:XmlDocument) =
            match prjParam.TargetType with
            | XGK ->
                let dic = collectXgkBasicParameters xdoc
                let readRange prefix =
                    let s = dic[$"{prefix}_START"]
                    let e = dic[$"{prefix}_END"]
                    (s, e)

                let specs =
                    [
                        0.1,   "T_100US_AREA_RANGE"
                        1.0,   "T_001MS_AREA_RANGE"
                        10.0,  "T_010MS_AREA_RANGE"
                        100.0, "T_100MS_AREA_RANGE"
                    ] |> map (fun (res, prefix) -> res, readRange prefix)
                      |> map XgkTimerResolutionSpec
                x.XgxTimerResolutionSpec <- specs
            | _ -> ()

    type XgxPOUParams with

        member x.GenerateXmlString(prjParam: XgxProjectParams, scanName:string option) = x.GenerateXmlNode(prjParam, scanName).OuterXml

        member private x.GroupStatementsByUdtDeclaration() =
            x.CommentedStatements
            |> groupBy (fun cs ->
                match cs.Statement with
                | DuUdtDecl _ -> "udt-decl"
                | DuUdtDefinitions _ -> "udt-instances"
                | _ -> "non-udt")
            |> Tuple.toDictionary

        /// XgxPOUParams 의 commented statements 중에서 UDT 선언을 제외한 나머지를 복사하여 반환
        member x.DuplicateExcludingUdtDeclarations() : XgxPOUParams =
            let g = x.GroupStatementsByUdtDeclaration()
            { x with CommentedStatements = g.TryFindIt("non-udt").DefaultValue([]) }

        /// XgxPOUParams 의 commented statements 중에서 UDT 선언문 반환
        member x.GetUdtDeclarations() : UdtDecl list =
            let g = x.GroupStatementsByUdtDeclaration()
            match g.TryFindIt("udt-decl") with
            | Some decl -> decl |> map (fun cs ->
                match cs.Statement with
                | DuUdtDecl udt -> udt
                | _ -> failwith "Not a UDT declaration")
            | None -> []

        /// XgxPOUParams 의 commented statements 중에서 UDT 변수 정의문 반환
        member x.GetUdtDefinitions() : UdtDefinition list =
            let g = x.GroupStatementsByUdtDeclaration()
            match g.TryFindIt("udt-instances") with
            | Some inst -> inst |> map (fun cs ->
                match cs.Statement with
                | DuUdtDefinitions udt -> udt
                | _ -> failwith "Not a UDT declaration")
            | None -> []

        /// POU 단위로 xml rung 생성
        member x.GenerateXmlNode(prjParam: XgxProjectParams, scanName:string option) : XmlNode =
            let {   TaskName = taskName
                    POUName = pouName
                    Comment = prologComment
                    GlobalStorages = globalStorages
                    LocalStorages = localStorages
                    CommentedStatements = commentedStatements } =
                x

            let newLocalStorages, newCommentedXgiStatements =
                css2Css prjParam localStorages.Values commentedStatements

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
            let rungsXml = generateRungs prjParam prologComment newCommentedXgiStatements

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
        member x.GenerateXmlString() = x.GenerateXmlDocument().OuterXml

        member x.GenerateXmlDocument() : XmlDocument =
            let xdoc, prjParam =
                let prjParam = x
                match prjParam.TargetType, prjParam.ExistingLSISprj with
                | XGK, Some existing ->
                    let doc = DualXmlDocument.loadFromFile existing
                    let counters = collectCounterAddressesXgk doc
                    let timers = collectTimerAddressesXgk doc
                    let newPrjParam = {
                        prjParam with
                            CounterCounterGenerator = counterGeneratorOverrideWithExclusionList prjParam.CounterCounterGenerator counters
                            TimerCounterGenerator = counterGeneratorOverrideWithExclusionList prjParam.TimerCounterGenerator timers
                    }
                    doc, newPrjParam
                | _, None ->
                    let doc = getTemplateXgxXmlDoc prjParam.TargetType
                    doc, prjParam
                | _, Some existing ->
                    let doc = DualXmlDocument.loadFromFile existing
                    doc, prjParam

            prjParam.Properties.FillPropertiesFromXmlDocument(prjParam, xdoc)
            prjParam.SanityCheck()

            let {   ProjectName = projName
                    TargetType = targetType
                    ProjectComment = projComment
                    GlobalStorages = globalStorages
                    EnableXmlComment = enableXmlComment
                    POUs = pous } =
                prjParam

            // todo : 사전에 처리 되었어야...
            for g in globalStorages.Values do
                g.IsGlobal <- true

            EnableXmlComment <- enableXmlComment

            pous |> iter (fun pou -> pou.SanityCheck(prjParam))


            let programs = xdoc.SelectNodes("//POU/Programs/Program")
            
            let existingTaskPous =
                    [   for p in programs do
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
             


            let xPathGlobalVar = getXPathGlobalVariable targetType

            (* Global variables 삽입 *)
            do
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

                (* UDT instance 삽입 : <Symbol> xml node 삽입 *)
                pous
                |> collect(fun pou -> pou.GetUdtDefinitions())
                |> map (fun udt -> udt.GenerateXmlNode(int Variable.Kind.VAR_GLOBAL))
                |> iter (xnGlobalVarSymbols.AdoptChild >> ignore)

            (* UDT 정의 삽입*)
            do
                let udtDecls = pous |> collect(fun pou -> pou.GetUdtDeclarations()) |> List.ofSeq
                match udtDecls with
                | [] -> ()
                | udtDecl::[] ->
                    let xnUdts = xdoc.GetXmlNode("//POU/UserDataTypes")
                    udtDecl.GenerateXmlNode() |> xnUdts.AdoptChild |> ignore
                | _ -> failwith "Only one UDT declaration is allowed"


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
                    let programXml =
                        pou
                            .DuplicateExcludingUdtDeclarations()
                            .GenerateXmlNode(x, mainScan)

                    let xnLocalVarSymbols = programXml.GetXmlNode("//LocalVar/Symbols")
                    pou.GetUdtDefinitions()
                    |> map (fun udt -> udt.GenerateXmlNode(int Variable.Kind.VAR_EXTERNAL))
                    |> iter (xnLocalVarSymbols.AdoptChild >> ignore)

                    xnPrograms.AdoptChild programXml |> ignore

            (* Local var 의 comment 및 초기값 global 에 반영 : hack *)
            do
                let xnGlobalVars = xdoc.GetXmlNodes($"{xPathGlobalVar}/Symbols/Symbol").ToArray()
                let localVars =
                    let locals = x.POUs |> Seq.collect (fun p -> p.GlobalStorages.Values) |> distinct |> toArray
                    let duplicated =
                        locals
                        |> groupBy (fun v -> v.Name)
                        |> filter (fun (_, v) -> v.Length > 1)
                    assert duplicated.IsEmpty()
                    locals |> map (fun v -> v.Name, v) |> dict

                for g in xnGlobalVars do
                    let name = g.GetAttribute("Name")
                    if localVars.ContainsKey name then
                        let l = localVars.[name]
                        let c, i = g.Attributes["Comment"], g.Attributes["InitValue"]
                        if c <> null && l.Comment <> "" && l.Comment <> c.Value then
                            let newComment = l.Comment //|> escapeXml
                            c.Value <- newComment

                        if targetType = XGI && i <> null && l.BoxedValue.ToString() <> i.Value then
                            let initValue =
                                match l.BoxedValue with
                                | :? bool as b -> if b then "true" else "false"
                                | _ as v -> v.ToString()
                            i.Value <- initValue

            if targetType = XGK then
                xdoc.MovePOULocalSymbolsToGlobalForXgk()
                xdoc.SanityCheckVariableNameForXgk()

            xdoc.Check targetType

        member x.SanityCheck() =
            let { GlobalStorages = globalStorages } = x
            let vars = globalStorages.Values |> toArray

            let checkDoubleCoil() =
                // todo: 이중 코일 체크: 불필요 한 듯.
                // project level 의 double coil check
                // - Global 변수 중에 non-terminal expression 을 사용한 경우를 찾아서 marking 해 두고, POU 에서 해당 변수 할당하는 경우를 찾아서 error 를 발생시킨다.
                ()
            let checkLWordUsage() =
                if x.TargetType = XGK then
                    // XGK 에서는 LWord 사용(double, long)을 지원하지 않는다.
                    for v in vars do
                        if v :? IMemberVariable || v :? TimerCounterBaseStruct then
                            // todo: timer, counter 등의 구조체 변수는 기본적으로 LWord 등으로 선언되어 있는데... 
                            ()
                        else
                            let t = v.DataType
                            match t  with
                            | _ when t.IsOneOf(typeof<int64>, typeof<uint64>) ->
                                failwith $"Error on variable declararion {v.Name} ({t.Name}): XGK does not support int64 types (LWORD)" 
                            | _ -> ()


            checkDoubleCoil()
            checkLWordUsage()

    and UdtDecl with
        member x.GenerateXmlNode() : XmlNode =
            let udt = $"""
                <UserDataType Version="256">
                    {x.TypeName}
                    <UserDataTypeVar Version="Ver 1.0" Count="{x.Members.Length}">
                        <Symbols>
                        </Symbols>
                    </UserDataTypeVar>
                </UserDataType>
                """ |> DualXmlNode.ofString
            let symbols = udt.GetXmlNode "UserDataTypeVar/Symbols"
            let mutable devicePos = 0
            for m in x.Members do
                let symbol = $"<Symbol></Symbol>" |> DualXmlNode.ofString
                let dataType = m.Type |> getDataType
                symbol.AddAttributes([
                    "Name", m.Name
                    "Type", dataType.ToPLCType()      // todo: ds type 과 PLC type 간 변환 필요.  int -> DINT, int16 -> int
                    "DevicePos", devicePos
                ]) |> ignore

                // get type length 
                devicePos <-
                    let bitLength = dataType.ToPLCBitSize()
                    devicePos + bitLength

                symbols.AdoptChild symbol |> ignore
            udt
    and UdtDefinition with
        /// UDT instance 정의문에 해당하는 <Symbol> xml node 를 생성해서 반환
        // kind: <GlobalVariable> tag 내에서의 symbol 인 경우 6, <LocalVar> tag 내에서의 symbol 인 경우 8
        member x.GenerateXmlNode(kind:int) : XmlNode =
            let typ =
                match x.ArraySize with
                | 1 -> x.TypeName
                | n when n > 1 -> $"ARRAY[0..{n-1}] OF {x.TypeName}"
                | _ as n -> failwith $"Invalid array size: {n}"
            let xmlSymbol =
                $"""<Symbol Name="{x.VarName}" Kind="{kind}" Type="{typ}" Device="A" ></Symbol>"""
                |> DualXmlNode.ofString
            xmlSymbol
        (* UDT array 초기화 방법 *)
        (*
	        <Symbol Name="people" Kind="6" Type="ARRAY[0..9] OF Person" State="0" Address="" Trigger="" InitValue="" Comment="" Device="" DevicePos="-1" TotalSize="0" OrderIndex="-1" HMI="0" EIP="0" SturctureArrayOffset="0" ModuleInfo="" ArrayPointer="0" PtrType="" Motion="0">
		        <MemberInitValues>
			        <Member MemberName="people[0].age" MemberValue="0"></Member>
			        <Member MemberName="people[1].age" MemberValue="1"></Member>
			        <Member MemberName="people[0].name" MemberValue="'zero'"></Member>
			        <Member MemberName="people[1].name" MemberValue="'one'"></Member>
		        </MemberInitValues>
	        </Symbol>
        *)


    let IsXg5kXGT(xmlProjectFilePath:string) =
        let xdoc = DualXmlDocument.loadFromFile xmlProjectFilePath
        xdoc.GetXmlNode("//Configurations/Configuration/Parameters/Parameter/XGTBasicParam") <> null
    let IsXg5kXGI(xmlProjectFilePath:string) =
        let xdoc = DualXmlDocument.loadFromFile xmlProjectFilePath
        xdoc.GetXmlNode("//Configurations/Configuration/Parameters/Parameter/XGIBasicParam") <> null
