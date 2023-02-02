namespace PLC.CodeGen.LSXGI

open System.Linq
open System.Xml

open Engine.Common.FS
open Engine.Core
open PLC.CodeGen.LSXGI
open PLC.CodeGen.Common

[<AutoOpen>]
module XgiXmlGeneratorModule =
    /// Program 부분 Xml string 반환: <Program Task="taskName" ..>pouName
    let createXmlStringProgram taskName pouName =
        sprintf """
			<Program Task="%s" Version="256" LocalVariable="1" Kind="0" InstanceName="" Comment="" FindProgram="1" FindVar="1" Encrytption="">%s
                <Body>
					<LDRoutine>
						<OnlineUploadData Compressed="1" dt:dt="bin.base64" xmlns:dt="urn:schemas-microsoft-com:datatypes">QlpoOTFBWSZTWY5iHkIAAA3eAOAQQAEwAAYEEQAAAaAAMQAACvKMj1MnqSRSSVXekyB44y38
XckU4UJCOYh5CA==</OnlineUploadData>
					</LDRoutine>
				</Body>
				<RungTable></RungTable>
			</Program>""" taskName pouName

    /// Task 부분 Xml string 반환: <Task Version=..>taskNameName
    let createXmlStringTask taskName kind priority index =
        sprintf """
            <Task Version="257" Type="0" Attribute="2" Kind="%d" Priority="%d" TaskIndex="%d"
                Device="" DeviceType="0" WordValue="0" WordCondition="0" BitCondition="0">%s</Task>""" kind priority index taskName



[<AutoOpen>]
module XgiExportModule =

    /// (조건=coil) seq 로부터 rung xml 들의 string 을 생성
    let internal generateRungs (prologComment:string) (commentedStatements:CommentedXgiStatements seq) : XmlOutput =
        let xmlRung (expr:FlatExpression option) xgiCommand y : RungGenerationInfo =
            let {Coordinate=c; Xml=xml} = rung (0, y) expr xgiCommand
            let yy = c / 1024
            { Xmls = [$"\t<Rung BlockMask={dq}0{dq}>\r\n{xml}\t</Rung>"]; Y = yy}

        let mutable rgi:RungGenerationInfo = {Xmls = []; Y = 0}

        // Prolog 설명문
        if prologComment.NonNullAny() then
            let xml = getCommentRungXml rgi.Y prologComment
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
                let xml = getCommentRungXml rgi.Y cmt
                rgi <- rgi.Add(xml)
            for stmt in stmts do
                match stmt with
                | DuAssign (expr, target) -> simpleRung expr target
                | DuAugmentedPLCFunction ({FunctionName = ("&&"|"||") as op; Arguments = args; Output=output }) ->
                    let psedoFunction (_args:Args):bool = failwithlog "THIS IS PSEUDO FUNCTION.  SHOULD NOT BE EVALUATED!!!!"
                    let expr = DuFunction { FunctionBody=psedoFunction; Name=op; Arguments=args }
                    simpleRung expr (output :?> IStorage)


                // <kwak> <timer>
                | Statement.DuTimer timerStatement ->
                    let command = FunctionBlockCmd(TimerMode(timerStatement))
                    let rgiSub = xmlRung None (Some command) rgi.Y
                    rgi <- {Xmls = rgiSub.Xmls @ rgi.Xmls; Y = 1+rgiSub.Y}

                | Statement.DuCounter counterStatement ->
                    let command = FunctionBlockCmd(CounterMode(counterStatement))
                    let rgiSub = xmlRung None (Some command) rgi.Y
                    rgi <- {Xmls = rgiSub.Xmls @ rgi.Xmls; Y = 1+rgiSub.Y}

                | DuAugmentedPLCFunction ({FunctionName = (">"|">="|"<"|"<="|"="|"!=") as op; Arguments = args; Output=output }) ->
                    let fn = operatorToXgiFunctionName op
                    let command = PredicateCmd(Compare(fn, output, args))
                    let rgiSub = xmlRung None (Some command) rgi.Y
                    rgi <- {Xmls = rgiSub.Xmls @ rgi.Xmls; Y = 1+rgiSub.Y}

                | DuAugmentedPLCFunction ({FunctionName = ("+"|"-"|"*"|"/") as op; Arguments = args; Output=output }) ->
                    let fn = operatorToXgiFunctionName op
                    let command = FunctionCmd(Arithmatic(fn, output, args))
                    let rgiSub = xmlRung None (Some command) rgi.Y
                    rgi <- {Xmls = rgiSub.Xmls @ rgi.Xmls; Y = 1+rgiSub.Y}
                | DuAugmentedPLCFunction ({FunctionName = XgiConstants.FunctionNameMove as _func; Arguments = args; Output=output }) ->
                    let condition = args[0] :?> IExpression<bool>
                    let source = args[1]
                    let target = output :?> IStorage
                    let command = ActionCmd(Move(condition, source, target))
                    let rgiSub = xmlRung None (Some command) rgi.Y
                    rgi <- {Xmls = rgiSub.Xmls @ rgi.Xmls; Y = 1+rgiSub.Y}
                | _ ->
                    failwithlog "Not yet"

        let rungEnd = generateEndXml (rgi.Y + 1)
        rgi <- rgi.Add(rungEnd)
        rgi.Xmls |> List.rev |> String.concat "\r\n"



    /// [S] -> [XS]
    let internal commentedStatementsToCommentedXgiStatements
        (prjParam:XgiProjectParams)
        (localStorages:IStorage seq)
        (commentedStatements:CommentedStatement list)
        : IStorage list * CommentedXgiStatements list
      =
        (* Timer 및 Counter 의 Rung In Condition 을 제외한 부수의 조건들이 직접 tag 가 아닌 condition expression 으로
            존재하는 경우, condition 들을 임시 tag 에 assign 하는 rung 으로 분리해서 저장.
            => 새로운 임시 tag 와 새로운 임시 tag 에 저장하기 위한 rung 들이 추가된다.
        *)

        let newCommentedStatements = ResizeArray<CommentedXgiStatements>()
        let newLocalStorages = ResizeArray<IStorage>(localStorages)
        for cmtSt in commentedStatements do
            let xgiCmtStmts = commentedStatement2CommentedXgiStatements prjParam newLocalStorages cmtSt
            let (CommentAndXgiStatements(_comment, xgiStatements)) = xgiCmtStmts
            if xgiStatements.Any() then
                newCommentedStatements.Add xgiCmtStmts
        newLocalStorages.ToFSharpList(), newCommentedStatements.ToFSharpList()


    type XgiPOUParams with
        member x.GenerateXmlString(prjParam:XgiProjectParams) = x.GenerateXmlNode(prjParam).OuterXml
        member x.GenerateXmlNode(prjParam:XgiProjectParams) : XmlNode =
            let { TaskName=taskName; POUName=pouName; Comment=comment;
                  GlobalStorages=globalStorages; LocalStorages=localStorages;
                  CommentedStatements=commentedStatements} = x
            let newLocalStorages, commentedXgiStatements =
                commentedStatementsToCommentedXgiStatements prjParam localStorages.Values commentedStatements

            let globalStoragesRefereces =
                [
                    // POU 에 사용된 모든 storages (global + local 모두 포함)
                    let allUsedStorages =
                        [
                            for cstmt in commentedStatements do
                                yield! cstmt.CollectStorages()
                        ] |> List.distinct

                    yield! newLocalStorages.Where(fun s -> s.IsGlobal)

                    for stg in allUsedStorages.Except(newLocalStorages) do
                        (* 'Timer1.Q' 등의 symbol 이 사용되었으면, Timer1 을 global storage 의 reference 로 간주하고, 이를 local var 에 external 로 등록한다. *)
                        match stg.Name with
                        | RegexPattern @"(^[^\.]+)\.(.*)$" [structName; _tail] ->
                            if globalStorages.ContainsKey structName then
                                yield globalStorages[structName]
                            else
                                logWarn $"Unknown struct name {structName}"
                        | _ ->
                            yield stg
                ] |> distinct
                  |> List.sortBy(fun stg -> stg.Name)

            (* storage 참조 무결성 체크 *)
            do
                for ref in globalStoragesRefereces do
                    let name = ref.Name
                    let inGlobal = globalStorages.ContainsKey name
                    let inLocal  = localStorages.ContainsKey name

                    if not (inGlobal || inLocal) then
                        failwithf "Storage '%s' is not defined" name

            let newLocalStorages = newLocalStorages.Except(globalStoragesRefereces)
            let localStoragesXml = storagesToLocalXml prjParam newLocalStorages globalStoragesRefereces
            let rungsXml = generateRungs comment commentedXgiStatements

            /// POU/Programs/Program
            let programTemplate = createXmlStringProgram taskName pouName |> XmlNode.fromString

            //let programTemplate = DsXml.adoptChild programs programTemplate

            /// LDRoutine 위치 : Rung 삽입 위치
            let posiLdRoutine = programTemplate.GetXmlNode "Body/LDRoutine"
            let onlineUploadData = posiLdRoutine.FirstChild
            (*
             * Rung 삽입
             *)
            let rungsXml = $"<Rungs>{rungsXml}</Rungs>" |> XmlNode.fromString
            for r in rungsXml.GetChildrenNodes()  do
                onlineUploadData.InsertBefore r |> ignore

            (*
             * Local variables 삽입
             *)
            let programBody = posiLdRoutine.ParentNode
            let localSymbols = localStoragesXml |> XmlNode.fromString
            programBody.InsertAfter localSymbols |> ignore

            programTemplate


    and XgiProjectParams with
        member private x.GetTemplateXmlDoc() =
            x.ExistingLSISprj
            |> Option.map XmlDocument.loadFromFile
            |? getTemplateXgiXmlDoc()

        member x.GenerateXmlString() = x.GenerateXmlDocument().Beautify()
        member x.GenerateXmlDocument() : XmlDocument =
            let { ProjectName=projName; ProjectComment=projComment; GlobalStorages=globalStorages;
                  EnableXmlComment = enableXmlComment; POUs=pous } = x

            // todo : 사전에 처리 되었어야...
            for g in globalStorages.Values do
                g.IsGlobal <- true

            EnableXmlComment <- enableXmlComment
            let xdoc = x.GetTemplateXmlDoc()
            (* validation : POU 중복 이름 체크 *)
            do
                let programs = xdoc.SelectNodes("//POU/Programs/Program")
                let existingTaskPous = [
                    for p in programs do
                        let taskName = p.GetAttribute("Task");
                        let pouName = p.FirstChild.OuterXml
                        taskName, pouName
                    ]
                let newTaskPous = [ for p in pous -> p.TaskName, p.POUName ]
                let duplicated = existingTaskPous @ newTaskPous |> List.groupBy id |> List.filter(fun (_, v) -> v.Length > 1)
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
                xnTasks.RemoveChildren()  |> ignore
                let pous = pous |> List.distinctBy(fun pou -> pou.TaskName)
                for i, pou in pous.Indexed() do
                    let index = if i <= 1 then 0 else i-1
                    let kind = if i = 0 then 0 else 2
                    let priority = kind

                    createXmlStringTask pou.TaskName kind priority index
                    |> XmlNode.fromString
                    |> xnTasks.AdoptChild
                    |> ignore

            (* Global variables 삽입 *)
            do
                let xnGlobalVar = xdoc.SelectSingleNode("//Configurations/Configuration/GlobalVariables/GlobalVariable")
                let countExistingGlobal = xnGlobalVar.Attributes.["Count"].Value |> System.Int32.Parse

                let existingGlobalSymbols = xnGlobalVar |> collectSymbolInfos

                (* existing global name 과 신규 global name 충돌 check *)
                do
                    let existingGlobalNames = existingGlobalSymbols |> map (name >> String.toUpper)
                    let currentGlobalNames = globalStorages.Keys |> map String.toUpper
                    match existingGlobalNames.Intersect(currentGlobalNames) |> Seq.tryHead with
                    | Some duplicated -> failwith $"ERROR: Duplicated global variable name : {duplicated}"
                    | _ -> ()

                (* existing global address 와 신규 global address 충돌 check *)
                do
                    let collectToUpper (addrs:string seq) = addrs |> filter (fun s -> s.NonNullAny()) |> map String.toUpper
                    let existingGlobalAddresses =
                        existingGlobalSymbols |> map (fun s -> s.Address) |> collectToUpper
                    let currentGlobalAddresses =
                        globalStorages.Values
                        |> filter(fun s -> s :? ITag || s :? IVariable)
                        |> map (fun s -> s.Address) |> collectToUpper
                    match existingGlobalAddresses.Intersect(currentGlobalAddresses) |> Seq.tryHead with
                    | Some duplicated -> failwith $"ERROR: Duplicated address usage : {duplicated}"
                    | _ -> ()
                    // todo : 실제로는 더 정밀한 충돌 check 필요.  %MX1 과 %MB0 은 서로 충돌하는 영역임.

                // symbolsGlobal = "<GlobalVariable Count="1493"> <Symbols> <Symbol> ... </Symbol> ... <Symbol> ... </Symbol>
                let globalStoragesXmlNode = storagesToGlobalXml x globalStorages.Values |> XmlNode.fromString
                let numNewGlobals = globalStoragesXmlNode.Attributes.["Count"].Value |> System.Int32.Parse

                xnGlobalVar.Attributes.["Count"].Value <- sprintf "%d" (countExistingGlobal + numNewGlobals)
                let xnGlobalVarSymbols = xnGlobalVar.GetXmlNode "Symbols"

                globalStoragesXmlNode.SelectNodes(".//Symbols/Symbol").ToEnumerables()
                |> iter (xnGlobalVarSymbols.AdoptChild >> ignore)


            (* POU program 삽입 *)
            do
                let xnPrograms = xdoc.SelectSingleNode("//POU/Programs")
                xnPrograms.RemoveChildren() |> ignore
                for pou in pous do
                    pou.GenerateXmlNode(x) |> xnPrograms.AdoptChild |> ignore

            xdoc

