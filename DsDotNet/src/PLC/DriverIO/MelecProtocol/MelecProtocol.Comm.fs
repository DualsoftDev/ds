namespace MelsecProtocol

open System
open System.Net.Sockets
open System.Text

type MxEthernet(ip: string, port: int, timeoutMs: int) =
    // TCP 연결 및 스트림 초기화
    let client = new TcpClient()
    let stream = 
        client.ReceiveTimeout <- timeoutMs
        client.SendTimeout <- timeoutMs
        client.Connect(ip, port)
        client.GetStream()

    // 기본 3E 프레임 헤더 구성 함수
    member private _.MakeHeader(dataLength: int) =
        [|
            yield! [| 0x50uy; 0x00uy |]  // Subheader: PC → PLC
            yield! [| 0x00uy; 0xFFuy |]  // Network No. / PC No.
            yield! [| 0xFFuy; 0x03uy |]  // I/O No. / Unit No.
            yield! [| 0x00uy; 0x00uy |]  // Station No. / Reserved
            yield! BitConverter.GetBytes(uint16 dataLength) // 데이터 길이
            yield! [| 0x00uy; 0x00uy |]  // 모니터링 타이머 (기본값)
        |]




    // 워드 읽기 명령 생성
    member x.BuildReadWordCommand(device: string, startAddr: int, count: int) =
        let deviceCode =
            match device with
            | "D" -> 0xA8uy
            | "M" -> 0x90uy
            | _ -> failwith $"Unsupported device: {device}"

        let addrBytes = BitConverter.GetBytes(startAddr)
        let addr32 = [| addrBytes[0]; addrBytes[1]; 0x00uy |] // 3바이트 주소

        let payload =
            [|
                yield! [| 0x01uy; 0x04uy |]  // Command: Read (0401)
                yield! [| 0x00uy; 0x00uy |]  // Subcommand
                yield! addr32               // Start address (3 bytes)
                yield deviceCode            // Device code
                yield! BitConverter.GetBytes(uint16 count) // Read point count
            |]

        let header = x.MakeHeader(payload.Length)
        Array.concat [ header; payload ]

    // 명령 전송 및 응답 수신
    member _.SendAndReceive(data: byte[]) =
        stream.Write(data, 0, data.Length)
        let buffer = Array.zeroCreate 1024
        let bytesRead = stream.Read(buffer, 0, buffer.Length)
        buffer.[0..bytesRead-1]

    // 응답 바이트 배열을 정수 배열로 변환
    member private _.BytesToIntArray(data: byte[]) =
        data
        |> Array.chunkBySize 2
        |> Array.map (fun (pair: byte[]) ->
            match pair with
            | [| l; h |] -> int h <<< 8 ||| int l
            | _ -> failwith "Invalid byte pair")

    // 외부 API: 워드 읽기
    member this.ReadWord(device: string, startAddr: int, count: int) =
        let cmd = this.BuildReadWordCommand(device, startAddr, count)
        let res = this.SendAndReceive(cmd)
        let wordData = res.[11..] // 응답 헤더 11바이트 이후부터 유효 데이터
        this.BytesToIntArray(wordData)

    // 외부 API: 연결 종료
    member _.Close() =
        stream.Close()
        client.Close()
