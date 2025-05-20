namespace Engine.Info

open System
open System.IO
open Microsoft.Data.Sqlite
open Engine.Core
open Dual.Common.Core.FS
open System.Collections.Generic
open System.Data
open Dapper
open Dual.Common.Db
open System.Runtime.CompilerServices
open MathNet.Numerics.Distributions



/// DB logging query 기준
/// StartTime: 조회 시작 기간.
/// Null 이면 사전 지정된 start time 을 사용.  (사전 지정된 값이 없을 경우, DateTime.MinValue 와 동일)
/// 모든 데이터 조회하려면 DateTime.MinValue 를 사용
[<AllowNullLiteral>]
type QueryCriteria(commonAppSettings:DSCommonAppSettings, modelId:int, startAt: DateTime option, endAt: DateTime option) =
    //new() = QuerySet(getNull<DSCommonAppSettings>(), -1, None, None)

    new(commonAppSettings, modelId, startAt: Nullable<DateTime>, endAt: Nullable<DateTime>) =
        QueryCriteria(commonAppSettings, modelId, startAt |> Option.ofNullable, endAt |> Option.ofNullable)

    member x.ModelId = modelId
    /// 사용자 지정: 조회 start time
    member x.TargetStart = startAt
    /// 사용자 지정: 조회 end time
    member x.TargetEnd = endAt
    member val StartTime = startAt |? DateTime.MinValue with get, set
    member val EndTime = endAt |? DateTime.MaxValue with get, set
    member val CommonAppSettings = commonAppSettings
    member val DsConfigJsonPath = "" with get, set

[<AutoOpen>]
module DBLoggerORM2 =

    type Fqdn = string
    type StorageKey = TagKind * Fqdn

    let getStorageKey (s: ORMStorage) : StorageKey = s.TagKind, s.Fqdn

    /// StorageKey(-> TagKind*Fqdn) 로 주어진 항목에 대한 조회 기간 전체의 summary (-> Count, Sum)
    /// durations: sec 단위 개별 실행 duration
    type Summary(logSet: LogSet, storageKey: StorageKey, durations:float seq) =
        /// storageKey 에 해당하는 모든 durations.  variance 를 구하기 위해서 모든 instance 필요.
        member val Durations = ResizeArray durations
        /// Container reference
        member x.LogSet = logSet
        member x.StorageKey = storageKey

    and Summary with
        /// Number rising
        member x.Count = x.Durations.Count
        member x.Sum = x.Durations |> Seq.sum
        /// 평균
        member x.Average = if x.Durations.IsEmpty() then 0.0 else x.Durations |> Seq.average

        /// 표본 분산
        member x.Variance =
            if x.Count > 1 then
                let mean = x.Average
                x.Durations
                |> map (fun x -> (x - mean) ** 2.0)
                |> Seq.sum |> fun sum -> sum / float (x.Count - 1)  // -->
                // 분산을 계산할 때, Seq.average를 사용하고 있지만, 분산 계산 시 표본 분산을 고려한다면 Seq.sum 후에 Count - 1로 나누는 것이 더 정확
                // 다음 대신 .. |> Seq.average
            else
                0.0
        /// 표준 편차
        member x.StdDev = sqrt x.Variance
        /// 표준 편차 (σ)
        member x.Sigma  = sqrt x.Variance

        /// 평균 (μ)
        member x.μ = x.Average
        /// 표준 편차 (σ)
        member x.σ = x.Sigma
        /// 분산
        member x.S = x.Variance
        /// 신뢰구간 -> L, U limit 반환.
        ///
        /// - zHalfSigma : 정규분포에서 α/2에 해당하는 Z-값.  (예를 들어, 95% 신뢰구간의 경우 Z-값은 약 1.96)
        member x.CalculateZScoreLimits(zHalfSigma:float) =
            let limit = zHalfSigma * x.σ
            let l, u = x.μ - limit, x.μ + limit
            l, u

        /// L, U -> 신뢰구간 구하기
        member x.CalculateConfidenceInterval(l:float, u:float) =
            let zL = (l - x.μ) / x.σ
            let zU = (u - x.μ) / x.σ

            // 정규 분포의 누적 분포 함수 (CDF) 계산
            let ΦZu = Normal.CDF(0.0, 1.0, zU)
            let ΦZl = Normal.CDF(0.0, 1.0, zL)

            ΦZu - ΦZl

        /// 'ci %' 신뢰구간에 해당하는 Z-score 계산
        ///
        /// - e.g: 95% 신뢰구간 -> 한쪽 끝 위치: 1 - (1 - 0.95)/2 = 0.975% -> 1.96
        static member ComputeZScoreFromConfidenceInterval(ci:float) =
            assert( 0.0 <= ci && ci <= 1.0)
            let cumulativeProbability  = 1.0 - (1.0 - ci)/2.0
            Normal(0.0, 1.0).InverseCumulativeDistribution(cumulativeProbability)

        // { CPK
        /// Cpk ≥ 1.33: 공정이 안정적이고, 제품이 규격 범위 내에서 일관되게 생산됩니다.
        member x.CalculateCpk (l: float, u: float) =
            let cpkUpper = (u - x.μ) / (3.0 * x.σ)
            let cpkLower = (x.μ - l) / (3.0 * x.σ)
            min cpkUpper cpkLower

        /// Cpk가 주어졌을 때 USL과 LSL 계산
        ///
        /// - nSigam : 3 Sigma 보다 6 Sigma 이면 상/하한 범위가 더 커진다.
        member x.CalculateSpecLimitsUsingCpk(cpk: float, ?nSigma:float) =
            let nSigma = nSigma |? 3.0
            let u = x.μ + cpk * nSigma * x.σ
            let l = x.μ - cpk * nSigma * x.σ
            l, u
        // } CPK


    /// DB logging 관련 전체 설정
    and LogSet(queryCriteria: QueryCriteria, systems: DsSystem seq, storages: ORMStorage seq, readerWriterType: DBLoggerType) as this =
        let summaryDic =
            storages
            |> map (fun s ->
                let key = getStorageKey s
                key, Summary(this, key, [||]))
            |> Tuple.toReadOnlyDictionary

        interface ILogSet

        member x.Summaries = summaryDic
        member x.ModelId = queryCriteria.ModelId
        member x.ReaderWriterType = readerWriterType
        member x.GetSummary(storageKey: StorageKey) = summaryDic[storageKey]

        member val Systems = systems |> toArray
        member val QuerySet = queryCriteria with get, set
        member val Storages = storages |> map (fun s -> getStorageKey s, s) |> Tuple.toReadOnlyDictionary

        // { mutables
        member val StoragesById = Dictionary<int, ORMStorage>()
        member val LastLogs = Dictionary<ORMStorage, Log>()
        member val TheLastLog: Log option = None with get, set
        //member val Disposables = new CompositeDisposable()
        // } mutables


    /// property table 항목 조회
    let queryPropertyAsync (modelId:int, propertyName: string, conn: IDbConnection, tr: IDbTransaction) =
        conn.QueryFirstOrDefaultAsync<string>(
            $"SELECT value FROM [{Tn.Property}] WHERE name = @Name AND modelId=@ModelId",
            {| Name = propertyName; ModelId=modelId |},
            tr
        )

    /// property table 항목 수정
    let updatePropertyAsync (modelId:int, propertyName: string, value: string, conn: IDbConnection, tr: IDbTransaction) =
        conn.ExecuteSilentlyAsync(
            $"""INSERT OR REPLACE INTO [{Tn.Property}]
                (name, value, modelId)
                VALUES(@Name, @Value, @ModelId);""",
            {| Name = propertyName; Value = value; ModelId=modelId |},
            tr
        )


    type QueryCriteria with

        /// 조회 기간 target 설정 값 필요시 db 에 반영하고, target 에 맞게 조회 기간 변경
        member x.SetQueryRangeAsync(modelId:int, conn: IDbConnection, tr: IDbTransaction) =
            task {
                match x.TargetStart with
                | Some s ->
                    do! updatePropertyAsync (modelId, PropName.Start, s.ToString(), conn, tr)
                    x.StartTime <- s
                | _ ->
                    let! str = queryPropertyAsync (modelId, PropName.Start, conn, tr)

                    x.StartTime <-
                        if isNull (str) then
                            DateTime.MinValue
                        else
                            DateTime.Parse(str)


                match x.TargetEnd with
                | Some e ->
                    do! updatePropertyAsync (modelId, PropName.End, e.ToString(), conn, tr)
                    x.EndTime <- e
                | _ ->
                    let! str = queryPropertyAsync (modelId, PropName.End, conn, tr)

                    x.EndTime <-
                        if isNull (str) then
                            DateTime.MaxValue
                        else
                            DateTime.Parse(str)

                logInfo $"Query range set: [{x.StartTime} ~ {x.EndTime}]"
            }


    let createConnectionWith (connStr) =
        new SqliteConnection(connStr) |> tee (fun conn -> conn.Open())

    let getNewTagKindInfosAsync (conn: IDbConnection, tr: IDbTransaction) =
        let tagKindInfos = GetAllTagKinds ()

        task {
            let! existingTagKindMap = conn.QueryAsync<ORMTagKind>($"SELECT * FROM [{Tn.TagKind}];", null, tr)       // WHERE modelId = {modelId}

            let existingTagKindHash =
                existingTagKindMap |> map (fun t -> t.Id, t.Name) |> HashSet

            return tagKindInfos |> filter (fun t -> not <| existingTagKindHash.Contains(t))
        }

type WriterDBSettingsExt =
    [<Extension>]
    static member CreateConnection(loggerDBSettings:WriterDBSettings): SqliteConnection =
        let connStr = $"Data Source={loggerDBSettings.ConnectionPath}"
        createConnectionWith connStr

    [<Extension>]
    static member DropDatabase(loggerDBSettings:WriterDBSettings) =
        use conn = loggerDBSettings.CreateConnection()
        conn.DropDatabase()

    [<Extension>]
    static member ComputeModelId(loggerDBSettings:WriterDBSettings): int =
        use conn = loggerDBSettings.CreateConnection()
        // db 가 아직 초기화되지 않은 경우의 처리???
        failwith "Not implemented"

    [<Extension>]
    static member FillModelId(loggerDBSettings:WriterDBSettings): int*string =
        Directory.CreateDirectory(Path.GetDirectoryName(loggerDBSettings.ConnectionPath)) |> ignore
        use conn = loggerDBSettings.CreateConnection()
        use tr = conn.BeginTransaction()
        let tableExists = conn.IsTableExistsAsync(Tn.Model).Result

        let mutable path = loggerDBSettings.ModelFilePath
        let mutable runtime = loggerDBSettings.DbWriter
        let lastModelInfoSpecified = path.NonNullAny() && runtime.NonNullAny()


        if not tableExists then
            if not lastModelInfoSpecified then
                failwithlog "ModelFilePath and DbWriter must be set for empty database!"

            // schema 새로 생성
            conn.ExecuteSilentlyAsync(sqlCreateSchema, tr).Wait()

        if not lastModelInfoSpecified then
            let propDic = conn.Query<ORMProperty>($"SELECT * FROM [{Tn.Property}]", null, tr) |> map (fun p -> p.Name, p.Value) |> Tuple.toDictionary
            path <- propDic[PropName.ModelFilePath]
            if runtime.IsNullOrEmpty() then
                runtime <- propDic[PropName.ModelRuntime]

        let param = {|Path = path; Runtime = runtime |}
        let optModel =
            let sql =
                $"""SELECT * FROM {Tn.Model}
                        WHERE path = @Path
                        AND runtime = @Runtime"""
            conn.TryQuerySingle<ORMModel>(sql, param, tr)

        match optModel with
        | Some m ->
            let propSql =
                $"""INSERT OR REPLACE INTO [{Tn.Property}]
                    (name, value)
                    VALUES(@Name, @Value);"""
            conn.Execute(propSql, {| Name = PropName.ModelFilePath; Value = path |}, tr) |> ignore
            conn.Execute(propSql, {| Name = PropName.ModelRuntime; Value = runtime |}, tr) |> ignore

            logInfo $"With new Model: id = {m.Id}, path={path}"
            loggerDBSettings.ModelId <- m.Id
        | _ ->
            let modelId =
                conn.InsertAndQueryLastRowIdAsync(tr,
                    $"""INSERT INTO [{Tn.Model}]
                        (path, runtime)
                        VALUES (@Path, @Runtime)
                    """,
                    param
                ).Result
            loggerDBSettings.ModelId <- modelId

            let newTagKindInfos = (getNewTagKindInfosAsync (conn, tr)).Result

            for (id, name) in newTagKindInfos do
                let query = $"INSERT INTO [{Tn.TagKind}] (id, name) VALUES (@Id, @Name);"
                conn.Execute(query, {| Id = id; Name = name |}, tr) |> ignore


        tr.Commit()

        loggerDBSettings.ModelId, path




[<AutoOpen>]
module DBLoggerORM3 =
    type DSCommonAppSettings with
        member x.ConnectionString = $"Data Source={x.WriterDBSettings.ConnectionPath}"
        member x.CreateConnection(): SqliteConnection = x.WriterDBSettings.CreateConnection()

