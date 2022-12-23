namespace PLC.CodeGen.LSXGI

open System.IO
open System.Reflection
open Engine.Common.FS
open Engine.Core
open PLC.CodeGen.LSXGI
open PLC.CodeGen.Common
open PLC.CodeGen.LSXGI.Config.POU.Program.LDRoutine
open PLC.CodeGen.Common.NewIEC61131
open PLC.CodeGen.Common.QGraph

[<AutoOpen>]
module internal XgiFile =
    let [<Literal>] XGIMaxX = 28

    /// text comment 를 xml wrapping 해서 반환
    let getCommentRung y cmt =
        let yy = y * 1024 + 1
        $"\t<Rung BlockMask=\"0\"><Element ElementType=\"{int ElementType.RungCommentMode}\" Coordinate=\"{yy}\">{cmt}</Element></Rung>"

    // <kwak>
    /// 추상적인 Rung info expression 으로부터 XGI ladder rung statement 를 생성한다.
    //let rungInfoToStatement (opt:CodeGenerationOption) (gri:(IExpressionTerminal * seq<PositionedRungXml>)) =
    //    let z = snd gri |> map(rungInfoToExpr)
    //    let condition = snd gri |> map(rungInfoToExpr) |> Seq.reduce mkOr
    //    let coil = (snd gri |> Seq.head).CoilOrigin
    //    let endCommand =
    //        match coil with
    //            | Coil(coil) ->
    //                match coil.Terminal with
    //                | Terminal(term) -> term |> createOutputCoil
    //                | _ -> failwithlogf "This Coil is not Terminal"
    //            | Function(func) ->
    //                match func with
    //                | :? FunctionPure as fp ->
    //                    match fp with
    //                    | CopyMode(tag, (tag1, tag2)) -> createOutputCopy(tag, tag1, tag2)
    //                    | CompareGT(tag, (tag1, tag2)) -> createOutputCompare(tag, GT, tag1, tag2)
    //                    | CompareLT(tag, (tag1, tag2)) -> createOutputCompare(tag, LT, tag1, tag2)
    //                    | CompareGE(tag, (tag1, tag2)) -> createOutputCompare(tag, GE, tag1, tag2)
    //                    | CompareLE(tag, (tag1, tag2)) -> createOutputCompare(tag, LE, tag1, tag2)
    //                    | CompareEQ(tag, (tag1, tag2)) -> createOutputCompare(tag, EQ, tag1, tag2)
    //                    | CompareNE(tag, (tag1, tag2)) -> createOutputCompare(tag, NE, tag1, tag2)
    //                    | Add(tag, target, value) -> createOutputAdd(tag, target, value)
    //                | :? FunctionBlock as fb ->
    //                    match fb with
    //                    | TimerMode(tag, time) -> createOutputTime(tag, time)
    //                    | CounterMode(tag, resetTag, count) -> createOutputCount(tag, resetTag, count)
    //                | :? CoilOutput as co ->
    //                    match co with
    //                    | CoilMode(tag) -> createOutputCoil(tag)
    //                    | PulseCoilMode(tag) -> createOutputPulse(tag)
    //                    | NPulseCoilMode(tag) -> createOutputNPulse(tag)
    //                    | ClosedCoilMode(tag) -> createOutputCoilNot(tag)
    //                    | SetCoilMode(tag) -> createOutputSet(tag)
    //                    | ResetCoilMode(tag) -> createOutputRst(tag)
    //                | _ -> failwithlogf "This Function is not support"
    //            | _ -> coil.GetCoilTerminal() |> createOutputCoil

    //    let comments =
    //        snd gri
    //        |> List.ofSeq
    //        |> List.collect(fun ri -> ri.Comments)
    //        |> List.map (fun c -> System.Xml.Linq.XText(c).ToString())      // 특수 문자 XML 대응

    //    Statement(condition, endCommand, comments)

    // <kwak>
    //let statementToTag (statements:Statement seq) =

    //    let terminals =
    //        statements
    //        |> Seq.collect(fun stmt ->
    //            let command = stmt.Command.UsedCommandTags
    //            let coil = stmt.Command.CoilTerminalTag
    //            let cond = stmt.Condition |> collectTerminals

    //            cond @ seq{coil} @ command
    //        )

    //    let plcTags = terminals |> Seq.where(fun t -> t :? PLCTag) |> Seq.distinct |> Seq.cast<PLCTag>
    //    let newTags = terminals |> Seq.where(fun t -> t :? PLCTag |> not)
    //                            |> Seq.distinctBy(fun t -> t.ToText())
    //                            |> Seq.map(fun t ->
    //                                match t with
    //                                | :? Coil as tag -> PLCTag(tag.ToText(), TagType.Dummy |> Some)
    //                                | :? CommandTag as cmdTag ->
    //                                                let newTag = PLCTag(cmdTag.ToText(), TagType.Dummy |> Some)
    //                                                newTag.Size <- cmdTag.Size()
    //                                                newTag
    //                                | _ -> PLCTag(t.ToText(), TagType.Dummy |> Some) )

    //    let instanceTags =
    //               statements
    //               |> Seq.where(fun stmt ->  stmt.Command.HasInstance)
    //               |> Seq.map(fun stmt ->  stmt.Command.Instance
    //                                       |> fun (inst, instType)
    //                                           -> PLCTag(inst, TagType.Instance |> Some, "", [|K.FBInstance, box(instType.ToString())|]) )

    //    plcTags @ newTags @ instanceTags


    /// Program 마지막 부분에 END 추가
    let generateEnd y =
        let yy = y * 1024 + 1
        sprintf """
            <Rung BlockMask="0">
                <Element ElementType="%d" Coordinate="%d" Param="90"></Element>
			    <Element ElementType="%d" Coordinate="%d" Param="END">END</Element>
			</Rung>""" (int ElementType.MultiHorzLineMode) yy (int ElementType.FBMode) (yy+93)


    /// X 항목으로 Max 32개 넘는지 여부 체크
    ///32개 기준 FB x 3개 +  Coil x 1개 사용기준
    let rec getXGIMaxX (x:int) (expr:FlatExpression) =
        match expr with
        | FlatTerminal(id, pulse, neg) -> x
        | FlatZero -> x
        | FlatNary(And, exprs) ->
            let mutable sx = x
            for exp in exprs do
                sx <- getXGIMaxX sx exp + 1
            sx-1// for loop 에서 마지막 +1 된 것 revert
        | FlatNary(Or, exprs) ->
            let mutable maxX = x
            for (i, exp) in (exprs |> Seq.indexed) do
                let sub = getXGIMaxX x exp
                maxX <- max maxX sub
            maxX
        | _ ->
            failwithlog "Unknown FlatExpression case"


    type RungGenerationInfo = {
        Xmls: XmlOutput list   // Rung 별 누적 xml.  역순으로 추가.  꺼낼 때 뒤집어야..
        Y: int }
    with
        member x.Add(xml) = {Xmls = xml::x.Xmls; Y = x.Y + 1 }
    /// (조건=coil) seq 로부터 rung xml 들의 string 을 생성
    let private generateRungs (prologComments:string seq) (commentedStatements:CommentedStatement seq) : XmlOutput =
        let xmlRung (expr:FlatExpression) xgiCommand y : RungGenerationInfo=
            let {Position=posi; Xml=xml} = rung 0 y expr xgiCommand
            { Xmls = [$"\t<Rung BlockMask={dq}0{dq}>\r\n{xml}\t</Rung>"]; Y = posi}

        let mutable rgi:RungGenerationInfo = {Xmls = []; Y = 0}

        // Prolog 설명문
        if prologComments.any() then
            let cmt = prologComments |> String.concat "\r\n"
            let xml = getCommentRung rgi.Y cmt
            rgi <- rgi.Add(xml)

        // Rung 별로 생성
        for CommentAndStatement(cmt, stmt) in commentedStatements do

            // 다중 라인 설명문을 하나의 설명문 rung 에..
            if cmt.NonNullAny() then
                let xml =getCommentRung rgi.Y cmt
                rgi <- rgi.Add(xml)


            //<kwak> 대체 version
            match stmt with
            | DuAssign (expr, (:? IExpressionTerminal as target)) ->
                let flatExpr = expr.Flatten() :?> FlatExpression
                let command:XgiCommand = CoilCmd(CoilMode(target)) |> XgiCommand
                let rgiSub = xmlRung flatExpr command rgi.Y
                rgi <- {Xmls = rgiSub.Xmls @ rgi.Xmls; Y = rgi.Y + rgiSub.Y}

            // <kwak> <timer>
            | DuTimer timerStatement ->
                let rungin = timerStatement.RungInCondition.Value :> IExpression
                let rungin = rungin.Flatten() :?> FlatExpression

                let command:XgiCommand = FunctionBlockCmd(TimerMode(timerStatement)) |> XgiCommand
                let rgiSub = xmlRung rungin command rgi.Y
                rgi <- {Xmls = rgiSub.Xmls @ rgi.Xmls; Y = rgi.Y + rgiSub.Y}

            | ( DuCounter _ | DuCopy _ ) ->
                failwith "Not yet"

            | DuVarDecl _ -> failwith "ERROR: Invalid"
            | _  -> failwith "ERROR"


            //<kwak> origina version
            //let expr = stmt.Condition |> FlatExpressionM.flatten
            //let xml, y' =
            //    if(getXGIMaxX 0 expr > XGIMaxX) then
            //        let exprNew =  stmt.Condition |> ExpressionM.mkNeg |> FlatExpressionM.flatten
            //        if(getXGIMaxX 0 exprNew > XGIMaxX) then
            //            failwithlog $"Or Expreesion Limit {XGIMaxX}"
            //        else
            //            let commandNew = stmt.Command.ReverseCmd()
            //            xmlRung exprNew commandNew y
            //    else
            //        xmlRung expr stmt.Command y

        let rungEnd = generateEnd rgi.Y
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
    //
    // symbolsLocal = "<LocalVar Version="Ver 1.0" Count="1493"> <Symbols> <Symbol> ... </Symbol> ... <Symbol> ... </Symbol> </Symbols> .. </LocalVar>
    // symbolsGlobal = "<GlobalVariable Version="Ver 1.0" Count="1493"> <Symbols> <Symbol> ... </Symbol> ... <Symbol> ... </Symbol> </Symbols> .. </GlobalVariable>
    let wrapWithXml rungs symbolsLocal symbolsGlobal (existingLSISprj:string option) =
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

            //let xxx = neoGlobals.SelectNodes("//Symbols/*") |> XmlExt.ToEnumerables |> toArray
            //let xx1 = neoGlobals.SelectNodes("//Symbols/Symbol") |> XmlExt.ToEnumerables |> toArray
            //let xx2 = neoGlobals.SelectNodes("//Symbols/Symbol*") |> XmlExt.ToEnumerables |> toArray
            //let xx3 = neoGlobals.SelectNodes("//GlobalVariable/Symbols/Symbol") |> XmlExt.ToEnumerables |> toArray
            //let xx4 = neoGlobals.SelectNodes("//*Symbol") |> XmlExt.ToEnumerables |> toArray

            neoGlobals.SelectNodes("//Symbols/Symbol")
            |> XmlExt.ToEnumerables
            //|> DsXml.getChildNodes
            |> iter (DsXml.adoptChildUnit posiGlobalVarSymbols)



        //tracefn "%s" posiGlobalVar.OuterXml
        //tracefn "%s" posiLdRoutine.OuterXml
        //tracefn "%s" xdoc.OuterXml

        xdoc.OuterXml



        //tracefn "%s" programTemplate.OuterXml

        //let templateFiles =
        //    emptyLSISprj
        //    |> Option.defaultWith(fun () ->
        //        let entry = System.Reflection.Assembly.GetEntryAssembly()
        //        let dir = Path.GetDirectoryName(entry.Location)
        //        Path.Combine(dir, "EmptyLSISProject.xml")
        //    )
        //let allLines =
        //    seq {
        //        for line in File.ReadAllLines(templateFiles) do
        //            match line with
        //            | ActivePattern.RegexPattern "\s*<InsertPoint Content=\"(\w+)\"></InsertPoint>" [insertType] ->
        //                match insertType with
        //                | "GlobalVariable" -> yield symbolsGlobal
        //                | "Rungs" -> yield rungs
        //                | "LocalVar" -> yield symbolsLocal
        //                | _ -> failwithlog "Unknown"
        //            | _ ->
        //                yield line
        //    }
        //allLines


    type XgiSymbol =
        | DuTag of ITagWithAddress
        | DuTimer of TimerStruct
        | DuCounter of CounterBaseStruct


    let generateXGIXmlFromStatement (prologComments:string seq) (commentedStatements:CommentedStatement seq) (xgiSymbols:XgiSymbol seq) (unusedTags:ITagWithAddress seq) (existingLSISprj:string option) =
        // TODO : 하드 코딩...  PLC memory 설정을 어디선가 받아서 처리해야 함.

        /// PLC memory manager
        let manager =
            /// PLC H/W memory configurations
            let hwconfs =
                let m (x:#MemoryConfigBase) = x :> MemoryConfigBase
                [
                    IQMemoryConfig(Memory.I, 0, 0, 64) |> m
                    IQMemoryConfig(Memory.Q, 0, 1, 64) |> m
                    MMemoryConfig(Memory.M, 640*1024*8) |> m     // 640KB memory
                    MMemoryConfig(Memory.R, 640*1024*8) |> m     // 640KB memory
                ]
            let manager = MemoryManager(hwconfs)

            //<kwak>
            /// 이미 할당된 주소 : 자동 할당시 이 주소를 피해야 한다.
            let alreadyAllocatedAddresses =
                /// 이미 할당된 주소 앞뒤로 buffer word 만큼 회피하기 위한 word address 를 생성한다.
                let toChunk bitAddress buffer =
                    let rec loop bitAddress =
                        [
                            match bitAddress with
                            | RegexPattern @"%M([BWDL])(\d+).(\d+)" [size; element; nth_] ->
                                yield! loop (sprintf "%%M%s%s" size element)
                            | RegexPattern @"%MX(\d+)" [Int32Pattern nth] ->
                                yield nth / 16
                            | RegexPattern @"%M([BWDL])(\d+)" [size; Int32Pattern element] ->
                                let ele =
                                    match size with
                                    | "B" -> element / 2
                                    | "W" -> element
                                    | "D" -> element * 2
                                    | "L" -> element * 4
                                    | _ ->
                                        failwith "ERROR"
                                yield ele
                            | _ ->
                                logWarn "Warn: unknown address [%s]" bitAddress
                                ()
                        ]

                    loop bitAddress
                    |> sort |> distinct
                    |> bind (fun n -> [n-buffer..n+buffer])
                    |> filter (fun n -> n >= 0)
                    |> map (sprintf "%%MW%d")



                let tagsUsedInFiles =
                    existingLSISprj
                    |> map (DsXml.load >> XGIXml.getGlobalAddresses)
                    |? Seq.empty
                    |> Seq.filter (fun t -> t.StartsWith("%M"))
                    |> Seq.bind (fun t -> toChunk t 10)

                let tags =
                    [ for s in xgiSymbols do
                        match s with
                        | DuTag t -> yield t.Address
                        | _ -> ()
                    ]

                let tagsUnusedInTags = [ for t in unusedTags -> t.Address] |> Set
                tagsUsedInFiles @ tags @ tagsUnusedInTags |> distinct

            alreadyAllocatedAddresses |> iter (fun t -> manager.MarkAllocated(t))
            manager

        //let generators =
        //    [
        //        "I",  fun () -> manager.AllocateTag(Memory.I, Size.X) |> Option.get
        //        "O",  fun () -> manager.AllocateTag(Memory.Q, Size.X) |> Option.get
        //        "M",  fun () -> manager.AllocateTag(Memory.R, Size.X) |> Option.get
        //        "IB", fun () -> manager.AllocateTag(Memory.I, Size.B) |> Option.get
        //        "OB", fun () -> manager.AllocateTag(Memory.Q, Size.B) |> Option.get
        //        "MB", fun () -> manager.AllocateTag(Memory.M, Size.B) |> Option.get
        //        "IW", fun () -> manager.AllocateTag(Memory.I, Size.W) |> Option.get
        //        "OW", fun () -> manager.AllocateTag(Memory.Q, Size.W) |> Option.get
        //        "MW", fun () -> manager.AllocateTag(Memory.M, Size.W) |> Option.get
        //        "ID", fun () -> manager.AllocateTag(Memory.I, Size.D) |> Option.get
        //        "OD", fun () -> manager.AllocateTag(Memory.Q, Size.D) |> Option.get
        //        "MD", fun () -> manager.AllocateTag(Memory.M, Size.D) |> Option.get
        //    ] |> Tuple.toDictionary

        let symbolInfos =
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
                            | _ -> failwith "ERROR"
                        let comment, kind = "FAKECOMMENT", Variable.Kind.VAR

                        //<kwak>
                        //let name, comment = t.FullName, t.Tag
                        //let plcType =
                        //    match t.IOType with
                        //    | Some tt when tt.Equals TagType.Instance -> t.FBInstance  //instance 타입은  주소에 저장 활용 (내부사용으로 주소값이 없음)
                        //    | _ ->  match t.Size with
                        //            | IEC61131.Size.Bit    ->  "BOOL"
                        //            | IEC61131.Size.Byte   ->  "BYTE"
                        //            | IEC61131.Size.Word   ->  "WORD"
                        //            | IEC61131.Size.DWord  ->  "DWORD"
                        //            | _ -> failwithlog "tag Size Unknown"

                        ///// one of {"I"; "O"; "M"}
                        //let device =
                        //    match t.Address with
                        //    | Some(addr) -> AddressM.getDevice addr
                        //    | _ ->
                        //        match t.IOType with
                        //        | Some tt when tt.Equals TagType.State -> "I"
                        //        | Some tt when tt.Equals TagType.Action -> "O"
                        //        | Some tt when tt.Equals TagType.Dummy -> "M"
                        //        | _ -> ""
                        //        //Trace.WriteLine("Unknown PLC device type: assume 'M'.")
                        //let addr =
                        //    if (t.Address.IsNullOrEmpty() && t.FBInstance.isNullOrEmpty())
                        //    then
                        //        let addr =
                        //            match t.Size with
                        //            | IEC61131.Size.Bit    ->  generators.[device]()
                        //            | IEC61131.Size.Byte   ->  generators.[device+"B"]()
                        //            | IEC61131.Size.Word   ->  generators.[device+"W"]()
                        //            | IEC61131.Size.DWord  ->  generators.[device+"D"]()
                        //            | _ -> failwithlog "tag gen Unknown"
                        //        t.Address <- AddressM.tryParse(addr)
                        //        t.AutoAddress <- true
                        //        addr
                        //    else t.StringAddress
                        //let kind =
                        //    match t.IOType with
                        //    | Some tt when tt.Equals TagType.Instance -> Variable.Kind.VAR
                        //    | _-> Variable.Kind.VAR_EXTERNAL

                        XGITag.createSymbol name comment device ((int)kind) addr plcType -1 "" //Todo : XGK 일경우 DevicePos, IEC Address 정보 필요
                    | DuTimer timer ->
                        let device, addr = "", ""
                        let kind = Variable.Kind.VAR
                        let plcType =
                            match timer.Type with
                            | TON | TOF | RTO -> timer.Type.ToString()

                        XGITag.createSymbol timer.Name $"TIMER {timer.Name}" device ((int)kind) addr plcType -1 "" //Todo : XGK 일경우 DevicePos, IEC Address 정보 필요
                    | DuCounter counter ->
                        failwith "Not Yet"
                        ()
            ]

        /// Symbol table 정의 XML 문자열
        let symbolsLocalXml = XGITag.generateSymbolVars (symbolInfos, false)

        let globalSym =
            [
                for s in symbolInfos do
                    if not (s.Device.IsNullOrEmpty()) then
                        XGITag.copyLocal2GlobalSymbol s
            ]

        let symbolsGlobalXml = XGITag.generateSymbolVars (globalSym, true)


        let rungsXml = generateRungs prologComments commentedStatements

        logInfo "Finished generating PLC code."
        wrapWithXml rungsXml symbolsLocalXml symbolsGlobalXml existingLSISprj

