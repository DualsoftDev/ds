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
            |> Seq.map(fun file -> file.Name,  [0..file.Length/8-1].Select(fun i-> i, XGTDeviceLWord(file.Name.ToUpper()[0], i*64))|>dict)
            |> dict

        let onHwByteTagChanged (changes:(char*int*byte) seq) =
            let dName = changes.Select(fun (c,i,b) -> c).Head()
            let offsets = changes.Select(fun (c,i,b) -> i).ToArray()
            let values = changes.Select(fun (c,i,b) -> b).ToArray()

            Console.WriteLine  $"""dev: {dName},offsets: {offsets.Select(fun s->s.ToString()).JoinWith(", ")} values: {values.Select(fun s->s.ToString()).JoinWith(", ")}"""
                                            
            client.WriteBytes($"{vendor.Location}/{dName}", offsets, values) |> ignore



        
        let doReadDevs (dName:string) (offsetByte:int) length  = 
            if length > pointMax then failwith $"err {length}"
            let offsetLword = offsetByte/8;
            let devs = 
                [|  
                    
                    for i = offsetLword to offsetLword+length-1 do
                        yield dicMemory[dName][i]
                |]
            
            conn.ReadRandomDevice(devs) 



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
                    | _  ->  ()
                )
        do  
            errCheckDeviceName()
            XGTConnection.ByteChangeSubject.Subscribe(onHwByteTagChanged) |> ignore
      
        member x.DoScan() = 
          
            Task.Factory.StartNew(loopScanHW, TaskCreationOptions.LongRunning) |> ignore

        
       