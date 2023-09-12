namespace DsXgComm

open Dual.Common.Core.FS
open XGCommLib
open System
open System.Threading

[<AutoOpen>]
module Connect =

    type DsXgConnection() =
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

        member x.Connect (connStr : string) : bool =
            x.Factory <-
                // DO NOT WORK : let t = Type.GetTypeFromProgID("XGCommLib.CommObjectFactory")
                let t = Type.GetTypeFromCLSID(Guid("7BBF93C0-7C64-4205-A2B0-45D4BD1F51DC")) // CommObjectFactory
                Activator.CreateInstance(t) :?> CommObjectFactory20
            x.CommObject <- x.Factory.GetMLDPCommObject20(connStr)
            let isCn = tryFunction tryCnt x.CommObject.Connect "" "Connecting"
            Thread.Sleep(500)
            isCn

        member x.CheckConnect() : bool=
            let mutable isCn = false
            if x.CommObject.IsConnected() <> 1 then
                isCn <- tryFunction tryCnt x.CommObject.Connect "" "Connecting"
            isCn

        member x.Disconnect () =
            x.CommObject.Disconnect()


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
            di.lSize <-   tag.ByteSize
            di.lOffset <- tag.ByteOffset
            di

        
        //ScanIO.Scan 와 동시사용시 Thread 충돌 가능  ScanIO.Scan 사용에 writeTags() 내부에 동작 Tag WriteValue 변경시 자동으로 써짐
        member x.WriteBit(deviceType:string, bitOffset:int, value:int) =
            if x.CommObject.WriteDevice_Bit (deviceType, bitOffset, value) <> 1 then
                   failwith $"WriteBit deviceType{deviceType}, bitOffset{bitOffset}, value{value}ERROR"

        //ScanIO.Scan 와 동시사용시 Thread 충돌 가능  ScanIO.Scan 사용에 writeTags() 내부에 동작 Tag WriteValue 변경시 자동으로 써짐
        member x.WriteDevices(tags:XgTagInfo array) =
            let writableTags = tags|>Seq.filter(fun t->t.WriteValue <> null)    //쓰기 대상 선별
            let batches = chunkBySumByteSize MAX_RANDOM_BYTE_POINTS writableTags

            batches
            |> Seq.iter(fun batch ->

                x.CommObject.RemoveAll()

                batch
                |> Seq.map(x.CreateWriteDevice)
                |> Seq.iter(x.CommObject.AddDeviceInfo)

                let wBuf = getBuffer(batch, false)
                if x.CommObject.WriteRandomDevice wBuf <> 1 then
                   failwith "WriteRandomDevice ERROR"
            )    

            writableTags.Iter(fun t->t.WriteValue <- null)     //쓰기 대상 선별 후 WriteValue 지우기

        //ScanIO.Scan 와 동시사용시 Thread 충돌 가능  PLCScanTagChangedSubject 사용 하거나 ScanIO.Scan 중지후 사용
        member x.ReadDevices(tags:XgTagInfo seq) =

            let batches = chunkBySumByteSize MAX_RANDOM_BYTE_POINTS tags
            batches
            |> Seq.collect(fun batch ->

                x.CommObject.RemoveAll()

                batch
                |> Seq.map(x.CreateWriteDevice)
                |> Seq.iter(x.CommObject.AddDeviceInfo)

                let rBuf = getBuffer(batch, true)

                if x.CommObject.ReadRandomDevice rBuf <> 1 then
                   failwith "ReadRandomDevice ERROR"

                bufferToTagValue(batch, rBuf)
            )
            



