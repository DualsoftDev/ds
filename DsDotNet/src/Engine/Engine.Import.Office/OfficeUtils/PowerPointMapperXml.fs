namespace Engine.Import.Office

open System
open System.IO
open System.Xml
open System.Xml.Serialization
open DocumentFormat.OpenXml.Packaging
open Engine.Core.MapperDataModule;
open Engine.Core
open Newtonsoft.Json




module PowerPointMapperXml =

        // ========== JSON 저장/불러오기 ==========
    let private jsonSettings = JsonSerializerSettings()
    let ModelConfigToJsonText (cfg: ModelConfig) : string =
        JsonConvert.SerializeObject(cfg, Formatting.Indented, jsonSettings)

    let ModelConfigFromJsonText (json: string) : ModelConfig =
        JsonConvert.DeserializeObject<ModelConfig>(json, jsonSettings)

    let SaveOpenXmlModelConfig (doc: PresentationDocument) (data: ModelConfig) : unit =
        if isNull doc || isNull doc.PresentationPart then
            ()
        else
            let json = ModelConfigToJsonText(data);

            let wrappedXml = $"<PowerPointMapper><![CDATA[{json}]]></PowerPointMapper>"
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

    
    let LoadOpenXmlModelConfig (xmlTexts: string seq) : ModelConfig =
        let empty = createDefaultModelConfig()

        let tryLoadFromXmlText (xml: string) : ModelConfig option =
            try
                let doc = XmlDocument()
                doc.LoadXml(xml)

                let root = doc.DocumentElement
                if isNull root || root.Name <> "PowerPointMapper" then None
                else
                    match root.FirstChild with
                    | :? XmlCDataSection as cdata when not (String.IsNullOrWhiteSpace(cdata.Data)) ->
                        try
                            Some(ModelConfigFromJsonText cdata.Data)
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
