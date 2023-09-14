namespace DsXgComm

open Dual.Common.Core.FS
open XGCommLib
open System
open System.Threading

[<AutoOpen>]
module Connect =

    type ScanReadWrite =
        | ReadOnly
        | WriteOnly
        | WriteRead
        with
        member x.IsWrite = 
            match x with
            |WriteRead|WriteOnly ->  true
            |ReadOnly -> false
        member x.IsRead = 
            match x with
            |WriteRead|ReadOnly ->  true
            |WriteOnly -> false

    type XGTConnection(connStr) =
        let mutable cts = new CancellationTokenSource()
        let mutable run:bool = false

        let (|?) = defaultArg
        let tryCnt = 3
        let rec tryFunction budget (f: 'T -> int) (arg: 'T) (name:string) : bool =
            if budget = 0 then
                failwithlog $"All attempts failed: {name}"
                false
            else
                let result = f arg
                if result <> 1 then
                    logWarn $"Retrying {name} with remaining {budget}"
                    Thread.Sleep 1000
                    tryFunction (budget - 1) f arg name
                else
                    true

        member val CommObject:CommObject20 = null with get, set
        member val Factory:CommObjectFactory20 = null with get, set
        member val Delayms:int = 10 with get, set

        member x.Connect () : bool =
            x.Factory <-
                // DO NOT WORK : let t = Type.GetTypeFromProgID("XGCommLib.CommObjectFactory")
                let t = Type.GetTypeFromCLSID(Guid("7BBF93C0-7C64-4205-A2B0-45D4BD1F51DC")) // CommObjectFactory
                Activator.CreateInstance(t) :?> CommObjectFactory20
            x.CommObject <- x.Factory.GetMLDPCommObject20(connStr)
            let isCn = tryFunction tryCnt x.CommObject.Connect "" "Connecting"
            Thread.Sleep(200)
            isCn

        member x.CheckConnect() : bool=
            let mutable isCn = false
            if x.CommObject.IsConnected() <> 1 then
                isCn <- tryFunction tryCnt x.CommObject.Connect "" "Connecting"
            isCn

   
        member x.Disconnect() =
            x.CommObject.Disconnect()

        member x.ReConnect() =
            x.Disconnect() |> ignore
            x.Connect()

        member x.CreateLWordDevice(deviceType:DeviceType, offset:int) : DeviceInfo =
            let di = x.Factory.CreateDevice()
            
            di.ucDeviceType <- Convert.ToByte(dTypeChar deviceType)
            di.ucDataType <- Convert.ToByte('B')
            di.lSize <- 8
            di.lOffset <- 8 * offset
            di

        member x.CreateWriteDevice(tag:XgTagInfo) : DeviceInfo =
            let di = x.Factory.CreateDevice()
            
            di.ucDeviceType <- Convert.ToByte(dTypeChar tag.Device)
            di.ucDataType <-  tag.RandomReadWriteDataType
            di.lSize <-   if tag.DataType = DataType.Bit then tag.BitOffset%8 else tag.ByteSize  
            di.lOffset <- tag.ByteOffset
            di

        
        //ScanIO.Scan 와 동시사용시 Thread 충돌 가능  ScanIO.Scan 사용에 writeTags() 내부에 동작 Tag WriteValue 변경시 자동으로 써짐
        member x.WriteBit(deviceType:string, bitOffset:int, value:int) =
            if x.CommObject.WriteDevice_Bit (deviceType, bitOffset, value) <> 1 then
                   failwith $"WriteBit deviceType{deviceType}, bitOffset{bitOffset}, value{value}ERROR"

        //ScanIO.Scan 와 동시사용시 Thread 충돌 가능  ScanIO.Scan 사용에 writeTags() 내부에 동작 Tag WriteValue 변경시 자동으로 써짐
        member x.WriteDevices(tags:XgTagInfo seq) =
            cts <- new CancellationTokenSource() 
            let writeAsync = async {x.WriteReadDevices (tags, WriteOnly)}
            try
            Async.RunSynchronously (writeAsync, cancellationToken = cts.Token)
            with :? OperationCanceledException ->
            printfn "WriteDevices Canceled"
        
        member x.ReadDevices(tags:XgTagInfo seq) =
            cts <- new CancellationTokenSource() 
            let readAsync = async {x.WriteReadDevices (tags, ReadOnly)}
            try
            Async.RunSynchronously (readAsync, cancellationToken = cts.Token)
            with :? OperationCanceledException ->
            printfn "ReadDevices Canceled"


        member x.WriteReadDevices(tags:XgTagInfo seq, readWrite:ScanReadWrite) =
            let addComObj(batch) =
                    x.CommObject.RemoveAll()
                    batch 
                    |> Seq.map(x.CreateWriteDevice) 
                    |> Seq.iter(x.CommObject.AddDeviceInfo)
                                    
            let getBatched(batchTags) = 
                    chunkBySumByteSize MAX_RANDOM_BYTE_POINTS batchTags
                            
            let getReadRandom()  = 
                    let batches = getBatched(tags) 
                    for batch in batches do
                        let rBuf = getBufferRead batch
                        addComObj batch
                        if x.CommObject.ReadRandomDevice rBuf <> 1 then
                            tracefn "ReadRandomDevice ERROR"
                        else
                            bufferToTagValue(batch, rBuf)
            let getWriteRandom()  = 
                    let writableTags = tags |> Seq.filter(fun t->t.WriteValue <> null) |> Seq.toArray    //쓰기 대상 선별
                    let batches = getBatched(writableTags) 
                    for batch in batches do
                        let rBuf = getBufferWrite batch
                        addComObj batch
                     
                        if x.CommObject.WriteRandomDevice rBuf <> 1 then
                            tracefn "WriteRandomDevice ERROR"
                        else
                                batch.Iter(fun t->t.WriteValue <- null)     //쓰기 대상 선별 후 WriteValue 지우기

            match readWrite with
            |ReadOnly ->  getReadRandom()
            |WriteOnly -> getWriteRandom()
            |WriteRead -> getWriteRandom()
                          getReadRandom()

        member x.ScanRun(tags:XgTagInfo seq, delayms:int) =
            x.ReConnect() |> ignore
             
            if not <| run then 
                run <- true
                let scanAsync = async {
                        while run do   
                            x.WriteReadDevices(tags, WriteRead)
                            do! Async.Sleep delayms
                }
                Async.StartImmediate(scanAsync, cts.Token)// |> ignore
 
                
        member x.ScanStop() =
            cts.Cancel()
            cts <- new CancellationTokenSource() 
            run <- false;
            x.Disconnect()



            



    