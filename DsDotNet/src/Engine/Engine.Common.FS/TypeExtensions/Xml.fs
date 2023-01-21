namespace Engine.Common.FS

open System.Linq
open System.Xml
open System.Xml.Linq
open System.Runtime.CompilerServices
open System.Text

//open FSharpPlus

[<AutoOpen>]
module DsXml =
    /// x.InnerText 을 반환
    let inline innerText x = ( ^T: (member InnerText:string) x )
    /// x.OuterXml 을 반환
    let inline outerXml x = ( ^T: (member OuterXml:string) x )
    /// x.InnerXml 을 반환
    let inline innerXml x = ( ^T: (member InnerXml:string) x )
    /// x.ChildNodes 을 반환
    let inline childNodes x =
        ( ^T: (member ChildNodes:System.Xml.XmlNodeList) x )
        |> fun xs -> xs.Cast<XmlNode>()

    /// XElement -> XmlNode
    let xElementToXmlNode (xElement:XElement) =
        let reader = xElement.CreateReader()
        let doc = new XmlDocument()
        doc.ReadNode(reader)

    /// XmlNode -> string
    let xmlNodeToXml (xmlNode:XmlNode) = xmlNode.OuterXml

    type XmlDocument with
        member x.Beautify() =
            x.OuterXml
            //// https://stackoverflow.com/questions/203528/what-is-the-simplest-way-to-get-indented-xml-with-line-breaks-from-xmldocument
            //let sb = new StringBuilder()

            //let settings = new XmlWriterSettings (
            //    Indent = true
            //    , IndentChars = "\t"
            //    , NewLineChars = "\r\n"
            //    , NewLineHandling = NewLineHandling.Replace
            //    , Encoding = Encoding.UTF8
            //    //, NewLineOnAttributes = true
            //)

            //use writer = XmlWriter.Create(sb, settings)
            //x.Save(writer)
            //sb.ToString()

    /// XmlNode -> XElement
    let xmlNodeToXElement (xmlNode:XmlNode) = xmlNodeToXml xmlNode |> XElement.Parse
    /// string -> XElement
    let xmlToXElement xmlContent = XDocument.Parse(xmlContent).Root
    /// string -> XmlNode
    let xmlToXmlNode = xmlToXElement >> xElementToXmlNode

    /// Load from XML string
    let loadXml (xml:string) =
        let xdoc = System.Xml.XmlDocument()
        xdoc.LoadXml xml
        xdoc

    /// Load from XML file
    let load (xmlFile:string) =
        let xdoc = System.Xml.XmlDocument()
        xdoc.Load xmlFile
        xdoc

    /// seed xml node 에서 path 를 만족하는 [xml node] 반환
    let getXmlNodes path (seed:XmlNode) =
        seed.SelectNodes(path).Cast<XmlNode>()

    /// seed xml node 에서 path 를 만족하는 하나의 xml node 반환
    let getXmlNode path (seed:XmlNode) =
        seed.SelectSingleNode(path)

    /// parent.ChildNodes 을 반환
    let getChildrenNodes (parent:XmlNode) =
        parent.ChildNodes.Cast<XmlNode>()

    /// 새로운 child 를 추가.
    let adoptChild (parent:XmlNode) child =
        // Error: "The node to be inserted is from a different document context"
        // https://stackoverflow.com/questions/3019136/error-the-node-to-be-inserted-is-from-a-different-document-context
        //necessary for crossing XmlDocument contexts
        let adopted = parent.OwnerDocument.ImportNode(child, true)
        parent.AppendChild(adopted)

    let insertAfter newChild (refChild:XmlNode) =
        let parent = refChild.ParentNode
        let adopted = parent.OwnerDocument.ImportNode(newChild, true)
        parent.InsertAfter(adopted, refChild)

    let insertBefore newChild (refChild:XmlNode) =
        let parent = refChild.ParentNode
        let adopted = parent.OwnerDocument.ImportNode(newChild, true)
        parent.InsertBefore(adopted, refChild)

    /// node 삭제
    let removeNode (victim:XmlNode) =
        let parent = victim.ParentNode
        parent.RemoveChild(victim)

    /// node 삭제
    let removeChildren (parent:XmlNode) =
        [ for child in getChildrenNodes parent do
            parent.RemoveChild(child) ]

    /// Child node 바꿔치기
    let replaceChild (oldChild:XmlNode) (newChild:XmlNode) =
        let parent = oldChild.ParentNode
        parent.ReplaceChild(oldChild, newChild)


    let adoptChildUnit parent xn = adoptChild   parent xn |> ignore
    let removeNodeUnit   xn      = removeNode   xn        |> ignore
    let insertAfterUnit  xn xr   = insertAfter  xn xr     |> ignore
    let insertBeforeUnit xn xr   = insertBefore xn xr     |> ignore

[<Extension>]
type XmlExt =
    // https://stackoverflow.com/questions/21871908/converting-xmlnodelist-to-liststring
    [<Extension>]
    static member ToEnumerables(xmlNodeList:XmlNodeList) =
        //System.Linq.Enumerable.Cast<XmlNode>(xmlNodeList)
        xmlNodeList.Cast<XmlNode>()

    [<Extension>]
    static member ToStrings(xmlNodeList:XmlNodeList) =
        xmlNodeList.ToEnumerables()
        |> Seq.map outerXml

