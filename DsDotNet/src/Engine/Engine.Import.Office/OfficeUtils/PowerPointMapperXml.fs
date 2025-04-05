namespace Engine.Import.Office

open System
open System.IO
open System.Xml
open System.Xml.Serialization
open DocumentFormat.OpenXml.Packaging
open Engine.Core.MapperDataModule;

module PowerPointMapperXml =

    let SaveOpenXmlMapperData (doc: PresentationDocument) (data: UserTagConfig) : unit =
        if isNull doc || isNull doc.PresentationPart then
            ()
        else
            let serializer = XmlSerializer(typeof<UserTagConfig>)
            let xmlString =
                use sw = new StringWriter()
                serializer.Serialize(sw, data)
                sw.ToString()

            let wrappedXml = $"<PowerPointMapper><![CDATA[{xmlString}]]></PowerPointMapper>"
            let bytes = System.Text.Encoding.UTF8.GetBytes(wrappedXml)

            // 기존 파트 제거
            let customParts = doc.PresentationPart.CustomXmlParts
            let existingPart =
                customParts
                |> Seq.cast<CustomXmlPart>
                |> Seq.tryFind (fun part ->
                    use reader = new StreamReader(part.GetStream())
                    let xml = reader.ReadToEnd()
                    xml.Contains("<PowerPointMapper>"))

            existingPart
            |> Option.iter (fun part -> doc.PresentationPart.DeletePart(part) |> ignore)

            let newPart = doc.PresentationPart.AddNewPart<CustomXmlPart>("application/xml")
            use stream = newPart.GetStream(FileMode.Create, FileAccess.Write)
            stream.Write(bytes, 0, bytes.Length)

    
    let LoadMapperData (xmlTexts: string seq) : UserTagConfig =
        let empty = createDefaultUserTagConfig()

        let tryLoadFromXmlText (xml: string) : UserTagConfig option =
            try
                let doc = XmlDocument()
                doc.LoadXml(xml)

                let root = doc.DocumentElement
                if isNull root || root.Name <> "PowerPointMapper" then None
                else
                    match root.FirstChild with
                    | :? XmlCDataSection as cdata when not (String.IsNullOrWhiteSpace(cdata.Data)) ->
                        try
                            Some(XmlToUserTagConfig cdata.Data)
                        with ex ->
                            Console.WriteLine($"[Deserialize] XML error: {ex.Message}")
                            None
                    | _ -> None
            with ex ->
                Console.WriteLine($"[LoadMapperData] Parse error: {ex.Message}")
                None

        xmlTexts
        |> Seq.choose tryLoadFromXmlText
        |> Seq.tryHead
        |> Option.defaultValue empty
