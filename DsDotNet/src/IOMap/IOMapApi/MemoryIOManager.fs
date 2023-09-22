namespace IOMapApi

open System.IO
open System.IO.MemoryMappedFiles

module MemoryIOManagerImpl =

    type MemoryIOManager() =
        static member Delete(device: string) = 
            let path = Path.Combine(MemoryUtilImpl.BasePath, device)
            if File.Exists(path) then 
                File.Delete(path) |> ignore
                true 
            else 
                false

        static member Create(device: string, sizeByte: int) =
            let path = Path.Combine(MemoryUtilImpl.BasePath, device)
            if not (File.Exists(path)) then
                Directory.CreateDirectory(Path.GetDirectoryName(path)) |> ignore
                use fs = File.Create(path)
                fs.SetLength(sizeByte)
                true
            else 
                false

        static member ClearData(device: string) =
            let path = Path.Combine(MemoryUtilImpl.BasePath, device)
            let data = Array.zeroCreate<byte> (int (FileInfo(path).Length))
            use mmf = MMF.CreateFromFile(path, FileMode.Open)
            use stream = mmf.CreateViewStream()
            stream.Write(data, 0, data.Length) 


        static member Load(device: string) =
            let filePath = Path.Combine(MemoryUtilImpl.BasePath, $@"{device}")
            MMF.CreateFromFile(filePath, FileMode.Open, MemoryUtilImpl.getMMFName device) 
            
        //static member Unload(device: string) =
        //    let map = MMF.OpenExisting(MemoryUtilImpl.getMMFName device, MemoryMappedFileRights.ReadWrite)
        //    map.Dispose()
            
