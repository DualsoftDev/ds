namespace Engine.Core

open System
open System.IO
open System.Collections.Generic
open System.Xml
open System.Runtime.Serialization
open Newtonsoft.Json

module MapperDataModule =

    // ========== 모델 타입 정의 ==========

    [<AllowNullLiteral>]
    type DeviceApi() =
        member val Area = "" with get, set
        member val Work = "" with get, set
        member val Device = "" with get, set
        member val Api = "" with get, set
        member val Tag = "" with get, set
        member val Color = 0 with get, set
        member val InAddress = "" with get, set
        member val OutAddress = "" with get, set
        
    [<Flags>]
    type IOColumn =
        | Case = 0
        | Flow = 1
        | Name = 2
        | DataType = 3
        | Input = 4
        | Output = 5
        | InSymbol = 6
        | OutSymbol = 7

    [<AllowNullLiteral>]
    type UserDeviceTag() =
        member val Case = "" with get, set
        member val Flow = "" with get, set
        member val Name = "" with get, set
        member val DataType = "" with get, set
        member val Input = "" with get, set
        member val Output = "" with get, set
        member val SymbolIn = "" with get, set
        member val SymbolOut = "" with get, set
        member x.DeviceApiName = $"{x.Flow}{DsText.TextDeviceSplit}{x.Name}"

    [<AllowNullLiteral>]
    type UserMonitorTag() =
        member val Name = "" with get, set
        member val DataType = "" with get, set
        member val Address = "" with get, set

    [<AllowNullLiteral>]
    [<DataContract(Name = "PowerPointMapper")>] // XML 루트 이름에 맞춤
    type UserTagConfig() =
        [<field: DataMember(Name = "DeviceApisProp")>]
        member val DeviceApis = ResizeArray<DeviceApi>() with get, set

        [<field: DataMember(Name = "UserMonitorTagsProp")>]
        member val UserMonitorTags = ResizeArray<UserMonitorTag>() with get, set

        [<field: DataMember(Name = "UserDeviceTagsProp")>]
        member val UserDeviceTags = ResizeArray<UserDeviceTag>() with get, set

        [<field: DataMember(Name = "HwIOProp")>]
        member val HwIO = "" with get, set
        
        [<field: DataMember(Name = "HwIPProp")>]
        member val HwIP = "" with get, set

    // ========== 생성자 및 변환 함수 ==========

    let createDefaultUserTagConfig() =
        UserTagConfig()

    // ========== XML 문자열 직렬화 ==========

    let UserTagConfigToXmlText (config: UserTagConfig) : string =
        let serializer = DataContractSerializer(typeof<UserTagConfig>)
        use stringWriter = new StringWriter()
        use xmlWriter = XmlWriter.Create(stringWriter, XmlWriterSettings(Indent = true))
        serializer.WriteObject(xmlWriter, config)
        xmlWriter.Flush()
        stringWriter.ToString()

    let XmlToUserTagConfig (xmlText: string) : UserTagConfig =
        let serializer = DataContractSerializer(typeof<UserTagConfig>)
        use stringReader = new StringReader(xmlText)
        use xmlReader = XmlReader.Create(stringReader)
        try
            serializer.ReadObject(xmlReader) :?> UserTagConfig
        with _ ->
            createDefaultUserTagConfig()

    // ========== JSON 저장/불러오기 ==========

    let private jsonSettings = JsonSerializerSettings()

    let SaveUserTagConfigWithPath (path: string) (cfg: UserTagConfig) =
        let json = JsonConvert.SerializeObject(cfg, Formatting.Indented, jsonSettings)
        File.WriteAllText(path, json)
        path

    let LoadUserTagConfig (path: string) =
        let json = File.ReadAllText(path)
        try
            let data = JsonConvert.DeserializeObject<UserTagConfig>(json, jsonSettings)
            if isNull data || isNull data.DeviceApis || isNull data.UserDeviceTags || isNull data.UserMonitorTags then
                createDefaultUserTagConfig()
            else
                data
        with _ ->
            createDefaultUserTagConfig()


