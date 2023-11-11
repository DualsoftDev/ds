namespace IO.Core
open System
open System.IO
open Dual.Common.Core.FS
open System.Reactive.Subjects

open Dapper
open Microsoft.Data.Sqlite

[<AutoOpen>]
module internal ZmqStreamManager =
    /// Client 고유 id 구분자 type.  byte[]
    type ClientIdentifier = byte[]
    let clientIdentifierToString (clientId:ClientIdentifier) = clientId |> map string |> String.concat "-"

    /// client 가 server 에 요청할 때의 정보 관리.  client id 및 request id.
    type ClientRequestInfo(clientId:ClientIdentifier, requestId:int) = 
        interface IClientRequestInfo
        member x.ClientId = clientId
        member x.RequestId = requestId

    type ExcetionWithClient(clientRequestInfo:ClientRequestInfo, errMsg:ErrorMessage) =
        inherit Exception(errMsg)
        member x.ClientId = clientRequestInfo.ClientId
        member x.RequestId = clientRequestInfo.RequestId

    /// 발생한 exception 이 요청 client 에 전달 되도록 clientId 를 갖는 예외 raise
    let raiseWithClientId (clientRequestInfo:ClientRequestInfo) (errMsg:ErrorMessage) =
        ExcetionWithClient(clientRequestInfo, errMsg) |> raisewithlog


    let lockedExe (locker:obj) f =
        if isNull(locker) then
            f()
        else
            lock locker f

    /// stream 의 주어진 offset 에서 type 'T 만큼의 byte 를 읽어서 반환
    let readTBytes<'T> (stream:FileStream) (tOffset:int): byte[] =
        let size = sizeof<'T>
        let byteOffset = tOffset * size
        let buffer = Array.zeroCreate<byte> (size)
        stream.Seek(int64 byteOffset, SeekOrigin.Begin) |> ignore
        stream.Read(buffer, 0, size) |> ignore
        buffer

    /// stream 의 주어진 offset 에서 type 'T 의 value 를 bytes 로 변환해서 write
    let writeTBytes<'T> (stream:FileStream) (tOffset:int) (value:'T) =
        let size = sizeof<'T>
        let byteOffset = tOffset * size
        let buffer = ByteConverter.ToBytes(value)
        assert(buffer.Length = size)
        stream.Seek(int64 byteOffset, SeekOrigin.Begin) |> ignore
        stream.Write(buffer, 0, size)



    type IOChangeInfo(clientRequestInfo:ClientRequestInfo, fileSpec:IOFileSpec, memoryType:MemoryType, offsets:int seq, value:obj) =
        interface IIOChangeInfo with
            member x.ClientRequestInfo = x.ClientRequestInfo :> IClientRequestInfo
            member x.IOFileSpec = fileSpec
            member x.Offsets = x.Offsets
            member x.Value = value
            member x.MemoryType = memoryType
            
        member x.ClientRequestInfo = clientRequestInfo
        member x.IOFileSpec = fileSpec

        /// 값이 변경된 offset.  value 의 type 에 따라 다르게 해석
        /// bool: 전체의 bit offset.  byte offset = Offset / 8
        /// uint64: uint64 기준의 offset.   byte offset = Offset * 8
        /// ....
        member val Offsets = offsets |> toArray
        member x.Value = value
        member x.MemoryType = memoryType

    type StringChangeInfo(clientRequestInfo:ClientRequestInfo, fileSpec:IOFileSpec, keys:string seq, value:obj) =
        interface IStringChangeInfo with
            member x.ClientRequestInfo = x.ClientRequestInfo :> IClientRequestInfo
            member x.IOFileSpec = fileSpec
            member x.Keys = x.Keys
            member x.Value = value
            
        member x.ClientRequestInfo = clientRequestInfo
        member x.IOFileSpec = fileSpec

        member val Keys = keys |> toArray
        member x.Value = value

    type WriteOK(changes:IOChangeInfo seq) =
        member val Changes = changes |> toArray

    type StreamManager(fileSpec:IOFileSpec) as this =
        let stream:FileStream = fileSpec.FileStream
        let locker = obj()  // 객체를 lock용으로 사용
        let ioChangedSubject = new Subject<IOChangeInfo>()

        do
            fileSpec.StreamManager <- this

        interface IStreamManager

        member x.FileSpec = fileSpec
        member x.FileStream = stream
        member x.Flush() = stream.Flush()

        member x.readBits (bitOffsets: int[]) : bool[] =
            lock locker (fun () ->
                [|  for tOffset in bitOffsets do
                        let byteOffset = tOffset / 8
                        let byte_ = x.readU8 byteOffset
                        let shift = 1uy <<< (tOffset % 8)
                        let bit = byte_ &&& shift
                        yield bit <> 0uy
                |]
            );

        member x.readBit(bitOffset:int)   = x.readBits([|bitOffset|])   |> Seq.exactlyOne
        member x.readU8 (byteOffset:int)  = x.readU8s ([|byteOffset|])  |> Seq.exactlyOne
        member x.readU16(wordOffset:int)  = x.readU16s([|wordOffset|])  |> Seq.exactlyOne
        member x.readU32(dwordOffset:int) = x.readU32s([|dwordOffset|]) |> Seq.exactlyOne
        member x.readU64(lwordOffset:int) = x.readU64s([|lwordOffset|]) |> Seq.exactlyOne

        member x.readU8s (byteOffsets: int[]) : byte[] =
            lock locker (fun () ->
                [|  for tOffset in byteOffsets do
                        let bytes = readTBytes<byte> stream tOffset
                        assert(bytes.Length = 1)
                        yield bytes[0]
                |]
            );
        member x.readU16s (wordOffsets: int[]) : uint16[] =
            lock locker (fun () ->
                [|  for tOffset in wordOffsets do
                        let bytes = readTBytes<uint16> stream tOffset
                        assert(bytes.Length = 2)
                        yield System.BitConverter.ToUInt16(bytes, 0)
                |]
            )
        member x.readU32s (dwordOffsets: int[]) : uint32[] =
            lock locker (fun () ->
                [|  for tOffset in dwordOffsets do
                        let bytes = readTBytes<uint32> stream tOffset
                        assert(bytes.Length = 4)
                        yield System.BitConverter.ToUInt32(bytes, 0)
                |]
            )
        member x.readU64s (lwordOffsets: int[]) : uint64[] =
            lock locker (fun () ->
                [|  for tOffset in lwordOffsets do
                        let bytes = readTBytes<uint64> stream tOffset
                        assert(bytes.Length = 8)
                        yield System.BitConverter.ToUInt64(bytes, 0)
                |]
            )

        member x.writeBit (cri:ClientRequestInfo, offset:int, value:bool) = x.writeBits cri [|(offset, value)|]

        member x.writeBits (cri:ClientRequestInfo) (writeBitArgs:(int*bool) seq) =
            lock locker (fun () ->
                for (offset, value) in writeBitArgs do
                    let byteIndex, bitIndex = offset / 8, offset % 8
                    let currentByte = x.readU8(byteIndex)

                    // 비트를 설정하거나 클리어
                    let updatedByte =
                        if value then
                            currentByte ||| (1uy <<< bitIndex)   // OR 연산을 사용하여 비트 설정
                        else
                            currentByte &&& (~~~(1uy <<< bitIndex))  // AND 연산과 NOT 연산을 사용하여 비트 클리어

                    // 수정된 바이트를 해당 위치에 쓰기
                    writeTBytes<byte> stream byteIndex updatedByte

                x.Flush()
            )

        member x.writeU8s (cri:ClientRequestInfo)  (writeArg:(int*byte) seq) =
            lock locker (fun () ->
                for (offset:int, value:byte) in writeArg do
                    writeTBytes<byte> stream offset value
                x.Flush()
            )


        member x.writeU16 (cri:ClientRequestInfo) (wordOffset:int, value:uint16) = x.writeU16s cri ([|wordOffset, value|])
        member x.writeU16s (cri:ClientRequestInfo) (writeArg:(int*uint16) seq) =
            lock locker (fun () ->
                for (offset:int, value:uint16) in writeArg do
                    writeTBytes<uint16> stream offset value
                x.Flush()
            )

        member x.writeU32 (cri:ClientRequestInfo) (wordOffset:int, value:uint32) = x.writeU32s cri ([|wordOffset, value|])
        member x.writeU32s (cri:ClientRequestInfo) (writeArg:(int*uint32) seq) =
            lock locker (fun () ->
                for (offset:int, value:uint32) in writeArg do
                    writeTBytes<uint32> stream offset value
                x.Flush()
            )

        member x.writeU64 (cri:ClientRequestInfo) (wordOffset:int, value:uint64) = x.writeU64s cri ([|wordOffset, value|])
        member x.writeU64s (cri:ClientRequestInfo) (writeArg:(int*uint64) seq) =
            lock locker (fun () ->
                for (offset:int, value:uint64) in writeArg do
                    writeTBytes<uint64> stream offset value
                x.Flush()
            )

        member x.clear() =
            lock locker (fun () ->
                stream.Seek(0L, SeekOrigin.Begin) |> ignore
                for i in 0L .. (stream.Length - 1L) do
                    stream.WriteByte(0uy)
                x.Flush()
            )

        member x.IOChangedSubject = ioChangedSubject

[<AutoOpen>]
module internal ZmqBufferManagerExtension =
    type KeyValueRow() =
        member val Key = "" with get, set
        member val Value = "" with get, set
    let createConnection(connStr) =
        new SqliteConnection(connStr) |> tee (fun conn -> conn.Open())

    type IOFileSpec with
        member x.InitiaizeFile(baseDir:string) =
            if x.IsStringStorage then
                // database table 초기화
                let path = Path.Combine(baseDir, $"{x.Name}.sqlite3") |> Path.GetFullPath
                x.ConnectionString <- $"Data Source={path}"
                use conn = createConnection(x.ConnectionString)
                let sqlCreateSchema = """
                    CREATE TABLE IF NOT EXISTS [string] (
                        [key] TEXT PRIMARY KEY,
                        [value] TEXT
                    );
                """
                conn.Execute sqlCreateSchema |> ignore
                noop()
            else
                let path = Path.Combine(baseDir, x.Name)
                let mutable fs:FileStream = null
                if (File.Exists path) then
                    // ensure that the file has the correct length
                    fs <- new FileStream(path, FileMode.Open, FileAccess.ReadWrite)
                    if (fs.Length <> x.Length) then
                        failwithf($"File [{path}] length mismatch : {fs.Length} <> {x.Length}")
                else
                    logInfo($"Creating new file : {path}")
                    Directory.CreateDirectory(Path.GetDirectoryName(path)) |> ignore
                    // create zero-filled file with length = x.Length
                    fs <- new FileStream(path, FileMode.Create, FileAccess.ReadWrite)
                    let buffer = Array.zeroCreate<byte> x.Length
                    fs.Write(buffer, 0, x.Length)
                    fs.Seek(0, SeekOrigin.Begin) |> ignore
                    fs.SetLength(x.Length)
                    Console.WriteLine($"File length : {fs.Length}")
                    fs.Flush()
                x.FileStream <- fs
        member x.ReadStrings(keys:string[]) =
            use conn = createConnection(x.ConnectionString)
            if keys.Length = 0 then
                let kvs =
                    conn.Query<KeyValueRow>("SELECT [key], [value] FROM [string]")
                    //|> map (fun (kv:KeyValuePair) -> kv.Key, kv.Value) |> toArray
                    |> map (fun kv -> kv.Key, kv.Value) |> toArray
                let keys, values = Array.unzip kvs
                keys, values
            else
                let values =
                    [| for key in keys do
                        let values = conn.Query<string>("SELECT [value] FROM [string] WHERE key = @Key", {| Key = key |}) |> toArray

                        if values.Length = 0 then
                            null
                            //failwithf $"String tag not found: {key}"
                        else 
                            values[0]
                    |]
                keys, values

        member x.WriteString(key:string, value:string) =
            use conn = createConnection(x.ConnectionString)
            let sqlUpdate = "INSERT OR REPLACE INTO [string] ([key], [value]) VALUES (@Key, @Value)"
            conn.Execute(sqlUpdate, {| Key = key; Value = value |}) |> ignore

    type StreamManager with
        member x.VerifyIndices(clientRequestInfo:ClientRequestInfo, memoryType:MemoryType, offset:int) =
            let byteOffset = 
                match memoryType with
                | MemoryType.Bit   -> offset / 8
                | (MemoryType.Byte | MemoryType.Word | MemoryType.DWord | MemoryType.LWord) -> offset * (int memoryType)/8
                | _ -> failwithf($"Invalid data type: {memoryType}")
                |> int64

            let length = x.FileStream.Length
            if byteOffset < 0 then
                raiseWithClientId clientRequestInfo ($"Invalid offset.  non-negative value required : {byteOffset}")
            if byteOffset >= length then
                raiseWithClientId clientRequestInfo ($"Invalid offset: {byteOffset}.  Exceed length limit {length})")
        member x.VerifyOffsets(clientRequestInfo:ClientRequestInfo, memoryType:MemoryType, offsets:int[]) =
            offsets |> iter (fun offset -> x.VerifyIndices(clientRequestInfo, memoryType, offset))

        member x.Verify(clientRequestInfo:ClientRequestInfo, memoryType:MemoryType, indices:int[], numValues:int) =
            x.VerifyOffsets (clientRequestInfo, memoryType, indices)
            if indices.Length <> numValues then
                raiseWithClientId clientRequestInfo $"The number of indices and values should be the same."
            
    type IStringChangeInfo with
        member x.GetKeysAndValues() =
            Array.zip x.Keys (x.Value :?> string array)

    type IIOChangeInfo with
        member x.GetTagNameAndValues() =
            let fs, dataType, offsets, objValues = x.IOFileSpec, x.MemoryType, x.Offsets, x.Value
            let path = fs.GetPath()
            let addrResolver = fs.Vendor.AddressResolver
            [
                for (i, offset) in offsets |> indexed do
                    let contentBitLength = int dataType
                    let value = 
                        match dataType with
                        | MemoryType.Bit   -> (objValues :?> bool[]  )[i] |> box
                        | MemoryType.Byte  -> (objValues :?> byte[]  )[i] |> box
                        | MemoryType.Word  -> (objValues :?> uint16[])[i] |> box
                        | MemoryType.DWord -> (objValues :?> uint32[])[i] |> box
                        | MemoryType.LWord -> (objValues :?> uint64[])[i] |> box
                        | _ -> failwithf($"Invalid data type: {dataType}")

                    let tag = addrResolver.GetTagName(path, offset, contentBitLength)
                    yield tag, value
            ]

        /// x.Value 를 byte[] 로 변환.  서버 모듈에서만 호출되어야 한다.
        member x.GetValueBytes(): byte[] =
            let objValues = x.Value
            match x.MemoryType with
            | MemoryType.Bit   -> (objValues :?> bool[])   |> map (fun b -> if b then 1uy else 0uy)
            | MemoryType.Byte  -> (objValues :?> byte[])
            | MemoryType.Word  -> (objValues :?> uint16[]) |> ByteConverter.ToBytes<uint16>
            | MemoryType.DWord -> (objValues :?> uint32[]) |> ByteConverter.ToBytes<uint32>
            | MemoryType.LWord -> (objValues :?> uint64[]) |> ByteConverter.ToBytes<uint64>
            | _ -> failwithf($"Invalid data type: {x.MemoryType}")

