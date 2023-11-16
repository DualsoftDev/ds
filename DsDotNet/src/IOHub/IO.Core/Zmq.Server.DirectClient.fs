namespace IO.Core

open System.Threading
open Dual.Common.Core.FS

/// 서버에 통신을 통하지 않고 직접적으로 접근해서 Tag 값을 read/write
/// 서버를 생성한 host program 에서만 사용 가능
[<AllowNullLiteral>]
type ServerDirectAccess(ioSpec:IOSpec, cancellationToken:CancellationToken) =
    inherit Server(ioSpec, cancellationToken)

    let serverRequestInfo = ClientRequestInfo(-1 |> ByteConverter.ToBytes<int>, -1)
    member x.Read(tag:string):obj =
        match tag with
        | AddressPattern ap ->
            let offset = ap.Offset
            let bufferManager = ap.IOFileSpec.StreamManager :?> StreamManager

            match ap.MemoryType with
            | MemoryType.Bit   -> bufferManager.readBit(offset) :> obj
            | MemoryType.Byte  -> bufferManager.readU8(offset)
            | MemoryType.Word  -> bufferManager.readU16(offset)
            | MemoryType.DWord -> bufferManager.readU32(offset)
            | MemoryType.LWord -> bufferManager.readU64(offset)
            | _ -> failwithf($"Unknown data type : {ap.MemoryType}")
        | _ -> failwithlog "ERROR"

    member x.Write(tag:string, value:obj) =
        match tag with
        | AddressPattern ap ->
            let offset = ap.Offset
            let bufferManager = ap.IOFileSpec.StreamManager :?> StreamManager

            match ap.MemoryType with
            | MemoryType.Bit   -> bufferManager.writeBits serverRequestInfo [|(offset, value :?> bool)|]
            | MemoryType.Byte  -> bufferManager.writeU8s  serverRequestInfo [|(offset, value :?> byte)|]
            | MemoryType.Word  -> bufferManager.writeU16s serverRequestInfo [|(offset, value :?> uint16)|]
            | MemoryType.DWord -> bufferManager.writeU32s serverRequestInfo [|(offset, value :?> uint32)|]
            | MemoryType.LWord -> bufferManager.writeU64s serverRequestInfo [|(offset, value :?> uint64)|]
            | _ -> failwithf($"Unknown data type : {ap.MemoryType}")

            // TODO: 서버를 통한 직접 변경 내용을 client 에게 공지
            //notifyIoChange()
        | _ -> failwithlog "ERROR"



