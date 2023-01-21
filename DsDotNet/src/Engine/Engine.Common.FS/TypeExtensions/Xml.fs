namespace Engine.Common.FS

open System.Linq
open System.Xml
open System.Xml.Linq
open System.Runtime.CompilerServices
open System.Text

[<AutoOpen>]
module XmlNodeExtension =
    type XElement with
        /// XElement -> XmlNode
        member x.ToXmlNode() =
            let reader = x.CreateReader()
            let doc = new XmlDocument()
            doc.ReadNode(reader)
    type XmlNode with
        member x.ToText() = x.OuterXml

    [<RequireQualifiedAccess>]
    module XElement =
        let fromString(str:string) = XDocument.Parse(str).Root

    [<RequireQualifiedAccess>]
    module XmlNode =
        let fromString(str:string) = (XElement.fromString str).ToXmlNode()

    [<RequireQualifiedAccess>]
    module XmlDocument =
        /// Load from XML string
        let fromString(xml:string) =
            let xdoc = System.Xml.XmlDocument()
            xdoc.LoadXml xml
            xdoc
        /// Load from XML file
        let loadFromFile (xmlFile:string) =
            let xdoc = System.Xml.XmlDocument()
            xdoc.Load xmlFile
            xdoc

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
    let unused_xmlNodeToXElement (xmlNode:XmlNode) = xmlNode.ToText() |> XElement.Parse


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

