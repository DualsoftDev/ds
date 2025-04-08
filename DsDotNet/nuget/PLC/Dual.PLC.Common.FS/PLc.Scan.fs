namespace Dual.PLC.Common.FS

open System
open System.Collections.Generic
open System.Threading

type ScanAddress = string
type TagInfo =
    {
        Name : string 
        Address : ScanAddress
        Comment : string
        IsOutput : bool
    }

[<AbstractClass>]
type PlcScanBase(ip: string, scanDelay: int, isMonitorOnly:bool) =

    let tagValueChanged = Event<PlcTagValueChangedEventArgs>()
    let connectChanged = Event<ConnectChangedEventArgs>()
    let notifiedOnce = HashSet<obj>() // 자식 클래스에서 실제 타입으로 사용

    let mutable cancelToken = new CancellationTokenSource()
    let mutable isRunning = false

    do
        if String.IsNullOrWhiteSpace(ip) then
            invalidArg "ip" "PLC IP는 비어 있을 수 없습니다."

    // ---------------------------
    // 🔔 이벤트 노출
    // ---------------------------
    [<CLIEvent>]
    member _.TagValueChangedNotify = tagValueChanged.Publish

    [<CLIEvent>]
    member _.ConnectChangedNotify = connectChanged.Publish

    member _.TriggerTagChanged(evt: PlcTagValueChangedEventArgs) = tagValueChanged.Trigger(evt)
    member _.TriggerConnectChanged(evt: ConnectChangedEventArgs) = connectChanged.Trigger(evt)

    // ---------------------------
    // 📡 연결 및 스캔 추상 멤버
    // ---------------------------
    abstract member Connect: unit -> unit
    abstract member Disconnect: unit -> unit
    abstract member IsConnected: bool
    abstract member WriteTags: unit -> unit
    abstract member ReadTags: unit -> unit
    abstract member PrepareTags: TagInfo seq -> IDictionary<ScanAddress, PlcTagBase>

    // ---------------------------
    // 🟢 현재 스캔 상태 확인용
    // ---------------------------
    member _.IsScanning = isRunning

    // ---------------------------
    // 🚀 스캔 시작
    // ---------------------------
    member this.Scan(tags: TagInfo seq) : IDictionary<ScanAddress, PlcTagBase> =
        cancelToken.Cancel()

        let tagMap = this.PrepareTags(tags)

        async {
            while isRunning do
                do! Async.Sleep 50

            cancelToken <- new CancellationTokenSource()
            isRunning <- true
            try
                try
                    while not cancelToken.IsCancellationRequested do
                        if isMonitorOnly
                        then 
                            if tagMap.Values |> Seq.exists(fun f-> f.GetWriteValue().IsSome)
                            then // 모니터 모드인데 태그 쓰기 요청이 있는 경우
                                let errTags = tagMap.Values |> Seq.filter(fun f-> f.GetWriteValue().IsSome)
                                failwith $"모니터 모드에서 태그 쓰기 요청이 있습니다: {errTags|>Seq.head}"
                        else 
                            this.WriteTags()

                        this.ReadTags()

                        do! Async.Sleep scanDelay
                with ex ->
                    printfn "[PLC SCAN ERROR] %s: %s" ip ex.Message
            finally
                isRunning <- false
                cancelToken.Cancel()
                
        } |> Async.Start
        
        tagMap

    // ---------------------------
    // 🛑 스캔 중단
    // ---------------------------
    member this.StopScan() =
        if not cancelToken.IsCancellationRequested then
            cancelToken.Cancel()
