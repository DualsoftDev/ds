namespace DsMxComm

open Dual.Common.Core.FS
open System
open System.Runtime.CompilerServices

[<AutoOpen>]
module MxTagParserModule =

    type MxDeviceType =
        | MxBit | MxWord 
        with
            member x.Size =
                match x with
                | MxBit -> 1
                | MxWord -> 16

    /// Mitsubishi PLC의 다양한 장치 유형을 정의하는 타입
    [<AutoOpen>]
    type MxDevice =
        | X | Y | M | L | B | F | Z | V
        | D | W | R | ZR | T | ST | C    
        | SM | SD | SW | SB | DX | DY
        with
            member x.ToText = x.ToString()
            static member Create s =
                match s with
                | "DX" -> X
                | "DY" -> Y
                | _ -> Enum.Parse(typeof<MxDevice>, s) :?> MxDevice
            member x.IsHexa = 
                match x with
                | X | Y | B | W | SW | SB | SW -> true
                | _ -> false

            
    /// 주소에서 MxDevice와 인덱스를 추출하는 함수
    let tryParseMxTag (address: string) : (MxDevice * MxDeviceType * int) option =
        let getMxDeviceType(melsecHead: MxDevice) (bit: string option) = 
            match bit with
            | Some _ -> MxBit
            | None -> // 단순 주소 형식 (예: X12, Y232, D122)
                match melsecHead with
                | MxDevice.DX | MxDevice.X | MxDevice.DY | MxDevice.Y 
                | MxDevice.M | MxDevice.L | MxDevice.B | MxDevice.F | MxDevice.SB | MxDevice.SM -> MxBit
                | _ -> MxWord

        match address with
        | RegexPattern @"^([A-Z]+)(\d+)(?:\.(\d+))?$" [device; d1; d2] -> 
            try
                let parsedDevice = MxDevice.Create device
                let devType = getMxDeviceType parsedDevice  (if d2 = null then None else Some d2)
                Some (parsedDevice, devType, Convert.ToInt32(d1))
            with
            | :? ArgumentException -> None
        | _ -> None

[<Extension>]   // For C#
type MxTagParser =
   
    [<Extension>]
    static member Parse(tag:string): MxDevice * MxDeviceType * int =
        tryParseMxTag tag |? (getNull<MxDevice * MxDeviceType * int>())
