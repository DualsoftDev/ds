namespace IOMapApi

open System
open System.Data
open System.Threading
open System.IO.MemoryMappedFiles
open System.IO

type MMF = MemoryMappedFile

module MemoryIOApi =

    let READ  = MemoryMappedFileRights.Read
    let WRITE = MemoryMappedFileRights.Write
    let READWRITE = MemoryMappedFileRights.ReadWrite

                   
    type MemoryIO(device:string) as this =
        let path = Path.Combine(MemoryUtilImpl.BasePath, $@"{device}")
        let filesize = FileInfo(path).Length |> int
        
        let bitSize = 8

        let semaphore = match Semaphore.TryOpenExisting(MemoryUtilImpl.getSemaphoreKey device)
                        with true, s -> s 
                             | _ -> new Semaphore(1, 1, MemoryUtilImpl.getSemaphoreKey device)


        let execWrite(action: Action) =
            if not (semaphore.WaitOne(MemoryUtilImpl.SemaphoreReleaseTime)) then semaphore.Release() |> ignore

            let task = System.Threading.Tasks.Task.Run(fun () ->
                try
                    action.Invoke()
                finally
                    semaphore.Release() |> ignore
            )
            task.Wait(MemoryUtilImpl.WriteTimeout) |>ignore
            

        let bitOutOfRangeExceptions position = 
            if position < 0 || position > 7 then
                raise (new System.ArgumentOutOfRangeException("position", "bit index must be in the range [0, 7]."))
          
        let openMMFWithExceptions  =
            try
                MMF.OpenExisting(MemoryUtilImpl.getMMFName device, READWRITE)
            with 
            | :? System.IO.FileNotFoundException  -> failwithf $"Check device {device} IOMapService"
            | :? System.UnauthorizedAccessException -> failwithf "Check UnauthorizedAccess"
            | _ -> failwithf "Check IOMapService"

        let readwriteMMF = openMMFWithExceptions 
        let acc = readwriteMMF.CreateViewAccessor(0, int64 filesize, MemoryMappedFileAccess.ReadWrite)
        do
            if not (File.Exists path) then failwithf "Memory file missing: %s" path

        member _.Device = device
        member _.MemorySize = filesize
        member _.Dispose() = readwriteMMF.Dispose();acc.Dispose()

            
        member _.Read(offset:int, length) =
            let buffer = Array.zeroCreate<byte> length
            acc.ReadArray(offset, buffer, 0, buffer.Length) |> ignore
            buffer

        member _.ReadBit(offset, positionBit) =
            bitOutOfRangeExceptions positionBit
            let byteValue = acc.ReadByte(offset|>int64)
            (byteValue &&& (1uy <<< positionBit)) <> 0uy

        member _.Write(data:byte array, offsetByte:int) =
            execWrite(Action(fun () -> acc.WriteArray(offsetByte, data, 0, data.Length)))

   
        member _.WriteBit(value: bool, offsetByte: int, positionBit: int) =
            execWrite(Action(fun () ->
                bitOutOfRangeExceptions positionBit
                let setBitInByte (byte: byte) (bitPosition: int) (value: bool) =
                    if value then byte ||| (1uy <<< bitPosition)
                    else byte &&& (0xFFuy - (1uy <<< bitPosition))

                let byteValue = acc.ReadByte(int64 offsetByte)
                let updatedByteValue = setBitInByte byteValue (positionBit % 8) value
                acc.Write(int64 offsetByte, updatedByteValue)))

        member _.GetMemoryData () = this.Read(0, int filesize)
        member _.ClearMemoryData () =
            let buffer = Array.zeroCreate<byte> filesize
            this.Write(buffer, 0)
        member _.GetMemoryAsDataTable() : DataTable =
            let dt = new DataTable(device)
            dt.Columns.AddRange([|for c in 0..9 -> new DataColumn($"{c}", typeof<byte>)|])
            for v in this.GetMemoryData () |> Seq.chunkBySize 10 do
                dt.Rows.Add(Seq.map box v |> Seq.toArray) |> ignore
            dt
