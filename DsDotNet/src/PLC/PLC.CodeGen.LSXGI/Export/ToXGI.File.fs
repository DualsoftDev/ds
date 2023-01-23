namespace PLC.CodeGen.LSXGI

open System.Reflection

open Engine.Common.FS
open PLC.CodeGen.LSXGI
open PLC.CodeGen.LSXGI.Config.POU.Program.LDRoutine

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
    let getTemplateXgiXmlDoc = getTemplateXgiXml >> XmlDocument.fromString


    /// rung 및 local var 에 대한 문자열 xml 을 전체 xml project file 에 embedding 시켜 outputPath 파일에 저장한다.
    /// Template file (EmptyLSISProject.xml) 에서 marking 된 위치를 참고로 하여 rung 및 local var 위치 파악함.
    (*
         symbolsLocal =        "<LocalVar Version="Ver 1.0" Count="1493"> <Symbols> <Symbol> ... </Symbol> ... <Symbol> ... </Symbol> </Symbols> .. </LocalVar>
         symbolsGlobal = "<GlobalVariable Version="Ver 1.0" Count="1493"> <Symbols> <Symbol> ... </Symbol> ... <Symbol> ... </Symbol> </Symbols> .. </GlobalVariable>
    *)
    let wrapWithXml (rungs:XmlOutput) symbolsLocal symbolsGlobal (existingLSISprj:string option) =
        let xdoc =
            existingLSISprj
            |> Option.map XmlDocument.loadFromFile
            |? getTemplateXgiXmlDoc()

        let pouName = "DsLogic"
        if null <> xdoc.SelectSingleNode(sprintf "//POU/Programs/Program/%s" pouName) then
            failwithlogf "POU name %s already exists.  Can't overwrite." pouName

        let programs = xdoc.SelectSingleNode("//POU/Programs")

        // Dirty hack "Scan Program" vs "?? ????"
        let taskName =
            xdoc.SelectNodes("//POU/Programs/Program").ToEnumerables()
            |> map (fun xmlnode -> xmlnode.Attributes.["Task"].Value)
            |> Seq.tryHead
            |> Option.defaultValue "Scan Program"

        printfn "%A" taskName


        /// POU/Programs/Program
        let programTemplate =
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
            |> XmlNode.fromString



        let programTemplate = programs.AdoptChild  programTemplate

        /// LDRoutine 위치 : Rung 삽입 위치
        let posiLdRoutine = programTemplate.GetXmlNode "Body/LDRoutine"
        let onlineUploadData = posiLdRoutine.FirstChild

        (*
         * Rung 삽입
         *)
        let rungsXml = $"<Rungs>{rungs}</Rungs>" |> XmlNode.fromString
        for r in rungsXml.GetChildrenNodes() do
            onlineUploadData.InsertBefore r |> ignore

        (*
         * Local variables 삽입
         *)
        let programBody = posiLdRoutine.ParentNode
        let localSymbols = symbolsLocal |> XmlNode.fromString
        programBody.InsertAfter localSymbols |> ignore

        (*
         * Global variables 삽입
         *)
        let posiGlobalVar = xdoc.SelectSingleNode("//Configurations/Configuration/GlobalVariables/GlobalVariable")
        let countExistingGlobal = posiGlobalVar.Attributes.["Count"].Value |> System.Int32.Parse
        let globalSymbolXmls =
            // symbolsGlobal = "<GlobalVariable Count="1493"> <Symbols> <Symbol> ... </Symbol> ... <Symbol> ... </Symbol>
            let neoGlobals = symbolsGlobal |> XmlNode.fromString
            let numNewGlobals = neoGlobals.Attributes.["Count"].Value |> System.Int32.Parse

            posiGlobalVar.Attributes.["Count"].Value <- sprintf "%d" (countExistingGlobal + numNewGlobals)
            let posiGlobalVarSymbols = posiGlobalVar.GetXmlNode "Symbols"

            neoGlobals.SelectNodes("//Symbols/Symbol").ToEnumerables()
            |> iter (posiGlobalVarSymbols.AdoptChild >> ignore)

        xdoc.ToText()


