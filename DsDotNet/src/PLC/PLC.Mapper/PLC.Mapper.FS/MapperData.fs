namespace PLC.Mapper.FS

open System.Collections.Generic
open System.Xml.Serialization
open Newtonsoft.Json
open System
open System.IO

module MapperDataModule =

    // ========== 모델 타입 정의 ==========

    [<AllowNullLiteral>]
    type DeviceApi() =
        member val Group = "" with get, set
        member val Device = "" with get, set
        member val Api = "" with get, set
        member val Tag = "" with get, set
        member val Color = 0 with get, set
        member val Address = "" with get, set

    [<AllowNullLiteral>]
    type DsApiTag() =
        member val Case = "" with get, set
        member val Flow = "" with get, set
        member val Name = "" with get, set
        member val DataType = "" with get, set
        member val Input = "" with get, set
        member val Output = "" with get, set
        member val SymbolIn = "" with get, set
        member val SymbolOut = "" with get, set

    [<Flags>]
    type UserTagColumn =
        | Case      = 0x00 
        | Flow      = 0x01
        | Name      = 0x02 
        | DataType  = 0x03 
        | Input     = 0x04 
        | Output    = 0x05 
        | SymbolIn  = 0x06
        | SymbolOut = 0x07

    /// 단순히 DsApiTag 컬렉션만 포함한 구성
    type DsApiTagConfig = {
        UserTags: DsApiTag array
    }

    /// 전체 매핑 정보 포함 구조
    [<AllowNullLiteral>]
    [<XmlRoot("PowerPointMapper")>]
    type MapperData() =
        member val DeviceApisProp = new List<DeviceApi>() with get, set
        member val TagsProp = new List<DsApiTag>() with get, set

    /// JSON 저장용 MapperData 래퍼
    type MapperDataConfig = {
        Mapper: MapperData
    }

    // ========== 유틸 ==========

    let createDefaultUserTagConfig () : DsApiTagConfig =
        { UserTags = [||] }

    let createDefaultMapperData () =
        MapperData(DeviceApisProp = new List<DeviceApi>(), TagsProp = new List<DsApiTag>())

    let private jsonSettings = JsonSerializerSettings(NullValueHandling = NullValueHandling.Ignore)

    // ========== JSON 저장/불러오기 함수 ==========

    let SaveUserTagConfig (path: string) (cfg: DsApiTagConfig) =
        let json = JsonConvert.SerializeObject(cfg, Formatting.Indented, jsonSettings)
        File.WriteAllText(path, json)
        path

    let LoadUserTagConfig (path: string) : DsApiTagConfig =
        let json = File.ReadAllText(path)
        JsonConvert.DeserializeObject<DsApiTagConfig>(json, jsonSettings)

    let SaveMapperData (path: string) (mapper: MapperData) =
        let wrapped = { Mapper = mapper }
        let json = JsonConvert.SerializeObject(wrapped, Formatting.Indented, jsonSettings)
        File.WriteAllText(path, json)
        path

    let LoadMapperData (path: string) : MapperData =
        let json = File.ReadAllText(path)
        let wrapper = JsonConvert.DeserializeObject<MapperDataConfig>(json, jsonSettings)
        wrapper.Mapper
