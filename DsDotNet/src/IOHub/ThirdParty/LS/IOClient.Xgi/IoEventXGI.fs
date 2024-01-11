namespace IOClient.Xgi

open System
open System.Linq
open System.Diagnostics
open System.Threading
open IO.Core
open Dual.Common.Core.FS
open System.Threading.Tasks
open XGTComm
 
module IoEventXGIImpl =
    let MAX_ARRAY_BYTE_SIZE = 512 //MAX_ARRAY_BYTE_SIZE
    let pointMax = 64 //MAX_RANDOM_BYTE_POINTS
    let longSize = 8 // 8byte

    type IoEventXGI(ipPort, vendors:VendorSpec seq, client:Client) =
        let connWrite = XGTConnection(ipPort, true);
        let vendorDic = vendors.SelectMany(fun f-> f.Files.Select(fun s->s.GetPath().ToLower(), f.AddressResolver)) |> dict
        let vendorXgi = vendors.First(fun f->f.Location = "xgi")
        
        let doWriteBitDevs (dName:string) (offsets:int[])  (values:bool[])   = 
            if offsets.length() > pointMax then failwithf $"err {offsets.length()}"
            let devs =
                offsets
                |> Seq.mapi(fun i offset -> 
                            let dev = XGTDeviceBit(dName.ToUpper()[0], offset)
                            dev.Value <- values[i]
                            dev )

            connWrite.WriteRandomDevice(devs.OfType<XGTDevice>().ToArray()) 


        let onIOTagChanged (change:TagChangedInfo) =
            match change with
            | :? IOTagChangedInfo as change ->
                let n = change.Offsets.Length
                let offsets = change.Offsets
                let values = change.Values
                Console.WriteLine($"Total {n} tag changed on {change.Path} with bitLength={change.ContentBitLength}");
                match change.ContentBitLength with
                | 1 ->
                    let values = values :?> bool[]
                    let oos = offsets.ChunkBySize(pointMax).ToArray()
                    let vvs = values.ChunkBySize(pointMax).ToArray()
                    vvs |> Seq.iteri(fun i vs->
                        doWriteBitDevs (change.Path.Split('/').Last()) oos[i] vs |>ignore
                    )    
                    
                | 8 -> ()//성능때문에 bit를  IOHub로 부터 byte로 받음 ()
                   
                | 16 ->()
                    //let values = values :?> uint16[]
                    //offsets.Iter(fun i-> storages[uint16Tags[i]].BoxedValue <- values[i])
                | 32 ->()
                    //let values = values :?> uint32[]
                    //offsets.Iter(fun i-> storages[uint32Tags[i]].BoxedValue <- values[i])
                | 64 ->()
                    //let values = values :?> uint64[]
                    //offsets.Iter(fun i-> storages[uint64Tags[i]].BoxedValue <- values[i])
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
            client.TagChangedSubject.Subscribe(onIOTagChanged) |> ignore
      
 
       