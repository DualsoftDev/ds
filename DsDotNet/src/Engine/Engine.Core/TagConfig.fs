namespace Engine.Core

open System
open System.IO
open System.Collections.Generic
open System.Xml
open System.Runtime.Serialization
open Newtonsoft.Json

module MapperDataModule =

    // ========== 모델 타입 정의 ==========
    type MapperTag (name: string, address:string) =
        member this.Name = name
        member this.Address = address
        member this.OpcName = $"{name}_{address}" |> validStorageName

    [<AllowNullLiteral>]
    type DeviceApi() =
        member val Area = "" with get, set
        member val Work = "" with get, set
        member val Device = "" with get, set
        member val Api = "" with get, set
        member val Color = 0 with get, set
        member val InAddress = "" with get, set
        member val OutAddress = "" with get, set
        member val MapperTag = MapperTag("", "") with get, set
        
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
    type DeviceTag() =
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
    type TagConfig() =
        [<field: DataMember(Name = "DeviceApisProp")>]
        member val DeviceApis = ResizeArray<DeviceApi>() with get, set
        
        [<field: DataMember(Name = "DeviceTagsProp")>]
        member val DeviceTags = ResizeArray<DeviceTag>() with get, set

        [<field: DataMember(Name = "UserMonitorTagsProp")>]
        member val UserMonitorTags = ResizeArray<UserMonitorTag>() with get, set

    // ========== 생성자 및 변환 함수 ==========

    let createDefaultTagConfig() =
        let user = TagConfig()
        user.UserMonitorTags <- ResizeArray<UserMonitorTag>()
        user.DeviceTags <- ResizeArray<DeviceTag>()
        user.DeviceApis <- ResizeArray<DeviceApi>()
        user



