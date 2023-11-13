namespace IOMap.LS

open XGCommLib
open System
open System.Diagnostics
open IOMapApi.MemoryIOApi
open IOMapApi.MemoryIOManagerImpl
open System.Threading

 
module ScanImpl =
    let sizeMax = 512 //MAX_ARRAY_BYTE_SIZE
    let pointMax = 64 //MAX_RANDOM_BYTE_POINTS
    let longSize = 8 // 8byte

    type ScanIO(ip, cpu) =

        let conn = XGTConnection(ip);
       
        let memorySet = 
            let mInfos = HwModelManager.GetMemoryInfos(cpu) 
            let createResults = 
                mInfos
                |> Seq.map(fun (d, path)->
                    let size =  d.nSizeWord*2
                    path, MemoryIOManager.Create(path, size))
                |> Seq.toArray

            createResults 
            |> Seq.filter snd
            |> Seq.iter(fun (f, b) -> 
                   Debug.WriteLine $"new memory set created : {f}" )
                  

            if createResults |> Seq.exists snd
            then                    // new memory set created 되면 1초 대기
                Thread.Sleep(1000)  // IOMapService  로딩 대기 1초 간격으로 신규파일 자동로딩

            mInfos
            |>Seq.map(fun (d, key) -> d.strDevice, MemoryIO(@$"{key}"))
            |> dict
       
            
        let settingDev (di:DeviceInfo)  offset pointDword = 
            conn.CommObject.RemoveAll()
            for i = 0 to pointDword-1 do
                di.lOffset <- offset + i*longSize
                conn.CommObject.AddDeviceInfo(di)
            

        let performScan operationType offset pointDword maxByte=
            let di = conn.CreateScanDevice(operationType)
            let buf = Array.zeroCreate<byte>(pointDword*8)
            settingDev di offset pointDword

            match operationType with
            | "Q" | "M" -> //'Q', 'M' 영역만 DS 가 출력 전용으로 사용한다.
                
                let writeToPLC = memorySet[operationType].Read(offset, pointDword*8)
                for i = 0 to pointDword-1 do
                    for j = 0 to longSize-1 do
                        buf[i*longSize+j] <- writeToPLC[i*longSize+j]
                conn.WriteRandomDevice(buf) 
                Console.WriteLine($"WriteRandomDevice {operationType}, offset {offset}, maxSizeByte {maxByte}")

            | "I" | "R" -> //나머지는 DS가 읽기만 하기
                conn.ReadRandomDevice(buf) 
                memorySet[operationType].Write(buf, offset)
                Console.WriteLine($"ReadRandomDevice {operationType}, offset {offset}, maxSizeByte {maxByte}")


            | _  ->  ()

        do  

            
            conn.Connect() |> ignore

        member x.DoScan() = 
            memorySet 
            |> Seq.where(fun m -> ["I";"Q";"M";"R"]|>Seq.contains m.Key)
            |> Seq.iter(fun m ->
                let maxByte = m.Value.MemorySize
                let dName = m.Key
                //Console.WriteLine($"strDevice {dName}, offset {0}, maxSizeByte {maxSizeByte}")

                //for i=0 to maxByte/512 do
                for i=0 to 2 do
                    let offsetByte = i*512
                    if maxByte >= offsetByte+512
                    then 
                        performScan dName offsetByte pointMax  maxByte
                    elif maxByte <> offsetByte then  //마지막 크기가 넘어가면 pointDword 차이 있을경우수집
                        performScan dName offsetByte ((maxByte-offsetByte)/64)  maxByte

              
            )
            
             