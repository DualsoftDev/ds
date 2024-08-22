namespace IOMapForModeler

open Engine.Core
open System.ComponentModel

type HwIOType =
    | Input
    | Output

type IHwTag =
    abstract member IOType : HwIOType
    abstract member Name : string
    abstract member Address : string
    abstract member Value : obj with get, set
    [<Browsable(false)>]
    abstract member DeviceType : string
    abstract member DataType : DataType
    [<Browsable(false)>]
    abstract member Index  : int
    [<Browsable(false)>]
    abstract member MemoryName : string
    abstract member GetTarget : unit -> IQualifiedNamed
    abstract member GetDeviceAddress : unit -> string
    abstract member GetTagAddress : unit -> string
    abstract member ActionOutput : unit -> unit
