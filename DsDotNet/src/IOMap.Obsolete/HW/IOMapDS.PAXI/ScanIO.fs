namespace IOMap.LS

open System
open System.Diagnostics
open IOMapApi.MemoryIOApi
open IOMapApi.MemoryIOManagerImpl

 
module ScanImpl =
    let sizeMax = 512 //MAX_ARRAY_BYTE_SIZE
    let pointMax = 64 //MAX_RANDOM_BYTE_POINTS

    type ScanIO(ip, cpu:string) =

        let conn = MNC2Connection(ip);
       
        let memorySet = 
            let createResults = 
                [@$"{cpu}\I";@$"{cpu}\O"]
                |> List.map(fun path ->
                    path, MemoryIOManager.Create(path, 64))
                |> List.toArray

            createResults 
            |> Seq.filter snd
            |> Seq.iter(fun (f, b) -> 
                   Debug.WriteLine $"new memory set created : {f}"
                   )
         

            createResults
            |>Seq.map(fun (d, key) -> d, MemoryIO(@$"{d}"))
            |> dict
        

        let performScan operationType offset pointByte=
            let buf = Array.zeroCreate<byte>(pointByte)

            match operationType with
            | "O" -> //'Q', 'M' 영역만 DS 가 출력 전용으로 사용한다.
                
                let writeToPLC = memorySet[operationType].Read(offset, pointByte*8)
                for i = 0 to pointByte-1 do
                        buf[i] <- writeToPLC[i]

                conn.WriteRandomDevice(buf) 
            | "I"  ->   //나머지는 DS가 읽기만 하기
                
                conn.ReadRandomDevice(buf) 
                memorySet[operationType].Write(buf, offset)
            | _ -> ()
        do  
            conn.Connect() |> ignore

        member x.DoScan() = 
            memorySet.Values
            |> Seq.iter(fun device ->
                let maxSizeByte = device.MemorySize |> int

                for i=0 to (maxSizeByte-1) do
                    performScan device.Device i pointMax  
                    Console.WriteLine($"strDevice {device.Device}, offset {i}, maxSizeByte {maxSizeByte}")
              
            )
            
             