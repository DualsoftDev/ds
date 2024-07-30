namespace IOClient.DS

open System
open System.Linq
open System.Diagnostics
open System.Threading
open IO.Core
open Dual.Common.Core.FS
open System.Threading.Tasks
open Engine.Cpu
open Engine.Core
open Engine.CodeGenCPU
 
module ScanDSImpl =
    let MAX_ARRAY_BYTE_SIZE = 512 //MAX_ARRAY_BYTE_SIZE
    let pointMax = 64 //MAX_RANDOM_BYTE_POINTS
    let longSize = 8 // 8byte

    type IoEventDS(dsCPU:DsCPU, vendors:VendorSpec seq,  client:Client, _server:Server) =
        let vendorDic = vendors.SelectMany(fun f-> f.Files.Select(fun s->s.GetPath().ToLower(), f.AddressResolver)) |> dict
        let tagSet = dsCPU.TagIndexSet
        let TaskDevTags = dsCPU.Storages.Where(fun f-> TagKindExt.GetTaskDevTagKind(f.Value).IsSome)
                            |> Seq.groupBy(fun f->f.Value.Address)
                            |> Seq.map(fun (k, v) -> k, v.Select(fun s->s.Value))
                            |> dict

        //let boolTags =   tagSet.Where(fun f->f.Value|>fst = typedefof<bool>).Select(fun s->s.Value|>snd, s.Key) |>dict
        //let byteTags =   tagSet.Where(fun f->f.Value|>fst = typedefof<byte>).Select(fun s->s.Value|>snd, s.Key) |>dict
        //let uint16Tags = tagSet.Where(fun f->f.Value|>fst = typedefof<uint16>).Select(fun s->s.Value|>snd, s.Key) |>dict
        //let uint32Tags = tagSet.Where(fun f->f.Value|>fst = typedefof<uint32>).Select(fun s->s.Value|>snd, s.Key) |>dict
        //let uint64Tags = tagSet.Where(fun f->f.Value|>fst = typedefof<uint64>).Select(fun s->s.Value|>snd, s.Key) |>dict
        let vendorDs = vendors.First(fun f->f.Location = "")
        let errCheckDeviceName() = 
            vendorDs.Files.Select(fun f-> f.Name|>textToDataType).ToArray()|>ignore
        
        let onIOTagChanged (change:TagChangedInfo) =
            match change with
            | :? IOTagChangedInfo as change ->
                let n = change.Offsets.Length
                let offsets = change.Offsets
                let values = change.Values
                Console.WriteLine($"Total {n} tag changed on {change.Path} with bitLength={change.ContentBitLength}");
                match change.ContentBitLength with
                | 1 -> ()
                    //let values = values :?> bool[]
                    //offsets.Iter(fun i-> storages[boolTags[i]].BoxedValue <- values[i])
                | 8 -> //성능때문에 bit를  IOHub로 부터 byte로 받음
                    let values = values :?> byte[]
                    offsets |> Seq.iteri(fun i offset-> 
                    
                        for bitIndex in 0 .. 7 do
                            let absoluteBitIndex = offset * 8 + bitIndex
                            let value = (values.[i] &&& (1uy <<< bitIndex))
                            let address = vendorDic[change.Path.ToLower()].GetTagName(change.Path.Split('/').Last(), absoluteBitIndex, 1)
                            if TaskDevTags.ContainsKey address then 
                                TaskDevTags.[address].Iter(fun s -> s.BoxedValue <- value)
                            else 
                                Console.WriteLine($"ds TaskDevTags : {address} not exist");

                        )
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


        let writeDsData  (changes:Type*(IStorage seq))  =
            
            let dataType, tags = changes
            let indexSet = tags.Select(fun f-> tagSet[f.Name]|>snd).ToArray()
            
            match dataType.Name with
            | BOOL    -> client.WriteBits(dataType.Name.ToLower(), indexSet, tags.Select(fun f->Convert.ToBoolean(f.ObjValue)).ToArray()) |>ignore
            | UINT8   -> client.WriteBytes(dataType.Name.ToLower(), indexSet, tags.Select(fun f->Convert.ToByte(f.ObjValue)).ToArray()) |>ignore
            | UINT16  -> client.WriteUInt16s(dataType.Name.ToLower(), indexSet, tags.Select(fun f->Convert.ToUInt16(f.ObjValue)).ToArray()) |>ignore
            | UINT32  -> client.WriteUInt32s(dataType.Name.ToLower(), indexSet, tags.Select(fun f->Convert.ToUInt32(f.ObjValue)).ToArray()) |>ignore
            | UINT64  -> client.WriteUInt64s(dataType.Name.ToLower(), indexSet, tags.Select(fun f->Convert.ToUInt64(f.ObjValue)).ToArray()) |>ignore
            | INT8     
            | INT16   
            | INT32   
            | INT64  
            | FLOAT32   
            | FLOAT64
            | CHAR    
            | STRING //client.WriteString 필요
            | _-> failwithf $"{tags.First().Name} : {dataType.Name} not support err"
        let writeVendorData  (tags:IStorage seq) =
            
            let TaskDevTags = tags.Select(fun f-> dsCPU.Storages[f.Name])
            TaskDevTags.Iter(fun t->  
                if TagKindExt.GetTaskDevTagKind(t).Value <> TaskDevTag.actionOut 
                then failwithf $"writeVendorData error {t} is input tag"
                else
                    let dev =t.Address.Substring(0,1) //예외확인
                    let prov = vendorDic.First(fun f-> f.Key.Split('/').Last() = dev)
                    match prov.Value.GetAddressInfo(t.Address) with
                        | true, _memType, _offset, _contentBitLength -> 
                            ()
                            //let sm = vspec.StreamManager :?>  StreamManager
                            //let clients  = server.Clients
                            //client.w
                            //()
                            //vspec.StreamManager
                            //match contentBitLength with
                            //| 1 ->  sm.writeBit(ClientRequestInfo)
                           
                        | _ -> ()
                
                        )

        let onDSTagChanged (changes:IStorage seq) = 
            let dsMemory     = changes |> Seq.filter(fun f-> TagKindExt.GetTaskDevTagKind(f).IsNone)
            let vendorMemory = changes |> Seq.filter(fun f-> TagKindExt.GetTaskDevTagKind(f).IsSome)
       
            dsMemory
                |> Seq.groupBy(fun f-> f.DataType)
                |> Seq.iter(writeDsData) |>ignore

            vendorMemory
               |> writeVendorData

        let updateIOServer (name:string) = 
            let offsets = [||]
            let dataSet = [||]

            client.WriteUInt64s(name, offsets, dataSet) |> ignore

        do  
            errCheckDeviceName()
            client.TagChangedSubject.Subscribe(onIOTagChanged) |> ignore
            dsCPU.TagChangedForIOHub.Subscribe(onDSTagChanged) |> ignore
            dsCPU.RunInBackground()
            dsCPU.MySystem.Flows.Iter(fun f ->
                    let reals = f.Graph.Vertices.OfType<Real>()
                    if reals.Any() then
                        (reals.First().TagManager :?> VertexTagManager).SF.Value <- true;
            )
         
      
 
       