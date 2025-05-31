namespace XgtProtocol

open System
open Dual.PLC.Common.FS

/// XGTTag: LS(XGT) 전용 태그 표현
type XGTTag(name: string, address: string, dataSizeType: PlcDataSizeType, bitOffset: int, isOutput: bool, ?comment: string) =
    inherit PlcTagBase(name, address, dataSizeType, ?comment = comment)

    let step = 100
    let typeSize = PlcDataSizeType.TypeBitSize dataSizeType  

    /// 보조 생성자 (XGI 여부 기반 파싱)
    new (address: string, isXgi: bool, isOutput) =
        let size, offset =
            match isXgi with
            | true  -> LsXgiTagParser.Parse address |> fun (_, s, o) -> s, o
            | false -> LsXgkTagParser.Parse address |> fun (_, s, o) -> s, o
        let dataType = PlcDataSizeType.FromBitSize size
        XGTTag(address, address, dataType,  offset, isOutput)

        
    member val LWordOffset = -1 with get, set
    member val QWordOffset = -1 with get, set
    /// LWord 태그 이름 (e.g., %ML0)
    member x.LWordTag =
        if address.StartsWith("%") then
            sprintf "%%%sL%d" x.Device (x.BitOffset / 64)
        else
            sprintf "%sL%d" x.Device (x.BitOffset / 64)

    /// QWord 태그 이름 (e.g., %MQ0)
    member x.QWordTag =
        if address.StartsWith("%") then
            sprintf "%%%sQ%d" x.Device (x.BitOffset / 128)
        else
            sprintf "%sQ%d" x.Device (x.BitOffset / 128)

    ///같은주소 다른표기때문에 동일 주소 확인용 %MW10 = %MX16 
    member x.AddressKey = $"{x.Device}_{x.BitOffset}"
    /// 디바이스 문자열 (e.g., "MB", "MW", "ZR")
    member _.Device =
        if address = ""
        then "A"  //주소가 없으면 Auto Dev xgt
        else 
            let dev = address.TrimStart('%')
            if dev.StartsWith("ZR") then dev.Substring(0, 2)
            else dev.Substring(0, 1)

    /// 비트 단위 오프셋 계산
    member _.BitOffset =
        if address.TrimStart('%').StartsWith("S") 
        then bitOffset / step * 16
        else bitOffset

    /// 메모리 타입 문자 (Bit → 'X', 그 외 → 'B')
    member _.MemType = if typeSize = 1 then 'X' else 'B'
    member x.GetAddressAlias(size:PlcDataSizeType) =
        let isXgi = address.StartsWith("%")
        let addressTemp = 
            match size with
            | Boolean -> if isXgi 
                         then sprintf "%sX%d" x.Device x.BitOffset
                         else sprintf "%s%d" x.Device x.BitOffset
                        
            | Byte    -> sprintf "%sB%d.%d" x.Device (x.BitOffset / 8) (x.BitOffset % 8)
            | UInt16  -> sprintf "%sW%d.%d" x.Device (x.BitOffset / 16) (x.BitOffset % 16)
            | UInt32  -> sprintf "%sD%d.%d" x.Device (x.BitOffset / 32) (x.BitOffset % 32)
            | UInt64  -> sprintf "%sL%d.%d" x.Device (x.BitOffset / 64) (x.BitOffset % 64)
            | _ -> failwith $"Unsupported data type: {size}"

        if isXgi then
            sprintf "%%%s" addressTemp
        else
            addressTemp

    /// 데이터 크기 (Bit → 내부 비트 인덱스, 그 외 → 바이트 크기)
    member x.Size =
        if typeSize = 1 then x.BitOffset % 8
        else typeSize / 8
        
    /// 읽기/쓰기 타입 판단
    override _.ReadWriteType: ReadWriteType = 
        if isOutput then Write else Read

    override x.IsMemory =
         if isOutput then false
         else 
            match x.Device with
            | "I" | "A"  -> false
            | _ -> true

    /// 버퍼 값을 읽어 현재 값으로 설정, 변경 여부 반환
    override x.UpdateValue(buffer: byte[]) : bool =

        // 128bit 여부 판단: LWordOffset = -1이면 128bit (QWordOffset 사용)
        let is128bit = x.LWordOffset = -1

        // 기준 오프셋 및 워드 단위 크기 선택
        let offset = if is128bit then x.QWordOffset else x.LWordOffset
        let wordSize = if is128bit then 16 else 8

        // 비트 단위 시작 위치 계산 (bit 단위 -> byte 단위)
        let startByteOffset = offset * wordSize + (x.BitOffset % (wordSize * 8)) / 8

        let newValue : obj =
            match x.DataType with
            | Boolean ->
                if x.Device = "S" then
                    let sVal = BitConverter.ToUInt16(buffer, startByteOffset) |> int
                    (sVal = (x.BitOffset % 64)) :> obj
                else
                    let bitPos = x.BitOffset % (if is128bit then 128 else 64)
                    let baseOffset = offset * wordSize
                    if bitPos < 64 then
                        let low = BitConverter.ToUInt64(buffer, baseOffset)
                        (low &&& (1UL <<< bitPos)) <> 0UL :> obj
                    else
                        let high = BitConverter.ToUInt64(buffer, baseOffset + 8)
                        (high &&& (1UL <<< (bitPos - 64))) <> 0UL :> obj

            | Byte   -> buffer.[startByteOffset] :> obj
            | UInt16 -> BitConverter.ToUInt16(buffer, startByteOffset) :> obj
            | UInt32 -> BitConverter.ToUInt32(buffer, startByteOffset) :> obj
            | UInt64 -> BitConverter.ToUInt64(buffer, startByteOffset) :> obj
            //| UInt128 ->
            //    let baseOffset = offset * 16
            //    let low  = BitConverter.ToUInt64(buffer, baseOffset)
            //    let high = BitConverter.ToUInt64(buffer, baseOffset + 8)
            //    struct (high, low) :> obj
            | _ -> failwith $"Unsupported data type: {x.DataType}"

        if base.Value <> newValue then
            base.Value <- newValue
            true
        else
            false
