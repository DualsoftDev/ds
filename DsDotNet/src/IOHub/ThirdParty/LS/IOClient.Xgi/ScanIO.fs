namespace IOClient.Xgi

open XGCommLib
open System
open System.Linq
open System.Diagnostics
open System.Threading
open XGTComm
open IO.Core
open Dual.Common.Core.FS
open System.Threading.Tasks
 
module ScanImpl =
    let MAX_ARRAY_BYTE_SIZE = 512 //MAX_ARRAY_BYTE_SIZE
    let pointMax = 64 //MAX_RANDOM_BYTE_POINTS
    let longSize = 8 // 8byte

    type ScanIO(ipPort, vendor:VendorSpec, client:Client) =
        let cancellationTokenSource = new CancellationTokenSource()
        let conn = XGTConnection(ipPort, true);
        let errCheckDeviceName() = 
            let nameErrs = vendor.Files.Filter(fun f->f.Name.length()>1)
            if nameErrs.any() 
            then 
                let errtext =nameErrs.Select(fun f-> f.Name).JoinWith(", ");
                failwithf $"error device name {errtext}"


        let dicMemory =
            vendor.Files
            |> Seq.map(fun file -> file.Name,  [0..file.Length-1].Select(fun i-> i, XGTDeviceLWord(file.Name.ToUpper()[0], i*64))|>dict)
            |> dict

        let onHwByteTagChanged (change:char*int*byte) =
            let dName, offset, value = change
            Console.WriteLine   change    
            client.WriteBytes(dName.ToString(), [|offset|], [|value|]) |> ignore


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

        
        let doReadDevs (dName:string) (offsetByte:int) length  = 
            if length > pointMax then failwith $"err {length}"
            let devs = 
                [|  
                    for i = offsetByte to (offsetByte + length-1) do
                        yield dicMemory[dName][i]
                |]
            
            conn.ReadRandomDevice(devs) 

        let updateIOServer (name:string) (devLWords:XGTDeviceLWord seq)  = 
            let offsets = devLWords.Select(fun f->f.Offset).ToArray()
            let dataSet = devLWords.Select(fun f->f.Value).ToArray()

            client.WriteUInt64s(name, offsets, dataSet) |> ignore

        let loopScanHW()  = 
            while not cancellationTokenSource.IsCancellationRequested do
                vendor.Files
                |> Seq.iter(fun file ->
                    match file.Name.ToUpper() with
                    | "Q" | "R" -> ()//'Q', 'R' 영역은  onTagChanged 활용
                    | "I" | "M" -> //나머지는 DS가 읽기만 하기
                        let readLength = file.Length
                        let maxIterations = readLength / MAX_ARRAY_BYTE_SIZE

                        for i = 0 to maxIterations do
                            let offsetByte = i * 512
                            let lengthToRead = 
                                if readLength - 512 >= offsetByte  then 64
                                else (readLength - offsetByte) % 64
                        
                            if lengthToRead > 0  then
                                doReadDevs (file.Name) offsetByte lengthToRead  |>ignore
                                //|> updateIOServer (file.GetPath())
                    | _  ->  ()
                )
        do  
            errCheckDeviceName()
            client.TagChangedSubject.Subscribe(onTagChanged) |> ignore
            XGTConnection.ByteChangeSubject.Subscribe(onHwByteTagChanged) |> ignore
                    
      
        member x.DoScan() = 
          
            Task.Factory.StartNew(loopScanHW, TaskCreationOptions.LongRunning) |> ignore

        
       