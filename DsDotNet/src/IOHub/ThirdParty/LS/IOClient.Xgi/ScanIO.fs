namespace IOClient.Xgi

open XGCommLib
open System
open System.Linq
open System.Diagnostics
open System.Threading
open XGTComm
open IO.Core
open Dual.Common.Core.FS

 
module ScanImpl =
    let sizeMax = 512 //MAX_ARRAY_BYTE_SIZE
    let pointMax = 64 //MAX_RANDOM_BYTE_POINTS
    let longSize = 8 // 8byte

    type ScanIO(ipPort, vendor:VendorSpec, client:Client) =
        let conn = XGTConnection(ipPort, true);
        let errCheckDeviceName() = 
            let nameErrs = vendor.Files.Filter(fun f->f.Name.length()>1)
            if nameErrs.any() 
            then 
                let errtext =nameErrs.Select(fun f-> f.Name).JoinWith(", ");
                failwithf $"error device name {errtext}"

        let onTagChanged (change:TagChangedInfo) =
            match change with
            | :? IOTagChangedInfo as change ->
                let n = change.Offsets.Length
                let offsets = change.Offsets
                let values = change.Values
                Console.WriteLine($"Total {n} tag changed on {change.Path} with bitLength={change.ContentBitLength}");
                match change.ContentBitLength with
                | 1 ->
                    let values = values :?> bool[]
                    for i = 0 to n - 1 do
                        Console.WriteLine($"  {offsets[i]}: {values[i]}");
                | 8 ->
                    let values = values :?> byte[]
                    for i = 0 to n - 1 do
                        Console.WriteLine($"  {offsets[i]}: {values[i]}");
                | 16 ->
                    let values = values :?> uint16[]
                    for i = 0 to n - 1 do
                        Console.WriteLine($"  {offsets[i]}: {values[i]}");
                | 32 ->
                    let values = values :?> uint32[]
                    for i = 0 to n - 1 do
                        Console.WriteLine($"  {offsets[i]}: {values[i]}");
                | 64 ->
                    let values = values :?> uint64[]
                    for i = 0 to n - 1 do
                        Console.WriteLine($"  {offsets[i]}: {values[i]}");
                | _ ->
                    failwithlog "Not supported"
            | :? StringTagChangedInfo as change ->
                let n = change.Keys.Length
                Console.WriteLine($"Total {n} string tag changed on {change.Path}");
                for i = 0 to n - 1 do
                    Console.WriteLine($"  {change.Keys[i]}: {change.Values[i]}")
            | _ ->
                failwithlog "ERROR"

        do  
            client.TagChangedSubject.Subscribe(onTagChanged) |> ignore
            errCheckDeviceName()



            //let doRead(dName offsetByte pointMax maxByte) = 
                

            //vendor.Files
            //|> Seq.iter(fun file ->
            //    match file.Name with
            //    | "Q" | "R" -> //'Q', 'R' 영역만 DS 가 출력 전용으로 사용한다.
            //        ()
            //        //let writeToPLC = memorySet[operationType].Read(offset, pointDword*8)
            //        //for i = 0 to pointDword-1 do
            //        //    for j = 0 to longSize-1 do
            //        //        buf[i*longSize+j] <- writeToPLC[i*longSize+j]
            //        //conn.WriteRandomDevice(buf) 
            //        //Console.WriteLine($"WriteRandomDevice {operationType}, offset {offset}, maxSizeByte {maxByte}")

            //    | "I" | "M" -> //나머지는 DS가 읽기만 하기
            //        let readLength = file.Length
            //        for i=0 to readLength/sizeMax do
            //            let offsetByte = i*512
            //            if readLength >= offsetByte+512
            //            then 
            //                performScan dName offsetByte pointMax  maxByte
            //            elif readLength <> offsetByte then  //마지막 크기가 넘어가면 pointDword 차이 있을경우수집
            //                performScan dName offsetByte ((maxByte-offsetByte)/64)  maxByte


            //        let items = 
            //            [
            //                for i in [0..(file.Length/8).chunkBySize()] do  
            //                    yield XGTDeviceLWord(file.Name[0], i)
            //            ]

            //        conn.ReadRandomDevice(items)
            //        //memorySet[operationType].Write(buf, offset)
            //        //Console.WriteLine($"ReadRandomDevice {operationType}, offset {offset}, maxSizeByte {maxByte}")

              //  | _  ->  ()
           // )


        //let memorySet = 
        //    let mInfos = HwModelManager.GetMemoryInfos(cpu) 
        //    let createResults = 
        //        mInfos
        //        |> Seq.map(fun (d, path)->
        //            let size =  d.nSizeWord*2
        //            path, MemoryIOManager.Create(path, size))
        //        |> Seq.toArray

        //    createResults 
        //    |> Seq.filter snd
        //    |> Seq.iter(fun (f, b) -> 
        //           Debug.WriteLine $"new memory set created : {f}" )
                  

        //    if createResults |> Seq.exists snd
        //    then                    // new memory set created 되면 1초 대기
        //        Thread.Sleep(1000)  // IOMapService  로딩 대기 1초 간격으로 신규파일 자동로딩

        //    mInfos
        //    |>Seq.map(fun (d, key) -> d.strDevice, MemoryIO(@$"{key}"))
        //    |> dict
       
            
        //let settingDev (di:DeviceInfo)  offset pointDword = 
        //    conn.CommObject.RemoveAll()
        //    for i = 0 to pointDword-1 do
        //        di.lOffset <- offset + i*longSize
        //        conn.CommObject.AddDeviceInfo(di)
            

        //let performScan operationType offset pointDword maxByte=
        //    let di = conn.CreateScanDevice(operationType)
        //    let buf = Array.zeroCreate<byte>(pointDword*8)
        //    settingDev di offset pointDword

        //    match operationType with
        //    | "Q" | "M" -> //'Q', 'M' 영역만 DS 가 출력 전용으로 사용한다.
                
        //        let writeToPLC = memorySet[operationType].Read(offset, pointDword*8)
        //        for i = 0 to pointDword-1 do
        //            for j = 0 to longSize-1 do
        //                buf[i*longSize+j] <- writeToPLC[i*longSize+j]
        //        conn.WriteRandomDevice(buf) 
        //        Console.WriteLine($"WriteRandomDevice {operationType}, offset {offset}, maxSizeByte {maxByte}")

        //    | "I" | "R" -> //나머지는 DS가 읽기만 하기
        //        conn.ReadRandomDevice(buf) 
        //        memorySet[operationType].Write(buf, offset)
        //        Console.WriteLine($"ReadRandomDevice {operationType}, offset {offset}, maxSizeByte {maxByte}")


        //    | _  ->  ()

      
        //member x.DoScan() = 
        //    memorySet 
        //    |> Seq.where(fun m -> ["I";"Q";"M";"R"]|>Seq.contains m.Key)
        //    |> Seq.iter(fun m ->
        //        let maxByte = m.Value.MemorySize
        //        let dName = m.Key
        //        //Console.WriteLine($"strDevice {dName}, offset {0}, maxSizeByte {maxSizeByte}")

        //        //for i=0 to maxByte/512 do
        //        for i=0 to 2 do
        //            let offsetByte = i*512
        //            if maxByte >= offsetByte+512
        //            then 
        //                performScan dName offsetByte pointMax  maxByte
        //            elif maxByte <> offsetByte then  //마지막 크기가 넘어가면 pointDword 차이 있을경우수집
        //                performScan dName offsetByte ((maxByte-offsetByte)/64)  maxByte

              
        //    )
            
             