namespace Engine.Info

open System.Collections.Generic
open System.Data
open System.Linq
open Dapper
open Dual.Common.Core.FS.CollectionModule
open Dual.Common.Core.FS
open Engine.Core
open DBLoggerORM
open System

[<AutoOpen>]
module internal DBLoggerAnalysisModule =
    /// log 를 fqdn 별로 그룹핑하여 반환한다.
    /// - log 는 시간순 정렬되어 있어야 한다.
    /// - fqdn 의 going ON 부터 finish ON 까지의 log 를 그룹핑하여 반환한다.
    /// - e.g X type 의 duration log 를 구한다면 (Xs1, Xe1), (Xs2, Xe2), 구간 사이의 log 들을 취합해서 반환
    ///     - 입력 : [ 1; 2; 3; Xs1; 4; 5; 6; Xe1; 7; 8; Xs2; 9; 10; Xe2; ...]
    ///     - 출력 : [ [Xs1; 4; 5; 6; Xe1]; [Xs2; 9; 10; Xe2]; ...]
    let groupDurationsByFqdn (logs: ORMVwLog list) (fqdn:string) : ORMVwLog list list =
        // fqdn 에 해당하는 logging 시작 여부
        let mutable started = false
        // fqdn 시작부터 끝까지의 log 를 중간 저장하기 위한 list
        let mutable building = []
        let folder (acc:ORMVwLog list list) (l:ORMVwLog) =
            let isOn = l.Value.ToString() = "1"
            if l.Fqdn = fqdn && isOn then
                if started then
                    if l.TagKind = int VertexTag.finish then
                        building <- l::building
                        started <- false
                else
                    if l.TagKind = int VertexTag.going then
                        started <- true
            if started then
                building <- l::building
                acc
            else
                let res = if building.IsEmpty then acc else (building |> List.rev)::acc
                building <- []
                res

        let result = List.fold folder [] logs |> List.rev
        result


    type LogAnalInfo = {
        DsSystem: DsSystem
        /// 분석 대상 full log list
        Logs:ORMVwLog list
        /// Real 별 [시작 ~ 끝] log list
        PerRealLogs: Dictionary<Real, (ORMVwLog list list)>
    }

    type LogAnalInfo with
        static member Create(system:DsSystem, FList(logs:ORMVwLog list)) : LogAnalInfo =
            let reals = system.Flows |> bind (fun f -> f.Graph.Vertices.OfType<Real>()) |> toArray
            let dic =
                [
                    for r in reals do
                        let realLogs = groupDurationsByFqdn logs r.QualifiedName
                        r, realLogs
                ] |> Tuple.toDictionary

            {
                DsSystem = system
                Logs = logs
                PerRealLogs = dic
            }
        static member Create(system:DsSystem, conn:IDbConnection) : LogAnalInfo =
            let logs = conn.Query<ORMVwLog>($"SELECT * FROM {Vn.Log}") |> toFSharpList
            LogAnalInfo.Create(system, logs)

        member x.PrintStatistics() =
            let getTimeSpan (logs: ORMVwLog list) =
                headAndLast logs |> map (fun (h, t) -> t.At - h.At) |> Option.defaultValue (TimeSpan.FromSeconds 0.0)

            let total = getTimeSpan x.Logs
            tracefn $"Total time duration: {total}"
            tracefn "::: Per Real Logs"

            for KeyValue(r, lss) in x.PerRealLogs do
                tracefn $"::: Total cycles for {r.QualifiedName} = {lss.Length}"
                for (cycle, ls) in lss.Indexed() do
                    let realSpan = getTimeSpan ls
                    tracefn $"  :: Real duration for {r.QualifiedName}, {cycle+1}-th cycle = {realSpan}"

                    for c in r.Graph.Vertices.OfType<Call>() do
                        let callLogs =
                            let fqdn = c.QualifiedName
                            let logs = ls |> filter (fun l -> l.Fqdn = fqdn)
                            groupDurationsByFqdn logs fqdn

                        let spans = callLogs |> map getTimeSpan |> toArray
                        assert (spans.Length = 1)   // 동일 call 이 하나의 real 안에서 여러번 호출되는 경우는 없다고 가정
                        tracefn $"    - {c.Name} = {spans[0]} on cycle {cycle+1}/{lss.Length}"
            ()


/// Log 분석 정보: WebServer 에서 데이터 생성하고, Browser 에서 보여주기 위한 data
[<AutoOpen>]
module DBLoggerAnalysisDTOModule =
    type LogSpan = DateTime * DateTime
    let private dummySpan:LogSpan = (DateTime.MinValue, DateTime.MinValue)
    // Span 클래스 정의
    type Span(span:LogSpan) =
        new() = Span(dummySpan)
        new(s:DateTime, e:DateTime) = Span(LogSpan(s, e))
        member val Start = fst span with get, set
        member val End = snd span with get, set

    // FqdnSpan 클래스 정의
    type FqdnSpan(span:LogSpan, fqdn: string) =
        inherit Span(span)
        new() = FqdnSpan(dummySpan, "")
        member val Fqdn = fqdn with get, set

    // CallSpan 클래스 정의
    type CallSpan(span:LogSpan, fqdn: string) =
        inherit FqdnSpan(span, fqdn)
        new() = CallSpan(dummySpan, "")

    // RealSpan 클래스 정의
    type RealSpan(span:LogSpan, fqdn: string, flowName:string, callSpans: CallSpan[]) =
        inherit FqdnSpan(span, fqdn)
        new() = RealSpan(dummySpan, "", "", [||])
        member val FlowName = flowName with get, set 
        member val CallSpans = callSpans with get, set

    // SystemSpan 클래스 정의
    type SystemSpan(span: LogSpan, fqdn: string, realSpans: Dictionary<string, RealSpan list>) =
        inherit FqdnSpan(span, fqdn)
        new() = SystemSpan(dummySpan, "", Dictionary<string, RealSpan list>())
        member val RealSpans = realSpans with get, set

    type SystemSpan with
        static member CreateSpan(system: DsSystem, logs: ORMVwLog list) : SystemSpan =
            let logAnalInfo = LogAnalInfo.Create(system, logs)

            let createCallSpan (logs: ORMVwLog list) (call: Call) : CallSpan =
                let fqdn = call.QualifiedName
                let callLogs = logs |> List.filter (fun log -> log.Fqdn = fqdn)
                let span = 
                    match callLogs with
                    | [] -> dummySpan
                    | _ -> (callLogs.Head.At, callLogs.Last().At)
                CallSpan(span, fqdn)

            let createRealSpan (real: Real) (logs: ORMVwLog list) : RealSpan =
                let span = 
                    match logs with
                    | [] -> dummySpan
                    | _ -> (logs.Head.At, logs.Last().At)
                let calls = real.Graph.Vertices.OfType<Call>() |> Seq.map (createCallSpan logs) |> Seq.toArray
                let flowName = real.Flow.Name
                RealSpan(span, real.QualifiedName, flowName, calls)

            let createRealSpans (realLogs: ORMVwLog list list) (real: Real) : RealSpan list =
                realLogs |> List.map (createRealSpan real)

            let realSpans =
                logAnalInfo.PerRealLogs
                |> map (fun (KeyValue(r, lss)) -> r.QualifiedName, createRealSpans lss r)
                |> Tuple.toDictionary

            let span = 
                match logs with
                | [] -> dummySpan
                | _ ->
                    let ats = logAnalInfo.PerRealLogs.Values |> collect id |> collect id |> map(fun l -> l.At) |> toArray
                    let s, e = ats |> Seq.min, ats |> Seq.max
                    (s, e)
                    //(logs.Head.At, logs.Last().At)

            SystemSpan(span, system.Name, realSpans)


        static member CreatFlatSpan(system: DsSystem, logs: ORMVwLog list) : (string * Span[])[] =
            let sysSpan = SystemSpan.CreateSpan(system, logs)
            let namedSpans =
                [|
                    yield sysSpan.Fqdn, new Span(sysSpan.Start, sysSpan.End)
                    for KeyValue(rFqdn, rss) in sysSpan.RealSpans do
                        for rs in rss do
                            yield rFqdn, rs
                            for cs in rs.CallSpans do
                                yield cs.Fqdn, cs
                |]
            let grs = namedSpans.GroupBy(fun (fqdn, _) -> fqdn)
            let result = 
                grs |> map (fun gr ->
                    gr.Key, gr |> map snd |> toArray
                ) |> toArray

            result

    type FlatSpans = (string * Span[])[]


// For C# interop
module SystemSpanEx =
    /// 주어진 system 에 대한 log 목록을 분석해서 SystemSpan 결과를 반환
    /// System > Real > Call 의 계층 구조를 가지는 Span 정보를 생성한다.
    let CreateSpan(system: DsSystem, logs: ORMVwLog seq) : SystemSpan =
        let logList = logs |> toFSharpList
        SystemSpan.CreateSpan(system, logList)

    let CreateFlatSpan(system: DsSystem, logs: ORMVwLog seq) : FlatSpans =
        let logList = logs |> toFSharpList
        SystemSpan.CreatFlatSpan(system, logList)
