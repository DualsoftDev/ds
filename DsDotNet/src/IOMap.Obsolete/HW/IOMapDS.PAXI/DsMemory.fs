open System.IO
open System.IO.MemoryMappedFiles
open System.Threading
open System
open IOMapDSApi.Global
open System.Data

type MMF = MemoryMappedFile

type MemoryIO(device: string) =

    let filePath = Path.Combine(BasePath, device) 
    let memorySize = (new FileInfo(filePath)).Length
    let semaphore = 
        try  Semaphore.OpenExisting(getSemaphoreKey device)
        with | _ -> new Semaphore(1, 1, getSemaphoreKey device)

    let checkPreconditions() =
        if WriteTimeout >= SemaphoreReleaseTime 
            then failwithf "Semaphore release time must be greater than write timeout"
        if not (File.Exists filePath) 
            then failwithf $"Memory file is not exist : path {filePath}"
        MMF.OpenExisting(getMMFName device) |> ignore

    do checkPreconditions()

    member _.Write(data: byte array, offset: int64) =
        use mmf = MMF.OpenExisting(getMMFName device, MemoryMappedFileRights.Write)
        if not (semaphore.WaitOne(SemaphoreReleaseTime)) 
            then //semaphoreReleaseTime 까지 기다린 후 데드락 방지를 위한 강제 릴리즈 
                semaphore.Release() |> ignore

        let writeOperation = 
            System.Threading.Tasks.Task.Factory.StartNew(Action(fun () ->
                use stream = mmf.CreateViewStream(offset, int64 data.Length)
                stream.Write(data, 0, data.Length)))
        try
            if not (writeOperation.Wait(WriteTimeout)) then 
                failwith "Write operation timed out!"
        finally
            semaphore.Release() |> ignore

    member _.Read(offset: int64, length: int) =
        use mmf = MMF.OpenExisting(getMMFName device, MemoryMappedFileRights.Read)
        use accessor = mmf.CreateViewAccessor(offset, int64 length, MemoryMappedFileAccess.Read)
        let buffer = Array.zeroCreate<byte> length
        accessor.ReadArray<byte>(0L, buffer, 0, buffer.Length) |> ignore
        buffer

    static member GetMemorySize device = 
        (new FileInfo(Path.Combine(BasePath, device))).Length

    static member GetMemoryChunkBySize10 device = 
        MemoryIO(device).Read(0L, int (MemoryIO.GetMemorySize device)) |> Seq.chunkBySize 10

    static member GetMemoryData device = 
        MemoryIO(device).Read(0L, int (MemoryIO.GetMemorySize device)) 

    static member GetMemoryAsDataTable(device: string) : DataTable =
        let dt = new DataTable(device)
        for c in [0..9] do dt.Columns.Add($"{c}", typeof<byte>) |> ignore
        for v in MemoryIO.GetMemoryChunkBySize10 device do
            dt.Rows.Add(v |> Seq.map(box) |> Seq.toArray) |> ignore
        dt