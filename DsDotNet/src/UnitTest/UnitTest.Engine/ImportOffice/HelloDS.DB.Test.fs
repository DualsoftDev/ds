namespace T.DB
open Dual.UnitTest.Common.FS

open T

open Dapper
open Engine.Core
open Dual.Common.Core.FS
open NUnit.Framework
open Engine.Import.Office
open System.Linq
open Microsoft.Data.Sqlite
open Engine.Info
open Engine.Cpu
open System.Text.Json
open Engine.Runtime
open System.IO
open System
open Engine.TestSimulator
open T.CPU


[<AutoOpen>]
module HelloDSDBTestModule =

    type HelloDSDBTest() =
        inherit EngineTestBaseClass()
        let runtimeModel, pathDB = 
            let helloDSPath = @$"{__SOURCE_DIRECTORY__}/../../../../Apps/OfficeAddIn/PowerPointAddInHelper/Utils/HelloDS.pptx"
            RuntimeTestCommon.getRuntimeModelForSim  helloDSPath
        let system = runtimeModel.System

        let createConnection(path) =
            let connStr =
                $"Data Source={path}"
            new SqliteConnection(connStr) |> tee (fun conn -> conn.Open())

        let getLogs(path:string) =
            use conn = createConnection(path)
            let logs = conn.Query<ORMVwLog>($"SELECT * FROM {Vn.Log}")
            logs

        [<Test>]
        member __.``HelloDS vertices read test``() =
            let flowNames = system.Flows |> map (fun f -> f.Name) |> toArray
            SeqEq flowNames ["STN1"]          // 숨김 페이지: "STN2"; "STN3"; "Flow1"; "KIT"
            let stn1 = system.Flows |> Seq.exactlyOne
            let reals = stn1.Graph.Vertices.OfType<Real>() |> toArray
            let realNames = reals |> map (fun r -> r.Name) |> toArray
            SeqEq realNames [|"Work1"; "Work2"|]
            reals[0].QualifiedName === "HelloDS.STN1.Work1"

            let callsInReal1 = reals[0].Graph.Vertices.OfType<Call>() |> toArray
            let callInReal1Names = callsInReal1 |> map (fun c -> c.Name) |> toArray
            SeqEq callInReal1Names [|
                "Device1.ADV"
                "Device2.ADV"
                "Device3.ADV"
                "Device4.ADV"
                "Device1.RET"
                "Device2.RET"
                "Device3.RET"
                "Device4.RET"
                |]

            let callsInReal2 = reals[1].Graph.Vertices.OfType<Call>() |> toArray
            let callInReal2Names = callsInReal2 |> map (fun c -> c.Name) |> toArray
            SeqEq callInReal2Names [||]

            let callDev1Adv = callsInReal1[0]
            callDev1Adv.Name === "Device1.ADV"
            ()

        [<Test>]
        member __.``HelloDS stroage test``() =
            (* Via Storages *)
            let storages = system.TagManager.Storages
            tracefn $"---- Storage"
            for KeyValue(k, v) in storages do
                tracefn $"Storage: {k} = {v}"

            let var = storages["HelloDS_STN1_Work1__Device2_ADV__ready"]

            //for KeyValue(k, v) in globalStorage do
            //    yield k, v.Tag
            ()

            (* 별도 함수 *)
            tracefn $"---- Fqdn objects"
            let dic = collectFqdnObjects system
            for KeyValue(k, v) in dic do
                tracefn $"Fqdn: {k} = {v}"
            ()

            let logs = getLogs(pathDB).ToFSharpList()
            let lls = logs.DistinctBy(fun l -> l.Name).ToArray()
            for l in logs do
                dic.ContainsKey(l.Fqdn) === true

            let g = groupDurationsByFqdn logs "HelloDS.STN1.Work1"
            ()

        [<Test>]
        member __.``HelloDS log anal test``() =
            let logs = getLogs(pathDB).ToFSharpList()
            let logAnalInfo = LogAnalInfo.Create(system, logs)
            logAnalInfo.PrintStatistics()


            let sysSpan = SystemSpan.CreateSpan(system, logs)

            let text1    = JsonSerializer.Serialize sysSpan
            let sysSpan1 = JsonSerializer.Deserialize<SystemSpan>(text1)
            let text2    = JsonSerializer.Serialize sysSpan1
            text1 === text2

            let flatSpans = SystemSpan.CreatFlatSpan(system, logs)
            ()


        [<Test>]
        member __.``Load logger database test``() =
            let conn = createConnection(pathDB)
            let loggerDb =
                ORMDBSkeletonDTOExt.CreateAsync(1, conn, null).Result |> ORMDBSkeleton
            let log1 = conn.QueryFirst<ORMLog>($"SELECT * FROM {Vn.Log} WHERE id = 1;")
            let vwLog1 = loggerDb.ToView(log1)

            ()
