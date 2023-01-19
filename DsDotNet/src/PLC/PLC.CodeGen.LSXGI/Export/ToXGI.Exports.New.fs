namespace PLC.CodeGen.LSXGI

open System.Collections.Generic
open Engine.Core
open Engine.Common.FS

[<AutoOpen>]
module ToXGIFileNew =

    type XgiPOUParams = {
        /// POU name.  "DsLogic"
        Name: string
        /// POU ladder 최상단의 comment
        Comment: string
        LocalStorages:Storages
        CommentedStatements: CommentedStatement list
    }
    type XgiProgramParams = {
        /// Program (=task) name.  "Main Program", "Devices", "ExSystems"
        Name:string
        POUs: XgiPOUParams list
    }
    type XgiProjectParams = {
        GlobalStorages:Storages
        ExistingLSISprj: string option
        Programs: XgiProgramParams list
    }

    (* Storages -> [XgiSymbol] -> [SymbolInfo] -> Xml *)
    let private storagesToXml(isLocal:bool) (storages:Storages) = ()
    let storagesToLocalVarXml(storages:Storages) =
        ()

    (*
    type XgiPOUParams with
        member x.GenerateXmlString(taskName:string) =
            let {Name=pouName; Comment=comment; LocalStorages=localStorages; CommentedStatements=commentedStatements} = x
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



            //let programTemplate = DsXml.adoptChild programs programTemplate

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
    *)


    /// rung 및 local var 에 대한 문자열 xml 을 전체 xml project file 에 embedding 시켜 outputPath 파일에 저장한다.
    /// Template file (EmptyLSISProject.xml) 에서 marking 된 위치를 참고로 하여 rung 및 local var 위치 파악함.
    (*
         symbolsLocal = "<LocalVar Version="Ver 1.0" Count="1493"> <Symbols> <Symbol> ... </Symbol> ... <Symbol> ... </Symbol> </Symbols> .. </LocalVar>
         symbolsGlobal = "<GlobalVariable Version="Ver 1.0" Count="1493"> <Symbols> <Symbol> ... </Symbol> ... <Symbol> ... </Symbol> </Symbols> .. </GlobalVariable>
    *)
    let xxxwrapWithXml2 (rungs:XmlOutput) symbolsLocal symbolsGlobal (existingLSISprj:string option) =
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