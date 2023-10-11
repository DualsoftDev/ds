namespace IO.Core
open System
open System.IO
open Dual.Common.Core.FS

[<AutoOpen>]
module ZmqBufferManager =
    type BufferManager(stream:FileStream) =
        let locker = obj()  // 객체를 lock용으로 사용
        //member x.Type = typ
        member x.FileStream = stream
        member x.Flush() = stream.Flush()
        member x.readBits (offset: int, count: int) : bool[] =
            let startByte = offset / 8
            let endByte = startByte + count / 8
            let byteCount = endByte - startByte + 1
            let buffer:byte[] = x.readU8s(startByte, byteCount)
            // buffer 내용을 참조해서 bool 배열로 변환

            let bits =
                buffer
                   |> Array.map (fun b -> Convert.ToString(b, 2).PadLeft(8, '0'))
                   |> Array.collect (fun s -> s.ToCharArray())
                   |> Array.skip  (offset % 8)
                   |> Array.map (fun c -> c = '1')
                   |> Array.take count
                   |> Array.ofSeq
            bits
        member x.readBit(offset:int) = x.readBits(offset, 1)[0]
        member x.readU8 (offset:int) = x.readU8s(offset, 1)[0]
        member x.readU16(offset:int) = x.readU16s(offset, 1)[0]
        member x.readU32(offset:int) = x.readU32s(offset, 1)[0]
        member x.readU64(offset:int) = x.readU64s(offset, 1)[0]

        member x.readU8s (offset: int, count: int) : byte[] =
            lock locker (fun () ->
                let buffer = Array.zeroCreate<byte> count
                stream.Seek(int64 offset, SeekOrigin.Begin) |> ignore
                stream.Read(buffer, 0, count) |> ignore
                buffer
            );
        member x.readU16s (offset: int, count: int) : uint16[] =
            lock locker (fun () ->
                let buffer = Array.zeroCreate<byte> (count * 2)
                stream.Seek(int64 (offset * 2), SeekOrigin.Begin) |> ignore
                stream.Read(buffer, 0, count * 2) |> ignore
                Array.init count (fun i -> System.BitConverter.ToUInt16(buffer, i * 2))
            )
        member x.readU32s (offset: int, count: int) : uint32[] =
            lock locker (fun () ->
                let buffer = Array.zeroCreate<byte> (count * 4)
                stream.Seek(int64 (offset * 4), SeekOrigin.Begin) |> ignore
                stream.Read(buffer, 0, count * 4) |> ignore
                Array.init count (fun i -> System.BitConverter.ToUInt32(buffer, i * 4))
            )
        member x.readU64s (offset: int, count: int) : uint64[] =
            lock locker (fun () ->
                let buffer = Array.zeroCreate<byte> (count * 8)
                stream.Seek(int64 (offset * 8), SeekOrigin.Begin) |> ignore
                stream.Read(buffer, 0, count * 8) |> ignore
                Array.init count (fun i -> System.BitConverter.ToUInt64(buffer, i * 8))
            )


        member x.writeBit(offset:int, value:bool) =
            // 바이트 위치 및 해당 바이트 내의 비트 위치 계산
            let byteIndex = offset / 8
            let bitIndex = offset % 8

            // 해당 위치의 바이트를 읽기
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
                stream.Seek(int64 (offset * 1), SeekOrigin.Begin) |> ignore
                stream.WriteByte(value)
            )

        member x.writeU16(offset:int, value:uint16) =
            lock locker (fun () ->
                let buffer = System.BitConverter.GetBytes(value)
                stream.Seek(int64 (offset * 2), SeekOrigin.Begin) |> ignore
                stream.Write(buffer, 0, buffer.Length)
            )

        member x.writeU32(offset:int, value:uint32) =
            lock locker (fun () ->
                let buffer = System.BitConverter.GetBytes(value)
                stream.Seek(int64 (offset * 4), SeekOrigin.Begin) |> ignore
                stream.Write(buffer, 0, buffer.Length)
            )

        member x.writeU64(offset:int, value:uint64) =
            lock locker (fun () ->
                let buffer = System.BitConverter.GetBytes(value)
                stream.Seek(int64 (offset * 8), SeekOrigin.Begin) |> ignore
                stream.Write(buffer, 0, buffer.Length)
            )

[<AutoOpen>]
module ZmqBufferManagerExtension =
    type IOFileSpec with
        member x.InitiaizeFile(dir:string) : FileStream =
            let path = Path.Combine(dir, x.Name)
            let mutable fs:FileStream = null
            if (File.Exists path) then
                // ensure that the file has the correct length
                fs <- new FileStream(path, FileMode.Open, FileAccess.ReadWrite)
                if (fs.Length <> x.Length) then
                    failwithf($"File [{path}] length mismatch : {fs.Length} <> {x.Length}")
            else
                logInfo($"Creating new file : {path}")
                // create zero-filled file with length = x.Length
                fs <- new FileStream(path, FileMode.Create, FileAccess.ReadWrite)
                let buffer = Array.zeroCreate<byte> x.Length
                fs.Write(buffer, 0, x.Length)
                fs.Seek(0, SeekOrigin.Begin) |> ignore
                fs.SetLength(x.Length)
                let xxx = fs.Length
                Console.WriteLine($"File length : {fs.Length}")
                fs.Flush()
            fs

    type BufferManager with
        member x.VerifyIndices(offset:int) =
            let offset = int64 offset
            let length = x.FileStream.Length
            if offset < 0 then
                failwithf($"Invalid offset.  non-negative value required : {offset}")
            if offset >= length then
                failwithf($"Invalid offset: {offset}.  Exceed length limit {length})")
        member x.VerifyIndices(offsets:int[]) =
            offsets |> iter (fun offset -> x.VerifyIndices(offset))

