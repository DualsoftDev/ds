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

