module PacketImpl

open System
open System.Linq
open System.Net.Sockets
open System.Text
open Dual.Common.Core.FS
open AddressConvert
open Cluster


/// Source of Frame : * 클라이언트(HMI) -> 서버(PLC): 0x33, * 서버(PLC) -> 클라이언트(HMI): 0x11
module FrameType =
    /// Client 에서 Server 에 request 하는 packet 에 사용됨
    let ClientToServer = 0x33uy
    /// Server 에서 주는 response type 에 사용됨
    let ServerToClient = 0x11uy



/// PLC 통신 command
type Command =
    /// 읽기 명령.  84(=0x54)
    | ReadRequest
    /// 읽기 명령에 대한 response.  85(=0x55)
    | ReadResponse
    /// 쓰기 요청 명령.  88(=0x58)
    | WriteRequest
    /// 쓰기 요청에 대한 response.  89(=0x59)
    | WriteResponse
    /// PLC 상태에 대한 query 요청 : Run state, 운전모드, 시스템 경고/에러, ... 176(=0xB0)
    /// XGB PLC 에서는 지원하지 않음
    | StatusRequest
    /// PLC 상태에 대한 response 177(=0xB1)
    | StatusResponse
        member x.ToUInt16() =
            match x with
            | ReadRequest    -> 0x54us
            | ReadResponse   -> 0x55us
            | WriteRequest   -> 0x58us
            | WriteResponse  -> 0x59us
            | StatusRequest  -> 0xB0us
            | StatusResponse -> 0xB1us



let mutable gLogger : log4net.ILog = null


let errorCodeBook =
    [
        (*1  *) (0x0001us, "개별 읽기/쓰기 요청시 블록 수가 16 보다 큼")
        (*2  *) (0x0002us, "X,B,W,D,L 이 아닌 데이터 타입을 수신했음")
        (*3  *) (0x0003us, "서비스 되지 않는 디바이스를 요구한 경우(XGK: P, M, L, K, R…, XGI: I, Q, M….)")
        (*4  *) (0x0004us, "각 디바이스별 지원하는 영역을 초과해서 요구한 경우")
        (*5  *) (0x0005us, "한번에 최대 1400byes 까지 읽거나 쓸 수 있는데 초과해서 요청한 경우 (개별 블록 사이즈)")
        (*6  *) (0x0006us, "한번에 최대 1400byes 까지 읽거나 쓸 수 있는데 초과해서 요청한 경우 (블록별 총 사이즈)")
        (*117*) (0x0075us, "전용 서비스에서 프레임 헤더의 선두 부분이 잘못된 경우(‘LSIS-GLOFA’)")
        (*118*) (0x0076us, "전용 서비스에서 프레임 헤더의 Length가 잘못된 경우")
        (*119*) (0x0077us, "전용 서비스에서 프레임 헤더의 Checksum이 잘못된 경우")
        (*120*) (0x0078us, "전용 서비스에서 명령어가 잘못된 경우")
    ] |> Tuple.toDictionary

let getErrorMessage errorCode =
    match errorCodeBook.TryGetValue(errorCode) with
    | true, msg ->
        sprintf "LS Protocol Error: %s(0x%x)" errorCodeBook.[errorCode] errorCode
    | _ ->
        sprintf "LS Protocol Error with unknown code = %0xx" errorCode



/// tags 가 모두 동일 크기이면 해당 크기를 반환, 모두 동일하지 않으면 None
let tryGetAllEqualByteSize (tags:string[]) =
    tags |> Array.map getByteSize |> Array.distinct |> Array.tryExactlyOne


let str2bytes (s:string) = ASCIIEncoding.ASCII.GetBytes(s)
let bytes2str (bytes:byte[]) = Encoding.ASCII.GetString(bytes)

/// "LSIS-XGT"(8B) + Reserved(2B) + PLC Info(2B) + Cpu Info(1B)
///  + Source of Frame(1B) + InvokeId(2B) + Data Block Length(2B)
///  + Module Position(1B) + Reserved(BCC 1B)
let headerPacketLength = 20



/// "LSIS-XGT"
let internal lsis = "LSIS-XGT"

/// command 확인용 id
let internal defaultInvokeId = [|0xFFuy; 0xFFuy|]

/// int 를 binary digit 문자열로 변환
let toBinary (n:int) = Convert.ToString (n, 2)

/// buffer 의 모든 byte sum % 256 의 check sum 을 구한다.
let getChecksum (buffer:byte []) =
    buffer
    |> Array.map int
    |> Array.fold (fun acc item -> (acc + item) % 256) 0
    |> byte


/// cpu = {CpuType.Xgi, ...}
/// tags = {"%DW1000", ...}
/// 총 20 byte (headerPacketLength)
let createHeader (cpu:CpuType) (blockLength:int) =
    let blBytes = BitConverter.GetBytes(blockLength |> uint16)
    let hdr =
        [|
            yield! str2bytes lsis // company id : "LSIS-XGT"
            0uy; 0uy        // reserved
            0uy; 0uy        // PLC info

            cpu.ToByte()
            FrameType.ClientToServer
            yield! defaultInvokeId // (2B)

            yield! blBytes
        |]
    [|
        yield! hdr
        yield 0uy   // reserved(1B)
        yield getChecksum hdr     // BCC(1B) 0x00: (Application Header 예약영역 의 Byte Sum)
    |]



type PacketHeader = {
    CompanyId:string
    PLCInfo:uint16
    CpuType:CpuType
    InvokeId:uint16
    Length:uint16
    Bcc:byte
}
/// Response 로 받은 Header (총 20 byte, headerPacketLength) 검사
///
/// PacketHeader 반환
let verifyReponseHeader (cpu:CpuType option) (buffer:byte []) =
    let companyId = Encoding.Default.GetString(buffer.[0..7])
    assert (companyId = lsis)

    let reserved       = buffer.[8..9].ToUInt16()
    let plcInfo        = buffer.[10..11].ToUInt16()
    let cputype        = CpuType.FromByte(buffer.[12])
    let frameType      = buffer.[13]
    let invokeId       = buffer.[14..15].ToUInt16()
    /// data block length
    let blockLength    = buffer.[16..17].ToUInt16()    // 14, 0
    let modulePosition = buffer.[18]    // 0
    let bcc            = buffer.[19]   // 29
    let checkSum       = getChecksum buffer.[0..18]

    assert (reserved = 0us)

    (*
        * 서버 -> 클라이언트:
            Bit00~05: CPU TYPE.  01(XGK/R-CPUH), 02(XGK-CPUS), 05(XGI-CPUU)
            Bit06: 0 (이중화 Master / 단독), 1(이중화 Slave)
            Bit07: 0(CPU 동장 정상), 1(CPU 동작 에러)
            Bit08~12: 시스템 상태: 1(RUN),2(STOP), 4(ERROR), 8(DEBUG)
            Bit13~15 : Reserved
    *)
    assert (buffer.[10] = 4uy || buffer.[10] = 7uy || buffer.[10] = 0x15uy)    // PLC info (XGK = 4, XGBmk=7, XGI = 0x15 = 21)
    assert (buffer.[11] = 1uy || buffer.[11] = 2uy)    // PLC info, 2 for XGBmk

    cpu |> Option.iter (fun cpu -> assert (cputype = cpu)) // 160uy
    assert (frameType = FrameType.ServerToClient) // 0x11uy = 17uy

    assert (buffer.[14..15] = defaultInvokeId) // Invoke id

    logDebug "BlockLength=%d, ModulePosition=%d" blockLength modulePosition
    logDebug "Bcc=%d, CheckSum=%d" bcc checkSum
    assert(bcc = checkSum)

    {
        CompanyId = companyId
        PLCInfo = plcInfo
        CpuType = cputype
        InvokeId = invokeId
        Length = blockLength
        Bcc = bcc
    }




[<AutoOpen>]
module StatusRequest =
    let createStatusRequestPacket () =
        let cpu = CpuType.Xgk   // 임의 CPU로 설정
        let blockLength = 6 // 6 = 명령어(2B) + DataType(2B) + 예약영역(2B)
        let header = createHeader cpu blockLength

        (
            [|
                yield! header   // 마지막은 length
                yield! Command.StatusRequest.ToUInt16().ToBytes()

                // datatype : don't care
                yield! 0us.ToBytes()

                // reserved area2
                yield! 0us.ToBytes()
            |],
            56)          // 56 = Header(20 B) + Response(12 B) + Status Data(24 B)

    type PLCStatus = {
        PLCInfo:uint16
        CpuType:CpuType
        ErrorState:uint16
        IsRunning:bool
    }


    (* OS Version
        XGK-CPUU, CPUH, CPUA, CPUS, CPUE        V4.55
        XGI-CPUU/D, CPUU, CPUH, CPUS, CPUE      V4.06
        XGK-CPUSN, CPUHN, CPUUN                 V1.05
        XGI-CPUUN                               V1.12
        XGR-CPUH/F, CPUH/T, CPUH/S              V2.72
        XG5000                                  V4.21
     *)
    let parseStatusResponsePacket (packet:byte []) =
        /// status data buffer. 32 byte 부터 시작
        let bf = packet.[32..]
        let slotInfo  = bf.[0..3].ToUInt32()
        let cpu       = bf.[4..5].ToUInt16()
        let connState = bf.[6..7].ToUInt16()
        let sysState  = bf.[8..11].ToUInt32()
        let sysError  = bf.[12..15].ToUInt32()
        let sysWarn   = bf.[16..19].ToUInt32()
        let osVersion = bf.[20..21].ToUInt16()
        let reserved  = bf.[22..23].ToUInt16()
        let rightmostOne x = x &&& 1u
        let toBool = function
            | 0u -> false
            | _ -> true


        {
            PLCInfo = packet.[10..11].ToUInt16()
            CpuType = CpuType.FromByte(packet.[12])
            ErrorState = packet.[26..27].ToUInt16()
            IsRunning = (sysState >>> 0) |> rightmostOne |> toBool
        }



[<AutoOpen>]
module RandomReadWrite =
    /// Command(2B) + Data type(2B) + Reserved(2B) + Error State(2B) + Error Code or Num Tags(2B)
    let commandPacketLength = 10

    /// Command(2B) + Data type(2B) + Reserved(2B) + Error State(2B) + Error Code or Num Tags(2B)
    /// + { Data Length(2B) + Actual Data(nB) } * m
    let getReadResponsePacketLength (tags:string []) =
        // 통합 data size 방식
        match tags |> tryGetAllEqualByteSize with
        | Some dataSize -> commandPacketLength + tags.Length * (2 + dataSize)   // 2 : length 기록용
        | _ -> failwith "Mixing different size tags not allowed!"


        //! 개별 data size 총합 : NOT working
        //commandPacketLength + (tags |> Array.map (getByteSize >> ((+) 2)) |> Array.sum)




    /// Non-IEC(XGK) 에서 Bit tag 주소를 통신을 위해서 변환
    /// n 자리 decimal byte offset + 1 자리 hex bit offset --> full bit offset
    /// e.g "%MX1F" --> "%MX31" (31 = 1 * 16 + 15)
    /// e.g "%MX0241A" --> "%MX3866" (3866 = 241 * 16 + 10)
    let bitHex2DecOnDemand cpu (tag:string) =
        match (cpu, tryParseTag tag) with
        | CpuType.Xgk, Some anal when anal.DataType = DataType.Bit ->
            let bi = anal.BitOffset
            let dev = anal.Device.ToString()
            let dataType = anal.DataType.ToMnemonic()
            let serial = sprintf "%%%s%s%d" dev dataType bi
            serial
        | _ -> tag

    /// 구조화된 data block 생성.  Read/Write 공용
    ///
    /// read request packet 생성 시에는 writeValue 값 empty
    ///
    /// write request packet 생성 시에는 writeValue.Length = tags.Length
    let createRandomDataBlock cpu (tags:string []) (writeValue:uint64 []) =
        [|
            // 변수 갯수.  블록 수(2B)
            // variable number : 한번에 최대 16개까지 읽을 수 있음
            yield tags |> Seq.length |> byte
            yield 0uy

            for tg in tags do
                let t = tg |> bitHex2DecOnDemand cpu
                // 개별 변수 명 길이(2B)
                yield (byte)t.Length
                yield 0uy

                // 변수 명
                yield! str2bytes(t)

            if writeValue.Any() then
                // random write 인 경우
                assert(tags.Length = writeValue.Length)
                assert(tags |> Array.map (getBitSize) |> Array.distinct |> Array.length = 1)
                let dataLength = getByteSize tags[0]
                let dataType = getDataType tags[0]
                for data in writeValue do
                    // data 크기(2B)
                    yield! dataLength |> uint16 |> fun x -> x.ToBytes()

                    //logDebug "Createing write data %A" data
                    match dataType with
                    | DataType.Bit -> yield data |> byte
                    | DataType.Byte -> yield data |> byte
                    | DataType.Word -> yield! (data |> uint16).ToBytes()
                    | DataType.DWord -> yield! (data |> uint32).ToBytes()
                    | DataType.LWord -> yield! (data |> uint64).ToBytes()
                    | _ -> failwith "ERROR"
        |]


    /// Data Request packet 생성 + 해당 packet 보낸 후, 서버로부터 받을 예상 byte 수
    /// cpu = {CpuType.Xgi, ...}
    // 8.1.2
    let private createRandomRequestPacket cpu tags values =
        let blocks = createRandomDataBlock cpu tags values
        let blockLength = blocks.Length + 6 // 6 = 명령어(2B) + DataType(2B) + 예약영역(2B)
        let isRead = values.IsEmpty()
        //logDebug "Block length=%d" blockLength
        assert(tags |> Seq.forall(String.IsNullOrEmpty >> not))
        let header = createHeader cpu blockLength
        let ackPacketLength = if isRead then (getReadResponsePacketLength tags) else 10
        let expectedResponsePacketLength = ackPacketLength + headerPacketLength

        let dataType = getDataType tags[0]
        let cmd = if isRead then Command.ReadRequest else Command.WriteRequest

        (   [|
                yield! header   // 마지막은 length
                yield! cmd.ToUInt16().ToBytes()

                yield! dataType.ToUInt16().ToBytes()

                // reserved area2
                yield! 0us.ToBytes()

                yield! blocks |],
            expectedResponsePacketLength)


    /// read request 를 위한 packet 생성 + 해당 packet 을 보낼 때, 서버로 부터 받을 예상 byte 수
    let createRandomReadRequestPacket cpu tags = createRandomRequestPacket cpu tags Array.empty

    /// write request 를 위한 packet 생성 + 해당 packet 을 보낼 때, 서버로 부터 받을 예상 byte 수
    let createRandomWriteRequestPacket cpu tags values = createRandomRequestPacket cpu tags values


    /// response buffer 확인해서 tag 의 갯수와 datablock 의 buffer 를 반환
    // datablock 은 입력으로 주어진 buffer.[30..] 이다.
    let verifyRandomResponse cpu (cmdResponse:Command) (dataType:DataType) (buffer:byte []) =
        let header = verifyReponseHeader cpu buffer
        let blockLength = header.Length
        //logDebug "Block length=%d" blockLength

        assert(buffer.[20..21].ToUInt16() = cmdResponse.ToUInt16())
        assert(buffer.[22..23].ToUInt16() = dataType.ToUInt16())


        //logDebug "DataType Checked OK!!"

        // reserved : 값이 수시로 바뀌는 듯... [0..2] 사이의 값
        //assert(buffer.[24] = 0uy)
        //assert(buffer.[25] = 0uy)

        let errorState = buffer.[26..27].ToUInt16() // 0, 0
        //logDebug "Error State=%d" errorState

        if errorState <> 0us then
            // NAK
            let errorCode = buffer.[28..29].ToUInt16() // 0, 0
            let msg = errorCode |> getErrorMessage
            logError "%s" msg
            // todo fail 해서는 안된다.  반환값 type 을 Result<> 로 변경 고려!!
            failwithlogf "%s" msg


        // ACK case

        /// 전체 읽어 들일 tag 수
        let numTags = buffer.[28..29].ToUInt16()
        let dataBlock = buffer.[30..]
        //logDebug "Num Tags=%d" numTags
        (numTags, dataBlock)





    /// response buffer 확인해서 tag 의 갯수와 datablock 의 buffer 를 반환
    let analyzeRandomResponse cpu isRead dataType buffer =
        let cmd = if isRead then Command.ReadResponse else Command.WriteResponse
        verifyRandomResponse cpu cmd dataType buffer


    /// 복수개의 tag 읽기에 대한 response 를 분석.
    /// tags : 읽기 request 에 사용된 tags 들
    /// buffer : response packet
    /// 각 tag 별로 읽어 들인 값의 pair 를 collection 으로 반환
    let analyzeRandomReadResponse cpu (tags:string[]) buffer =

        let numTags, dataBlock =
            let dataType = getDataType tags[0]
            analyzeRandomResponse (Some cpu) true dataType buffer

        let rec readTag numRemaining (buf:byte[]) =
            if numRemaining = 0us then
                Array.empty
            else
                [|
                    /// tag 하나의 data length : Bit/Byte = 1, Word = 2, DWord = 4
                    let len = buf.[0..1].ToUInt16()
                    match len with
                    | 1us ->
                        yield buf.[2] |> uint64
                        yield! readTag (numRemaining-1us) buf.[3..]
                    | 2us ->
                        yield BitConverter.ToUInt16(buf.[2..], 0) |> uint64
                        yield! readTag (numRemaining-1us) buf.[4..]
                    | 4us ->
                        yield BitConverter.ToUInt32(buf.[2..], 0) |> uint64
                        yield! readTag (numRemaining-1us) buf.[6..]
                    | 8us ->
                        yield BitConverter.ToUInt64(buf.[2..], 0) |> uint64
                        yield! readTag (numRemaining-1us) buf.[10..]
                    | _ ->
                        failwith "Invalid size"
                |]

        let values = readTag numTags dataBlock
        logDebug "Values are %A (%s)" values (values |> Array.map (sprintf "0x%02x") |> String.concat(" "))

        (tags, values) ||> Array.zip



    /// response buffer 확인해서 write 한 tag 의 갯수를 반환
    let analyzeRandomWriteResponse cpu (tags:string[]) buffer =
        let dataType = getDataType tags[0]
        analyzeRandomResponse (Some cpu) false dataType buffer |> fst |> int



    /// packet 을 PLC 로 보내고, 응답으로 length 길이 만큼 읽은 buffer 를 반환한다.
    /// streamer 에 network stream 에 장애 발생시, 재접속하는 기능을 포함하는 steramer 제공시, 이를 반영한다.
    let rec internal rawSendPacketAndGetResponse (streamer:unit -> NetworkStream) (packet:byte []) length =
        try
            let stream = streamer()
            lock stream (fun () ->
                stream.Write(packet, 0, packet |> Array.length)

                let buffer = Array.zeroCreate<byte>(length)
                let nBytes = stream.Read(buffer, 0, length)

                //buffer |> Array.map (sprintf "%02x") |> String.concat(" ") |> logDebug "%s"

                if nBytes <> length then
                    assert(nBytes = 30)

                    //[0.. (packet.Length-1)] |> List.map (sprintf "%3d") |> String.concat("") |> logDebug "Sent packet:%s"
                    //packet |> Array.map (sprintf "%02x") |> Array.map (sprintf "%3s") |> String.concat("") |> logDebug "Sent packet:%s"
                    let errorStatus = buffer.[26..27].ToUInt16()
                    assert(errorStatus = 0xFFFFus || errorStatus = 0x00FFus)
                    let errorCode = buffer.[28..29].ToUInt16()
                    errorCode |> getErrorMessage |> logErrorWithStackTrace
                buffer)
        with exn ->
            logError "Exception on sendPacketAndGetResponse:\n%O" exn
            match exn.InnerException with
            | :? SocketException as sckEx ->
                rawSendPacketAndGetResponse streamer packet length
            | _ ->
                failwithlogf "Unknown exception: %s" exn.Message

    /// 복수개의 tags(최대 16개까지) 들을 읽는다.
    let internal rawReadRandomTagsWithNames stream cpu (tags:string []) =
        assert(tags.Length <= 16)
        logDebug "=== Random read: count=%d, tags=%A" tags.Length tags

        createRandomReadRequestPacket cpu tags   // 보낼 packet 및 response packet 의 length 를 구함
        ||> rawSendPacketAndGetResponse stream   // packet  전송하고, (해당 길이에 맞는지 check 해서) response packet 구함
        |> analyzeRandomReadResponse cpu tags    // 응답 packet 분석해서 [ (tag * value) ] 를 반환



[<AutoOpen>]
module BlockReadWrite =

    let createBlockWriteRequestPacket cpu (tag:string) (values:byte []) =
        /// 연속 쓰기 명령 크기 = 12 : Command(2B) + Data type(2B) + Reserved(2B) + Num Blocks(2B) + Start Tag Name Length(2B) ++++++++ Tag Name(N B) + Data Length(2B)
        let commandPacketLength = 12
        let blockLength = commandPacketLength + tag.Length + values.Length
        let header = createHeader cpu blockLength

        /// header length(20B) + 명령어(2B) + 데이터 타입(2B) + 예약(2B) + 에러상태(2B) + 블록갯수(2B = 1)
        let expectedResponsePacketLength = 30

        (
            [|
                yield! header   // 마지막은 length
                yield! Command.WriteRequest.ToUInt16().ToBytes()
                yield! DataType.Continuous.ToUInt16().ToBytes()

                // reserved area2
                yield! 0us.ToBytes()

                // 블록수
                yield! 1us.ToBytes()

                // 시작 변수 길이
                yield! tag.Length |> uint16 |> fun x -> x.ToBytes()
                // 변수 명 : 시작 위치
                yield! str2bytes(tag)

                yield! values.Length |> uint16 |> fun x -> x.ToBytes()
                // 쓸 data
                yield! values
            |],
            expectedResponsePacketLength
        )


    /// 연속 읽기 명령
    let createBlockReadRequestPacket cpu (tag:string) count =
        assert(count <= getMaxBlockReadByteCount cpu) // 최대 1400 byte 까지 읽을 수 있음
        // todo : byte 단위로만 읽어야 하는데, 선두 주소가 byte 가 아닌 경우 동작하는지 check 필요
        // let tagAnal = tryParseTag (cpu:CpuType) tag


        /// 연속 읽기 명령 크기 = 12 : Command(2B) + Data type(2B) + Reserved(2B) + Error State(2B) + Error Code or Num Tags(2B) + Data Length(2B)
        let commandPacketLength = 12
        let blockLength = commandPacketLength + tag.Length
        let header = createHeader cpu blockLength
        let expectedResponsePacketLength = commandPacketLength + count + headerPacketLength

        (
            [|
                yield! header   // 마지막은 length
                yield! Command.ReadRequest.ToUInt16().ToBytes()
                yield! DataType.Continuous.ToUInt16().ToBytes()

                // reserved area2
                yield! 0us.ToBytes()

                // 블록수
                yield! 1us.ToBytes()

                // 시작 변수 길이
                yield! tag.Length |> uint16 |> fun x -> x.ToBytes()
                // 변수 명 : 시작 위치
                yield! str2bytes(tag)

                // 읽을 변수 갯수
                yield! count |> uint16 |> fun x -> x.ToBytes()
            |],
            expectedResponsePacketLength
        )



    /// 연속 읽기에 대한 response buffer 분석.  읽은 갯수만큼 byte array 로 반환한다.
    let analyzeBlockReadResponse cpu buffer =
        let header = verifyReponseHeader (Some cpu) buffer
        let blockLength = header.Length
        //logDebug "Block length=%d" blockLength

        assert(buffer.[20..21].ToUInt16() = Command.ReadResponse.ToUInt16())
        assert(buffer.[22..23].ToUInt16() = DataType.Continuous.ToUInt16())

        //logDebug "DataType Checked OK!!"

        // reserved : 값이 수시로 바뀌는 듯... [0..2] 사이의 값
        //assert(buffer.[24] = 0uy)
        //assert(buffer.[25] = 0uy)

        let errorState = buffer.[26..27].ToUInt16() // 0, 0
        //logDebug "Error State=%d" errorState

        if errorState <> 0us then
            // NAK
            let errorCode = buffer.[28..29].ToUInt16() // 0, 0
            let msg = errorCode |> getErrorMessage
            logError "%s" msg
            // todo fail 해서는 안된다.  반환값 type 을 Result<> 로 변경 고려!!
            failwithlogf "%s" msg


        // ACK case

        /// 전체 읽어 들일 block 수
        let numBlocks = buffer.[28..29].ToUInt16()
        assert(numBlocks = 1us)

        let numBytes = buffer.[30..31].ToUInt16()
        let dataBlock = buffer.[32..]
        //logDebug "Num Bytes=%d" numBytes
        (numBytes, dataBlock)

    /// 연속 쓰기에 대한 response buffer 분석
    let analyzeBlockWriteResponse cpu buffer =
        let header = verifyReponseHeader (Some cpu) buffer
        let blockLength = header.Length
        logDebug "Block length=%d" blockLength

        assert(buffer.[20..21].ToUInt16() = Command.WriteResponse.ToUInt16())
        assert(buffer.[22..23].ToUInt16() = DataType.Continuous.ToUInt16())
        //assert(buffer.[24..25].ToUInt16() = 0us)    // 예약 영역: Don't care
        let errorState = buffer.[26..27].ToUInt16()
        if errorState <> 0us then    // Error state
            let errorCode = buffer.[28..29].ToUInt16()
            failwithlogf "Failed with error code %d" errorCode

        assert(buffer.[28..29].ToUInt16() = 1us)    // Number of blocks

        assert(buffer.Length = 30)


    /// 연속 읽기 :
    /// startTag : 읽기 시작 위치의 tag 명
    /// count : 읽을 byte 수.  최대 1400
    let rawReadBlock stream cpu startTag count =
        //logDebug "=== Block read: start=%s, count=%d" startTag count
        assert(count <= getMaxBlockReadByteCount cpu)
        let bytes =
            createBlockReadRequestPacket cpu startTag count  // 보낼 packet 및 response packet 의 length 를 구함
            ||> rawSendPacketAndGetResponse stream           // packet  전송하고, (해당 길이에 맞는지 check 해서) response packet 구함
            |> analyzeBlockReadResponse cpu                  // 응답 packet 분석해서 length * values[] 를 반환
            |> snd                                           // values[] 만 반환 (byte [])
        assert(bytes.Length = count)
        bytes



