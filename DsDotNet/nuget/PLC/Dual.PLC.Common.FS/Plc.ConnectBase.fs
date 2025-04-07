namespace Dual.PLC.Common.FS

open System
open System.Net.Sockets
open System.Threading
open System.Threading.Tasks

type ConnectState =
    | Connected | ConnectFailed | Reconnected | ReconnectFailed | Disconnected

type ConnectChangedEventArgs = 
    { Ip: string; State: ConnectState }
    
type PlcTagValueChangedEventArgs = 
    { Ip: string; Tag: PlcTagBase }
    
/// 이더넷 기반 PLC 통신 베이스 클래스
[<AbstractClass>]
type PlcEthernetBase(ip: string, port: int, timeoutMs: int) =
    let mutable client: TcpClient option = None
    let mutable connected = false

    /// 연결 정보
    member _.Ip = ip
    member _.Port = port
    member _.IsConnected = connected

        /// 연결 시도 (timeoutMs 적용)
    member this.Connect() =
        try
            let tcpClient = new TcpClient()
            let cts = new CancellationTokenSource(timeoutMs)

            let task = tcpClient.ConnectAsync(ip, port)
            let completed = task.Wait(timeoutMs, cts.Token)

            if completed && tcpClient.Connected then
                client <- Some tcpClient
                connected <- true
                true
            else
                tcpClient.Close()
                false

        with
        | :? TaskCanceledException -> false
        | :? SocketException -> false
        | ex ->
            eprintfn $"[PLC Connect] 예외: {ex.Message}"
            false

    /// 재연결
    member this.ReConnect() =
        this.Disconnect() |> ignore
        this.Connect()

    /// 연결 종료
    member this.Disconnect() =
        match client with
        | Some tcpClient ->
            tcpClient.Close()
            connected <- false
            true
        | None -> false

    /// 내부 스트림 획득
    member this.GetStream() =
        match client with
        | Some tcp when connected -> tcp.GetStream()
        | _ -> failwith "PLC 연결이 되어 있지 않습니다."

    /// 프레임 전송
    member this.SendFrame(frame: byte[]) =
        let stream = this.GetStream()
        stream.Write(frame, 0, frame.Length)

    /// 프레임 수신
    member this.ReceiveFrame(bufferSize: int) : byte[] =
        let stream = this.GetStream()
        let buffer = Array.zeroCreate<byte> bufferSize
        let _ = stream.Read(buffer, 0, buffer.Length)
        buffer

    /// 읽기 프레임 생성 (제조사별 구현)
    abstract member CreateReadFrame: address:string * dataType:PlcDataSizeType -> byte[]
    
    /// 복수 주소 데이터 읽기 - 제조사별 override 필요
    abstract member CreateMultiReadFrame: addresses: string[] * dataType: PlcDataSizeType -> byte[]

    /// 복수 주소 응답 파싱 - 제조사별 override 필요
    abstract member ParseMultiReadResponse: buffer: byte[] * count: int * dataType: PlcDataSizeType * readData:byte[]-> unit

    /// 쓰기 프레임 생성 (제조사별 구현)
    abstract member CreateWriteFrame: address:string * dataType:PlcDataSizeType * value:obj -> byte[]

    /// 읽기 응답 파싱 (제조사별 구현)
    abstract member ParseReadResponse: byte[] * PlcDataSizeType -> obj

    /// 단일 주소 데이터 읽기
    member this.Read(address: string, dataType: PlcDataSizeType) : obj =
        let frame = this.CreateReadFrame(address, dataType)
        this.SendFrame(frame)
        let buffer = this.ReceiveFrame(256)
        this.ParseReadResponse(buffer, dataType)

    /// 복수 주소 읽기 기본 구현
    member this.Reads(addresses: string[], dataType: PlcDataSizeType, readBuffer:byte[]) =
        if addresses.Length = 0 then failwith "주소가 없습니다."
        let frame = this.CreateMultiReadFrame(addresses, dataType)
        this.SendFrame(frame)
        let buffer = this.ReceiveFrame(512) // 제조사에 따라 버퍼 크기 조정
        this.ParseMultiReadResponse(buffer, addresses.Length, dataType, readBuffer)

    /// 단일 주소 데이터 쓰기
    member this.Write(address: string, dataType: PlcDataSizeType, value: obj) : bool =
        try
            let frame = this.CreateWriteFrame(address, dataType, value)
            this.SendFrame(frame)
            let _ = this.ReceiveFrame(256)
            true
        with _ -> false