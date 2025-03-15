namespace Dsu.PLCConverter.FS

open System
open System.Text
open System.Xml
open System.Xml.Linq
open System.IO
open Dsu.PLCConverter.FS.XgiSpecs

// XML 생성과 변환을 위한 모듈
// https://nbevans.wordpress.com/2015/04/15/super-skinny-xml-document-generation-with-f/
module XgiBaseXML =

    // XML 선언을 생성하는 함수
    let FXDeclaration version encoding standalone = XDeclaration(version, encoding, standalone)

    // XML 로컬 네임을 생성하는 함수 (네임스페이스 포함)
    let FXLocalName localName namespaceName = XName.Get(localName, namespaceName)

    // XML 네임을 생성하는 함수 (확장 이름 사용)
    let FXName expandedName = XName.Get(expandedName)

    // XML 문서를 생성하는 함수 (선언과 콘텐츠 포함)
    let FXDocument xdecl content = XDocument(xdecl, content |> Seq.map (fun v -> v :> obj) |> Seq.toArray)

    // XML 주석을 생성하는 함수
    let FXComment (value: string) = XComment(value) :> obj

    // 네임스페이스 포함 엘리먼트를 생성하는 함수
    let FXElementNS localName namespaceName content =
        XElement(FXLocalName localName namespaceName, content |> Seq.map (fun v -> v :> obj) |> Seq.toArray) :> obj

    // 엘리먼트를 생성하는 함수 (확장 이름 사용)
    let FXElement expandedName content =
        XElement(FXName expandedName, content |> Seq.map (fun v -> v :> obj) |> Seq.toArray) :> obj

    // 네임스페이스 포함 속성을 생성하는 함수
    let FXAttributeNS localName namespaceName value = XAttribute(FXLocalName localName namespaceName, value) :> obj

    // 속성을 생성하는 함수 (확장 이름 사용)
    let FXAttribute expandedName value = XAttribute(FXName expandedName, value) :> obj

    // XML 속성 지정 연산자
    let (=>) x y = FXAttribute x y

   
    // XDocument 확장: 메모리 스트림으로 저장
    type XDocument with
        /// UTF-8 인코딩, 들여쓰기 및 문자 검사 설정을 사용해 XML 문서를 메모리 스트림에 저장
        member doc.Save() =
            let ms = new MemoryStream()
            use xtw = XmlWriter.Create(ms, XmlWriterSettings(Encoding = Encoding.UTF8, Indent = true, CheckCharacters = true))
            doc.Save(xtw)
            ms.Position <- 0L
            ms

    /// XDocument를 XmlDocument로 변환
    let toXmlDocument (xdoc: XDocument) =
        let xmlDocument = XmlDocument()
        use xmlReader = xdoc.CreateReader()
        xmlDocument.Load(xmlReader)
        xmlDocument

    /// XmlDocument를 XDocument로 변환 (더러운 방식)
    let toXDocument_Dirty (xmlDocument: XmlDocument) =
        use nodeReader = new XmlNodeReader(xmlDocument)
        nodeReader.MoveToContent() |> ignore
        XDocument.Load(nodeReader)

    /// XmlDocument를 XDocument로 변환 (깨끗한 방식)
    let toXDocument (xmlDocument: XmlDocument) = XDocument.Parse(xmlDocument.OuterXml)

    /// XDocument를 문자열로 출력 (혼합 콘텐츠 XML 구문 지원)
    let printXDoc (xdoc: XDocument) =
        let sb = StringBuilder()
        let xmlWriterSettings = XmlWriterSettings(
            Indent = false,
            OmitXmlDeclaration = true,
            IndentChars = "  ",
            NewLineChars = "\r\n",
            NewLineHandling = NewLineHandling.Replace,
            NewLineOnAttributes = true
        )
        let writer = XmlWriter.Create(sb, xmlWriterSettings)
        let xmlDoc = toXmlDocument xdoc
        xmlDoc.Save(writer)
        
        // 개행 처리와 함께 XML 출력
        let xml = sb.ToString().Split([| '<' |], StringSplitOptions.RemoveEmptyEntries) |> String.concat "\r\n<"
        sprintf "<%s" xml
