[<AutoOpen>]
module LsTagInfoImpl

open System
open System.IO
open System.Collections.Generic
open Dual.PLC.Common.FS


type DeviceType =
        | P  | T
        | M  | C
        | L  | R
        | K  | I
        | F  | Q
        | D  | A
        | U  | W
        | N  | S
        | Z  | ZR //S Step제어용 디바이스 수집 불가

type LsTagInfo =
    {
        /// Original Tag name
        Tag: string
        Device: DeviceType
        DataType: PlcDataSizeType
        BitOffset: int
    }

