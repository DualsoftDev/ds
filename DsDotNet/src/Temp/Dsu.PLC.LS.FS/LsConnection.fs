namespace Dsu.PLC.LS

open System.Net.Sockets
open FSharpPlus
open Engine.Common.FS
open Engine.Common.FS.Prelude
open Dsu.PLC.Common
open Dsu.PLC
open PacketImpl
open AddressConvert
open Cluster
open System.Diagnostics
open System


/// LS 산전 PLC connection parameters
// Tcp port = 2004, Udp port = 2005
type LsConnectionParameters(ip, ?port, ?protocol, ?timeout) =
    inherit ConnectionParametersEthernet(ip, port |? 2004us, protocol |? TransportProtocol.Tcp)
    do
        base.Timeout <- TimeSpan.FromMilliseconds(timeout |? 2000.0)

/// LS 산전 PLC CPU
type LsCpu(cpu, running) =
    member x.CpuType = cpu
    member val IsRunning = running with get, set
    interface ICpu with
        member x.Model = cpu.ToString()
        member x.IsRunning = x.IsRunning
        member x.Run() = ()
        member x.Stop() = ()

/// LS 산전 PLC Tag
[<DebuggerDisplay("{Name}")>]
type LsTag internal(conn:ConnectionBase, name:string, cpu:CpuType) as this =
    inherit TagBase(conn)
    let parsed = tryParseTag cpu (name) |> Option.get
    let dataLengthType = parsed.DataType.ToDataLengthType()
    do
        this.Name <- name
        base.Type <- parsed.DataType.ToTagType()


        let tName = conn.GetType().Name
        assert(tName = "LsConnection" || tName = "LsSwConnection" || tName = "LsXg5000Connection" || tName = "LsXg5000COMConnection")

    member x.Anal = parsed
    override x.IsBitAddress = name.StartsWith("%MX")
    override x.DataLengthType = dataLengthType

/// LS 산전 PLC Tag for XGK
and LsTagXgk(conn:ConnectionBase, name) =
    inherit LsTag(conn, name, CpuType.Xgk)

/// LS 산전 PLC Tag for XGB MK
and LsTagXgbMk(conn:ConnectionBase, name) =
    inherit LsTag(conn, name, CpuType.XgbMk)

/// LS 산전 PLC Tag for XGI : 61131-3
and LsTagXgi(conn:ConnectionBase, name) =
    inherit LsTag(conn, name, CpuType.Xgi)

/// XG-5000
and LsTagXg5000(conn:ConnectionBase, name) =
    inherit LsTag(conn, name, CpuType.Xgk)

/// LS 산전 H/W PLC connection
and LsConnection(parameters:LsConnectionParameters) as this =
    inherit ConnectionBase(parameters :> IConnectionParameters)

    let mutable conn:TcpClient = null
    let mutable cpu = CpuType.Unknown
    let mutable connSuccess = false;


    /// 초기 연결 타임아웃 체크
    let timeoutCheck (asyncresult:System.IAsyncResult) =
        connSuccess <- false;
        let tcpclient = asyncresult.AsyncState :?> TcpClient;

        if ((tcpclient.Client <> null) && (tcpclient.Client.Connected))
        then
            tcpclient.EndConnect(asyncresult);
            connSuccess <- true;
        else
            connSuccess <- false;

    /// connection 생성
    let createConnection() =
        dispose conn
        conn <- new TcpClient()
        let beginConnect = conn.BeginConnect(parameters.Ip, (parameters.Port|> int), System.AsyncCallback(timeoutCheck), conn)
        beginConnect.AsyncWaitHandle.WaitOne(parameters.Timeout, false) |> ignore
        System.Threading.Thread.Sleep 200
        if(connSuccess)
            then
                conn <- new TcpClient(parameters.Ip, parameters.Port |> int)
            else
                failwithlogf "Ip[%s] Port[%d] 연결 확인이 필요 합니다."  parameters.Ip (parameters.Port |>int)

    /// server 에 의해 connection 이 끊긴 경우, 확인 후 재접속
    let reconnectOnDemand() =
        if (conn = null || not <| conn.Client.IsConnected()) then
            logDebug "Reconnecting to %s:%d" parameters.Ip parameters.Port
            System.Threading.Thread.Sleep 1000
            createConnection()

    /// get connection stream.  장애시 재접속 포함
    let stream() =
        reconnectOnDemand()
        conn.GetStream()

    /// packet 을 PLC 로 보내고, 응답으로 length 길이 만큼 읽은 buffer 를 반환한다.
    let sendPacketAndGetResponse = rawSendPacketAndGetResponse stream

    /// CPU type 정보를 얻기 위해서 모든 PLC 기종에 존재하는 sample tag 를 전송하고 header 를 얻는다.
    let getPacketHeader() =
        let dummyCpu = CpuType.Xgi
        createRandomReadRequestPacket dummyCpu [|"%MX00"|]    // 보낼 packet 및 response packet 의 length 를 구함
        ||> sendPacketAndGetResponse                    // packet  전송하고, (해당 길이에 맞는지 check 해서) response packet 구함
        |> verifyReponseHeader None

    do
        let header = getPacketHeader()
        logDebug "Header : %A" header
        cpu <- header.CpuType

    /// LS 산전 PLC CPU 정보.  XGB(mk) 의 경우, Status request 명령을 지원 안함
    let lsCpu =
        lazy
            try
                let status =
                    createStatusRequestPacket()  // 보낼 packet 및 response packet 의 length 를 구함
                    ||> sendPacketAndGetResponse // packet  전송하고, (해당 길이에 맞는지 check 해서) response packet 구함
                    |> parseStatusResponsePacket // 응답 packet 분석해서 [ (tag * value) ] 를 반환

                assert(cpu = status.CpuType)
                LsCpu(status.CpuType, status.IsRunning)
            with exn ->
                LsCpu(cpu, true)



    /// 복수개의 tags(최대 16개까지) 들을 읽는다.
    let readRandomTagsWithNames (tags:string []) =
        assert(tags.Length <= 16)
        rawReadRandomTagsWithNames stream cpu tags

    /// 시작 tag 와 갯수를 주었을 때, 읽어 내는 방법
    let readBlock:ByteReader =
        fun startTag count ->
            rawReadBlock stream cpu startTag count

    /// 복수개의 tags(최대 16개까지) 들을 쓴다.
    /// write 한 tag 의 갯수를 반환
    let writeRandomTagsWithNames (tagsAndValues:(string * uint64)[]) =
        assert(tagsAndValues.Length <= 16)
        let tags, values = tagsAndValues |> unzip

        createRandomWriteRequestPacket cpu tags values   // 보낼 packet 및 response packet 의 length 를 구함
        ||> sendPacketAndGetResponse                     // packet  전송하고, (해당 길이에 맞는지 check 해서) response packet 구함
        |> analyzeRandomWriteResponse cpu tags           // 응답 packet 분석


    /// CPU type 에 맞는 tag 생성
    let createTag name =
        match cpu with
        | CpuType.Xgi   -> new LsTagXgi(this, name) :> TagBase;
        | CpuType.Xgk   -> new LsTagXgk(this, name) :> TagBase;
        | CpuType.XgbMk -> new LsTagXgbMk(this, name) :> TagBase;
        | _             -> failwith "ERROR"


    /// Random write 를 위해서 크기별로 최대 16점까지 모아서 write
    let groupTagsForRandomWrite cpu (tagsAndValues:(string*uint64) []) =
        let anals =
            tagsAndValues
            |> map fst
            |> map(fun t -> (t, tryParseTag cpu t|> Option.get))
            |> Tuple.toDictionary
        tagsAndValues
        |> groupBy (fun (t, _) -> anals.[t].DataType.ToDataLengthType())
        |> Array.map2nd (chunkBySize maxRandomReadTagCount)


    /// Channel tag (LW 기준으로 사용된 tag) 별로 원래 tag 가 무엇이었는지를 map 으로 생성
    let mapChannelTags (channelTags:string[]) (originalTags:string[]) =
        let ots = originalTags |> map (fun t -> (t, tryParseTag cpu t |> Option.get))
        let mapping =    // channel tag --> [original tag]
            [
                for ct in channelTags do
                    let ctAnal = tryParseTag cpu ct |> Option.get
                    let ctS, ctE = (ctAnal.BitOffset, ctAnal.BitOffset + ctAnal.BitLength)
                    for (ot, otAnal) in ots do
                        let ltS, ltE = (otAnal.BitOffset, otAnal.BitOffset + otAnal.BitLength)
                        if ctS <= ltS && ltE <= ctE then
                            yield (ct, ot)
            ] |> MultiMap.CreateFlat
        mapping

    /// (Block 읽기 등의 optimize 반영한) channelize
    let channelize (lsTags:LsTag []) =
        let tags = lsTags |> map name
        let tagsDic = lsTags |> Array.map(fun t -> (t.Name, t)) |> Tuple.toDictionary

        [
            for (channelTags, reader) in planReadTags  readRandomTagsWithNames readBlock cpu tags do
                let channelLsTags =
                    let mmap = mapChannelTags channelTags tags
                    mmap.EnumerateKeyAndValue()
                    |> map snd
                    |> map (fun t -> tagsDic.[t])


                yield LsChannelRequestExecutor(this, channelLsTags, reader)
        ]


    let getPlannedTagsReaders (tags:string []) =
        planReadTags readRandomTagsWithNames readBlock cpu tags

    /// Random / Discrete read
    let readRandomTags (tags:string []) =
        [
            for (channelTagsSplit, reader) in planReadTags  readRandomTagsWithNames readBlock cpu tags do
                yield! reader()
        ]

    /// Random / Discrete write
    let writeRandomTags (tagsAndValues:(string*uint64)[]) =
        /// data 크기별로, 최대 16개씩의 chunk
        let perSizes = groupTagsForRandomWrite cpu tagsAndValues
        [
            for (sz, tss) in perSizes do    // tss: data 크기가 fix 된 상태에서 16개씩의 chunk
            for ts in tss do
                assert(ts.Length <= maxRandomReadTagCount)
                writeRandomTagsWithNames ts
        ] |> sum


    /// Stream 반환.  네트워크 장애시 재접속 기능 포함
    member val Stream = stream()
    override x.Connect() =
        reconnectOnDemand()
        true
    override x.Disconnect() = conn.Dispose(); true
    override x.CreateTag(name) = createTag name
    override x.Cpu = lsCpu.Value :> ICpu


    /// 하나의 tag 값을 읽어 낸다.  return type : obj
    override x.ReadATag(tag) = x.ReadATag(tag.Name)


    /// 하나의 tag 값을 즉시 읽어 낸다.  return type: uint64.  ValueChanged event 등은 발생하지 않는다.
    member x.ReadATagUI8(tag:string) = readRandomTagsWithNames [|tag|] |> Array.head |> snd

    /// 하나의 tag 값을 즉시 읽어 낸다.  return type: obj.  ValueChanged event 등은 발생하지 않는다.
    member x.ReadATag(tag:string) =
        let anal = tryParseTag cpu tag |> Option.get
        x.ReadATagUI8(tag)
        |> anal.DataType.BoxUI8


    /// 복수개의 tag 값을 PLC 로부터 읽어 낸다.
    member x.ReadRandomTags (tags:LsTag []) =
        let tagsDic = tags |> Array.map(fun t -> (t.Name, t)) |> Tuple.toDictionary
        for (tag, value) in readRandomTags (tags |> map name) do
            let lsTag = tagsDic.[tag]
            /// uint64 를 type 에 맞게 casting 해서 넣어 주어야 함
            let v = lsTag.Anal.DataType.BoxUI8(value)
            lsTag.Value <- v

    /// 복수개의 tag 값을 PLC 로 write
    member x.WriteRandomTags (tags:LsTag []) =
        let tagsAndValues = tags |> map (fun t -> (t.Name, t.Anal.DataType.Unbox2UI8(t.Value)))
        writeRandomTags tagsAndValues
    member x.WriteRandomTags tagsAndValues = writeRandomTags tagsAndValues



    member x.GetPlannedReaders tags = getPlannedTagsReaders tags

    // 복수개의 tag 값을 즉시 읽어 낸다.
    // string 기반 복수개의 tag 를 최소한의 접근을 통해서 읽어 낼 수 있도록 구현.
    member x.ReadRandomTags (tags:string []) =
        //let cpu = getCpuType()
        //planTagsLookup readRandomTagsWithNames readBlock cpu tags
        readRandomTags tags


    /// 연속 byte 쓰기 : API 필요한가?
    member private x.WriteBlock (startTag:string) dataBlocks =
        createBlockWriteRequestPacket cpu startTag dataBlocks // 보낼 packet 및 response packet 의 length 를 구함
        ||> sendPacketAndGetResponse                          // packet  전송하고, (해당 길이에 맞는지 check 해서) response packet 구함
        |> analyzeBlockWriteResponse cpu                      // 응답 packet 분석


    override x.Channelize(tags:seq<TagBase>) =
        tags
        |> Seq.cast<LsTag>
        |> Array.ofSeq
        |> channelize
        |> Seq.cast<ChannelRequestExecutor>


and LsChannelRequestExecutor(conn, tags, reader:CachedTagsReader) =
    inherit ChannelRequestExecutor(conn :> ConnectionBase, tags |> Seq.cast<TagBase>)

    let tagsDic = tags |> map (fun t -> (t.Name, t)) |> Tuple.toDictionary

    override x.ExecuteRead() =
        for tag, value in reader() do
            let lsTag = tagsDic.[tag]
            let v = lsTag.Anal.DataType.BoxUI8(value)
            lsTag.Value <- v   /// uint64 를 type 에 맞게 casting 해서 넣어 주어야 함

        true
