namespace T.DB
open Dual.UnitTest.Common.FS

open T

open Dapper
open Engine.Core
open Dual.Common.Core.FS
open NUnit.Framework
open Engine.Parser.FS
open Engine.Import.Office
open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS
open Microsoft.Data.Sqlite
open Engine.Info
open Engine.Cpu
open System.Text.Json




[<AutoOpen>]
module HelloDSTestModule =

    type HelloDSTest() =
        inherit EngineTestBaseClass()
        do
            RuntimeDS.Package <- PCSIM

        let helloDSPath = @$"{__SOURCE_DIRECTORY__}/../../../../Apps/OfficeAddIn/PowerPointAddInHelper/Utils/HelloDS.pptx"
        let pptParms:PPTParams = {TargetType = WINDOWS; AutoIOM = true; CreateFromPPT = false;  CreateBtnLamp = true}
        let getSystem() =
            let result = ImportPPT.GetDSFromPPTWithLib (helloDSPath, false, pptParms)
            let { 
                System = system
                ActivePath =  exportPath 
                LoadingPaths = loadingPaths 
                LayoutImgPaths = layoutImgPaths 
            } = result

            system.TagManager === null
            let _ = DsCpuExt.GetDsCPU (system) PlatformTarget.WINDOWS
            system.TagManager.Storages.Count > 0 === true

            system

        let createConnection() =
            let connStr = 
                let path = @"Z:\ds\Logger.sqlite3"
                $"Data Source={path}"
            new SqliteConnection(connStr) |> tee (fun conn -> conn.Open())

        let getLogs() =
            use conn = createConnection()
            let logs = conn.Query<ORMVwLog>($"SELECT * FROM {Vn.Log}")
            logs


        [<Test>]
        member __.``HelloDS vertices read test``() =
            let system = getSystem()

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
        member __.``X HelloDS stroage test``() =
            let system = getSystem()

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
                tracefn $"Storage: {k} = {v}"
            ()

            let logs = getLogs().ToFSharpList()
            let lls = logs.DistinctBy(fun l -> l.Name).ToArray()
            for l in logs do
                dic.ContainsKey(l.Fqdn) === true

            let g = groupDurationsByFqdn logs "HelloDS.STN1.Work1"
            ()

        [<Test>]
        member __.``X HelloDS log anal test``() =
            let system = getSystem()
            let logs = getLogs().ToFSharpList()
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
        member __.``X Load logger database test``() =
            let conn = createConnection()
            let loggerDb = 
                ORMDBSkeletonDTOExt.CreateAsync(1, conn, null).Result |> ORMDBSkeleton
            let log1 = conn.QueryFirst<ORMLog>($"SELECT * FROM {Vn.Log} WHERE id = 1;")
            let vwLog1 = loggerDb.ToView(log1)

            ()
