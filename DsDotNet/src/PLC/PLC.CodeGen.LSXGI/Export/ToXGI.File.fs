namespace PLC.CodeGen.LSXGI

open System.Reflection
open Engine.Common.FS
open Engine.Core
open PLC.CodeGen.LSXGI
open PLC.CodeGen.Common
open PLC.CodeGen.LSXGI.Config.POU.Program.LDRoutine
open PLC.CodeGen.Common.NewIEC61131

[<AutoOpen>]
module internal XgiFile =
    let [<Literal>] XGIMaxX = 28

    /// text comment 를 xml wrapping 해서 반환
    let getCommentRung y cmt =
        let yy = y * 1024 + 1
        $"\t<Rung BlockMask=\"0\"><Element ElementType=\"{int ElementType.RungCommentMode}\" Coordinate=\"{yy}\">{cmt}</Element></Rung>"


    /// Program 마지막 부분에 END 추가
    let generateEnd y =
        let yy = y * 1024 + 1
        sprintf """
            <Rung BlockMask="0">
                <Element ElementType="%d" Coordinate="%d" Param="90"></Element>
			    <Element ElementType="%d" Coordinate="%d" Param="END">END</Element>
			</Rung>""" (int ElementType.MultiHorzLineMode) yy (int ElementType.FBMode) (yy+93)


    /// (조건=coil) seq 로부터 rung xml 들의 string 을 생성
    let private generateRungs (prologComments:string seq) (commentedStatements:CommentedXgiStatements seq) : XmlOutput =
        let xmlRung (expr:FlatExpression option) xgiCommand y : RungGenerationInfo =
            let {Coordinate=c; Xml=xml} = rung (0, y) expr xgiCommand
            let yy = c / 1024
            { Xmls = [$"\t<Rung BlockMask={dq}0{dq}>\r\n{xml}\t</Rung>"]; Y = yy}

        let mutable rgi:RungGenerationInfo = {Xmls = []; Y = 0}

        // Prolog 설명문
        if prologComments.any() then
            let cmt = prologComments |> String.concat "\r\n"
            let xml = getCommentRung rgi.Y cmt
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
                | DuTimer timerStatement ->
                    let command = FunctionBlockCmd(TimerMode(timerStatement))
                    let rgiSub = xmlRung None (Some command) rgi.Y
                    rgi <- {Xmls = rgiSub.Xmls @ rgi.Xmls; Y = rgi.Y + rgiSub.Y}

                | DuCounter counterStatement ->
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

    /// Template XGI XML 문자열을 반환
    let getTemplateXgiXmlWithVersion version =
        let assembly = Assembly.GetExecutingAssembly()
        let filename = sprintf "xgi-%s.template.xml" version
        EmbeddedResource.readFile assembly filename

    /// Template XGI XML 문자열을 반환
    let getTemplateXgiXml() =
        match getTemplateXgiXmlWithVersion "4.5.2" with
        | Some(xml) -> xml
        | None ->
            failwithlogf "INTERNAL ERROR: failed to read resource template"

    /// Template XGI XML 문서 (XDocument) 반환
    let getTemplateXgiXmlDoc = getTemplateXgiXml >> DsXml.loadXml


    /// rung 및 local var 에 대한 문자열 xml 을 전체 xml project file 에 embedding 시켜 outputPath 파일에 저장한다.
    /// Template file (EmptyLSISProject.xml) 에서 marking 된 위치를 참고로 하여 rung 및 local var 위치 파악함.
    (*
         symbolsLocal = "<LocalVar Version="Ver 1.0" Count="1493"> <Symbols> <Symbol> ... </Symbol> ... <Symbol> ... </Symbol> </Symbols> .. </LocalVar>
         symbolsGlobal = "<GlobalVariable Version="Ver 1.0" Count="1493"> <Symbols> <Symbol> ... </Symbol> ... <Symbol> ... </Symbol> </Symbols> .. </GlobalVariable>
    *)
    let wrapWithXml (rungs:XmlOutput) symbolsLocal symbolsGlobal (existingLSISprj:string option) =
        let xdoc =
            existingLSISprj
            |> Option.map DsXml.load
            |? getTemplateXgiXmlDoc()

        let pouName = "DsLogic"
        if null <> xdoc.SelectSingleNode(sprintf "//POU/Programs/Program/%s" pouName) then
            failwithlogf "POU name %s already exists.  Can't overwrite." pouName

        let programs = xdoc.SelectSingleNode("//POU/Programs")

        // Dirty hack "스캔 프로그램" vs "?? ????"
        let taskName =
            xdoc.SelectNodes("//POU/Programs/Program").ToEnumerables()
            |> map (fun xmlnode -> xmlnode.Attributes.["Task"].Value)
            |> Seq.tryHead
            |> Option.defaultValue "스캔 프로그램"

        printfn "%A" taskName


        /// POU/Programs/Program
        let programTemplate =
            sprintf """
			    <Program Task="%s" Version="256" LocalVariable="1" Kind="0" InstanceName="" Comment="" FindProgram="1" FindVar="1" Encrytption="">%s
                    <Body>
					    <LDRoutine>
                            <COMMENT> ========= Rung(s) 삽입 위치 </COMMENT>
						    <OnlineUploadData Compressed="1" dt:dt="bin.base64" xmlns:dt="urn:schemas-microsoft-com:datatypes">QlpoOTFBWSZTWY5iHkIAAA3eAOAQQAEwAAYEEQAAAaAAMQAACvKMj1MnqSRSSVXekyB44y38
    XckU4UJCOYh5CA==</OnlineUploadData>
					    </LDRoutine>
				    </Body>
                    <COMMENT> ========= LocalVar 삽입 위치 </COMMENT>
				    <RungTable></RungTable>
			    </Program>""" taskName pouName
            |> DsXml.xmlToXmlNode



        let programTemplate = DsXml.adoptChild programs programTemplate

        /// LDRoutine 위치 : Rung 삽입 위치
        let posiLdRoutine = programTemplate |> DsXml.getXmlNode "Body/LDRoutine"
        let onlineUploadData = posiLdRoutine.FirstChild

        (*
         * Rung 삽입
         *)
        let rungsXml = $"<Rungs>{rungs}</Rungs>" |> DsXml.xmlToXmlNode
        for r in DsXml.getChildNodes rungsXml do
            DsXml.insertBeforeUnit r onlineUploadData

        (*
         * Local variables 삽입
         *)
        let programBody = posiLdRoutine.ParentNode
        let localSymbols = symbolsLocal |> DsXml.xmlToXmlNode
        DsXml.insertAfterUnit localSymbols programBody

        (*
         * Global variables 삽입
         *)
        let posiGlobalVar = xdoc.SelectSingleNode("//Configurations/Configuration/GlobalVariables/GlobalVariable")
        let countExistingGlobal = posiGlobalVar.Attributes.["Count"].Value |> System.Int32.Parse
        let globalSymbolXmls =
            // symbolsGlobal = "<GlobalVariable Count="1493"> <Symbols> <Symbol> ... </Symbol> ... <Symbol> ... </Symbol>
            let neoGlobals = symbolsGlobal |> DsXml.xmlToXmlNode
            let numNewGlobals = neoGlobals.Attributes.["Count"].Value |> System.Int32.Parse

            posiGlobalVar.Attributes.["Count"].Value <- sprintf "%d" (countExistingGlobal + numNewGlobals)
            let posiGlobalVarSymbols = DsXml.getXmlNode "Symbols" posiGlobalVar

            neoGlobals.SelectNodes("//Symbols/Symbol")
            |> XmlExt.ToEnumerables
            |> iter (DsXml.adoptChildUnit posiGlobalVarSymbols)

        xdoc.OuterXml


    type XgiSymbol =
        | DuTag         of ITagWithAddress
        | DuXgiLocalVar of IXgiLocalVar
        | DuTimer       of TimerStruct
        | DuCounter     of CounterBaseStruct


    let generateXGIXmlFromStatement
        (prologComments:string seq) (commentedStatements:CommentedXgiStatements seq)
        (xgiSymbols:XgiSymbol seq) (unusedTags:ITagWithAddress seq) (existingLSISprj:string option)
      =
        let symbolInfos =
            let kindVar = int Variable.Kind.VAR
            [
                for s in xgiSymbols do
                    match s with
                    | DuTag t ->
                        let name, addr = t.Name, t.Address

                        let device, memSize =
                            match t.Address with
                            | RegexPattern @"%([IQM])([XBWL]).*$" [iqm; mem] -> iqm, mem
                            | _ -> "????", "????"

                        let plcType =
                            match memSize with
                            | "X" -> "BOOL"
                            | "B" -> "BYTE"
                            | "W" -> "WORD"
                            | "L" -> "DWORD"
                            | _ -> failwithlog "ERROR"
                        let comment = "FAKECOMMENT"

                        let initValue = null // PLCTag 는 값을 초기화 할 수 없다.
                        { defaultSymbolCreateParam with Name=name; Comment=comment; PLCType=plcType; Address=addr; InitValue=initValue; Device=device; Kind=kindVar; }
                        |> XGITag.createSymbolInfoWithDetail

                    | DuXgiLocalVar xgi ->
                        xgi.SymbolInfo
                    | DuTimer timer ->
                        let device, addr = "", ""
                        let plcType =
                            match timer.Type with
                            | TON | TOF | TMR -> timer.Type.ToString()

                        let param:XgiSymbolCreateParams =
                            let name, comment = timer.Name, $"TIMER {timer.Name}"
                            { defaultSymbolCreateParam with Name=name; Comment=comment; PLCType=plcType; Address=addr; InitValue=null; Device=device; Kind=kindVar; }
                        XGITag.createSymbolInfoWithDetail param
                    | DuCounter counter ->
                        let device, addr = "", ""
                        let plcType =
                            match counter.Type with
                            | CTU | CTD | CTUD -> $"{counter.Type}_INT"       // todo: CTU_{INT, UINT, .... } 등의 종류가 있음...
                            | CTR -> $"{counter.Type}"

                        let param:XgiSymbolCreateParams =
                            let name, comment = counter.Name, $"COUNTER {counter.Name}"
                            { defaultSymbolCreateParam with Name=name; Comment=comment; PLCType=plcType; Address=addr; InitValue=null; Device=device; Kind=kindVar; }
                        XGITag.createSymbolInfoWithDetail param
            ]

        /// Symbol table 정의 XML 문자열
        let symbolsLocalXml = XGITag.generateLocalSymbolsXml symbolInfos

        let globalSym = [
            for s in symbolInfos do
                if not (s.Device.IsNullOrEmpty()) then
                    XGITag.copyLocal2GlobalSymbol s
        ]

        let symbolsGlobalXml = XGITag.generateGlobalSymbolsXml globalSym

        let rungsXml = generateRungs prologComments commentedStatements

        logInfo "Finished generating PLC code."
        wrapWithXml rungsXml symbolsLocalXml symbolsGlobalXml existingLSISprj

