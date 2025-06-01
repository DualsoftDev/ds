namespace XgtProtocol

open System
open System.Net
open System.Text
open System.Net.Sockets
open System.Threading
open System.Text.RegularExpressions
open Dual.PLC.Common.FS
open XgtEthernetType

// 응답 관련 상수 정의
module ResponseConstants =
    [<Literal>]
    let MinimumResponseLength = 32
    
    [<Literal>]
    let ErrorStateOffset = 26
    
    [<Literal>]
    let CommandOffset = 20
    
    [<Literal>]
    let DataStartOffset = 32
    
    [<Literal>]
    let MultiReadDataStartOffset = 30
    
    [<Literal>]
    let ReadResponseCommand = 0x55uy
    
    [<Literal>]
    let StandardDataBlockSize = 8
    
    [<Literal>]
    let EFMTBDataBlockSize = 16

// 응답 검증 모듈
module ResponseValidation =
    
    /// 응답 버퍼 기본 검증
    let validateResponseBuffer (buffer: byte[]) =
        if buffer.Length < ResponseConstants.MinimumResponseLength then
            failwithf "응답 데이터가 너무 짧습니다. 실제: %d, 필요: %d" buffer.Length ResponseConstants.MinimumResponseLength
    
    /// PLC 에러 상태 검증
    let validateErrorState (buffer: byte[]) =
        let errorState = BitConverter.ToUInt16(buffer, ResponseConstants.ErrorStateOffset)
        if errorState <> 0us then
            let errorCode = buffer.[ResponseConstants.ErrorStateOffset]
            let description = getXgtErrorDescription errorCode
            failwithf "❌ PLC 응답 에러: 0x%02X - %s" errorCode description
    
    /// 읽기 응답 명령어 검증
    let validateReadCommand (buffer: byte[]) =
        let actualCommand = buffer.[ResponseConstants.CommandOffset]
        if actualCommand <> ResponseConstants.ReadResponseCommand then
            failwithf "응답 명령어가 아닙니다. 예상: 0x%02X, 실제: 0x%02X" ResponseConstants.ReadResponseCommand actualCommand
    
    /// 읽기 버퍼 크기 검증
    let validateReadBufferSize (readBuffer: byte[]) (requiredSize: int) =
        if readBuffer.Length < requiredSize then
            failwithf "readBuffer 크기 부족: %d < %d" readBuffer.Length requiredSize

// 데이터 추출 모듈
module DataExtractor =
    
    /// 데이터 타입에 따른 값 추출
    let extractValue (buffer: byte[]) (offset: int) (dataType: PlcDataSizeType) : obj =
        try
            match dataType with
            | PlcDataSizeType.Boolean -> box (buffer.[offset] = 1uy)
            | PlcDataSizeType.Byte -> box buffer.[offset]
            | PlcDataSizeType.UInt16 -> box (BitConverter.ToUInt16(buffer, offset))
            | PlcDataSizeType.UInt32 -> box (BitConverter.ToUInt32(buffer, offset))
            | PlcDataSizeType.UInt64 -> box (BitConverter.ToUInt64(buffer, offset))
            | _ -> failwithf "지원하지 않는 데이터 타입입니다: %A" dataType
        with
        | ex -> failwithf "데이터 추출 실패: %s" ex.Message
    
    /// 요소 크기 계산
    let calculateElementSize (dataType: PlcDataSizeType) : int =
        let elementSizeBits = PlcDataSizeType.TypeBitSize dataType
        (elementSizeBits + 7) / 8

// 멀티 읽기 응답 파서 (복수 타입 지원)
module MultiReadResponseParser =
    
    /// 멀티 읽기 응답 파싱 코어 로직 (복수 타입 지원)
    let parseCore (buffer: byte[]) (count: int) (dataTypes: PlcDataSizeType[]) (readBuffer: byte[]) (dataBlockSize: int) =
        ResponseValidation.validateResponseBuffer buffer
        ResponseValidation.validateErrorState buffer
        
        // 데이터 타입 배열 길이가 count와 일치하는지 검증
        if dataTypes.Length <> count then
            failwithf "데이터 타입 배열 길이(%d)가 count(%d)와 일치하지 않습니다." dataTypes.Length count
        
        // 전체 필요한 버퍼 크기 계산
        let totalRequiredSize = 
            dataTypes 
            |> Array.map DataExtractor.calculateElementSize
            |> Array.sum
        
        ResponseValidation.validateReadBufferSize readBuffer totalRequiredSize
        
        // 각 데이터 타입별로 추출 및 복사
        let mutable readBufferOffset = 0
        
        for i in 0 .. count - 1 do
            let dataType = dataTypes.[i]
            let elementSizeBytes = DataExtractor.calculateElementSize dataType
            let srcOffset = ResponseConstants.MultiReadDataStartOffset + (i * (dataBlockSize + 2)) + 2
            
            // 버퍼 범위 검증
            if srcOffset + elementSizeBytes > buffer.Length then
                failwithf "소스 버퍼 크기 부족: 필요 %d, 실제 %d" (srcOffset + elementSizeBytes) buffer.Length
            
            if readBufferOffset + elementSizeBytes > readBuffer.Length then
                failwithf "대상 버퍼 크기 부족: 필요 %d, 실제 %d" (readBufferOffset + elementSizeBytes) readBuffer.Length
            
            // 데이터 복사
            Array.Copy(buffer, srcOffset, readBuffer, readBufferOffset, elementSizeBytes)
            readBufferOffset <- readBufferOffset + elementSizeBytes
    
    /// 표준 멀티 읽기 응답 파싱 (복수 타입 지원)
    let parseStandard buffer count dataTypes readBuffer =
        parseCore buffer count dataTypes readBuffer ResponseConstants.StandardDataBlockSize
    
    /// EFMTB 멀티 읽기 응답 파싱 (복수 타입 지원)
    let parseEFMTB buffer count dataTypes readBuffer =
        parseCore buffer count dataTypes readBuffer ResponseConstants.EFMTBDataBlockSize

// 단일 읽기 응답 파서
module SingleReadResponseParser =
    
    /// 단일 읽기 응답 파싱
    let parse (buffer: byte[]) (dataType: PlcDataSizeType) : obj =
        ResponseValidation.validateResponseBuffer buffer
        ResponseValidation.validateErrorState buffer
        ResponseValidation.validateReadCommand buffer
        
        DataExtractor.extractValue buffer ResponseConstants.DataStartOffset dataType

// 복수 타입 값 추출 유틸리티
module MultiTypeExtractor =
    
    /// 복수 타입 데이터에서 개별 값들을 추출
    let extractValues (readBuffer: byte[]) (dataTypes: PlcDataSizeType[]) : obj[] =
        let mutable offset = 0
        let values = Array.zeroCreate dataTypes.Length
        
        for i in 0 .. dataTypes.Length - 1 do
            let dataType = dataTypes.[i]
            values.[i] <- DataExtractor.extractValue readBuffer offset dataType
            offset <- offset + DataExtractor.calculateElementSize dataType
        
        values
    
    /// 타입별 크기 정보 반환
    let getTypeSizes (dataTypes: PlcDataSizeType[]) : int[] =
        dataTypes |> Array.map DataExtractor.calculateElementSize

// 메인 응답 파서 모듈
[<AutoOpen>]
module XgtResponse =
    
    /// 멀티 읽기 응답 파싱 (복수 타입 지원)
    let parseMultiReadResponse (buffer: byte[]) (count: int) (dataTypes: PlcDataSizeType[]) (readBuffer: byte[]) =
        MultiReadResponseParser.parseStandard buffer count dataTypes readBuffer
    
    /// EFMTB 멀티 읽기 응답 파싱 (복수 타입 지원)
    let parseEFMTBMultiReadResponse (buffer: byte[]) (count: int) (dataTypes: PlcDataSizeType[]) (readBuffer: byte[]) =
        MultiReadResponseParser.parseEFMTB buffer count dataTypes readBuffer
    
    /// 단일 읽기 응답 파싱
    let parseReadResponse (buffer: byte[]) (dataType: PlcDataSizeType) : obj =
        SingleReadResponseParser.parse buffer dataType
    
    /// 멀티 읽기 + 값 추출 (복수 타입)
    let parseAndExtractValues (buffer: byte[]) (count: int) (dataTypes: PlcDataSizeType[]) : obj[] =
        let totalSize = dataTypes |> Array.map DataExtractor.calculateElementSize |> Array.sum
        let readBuffer = Array.zeroCreate totalSize
        MultiReadResponseParser.parseStandard buffer count dataTypes readBuffer
        MultiTypeExtractor.extractValues readBuffer dataTypes
    
    // 기존 호환성을 위한 단일 타입 함수들
    /// 멀티 읽기 응답 파싱 (단일 타입 - 호환성)
    let parseMultiReadResponseSingleType (buffer: byte[]) (count: int) (dataType: PlcDataSizeType) (readBuffer: byte[]) =
        let dataTypes = Array.create count dataType
        MultiReadResponseParser.parseStandard buffer count dataTypes readBuffer
    
    /// EFMTB 멀티 읽기 응답 파싱 (단일 타입 - 호환성)
    let parseEFMTBMultiReadResponseSingleType (buffer: byte[]) (count: int) (dataType: PlcDataSizeType) (readBuffer: byte[]) =
        let dataTypes = Array.create count dataType
        MultiReadResponseParser.parseEFMTB buffer count dataTypes readBuffer

// 응답 분석 유틸리티
module ResponseAnalyzer =
    
    /// 응답 정보 구조체
    type ResponseInfo = {
        BufferLength: int
        ErrorState: uint16
        Command: byte
        IsValid: bool
        ErrorMessage: string option
    }
    
    /// 응답 정보 추출 (디버깅/로깅용)
    let analyzeResponse (buffer: byte[]) : ResponseInfo =
        try
            let bufferLength = buffer.Length
            let errorState = 
                if bufferLength >= ResponseConstants.ErrorStateOffset + 2 then
                    BitConverter.ToUInt16(buffer, ResponseConstants.ErrorStateOffset)
                else 0us
            let command = 
                if bufferLength > ResponseConstants.CommandOffset then
                    buffer.[ResponseConstants.CommandOffset]
                else 0uy
            
            let isValid = bufferLength >= ResponseConstants.MinimumResponseLength && errorState = 0us
            let errorMessage = 
                if errorState <> 0us then
                    Some(getXgtErrorDescription buffer.[ResponseConstants.ErrorStateOffset])
                else None
            
            {
                BufferLength = bufferLength
                ErrorState = errorState
                Command = command
                IsValid = isValid
                ErrorMessage = errorMessage
            }
        with
        | ex -> 
            {
                BufferLength = buffer.Length
                ErrorState = 0us
                Command = 0uy
                IsValid = false
                ErrorMessage = Some($"분석 실패: {ex.Message}")
            }

