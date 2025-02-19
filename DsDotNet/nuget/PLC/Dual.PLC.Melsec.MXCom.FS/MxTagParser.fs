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
                | "X" -> X | "Y" -> Y | "M" -> M | "L" -> L | "B" -> B
                | "F" -> F | "Z" -> Z | "V" -> V | "D" -> D
                | "W" -> W | "R" -> R | "C" -> C | "T" -> T
                | "ZR" -> ZR 
                | "ST" -> ST
                | "SM" -> SM
                | "SD" -> SD
                | "SW" -> SW
                | "SB" -> SB
                | "DX" -> DX
                | "DY" -> DY
                | _ -> failwith $"Invalid MxDevice value: {s}"

            member x.IsHexa = 
                match x with
                | X | Y | B | W | SW | SB | SW -> true
                | _ -> false

    type MxTagInfo = 
        {
            Device: MxDevice
            DataTypeSize: MxDeviceType
            BitOffset: int
        }
            
    /// 주소에서 MxDevice와 인덱스를 추출하는 함수
    let tryParseMxTag (address: string) : MxTagInfo option =
        let getMxDeviceType(melsecHead: MxDevice) (bit: string option) = 
            match bit with
            | Some _ -> MxBit
            | None -> // 단순 주소 형식 (예: X12, Y232, D122)
                match melsecHead with
                | MxDevice.DX | MxDevice.X | MxDevice.DY | MxDevice.Y 
                | MxDevice.M | MxDevice.L | MxDevice.B | MxDevice.F | MxDevice.SB | MxDevice.SM -> MxBit
                | _ -> MxWord


        let getRecord (device, d1, d2)= 
            let parsedDevice = MxDevice.Create device
            let devType = getMxDeviceType parsedDevice  (if d2 = "" then None else Some d2)
            Some
                {
                    Device = parsedDevice
                    DataTypeSize = devType
                    BitOffset =
                        match devType  with
                        | MxBit ->
                            if d2 = "" 
                            then //XFFF
                                if parsedDevice.IsHexa
                                then Convert.ToInt32(d1, 16)
                                else Convert.ToInt32(d1)
                            else //D100.F
                                if parsedDevice.IsHexa
                                then Convert.ToInt32(d1, 16) + Convert.ToInt32(d2, 16)
                                else Convert.ToInt32(d1) + Convert.ToInt32(d2, 16)
                        | MxWord -> 
                                if parsedDevice.IsHexa
                                then Convert.ToInt32(d1, 16) * 16
                                else Convert.ToInt32(d1) * 16
                        
                }

        match address with
        | RegexPattern @"^([A-Z]+)(\d+)(?:\.(\d+))?$" [device; d1; d2] -> getRecord(device, d1, d2)
        | RegexPattern @"^([A-Z]+)([0-9A-F]+)(?:\.(\d+))?$" [device; d1; d2] -> getRecord(device, d1, d2)
        | _ -> None

        
[<Extension>]   // For C#
type MxTagParser =
   
    [<Extension>]
    static member Parse(tag:string): MxTagInfo =
        tryParseMxTag tag |? (getNull<MxTagInfo>())
