

// Learn more about F# at https://fsharp.org
// See the 'F# Tutorial' project for more help.

//open Dsu.PLC.LS

// Define your library scripting code here
#I @"bin\Debug\net47"
#r @"F:\Git\dual\common\PLCInterface\Dsu.PLC\bin\netcoreapp3.1\Dual.Common.FS.dll"
#r "Dual.PLC.Common.dll"
#r "Dsu.PLC.LS.dll"
#r "nuget: Log4Net" 

open PacketImpl


open System
open System.Net.Sockets
open System.Text
open Dual.Common.Core.FS
open Dsu.PLC.LS



#load "Packet.fs"



let plcIp = "192.168.0.223"
let conn = new TcpClient(plcIp, 2004);
let stream = conn.GetStream()

if not <| conn.Client.IsConnected() then
    failwith "Connection lost"


let tags = ["%MW0009"; "%MW1001"]
//let tags = ["%MW0009"; "%MW1000"; "%MW1001"]
//let tags = ["%MW1000"]

let testRead (tags:string []) =
    let packet, responseLength = createRandomReadRequestPacket CpuType.Xgi tags       // CpuType.Xgi / CpuType.Xgk
    let hexx = packet |> Array.map (sprintf "%x") |> String.concat(" ")
    stream.Write(packet, 0, packet |> Array.length)

    let buffer = Array.zeroCreate<byte>(responseLength)     // 변수1개=34, 2개=38, 3개=42
    let nBytes = stream.Read(buffer, 0, responseLength)
    //buffer |> Array.mapi (fun i b -> sprintf "[%d]%x(%d)" i b b) |> String.concat(" ")
    buffer |> Array.map (sprintf "%02x") |> String.concat(" ") |> tracefn "%s"

    let readOuts = analyzeRandomReadResponse tags buffer
    readOuts

let testWrite (tags:string []) (writeValues:uint64 []) =
    let packet, responseLength = createRandomWriteRequestPacket CpuType.Xgi tags writeValues
    let hexx = packet |> Array.map (sprintf "%x") |> String.concat(" ")
    stream.Write(packet, 0, packet |> Array.length)

    /// header length 20 + write instruction 10 byte
    let buffer = Array.zeroCreate<byte>(responseLength)     // 변수1개=34, 2개=38, 3개=42
    let nBytes = stream.Read(buffer, 0, responseLength)
    //buffer |> Array.mapi (fun i b -> sprintf "[%d]%x(%d)" i b b) |> String.concat(" ")
    buffer |> Array.map (sprintf "%02x") |> String.concat(" ") |> printfn "%s"

    analyzeRandomWriteResponse tags buffer




// 실전송: 4C 53 49 53 2D 58 47 54 00 00 00 00 A0 33 02 00 11 00 03 40 54 00 02 00 00 00 01 00 07 00 25 44 57 31 30 30 30
// 실수신: 4C 53 49 53 2D 58 47 54 00 00 04 01 A0 11 02 00 0E 00 00 21 55 00 02 00 00 02 00 00 01 00 02 00 00 00
// 기대치: 4C 53 49 53 2D 58 47 54 00 00 04 01 3F 02 00 0E 00 03 24 55 00 02 00 02 01 00 00 01 00 02 00 74 4E



let testLsConnection()=
    let conn = new LsConnection(LsConnectionParameters(plcIp))
    conn.ReadATag("MB0005")