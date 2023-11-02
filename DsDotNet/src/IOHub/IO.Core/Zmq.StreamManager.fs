namespace IO.Core
open System
open System.IO
open Dual.Common.Core.FS
open IO.Spec
open System.Reactive.Subjects
open System.Runtime.InteropServices
open System.ComponentModel

[<AutoOpen>]
module ZmqStreamManager =
    [<Obsolete("Use DualCommon nuget 0.1.15 of DualSoft-Common-Core-FS")>]
    let raisewithlog (ex:Exception): unit =
        logError $"{ex}"
        raise ex

    type ClientIdentifier = byte[]

    type ExcetionWithClient(clientId:ClientIdentifier, errMsg:ErrorMessage) =
        inherit Exception(errMsg)
        member x.ClientId = clientId

    /// 발생한 exception 이 요청 client 에 전달 되도록 clientId 를 갖는 예외 raise
    let raiseWithClientId (clientId:ClientIdentifier) (errMsg:ErrorMessage) =
        ExcetionWithClient(clientId, errMsg) |> raisewithlog


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



    type IOChangeInfo(fileSpec:IOFileSpec, offset:int, value:obj) =
        member val IOFileSpec = fileSpec
        /// 값이 변경된 offset.  value 의 type 에 따라 다르게 해석
        /// bool: 전체의 bit offset.  byte offset = Offset / 8
        /// uint64: uint64 기준의 offset.   byte offset = Offset * 8
        /// ....
        member val Offset = offset
        member val Value = value

    type StreamManager(fileSpec:IOFileSpec) as this =
        let stream:FileStream = fileSpec.FileStream
        let locker = obj()  // 객체를 lock용으로 사용
        let ioChangedSubject = new Subject<IOChangeInfo>()

        do
            fileSpec.StreamManager <- this

        interface IStreamManager

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

        member x.writeBit(bitIndex:int, value:bool) = x.writeBit(bitIndex / 8, bitIndex % 8, value)
        member x.writeBit(byteIndex:int, bitIndex:int, value:bool) = x.writeBits( [|(byteIndex, bitIndex, value)|] )

        member x.writeBits(writeBitArgs:(int*int*bool) seq) = // (byteIndex:int, bitIndex:int, value:bool) array) =
            lock locker (fun () ->
                for (byteIndex, bitIndex, value) in writeBitArgs do
                    let currentByte = x.readU8(byteIndex)

                    // 비트를 설정하거나 클리어
                    let updatedByte =
                        if value then
                            currentByte ||| (1uy <<< bitIndex)   // OR 연산을 사용하여 비트 설정
                        else
                            currentByte &&& (~~~(1uy <<< bitIndex))  // AND 연산과 NOT 연산을 사용하여 비트 클리어

                    // 수정된 바이트를 해당 위치에 쓰기
                    writeTBytes<byte> stream byteIndex updatedByte
                    let offset = byteIndex * 8 + bitIndex
                    ioChangedSubject.OnNext(IOChangeInfo(fileSpec, offset, value))
                x.Flush()                
            )

        member x.writeU8s (writeArg:(int*byte) seq) =
            lock locker (fun () ->
                for (offset:int, value:byte) in writeArg do
                    writeTBytes<byte> stream offset value
                    ioChangedSubject.OnNext(IOChangeInfo(fileSpec, offset, value))
                x.Flush()
            )


        member x.writeU16(wordOffset:int, value:uint16) = x.writeU16s ([|wordOffset, value|])
        member x.writeU16s (writeArg:(int*uint16) seq) =
            lock locker (fun () ->
                for (offset:int, value:uint16) in writeArg do
                    writeTBytes<uint16> stream offset value
                    ioChangedSubject.OnNext(IOChangeInfo(fileSpec, offset, value))
                x.Flush()
            )

        member x.writeU32(wordOffset:int, value:uint32) = x.writeU32s ([|wordOffset, value|])
        member x.writeU32s (writeArg:(int*uint32) seq) =
            lock locker (fun () ->
                for (offset:int, value:uint32) in writeArg do
                    writeTBytes<uint32> stream offset value
                    ioChangedSubject.OnNext(IOChangeInfo(fileSpec, offset, value))
                x.Flush()
            )

        member x.writeU64(wordOffset:int, value:uint64) = x.writeU64s ([|wordOffset, value|])
        member x.writeU64s (writeArg:(int*uint64) seq) =
            lock locker (fun () ->
                for (offset:int, value:uint64) in writeArg do
                    writeTBytes<uint64> stream offset value
                    ioChangedSubject.OnNext(IOChangeInfo(fileSpec, offset, value))
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
module ZmqBufferManagerExtension =
    type IOFileSpec with
        member x.InitiaizeFile(dir:string) =
            let path = Path.Combine(dir, x.Name)
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

    type StreamManager with
        member x.VerifyIndices(clientId:ClientIdentifier, offset:int) =
            let offset = int64 offset
            let length = x.FileStream.Length
            if offset < 0 then
                raiseWithClientId clientId ($"Invalid offset.  non-negative value required : {offset}")
            if offset >= length then
                raiseWithClientId clientId ($"Invalid offset: {offset}.  Exceed length limit {length})")
        member x.VerifyIndices(clientId:ClientIdentifier, offsets:int[]) =
            offsets |> iter (fun offset -> x.VerifyIndices(clientId, offset))

        member x.Verify(clientId:ClientIdentifier, indices:int[], numValues:int) =
            x.VerifyIndices (clientId, indices)
            if indices.Length <> numValues then
                raiseWithClientId clientId $"The number of indices and values should be the same."
            
    type IOChangeInfo with
        member x.GetTagName() =
            let x = x
            let fs, offset, value = x.IOFileSpec, x.Offset, x.Value
            let addrResolver = fs.Vendor.AddressResolver
            let tag =
                let byteSize = Marshal.SizeOf(value)
                let byteOffset, bitOffset, contentBitLength = 
                    match value with
                    | :? bool -> offset / 8, offset % 8, 1
                    | _ -> offset / byteSize, 0, (byteSize * 8)

                addrResolver.GetTagName(fs.GetPath(), byteOffset, bitOffset, contentBitLength)
            tag