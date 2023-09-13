namespace DsXgComm.Monitoring

open System.Reactive.Disposables
open System.Collections.Generic
open XGCommLib
open System.Threading
open DsXgComm.Connect
open AddressConvert
open System
open System.Reactive.Subjects
open System.Diagnostics
open Dual.Common.Core.FS
open System.Runtime.CompilerServices
open DsXgComm

[<AutoOpen>]
module MonitorUtil =
    let noop() = ()
    let getDiffBitPositions(n1:UInt64, n2:UInt64) =
        let xor = n1 ^^^ n2
        [
            for i in [0..63] do
                if (xor &&& (1UL<<<i)) <> 0UL then
                    yield i
        ]

    let creatTags (tags:string seq)= 
        let tags = tags |> Seq.distinct |> Array.ofSeq
        let tis = [|
            for t in tags do
                let ti = tryParseTag t
                let xti =
                    match ti with
                    | Some ti -> XgTagInfo(ti)
                    | _ -> failwithlog $"Unknown tag {t}"
                yield xti
        |]
        let tagInfos = tis |> Array.sortBy(fun t -> t.BitOffset)
        tagInfos


    [<Extension>]
    type ArrayExtension =
        [<Extension>] static member GetLWord(bs:byte[], lwOffset) = BitConverter.ToUInt64(bs, lwOffset*8)

type ChangedTagInfo(plcIp, tag, value) =
    member x.PLCIp:string = plcIp
    member x.Tag:string = tag
    member x.Value:obj = value

type internal LWBatch(lwTagNames:string seq, buffer:byte[], deviceInfos:DeviceInfo seq, tags:XgTagInfo seq) =
    member x.Tags = tags |> Array.ofSeq
    member x.LWTagNames = lwTagNames |> Array.ofSeq
    member x.DeviceInfos = deviceInfos |> Array.ofSeq
    member x.Buffer = buffer



type PLCMonitorEngine() =
    // ** static member 로 PLCTagChangedSubject 를 선언시, subscribe 된 event 가 실행되지 않았음...
    let mutable cts = new CancellationTokenSource()
    let plcTags = new HashSet<XgTagInfo>()
    member val PLCTags = plcTags
    member val IsPLCTagsCreated:bool = false  with get, set
    member x.Stop() = 
            cts.Cancel()
            cts <- new CancellationTokenSource() 

    member val PLCTagChangedSubject = new Subject<ChangedTagInfo>()

    member x.Test() =

        let sameAddresses = [ "%IX127.15.63"; "%IL2047.63"; "%ID4095.31"; "%IW8191.15"; "%IB16383.7" ]
        let tagInfos = sameAddresses |> List.map (tryParseTag >> Option.get)
        //let tags = [ 1; 3; 9; 31; 33; 65; 255; 1025; ] @ [ for i in [1..66] -> i*2*64+1 ]
        //           |> List.map (sprintf "%%MX%d")
        let tags = [ 
                     //"%IX127.15.63";
                     //"%IW8191.15";
                     //"%QX127.15.63";
                     //"%QW8191.15";
                     "%MX131071";
                     "%MW8191.15";
                     "%WX131071";
                     "%WW8191.15";
                     "%RX131071";
                     "%RW8191.15";
                ]
        x.Monitor("192.168.0.100:2004", tags, true, 50, false)

     member x.TestScan() =

        let sameAddresses = [ "%IX127.15.63"; "%IL2047.63"; "%ID4095.31"; "%IW8191.15"; "%IB16383.7" ]
        let tagInfos = sameAddresses |> List.map (tryParseTag >> Option.get)
        //let tags = [ 1; 3; 9; 31; 33; 65; 255; 1025; ] @ [ for i in [1..66] -> i*2*64+1 ]
        //           |> List.map (sprintf "%%MX%d")
        let tags = [ 
                        "%IX131071"  
                        "%QX131071"  
                        "%MX4194303" 
                        "%LX180159"  
                        "%RX524287"  
                        "%WX8388607" 
                        "%FX65535"   
                ]
        x.Monitor("192.168.0.100:2004", tags, true, 5, true)
    (*
     * tags: ["%MX1"; "%MX3"; "%MX9"; "%MX31"; "%MX64"; "%MX129"; ]
     * tagInfos : tag -> LWTag, StartBitOffset
        "%MX1" -> "%ML0", 1
        "%MX3" -> "%ML0", 3
        "%MX9" -> "%ML0", 9
        "%MX31" -> "%ML0", 31
        "%MX64" -> "%ML1", 0
        "%MX129" -> "%ML2", 1
     * chunkInfos : LWord 기준으로 여기에 포함된 tag 목록 정보
        [0]
            "%ML0" -> ["%MX1"; "%MX3"; "%MX9"; "%MX31"; ]
            "%ML1" -> ["%MX64";]
            "%ML2" -> ["%MX129";]
        [1?] LWord 가 64개 이상 인 경우 추가로 생성
     *)
    member private x.MonitorImpl(plcIp, tags:string seq, runSynchronously:bool, delayms:int, bScanWrite:bool) =
        logInfo $"Start PLC monitoring on {plcIp}"
        let conn = new DsXgConnection()
        let isConnected_ = conn.Connect(plcIp)  // don't remove
        
        let tagInfos = creatTags tags 
        plcTags.AddRange tagInfos |> ignore
        x.IsPLCTagsCreated <- true
        let chunkInfos =
            tagInfos
            |> Array.groupBy(fun ti -> ti.LWordTag)
            |> Array.chunkBySize 64

        noop()
        let batches = [|
            for ci in chunkInfos do
                let buffer = Array.zeroCreate<byte> (ci.Length * 8)
                for (n, (lwTag_, tis)) in ci |> Array.indexed do
                    for ti in tis do
                        ti.LWordOffset <- n
                        ti.BitSetChecker <-
                            fun buf ->
                                let lw = buf.GetLWord(n)
                                let bit = lw &&& (1UL <<< ti.BitOffset)
                                bit <> 0UL
                    logDebug $"{n}: {lwTag_}"

                let devices = [|
                    for (n, (lwTag_, tis)) in ci |> Array.indexed do
                        let ti = tis.[0]
                        let lwOffset = ti.BitOffset / 64
                        yield conn.CreateLWordDevice(ti.Device, lwOffset)
                |]

                let lwTags = ci |> Array.map fst
                let tags = ci |> Array.map snd |> Array.collect id
                yield LWBatch(lwTags, buffer, devices, tags)
        |]
        noop()

        /// 비동기 computation
        let compu = async {
            let notify (buffer:byte[]) (tagInfo:XgTagInfo) =
                let ci = ChangedTagInfo(plcIp, tagInfo.Tag, tagInfo.BitSetChecker(buffer))
                logDebug $"Tag change detected: {ci.Tag} = {ci.Value}"
                x.PLCTagChangedSubject.OnNext(ci)
                
            conn.CommObject.RemoveAll()

            /// 모든 monitoring tag 들이 1번의 batch 로 읽어 낼 수 있는지 여부 (LWord 접점 64 개 이하)
            let isSingleBatch = batches.Length = 1
            if isSingleBatch then
                batches.[0].DeviceInfos |> Seq.iter conn.CommObject.AddDeviceInfo

            let buffer = Array.zeroCreate<byte>(1024)
            let notifiedOnces = ResizeArray<LWBatch>()
            while true do
                if bScanWrite
                then 
                    conn.WriteDevices(tagInfos) //쓰기 대상이 있으면 먼저쓰고 읽기

                for batch in batches do
                    do! Async.Sleep delayms

                    if not isSingleBatch then
                        conn.CommObject.RemoveAll()
                        batch.DeviceInfos |> Seq.iter conn.CommObject.AddDeviceInfo

                    if conn.CommObject.ReadRandomDevice buffer <> 1 then
                        if not (conn.CheckConnect()) then 
                            failwith "Connection Failed"
                        if conn.CommObject.ReadRandomDevice buffer <> 1 then
                            failwith "ERROR"

                    let oldBuffer = batch.Buffer
                    if notifiedOnces.Contains(batch) then
                        // 최초 한번의 필수 notify를 이미 수행한 경우
                        // compare result
                        let range =
                            let numLW = oldBuffer.Length/8 - 1
                            [| 0..numLW |]
                        let lwsOld = range |> Array.map (fun n -> oldBuffer.GetLWord(n))
                        let lwsNew = range |> Array.map (fun n -> buffer.GetLWord(n))
                        for i in range do
                            let o = lwsOld.[i]
                            let n = lwsNew.[i]

                            if o <> n then
                                let diffBitPositions = getDiffBitPositions(o, n) |> HashSet
                                let changedTags =
                                    batch.Tags
                                    |> Array.filter(fun t ->
                                        t.LWordOffset = i
                                        && diffBitPositions.Contains(t.StartBitOffset))
                                changedTags |> Array.iter (notify buffer)
                    else
                        // 한번도 notify 하지 않은 경우
                        notifiedOnces.Add(batch)
                        batch.Tags |> Array.iter (notify buffer)

                    // copy result
                    for n in [0..oldBuffer.Length-1] do
                        oldBuffer.[n] <- buffer.[n]

                

            // 여기까지 수행될 일은 사실 없어야 한다.  (thread 자체가 kill 되므로)
            logInfo "Stoped PLC monitoring."
            assert(false)
        }

        (*
         * Monitoring thread 를 실행하고, 이를 취소할 수 있는 IDisposable 을 반환한다.
         * Hmi3D.Managed.PLCMonitor.Monitor(plcIp, tags, action) 에 의해서 본 함수가 호출된다.
         *)
        cts <- new CancellationTokenSource()
        if runSynchronously then
            compu |> Async.RunSynchronously       // for debug purpose
        else
            Async.Start(compu, cts.Token)

        Disposable.Create(fun () ->
            logInfo $"Stop PLC monitoring service on {plcIp}"
            cts.Cancel())

    member x.Monitor(plcIp, tags:string seq, runSynchronously:bool, delayms:int, bScanWrite:bool) =
        let tags = tags |> List.ofSeq
        match tags with
        | [] ->
            logWarn $"No monitoring target tags"
            Disposable.Create(fun () -> ())
        | _ ->
            x.MonitorImpl(plcIp, tags, runSynchronously, delayms, bScanWrite)

    member x.Monitor(plcIp, tags:string seq)           = x.Monitor(plcIp, tags, false, 50, false)
    member x.Scan(plcIp, tags:string seq, delayms:int) = x.Monitor(plcIp, tags, false, delayms, true)
   