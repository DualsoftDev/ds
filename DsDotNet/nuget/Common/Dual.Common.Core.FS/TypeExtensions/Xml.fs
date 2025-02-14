namespace Dual.Common.Core.FS

open System.Linq
open System.Xml
open System.Xml.Linq
open System.IO

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

        /// seed xml node 에서 path 를 만족하는 [xml node] 반환
        ///
        /// [주의사항]
        ///
        /// - someChildNode.GetXmlNodes("//SomePath") 와 같이 XPath 가 "//" 로 시작하면, 전체 document 에서 검색을 시작한다.
        ///
        /// - someChildNode.GetXmlNodes(".//SomePath") 와 같이 XPath 가 ".//" 로 시작하면, 현재 node 하부에서 검색을 시작한다.
        member x.GetXmlNodes(xpath:string) =
            x.SelectNodes(xpath).Cast<XmlNode>()

        /// seed xml node 에서 path 를 만족하는 하나의 xml node 반환
        member x.GetXmlNode(xpath:string) =
            x.SelectSingleNode(xpath)

        member x.TryGetAttribute(attributeName:string) =
            if x.Attributes <> null && x.Attributes[attributeName] <> null then
                Some x.Attributes[attributeName].Value
            else
                None
        member x.GetAttribute(attributeName:string) = x.TryGetAttribute(attributeName).Value
        member x.TryGetAttributes(attributeNames:string seq) =
            attributeNames
            |> Seq.map(fun n -> n, x.TryGetAttribute(n))
            |> Seq.filter (fun (n, a) -> a.IsSome)
            |> Seq.map (fun (n, a) -> n, a.Value)
            |> Tuple.toDictionary
        member x.GetAttributes() =
            [ for a in x.Attributes do
                a.Name, a.Value ]
            |> Tuple.toDictionary

        member x.AddAttribute(name, value) : XmlAttribute =
            let attr = x.OwnerDocument.CreateAttribute(name)
            attr.Value <- value
            x.Attributes.Append(attr)

        member x.AddAttributes( kvs: (string*obj) seq) : XmlAttribute array =
            [| for k, v in kvs do
                    x.AddAttribute(k, toString v) |]

        /// parent.ChildNodes 을 반환
        member parent.GetChildrenNodes() =
            parent.ChildNodes.Cast<XmlNode>()

        /// 새로운 child 를 추가.
        member parent.AdoptChild child =
            // Error: "The node to be inserted is from a different document context"
            // https://stackoverflow.com/questions/3019136/error-the-node-to-be-inserted-is-from-a-different-document-context
            //necessary for crossing XmlDocument contexts
            let adopted = parent.OwnerDocument.ImportNode(child, true)
            parent.AppendChild(adopted)

        member refChild.InsertAfter newChild =
            let parent = refChild.ParentNode
            let adopted = parent.OwnerDocument.ImportNode(newChild, true)
            parent.InsertAfter(adopted, refChild)

        member refChild.InsertBefore newChild =
            let parent = refChild.ParentNode
            let adopted = parent.OwnerDocument.ImportNode(newChild, true)
            parent.InsertBefore(adopted, refChild)

        /// node 삭제
        member victim.RemoveNode () =
            let parent = victim.ParentNode
            parent.RemoveChild(victim)

        /// node 삭제
        member parent.RemoveChildren() =
            [ for child in parent.GetChildrenNodes()  do
                parent.RemoveChild(child) ]

        member xmlNode.SelectMultipleNodes xpath =
            xmlNode.SelectNodes(xpath).Cast<XmlNode>()


    type XmlNodeList with
        member x.Do() = ()
        // https://stackoverflow.com/questions/21871908/converting-xmlnodelist-to-liststring
        member x.ToEnumerables() = x.Cast<XmlNode>()

    type XmlDocument with
        member x.Beautify() =
            //x.OuterXml        // raw string 반환하려면 이걸 사용.

            // StringWriter를 사용하여 XmlDocument를 문자열로 변환
            let settings =
                new XmlWriterSettings(
                    Indent = true
                    , NewLineOnAttributes = true
                    //, Encoding = Encoding.UTF8
                    )
            use stringWriter = new StringWriter()
            use xmlWriter = XmlWriter.Create(stringWriter, settings)
            x.Save(xmlWriter);
            stringWriter.ToString();

    [<RequireQualifiedAccess>]
    module XElement =
        let ofString(str:string) = XDocument.Parse(str).Root

    [<RequireQualifiedAccess>]
    module DualXmlDocument =
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

    [<RequireQualifiedAccess>]
    module DualXmlNode =
        let ofString(str:string) = (XElement.ofString str).ToXmlNode()
        let ofDocumentAndXPath (file:string) (xpath:string) : XmlNode =
            let xdoc = DualXmlDocument.loadFromFile file
            xdoc.SelectSingleNode xpath

