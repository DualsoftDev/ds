namespace T
open NUnit.Framework
open Dual.Common.Core.FS
open Dual.Common.UnitTest.FS
open Engine.Info
open Engine.Core
open System

[<AutoOpen>]
module DBLoggerTestModule =
    type DBLoggerTest() =
        inherit EngineTestBaseClass()
        let dllPath = @$"{__SOURCE_DIRECTORY__}/../Engine.Custom.Sample/bin/Debug/net8.0/Engine.Custom.Sample.dll"
        let nullToken = Nullable<int64>()

        let createStorage =
            let counter = counterGenerator 1
            let helper (fqdn:string) (tagKind:int) =
                ORMStorage(counter(), fqdn, fqdn, tagKind, "Boolean")
            helper

        let createLog =
            let counter = counterGenerator 1
            let helper (storage:ORMStorage) (at:DateTime) (value:obj) =
                let modelId = -1
                ORMLog(counter(), storage.Id, at, value, modelId, nullToken)
            helper

        // now 부터 1 초 간격의 DateTime 생성
        let nextSecond =
            let now = DateTime.Now
            let counter = counterGenerator 0
            let helper () =
                now.AddSeconds(counter())
            helper

        let cyl1Error = createStorage "cyl1.trxErr" (int VertexTag.errorTRx)
        (*             2s      1s
                      +--|--+  +--+
                ------+     +--+  +--------
        *)
        let logs =
            [   createLog cyl1Error (nextSecond()) true     // 최고
                do
                    nextSecond() |> ignore
                createLog cyl1Error (nextSecond()) false
                createLog cyl1Error (nextSecond()) true
                createLog cyl1Error (nextSecond()) false    // 최신
            ]

        let storages = [
            cyl1Error
            createStorage "my.Test2.TRXERR" (int VertexTag.errorTRx)
        ]

        let queryCriteria = QueryCriteria(getNull<DSCommonAppSettings>(), -1, None, None)
        let logSet = createTestLoggerInfoSetForReader(queryCriteria, storages, logs)
        let fqdn, kind = cyl1Error.Fqdn, int VertexTag.errorTRx

        [<Test>]
        member __.``Basic Test`` () =
            let cyl2Error = createStorage "cyl2.trxErr" (int VertexTag.errorTRx)
            let cyl2Error = createStorage "cyl2.trxErr" (int VertexTag.errorTRx)
            let k = 1000.0

            2 === DBLogger.Count(fqdn, kind, logSet)
            false === DBLogger.GetLastValue(fqdn, kind, logSet)

            let onsTimeSpans = DBLogger.Sum(fqdn, kind, logSet)
            onsTimeSpans === (2.0 + 1.0) * k

            let avgONs = DBLogger.Average(fqdn, kind, logSet)
            avgONs === 1.5 * k


            // ON log 하나만 추가 된 후, 동일 test : cycle 미 완성
            let lastOn = createLog cyl1Error (nextSecond()) true
            let mutable logs = logs @ [lastOn]
            let logSet = createTestLoggerInfoSetForReader(queryCriteria, storages, logs)

            2 === DBLogger.Count(fqdn, kind, logSet)
            true === DBLogger.GetLastValue(fqdn, kind, logSet)

            let onsTimeSpans = DBLogger.Sum(fqdn, kind, logSet)
            onsTimeSpans === (2.0 + 1.0) * k

            let avgONs = DBLogger.Average(fqdn, kind, logSet)
            avgONs === 1.5 * k


            // OFF log 하나 더 추가해서 duration 완성된 후, 동일 test
            let lastOn = createLog cyl1Error (nextSecond()) false
            logs <- logs @ [lastOn]
            let logSet = createTestLoggerInfoSetForReader(queryCriteria, storages, logs)

            3 === DBLogger.Count(fqdn, kind, logSet)
            false === DBLogger.GetLastValue(fqdn, kind, logSet)

            let onsTimeSpans = DBLogger.Sum(fqdn, kind, logSet)
            onsTimeSpans === (2.0 + 1.0 + 1.0) * k

            let avgONs = DBLogger.Average(fqdn, kind, logSet)
            avgONs === (4.0 / 3.0) * k

            ()

        [<Test>]
        member __.``Mean Time Test`` () =
            ()
