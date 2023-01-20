namespace PLC.CodeGen.LSXGI

open System.Linq
open Engine.Core
open Engine.Common.FS
open System.Xml

[<AutoOpen>]
module ToXGIFileNew =

    type XgiPOUParams = {
        /// POU name.  "DsLogic"
        POUName: string
        /// POU container task name
        TaskName: string
        /// POU ladder 최상단의 comment
        Comment: string
        LocalStorages:Storages
        CommentedStatements: CommentedStatement list
    }
    type XgiProjectParams = {
        GlobalStorages:Storages
        ExistingLSISprj: string option
        POUs: XgiPOUParams list
    }

    type XgiPOUParams with
        member x.GenerateXmlString() = x.GenerateXmlNode().OuterXml
        member x.GenerateXmlNode() : XmlNode =
            let {TaskName=taskName; POUName=pouName; Comment=comment; LocalStorages=localStorages; CommentedStatements=commentedStatements} = x
            let newLocalStorages, commentedXgiStatements = commentedStatementsToCommentedXgiStatements localStorages.Values commentedStatements
            let localStoragesXml = storagesToLocalXml newLocalStorages
            let rungsXml = generateRungs comment commentedXgiStatements

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
            let rungsXml = $"<Rungs>{rungsXml}</Rungs>" |> DsXml.xmlToXmlNode
            for r in DsXml.getChildrenNodes rungsXml do
                DsXml.insertBeforeUnit r onlineUploadData

            (*
             * Local variables 삽입
             *)
            let programBody = posiLdRoutine.ParentNode
            let localSymbols = localStoragesXml |> DsXml.xmlToXmlNode
            DsXml.insertAfterUnit localSymbols programBody

            programTemplate


    type XgiProjectParams with
        member private x.GetTemplateXmlDoc() =
            x.ExistingLSISprj
            |> Option.map DsXml.load
            |? getTemplateXgiXmlDoc()

        member x.GenerateXmlString() = x.GenerateXmlDocument().OuterXml
        member x.GenerateXmlDocument() : XmlDocument =
            let { GlobalStorages=globalStorages; ExistingLSISprj=existingLSISprj; POUs=pous } = x
            let xdoc = x.GetTemplateXmlDoc()

            (* xn = Xml Node *)

            (* Tasks/Task 삽입 *)
            do
                let xnTasks = xdoc.SelectSingleNode("//Configurations/Configuration/Tasks")
                DsXml.removeChildren xnTasks |> ignore
                let pous = pous |> List.distinctBy(fun pou -> pou.TaskName)
                for i, pou in pous.Indexed() do
                    let index = if i <= 1 then 0 else i-1
                    let kind = if i = 0 then 0 else 2
                    let priority = kind
                    $"""<Task Version="257" Type="0" Attribute="2" Kind="{kind}" Priority="{priority}" TaskIndex="{index}" """
                    + $"""Device="" DeviceType="0" WordValue="0" WordCondition="0" BitCondition="0">{pou.TaskName}</Task>"""
                    |> xmlToXmlNode
                    |> DsXml.adoptChildUnit xnTasks

            (* Global variables 삽입 *)
            do
                let xnGlobalVar = xdoc.SelectSingleNode("//Configurations/Configuration/GlobalVariables/GlobalVariable")
                let countExistingGlobal = xnGlobalVar.Attributes.["Count"].Value |> System.Int32.Parse
                // symbolsGlobal = "<GlobalVariable Count="1493"> <Symbols> <Symbol> ... </Symbol> ... <Symbol> ... </Symbol>
                let globalStoragesXmlNode = storagesToGlobalXml globalStorages.Values |> DsXml.xmlToXmlNode
                let numNewGlobals = globalStoragesXmlNode.Attributes.["Count"].Value |> System.Int32.Parse

                xnGlobalVar.Attributes.["Count"].Value <- sprintf "%d" (countExistingGlobal + numNewGlobals)
                let xnGlobalVarSymbols = DsXml.getXmlNode "Symbols" xnGlobalVar

                globalStoragesXmlNode.SelectNodes("//Symbols/Symbol")
                |> XmlExt.ToEnumerables
                |> iter (DsXml.adoptChildUnit xnGlobalVarSymbols)


            (* POU program 삽입 *)
            do
                let xnPrograms = xdoc.SelectSingleNode("//POU/Programs")
                for pou in pous do
                    pou.GenerateXmlNode() |> DsXml.adoptChildUnit xnPrograms

            xdoc

    // todo : 위의 코드를 이용해서 wrapWithXml 수정

