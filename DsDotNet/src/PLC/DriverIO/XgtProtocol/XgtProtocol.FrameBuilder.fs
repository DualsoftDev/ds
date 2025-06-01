namespace XgtProtocol

open System
open System.Text
open System.Text.RegularExpressions
open Dual.PLC.Common.FS
open XgtEthernetType
open XgtEthernetUtil
open FrameUtils
open ReadWriteBlockFactory
open DataConverter
open NetworkUtils

[<AutoOpen>]
module Constants =
    [<Literal>] 
    let MaxMultiWrite = 16
    [<Literal>] 
    let MaxMultiWriteEFMFB = 64

/// 공통 유틸리티 함수들
module private Utils =
    let validateCount items min max name =
        let count = Array.length items
        if count < min || count > max then
            failwith $"{name} 수는 {min}~{max}개여야 합니다. 현재: {count}"

    let validateSameDataType (items: PlcDataSizeType[]) =
        let distinctTypes = items |> Array.distinct
        if distinctTypes.Length > 1 then
            failwithf "복수의 주소에 대한 데이터 타입이 같아야합니다. 발견된 타입들: %s" 
                (distinctTypes |> Array.map string |> String.concat ", ")

    let getDataTypeCode = function
        | Boolean -> 0x00uy | Byte -> 0x01uy | UInt16 -> 0x02uy 
        | UInt32 -> 0x03uy | UInt64 -> 0x04uy
        | dataType -> failwithf "지원하지 않는 데이터 타입: %A" dataType

/// 주소 인코딩
module private AddressEncoder =
    let encodeXgt address dataType =
        let addr = (getReadBlock address dataType).Address
        let device = addr.Substring(0, 3)
        let number = addr.Substring(3).PadLeft(5, '0')
        let bytes = Encoding.ASCII.GetBytes(device + number)
        if bytes.Length <> 8 then 
            failwithf "주소 포맷 오류: %s" addr
        bytes

    let encodeEFMTB (address:string) dataType =
        let cleanAddr = address.TrimStart('%')
        let bytes = Array.zeroCreate<byte> 8
        bytes.[0] <- byte cleanAddr.[0]

        let offset = 
            Regex.Match(cleanAddr, @"\d+").Value
            |> fun s -> match Int32.TryParse(s) with
                        | true, v -> v
                        | false, _ -> failwithf "주소에서 유효한 숫자를 추출할 수 없습니다: %s" address

        match dataType with
        | Boolean ->
            bytes.[1] <- 0x58uy
            bytes.[2] <- byte (offset % 8)
            Array.Copy(BitConverter.GetBytes(offset / 8), 0, bytes, 4, 4)
        | _ ->
            let byteSize = PlcDataSizeType.TypeByteSize dataType
            bytes.[1] <- 0x42uy
            bytes.[2] <- byte byteSize
            Array.Copy(BitConverter.GetBytes(offset * byteSize), 0, bytes, 4, 4)
        bytes

/// 프레임 생성
module FrameBuilder =
    let private createHeader (frameID:byte[]) bodyLength protocolId direction =
        let header = Array.zeroCreate<byte> HeaderSize
        copyCompanyIdToFrame header
        let bodyLength = BitConverter.GetBytes(uint16 bodyLength)

        header.[12] <- protocolId
        header.[13] <- direction
        header.[14] <- frameID.[0]
        header.[15] <- frameID.[1]
        header.[16] <- bodyLength[0]
        header.[17] <- bodyLength[1]
        header.[18] <- 0uy // reserved
        header.[19] <- calculateChecksum header 19
        header

    let private createXgtHeader frameID bodyLength =
        createHeader frameID bodyLength 0x00uy (byte FrameSource.ClientToServer)
        
    let private createEFMTBHeader frameID bodyLength =
        createHeader frameID bodyLength 0x00uy (byte FrameSource.ClientToServer)

    let private setCommand (frame:byte[]) commandCode offset =
        let cmd = BitConverter.GetBytes(uint16 commandCode)
        frame.[offset] <- cmd.[0]
        frame.[offset + 1] <- cmd.[1]

    let private createMultiRead frameID addresses (dataTypes:PlcDataSizeType[]) maxCount perVarLength commandCode headerFunc encoder =
        Utils.validateCount addresses 1 maxCount "주소"
        Utils.validateSameDataType dataTypes
        
        let bodyLength = 8 + (perVarLength * addresses.Length)
        let frame = Array.zeroCreate<byte> (HeaderSize + bodyLength)
        
        // 헤더
        let header = headerFunc frameID bodyLength
        Array.Copy(header, 0, frame, 0, HeaderSize)
        
        // 바디
        setCommand frame commandCode 20
        frame.[22] <- if commandCode = CommandCode.ReadRequestEFMTB then 0x10uy 
                      else Utils.getDataTypeCode dataTypes.[0]
        frame.[26] <- byte addresses.Length
        
        // 주소들
        addresses 
        |> Array.iteri (fun i addr ->
            let offset = 28 + (i * perVarLength)
            let addrBytes = encoder addr dataTypes.[i]
            if perVarLength = 10 then
                frame.[offset] <- 0x08uy
                frame.[offset + 1] <- 0x00uy
                Array.Copy(addrBytes, 0, frame, offset + 2, 8)
            else
                Array.Copy(addrBytes, 0, frame, offset, 8))
        
        frame

    let createMultiReadFrame frameID addresses dataTypes =
        createMultiRead frameID addresses dataTypes MaxMultiWrite 10 
                       CommandCode.ReadRequest createXgtHeader AddressEncoder.encodeXgt

    let createMultiReadFrameEFMTB frameID addresses dataTypes =
        createMultiRead frameID addresses dataTypes MaxMultiWriteEFMFB 8
                       CommandCode.ReadRequestEFMTB createEFMTBHeader AddressEncoder.encodeEFMTB

    let createMultiWriteFrameFromBlock frameID blocks =

        let serializeBlock (block: ReadWriteBlock) =
            let dataBytes = toBytes block.DataType block.value
            let result = ResizeArray<byte>()
            result.AddRange(BitConverter.GetBytes(uint16 block.Address.Length))
            result.AddRange(Encoding.ASCII.GetBytes(block.Address))
            result.AddRange(BitConverter.GetBytes(uint16 dataBytes.Length))
            result.AddRange(dataBytes)
            result.ToArray()

        Utils.validateCount blocks 1 MaxMultiWrite "ReadWriteBlock"
        Utils.validateSameDataType (blocks |> Array.map (fun b -> b.DataType))
        
        let body = ResizeArray<byte>()
        body.AddRange(BitConverter.GetBytes(uint16 CommandCode.WriteRequest))
        body.AddRange(BitConverter.GetBytes(uint16 (toDataTypeCode blocks.[0].DataType)))
        body.AddRange(Array.zeroCreate<byte> 2)
        body.AddRange(BitConverter.GetBytes(uint16 blocks.Length))
        blocks |> Array.iter (serializeBlock >> body.AddRange)
        
        let header = createXgtHeader frameID body.Count
        Array.concat [header; body.ToArray()]

    let createMultiWriteFrameEFMTBFromBlock frameID blocks =
        Utils.validateCount blocks 1 MaxMultiWriteEFMFB "ReadWriteBlock"
        
        let createBlockData (block: ReadWriteBlock) =
            let info = getReadWriteBlock block.Address block.DataType block.value
            let dataBytes = toBytes block.DataType block.value
            
            let (typeFlag, metadataBytes) = 
                if block.DataType = Boolean then
                    ([| 0x58uy |], BitConverter.GetBytes(uint16 info.BitPosition))
                else
                    ([| 0x42uy |], BitConverter.GetBytes(uint16 dataBytes.Length))
            
            Array.concat [
                [| byte info.DeviceType |]
                typeFlag
                metadataBytes
                BitConverter.GetBytes(uint32 info.ByteOffset)
                dataBytes
            ]
        
        let dataBody = blocks |> Array.map createBlockData |> Array.concat
        let frame = Array.zeroCreate<byte> (HeaderSize + 8 + dataBody.Length)
        
        // 헤더 설정
        let header = createEFMTBHeader frameID (8 + dataBody.Length)
        Array.Copy(header, 0, frame, 0, HeaderSize)
        
        // 바디 설정
        setCommand frame CommandCode.WriteRequestEFMTB 20
        frame.[22] <- 0x10uy
        frame.[26] <- byte blocks.Length
        Array.Copy(dataBody, 0, frame, 28, dataBody.Length)
        frame