namespace IO.Core
open System
open System.IO
open Dual.Common.Core.FS
open IO.Spec

[<AutoOpen>]
module ZmqBufferManager =
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

    type BufferManager(fileSpec:IOFileSpec) as this =
        let stream:FileStream = fileSpec.FileStream
        let locker = obj()  // 객체를 lock용으로 사용

        do
            fileSpec.BufferManager <- this

        interface IBufferManager

        member x.FileStream = stream
        member x.Flush() = stream.Flush()

        [<Obsolete>]
        member x.readBits (bitOffset: int, count: int) : bool[] =
            let startByte = bitOffset / 8
            let endByte = startByte + count / 8
            let byteCount = endByte - startByte + 1
            let buffer:byte[] = x.readU8s(startByte, byteCount)
            // buffer 내용을 참조해서 bool 배열로 변환

            let bits =
                buffer
                   |> Array.map (fun b -> Convert.ToString(b, 2).PadLeft(8, '0'))
                   |> Array.collect (fun s -> s.ToCharArray())
                   |> Array.skip  (bitOffset % 8)
                   |> Array.map (fun c -> c = '1')
                   |> Array.take count
                   |> Array.ofSeq
            bits
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

        //member x.readBit(byteOffset:int, bitOffset:int) = x.readBits(byteOffset * 8 + bitOffset, 1)[0]
        member x.readBit(bitOffset:int) = x.readBits([|bitOffset|])  |> Seq.exactlyOne
        member x.readU8 (byteOffset:int) = x.readU8s([|byteOffset|]) |> Seq.exactlyOne
        member x.readU16(wordOffset:int) = x.readU16s([|wordOffset|]) |> Seq.exactlyOne
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
        [<Obsolete>]
        member x.readU8s (byteOffset: int, count: int) : byte[] =
            lock locker (fun () ->
                let offset = byteOffset
                let buffer = Array.zeroCreate<byte> count
                stream.Seek(int64 offset, SeekOrigin.Begin) |> ignore
                stream.Read(buffer, 0, count) |> ignore
                buffer
            );
        [<Obsolete>]
        member x.readU16s (wordOffset: int, count: int) : uint16[] =
            lock locker (fun () ->
                let offset = wordOffset * 2
                let buffer = Array.zeroCreate<byte> (count * 2)
                stream.Seek(int64 offset, SeekOrigin.Begin) |> ignore
                stream.Read(buffer, 0, count * 2) |> ignore
                Array.init count (fun i -> System.BitConverter.ToUInt16(buffer, i * 2))
            )
        [<Obsolete>]
        member x.readU32s (dwordOffset: int, count: int) : uint32[] =
            lock locker (fun () ->
                let offset = dwordOffset * 4
                let buffer = Array.zeroCreate<byte> (count * 4)
                stream.Seek(int64 offset, SeekOrigin.Begin) |> ignore
                stream.Read(buffer, 0, count * 4) |> ignore
                Array.init count (fun i -> System.BitConverter.ToUInt32(buffer, i * 4))
            )
        [<Obsolete>]
        member x.readU64s (lwordOffset: int, count: int) : uint64[] =
            lock locker (fun () ->
                let offset = lwordOffset * 8
                let buffer = Array.zeroCreate<byte> (count * 8)
                stream.Seek(int64 offset, SeekOrigin.Begin) |> ignore
                stream.Read(buffer, 0, count * 8) |> ignore
                Array.init count (fun i -> System.BitConverter.ToUInt64(buffer, i * 8))
            )


        member x.writeBit(bitIndex:int, value:bool) = x.writeBit(bitIndex / 8, bitIndex % 8, value)
        member x.writeBit(byteIndex:int, bitIndex:int, value:bool) =
            let currentByte = x.readU8(byteIndex)

            // 비트를 설정하거나 클리어
            let updatedByte =
                if value then
                    currentByte ||| (1uy <<< bitIndex)   // OR 연산을 사용하여 비트 설정
                else
                    currentByte &&& (~~~(1uy <<< bitIndex))  // AND 연산과 NOT 연산을 사용하여 비트 클리어

            // 수정된 바이트를 해당 위치에 쓰기
            x.writeU8(byteIndex, updatedByte)

        member x.writeU8 (offset:int, value:byte) =
            lock locker (fun () ->
                stream.Seek(int64 offset, SeekOrigin.Begin) |> ignore
                stream.WriteByte(value)
            )

        member x.writeU16(wordOffset:int, value:uint16) =
            lock locker (fun () ->
                let byteOffset = wordOffset * 2
                let buffer = System.BitConverter.GetBytes(value)
                stream.Seek(int64 byteOffset, SeekOrigin.Begin) |> ignore
                stream.Write(buffer, 0, buffer.Length)
            )

        member x.writeU32(dwordOffset:int, value:uint32) =
            lock locker (fun () ->
                let offset = dwordOffset * 4
                let buffer = System.BitConverter.GetBytes(value)
                stream.Seek(int64 offset, SeekOrigin.Begin) |> ignore
                stream.Write(buffer, 0, buffer.Length)
            )

        member x.writeU64(lwordOffset:int, value:uint64) =
            lock locker (fun () ->
                let offset = lwordOffset * 8
                let buffer = System.BitConverter.GetBytes(value)
                stream.Seek(int64 offset, SeekOrigin.Begin) |> ignore
                stream.Write(buffer, 0, buffer.Length)
            )
        member x.clear() =
            lock locker (fun () ->
                stream.Seek(0L, SeekOrigin.Begin) |> ignore
                for i in 0L .. (stream.Length - 1L) do
                    stream.WriteByte(0uy)
            )

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
                let xxx = fs.Length
                Console.WriteLine($"File length : {fs.Length}")
                fs.Flush()
            x.FileStream <- fs

    type BufferManager with
        member x.VerifyIndices(offset:int) =
            let offset = int64 offset
            let length = x.FileStream.Length
            if offset < 0 then
                failwithlogf($"Invalid offset.  non-negative value required : {offset}")
            if offset >= length then
                failwithlogf($"Invalid offset: {offset}.  Exceed length limit {length})")
        member x.VerifyIndices(offsets:int[]) =
            offsets |> iter (fun offset -> x.VerifyIndices(offset))

        member x.Verify(indices:int[], numValues:int) =
            x.VerifyIndices indices
            if indices.Length <> numValues then
                failwithf($"The number of indices and values should be the same.")
            
