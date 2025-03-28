namespace PLC.Mapper.FS

open System
open System.Drawing
open System.Collections.Generic
open System.Xml.Serialization

module DeviceApiModule =

    [<AllowNullLiteral>]
    type DeviceApi() =
        member val Group = "" with get, set
        member val Device = "" with get, set
        member val Api = "" with get, set
        member val Tag = "" with get, set
        member val Color = 0 with get, set
        member val Address = "" with get, set


    [<AllowNullLiteral>]
    [<XmlRoot("PowerPointMapper")>]
    type MapperData() =
        member val DeviceApisProp = new List<DeviceApi>() with get, set
        member val TagsProp = new List<string>() with get, set
        member val DevicesProp = new List<string>() with get, set
        member val FlowsProp = new List<string>() with get, set
