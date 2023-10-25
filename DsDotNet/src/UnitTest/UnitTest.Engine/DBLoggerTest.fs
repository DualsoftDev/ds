namespace T
open NUnit.Framework
open Dual.Common.Core.FS
open Dual.UnitTest.Common.FS
open Engine.Info
open Engine.Core
open System

[<AutoOpen>]
module DBLoggerTestModule =
    type DBLoggerTest() =
        inherit EngineTestBaseClass()
        let dllPath = @$"{__SOURCE_DIRECTORY__}/../Engine.Custom.Sample/bin/Debug/net7.0/Engine.Custom.Sample.dll"

        let createStorage =
            let counter = counterGenerator 1
            let helper (fqdn:string) (tagKind:int) =
                Storage(counter(), tagKind, fqdn, "Boolean", fqdn)
            helper

        let createLog =
            let counter = counterGenerator 1
            let helper (storage:Storage) (at:DateTime) (value:obj) =
                ORMLog(counter(), storage.Id, at, value)
            helper

        let nextSecond =
            let now = DateTime.Now
            let counter = counterGenerator 0
            let helper () =
                now.AddSeconds(counter())
            helper

        [<Test>]
        member __.``Basic Test`` () =
            let cyl1Error = createStorage "cyl1.trxErr" (int ApiItemTag.trxErr)
            let cyl2Error = createStorage "cyl2.trxErr" (int ApiItemTag.trxErr)
            let cyl2Error = createStorage "cyl2.trxErr" (int ApiItemTag.trxErr)
            let storages = [
                cyl1Error
                createStorage "my.Test2.TRXERR" (int ApiItemTag.trxErr)
            ]

            (*                2s      1s
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
                ] |> List.rev

            let logSet = createTestLoggerInfoSetForReader(storages, logs)
            let fqdn, kind = cyl1Error.Fqdn, int ApiItemTag.trxErr

            2 === DBLogger.CountLog(fqdn, kind, logSet)
            false === DBLogger.GetLastValue(fqdn, kind, logSet)

            let onsTimeSpans = DBLogger.CollectONDurations(fqdn, kind, logSet)
            onsTimeSpans |> SeqEq [TimeSpan.FromSeconds(2.0); TimeSpan.FromSeconds(1.0)]

            let avgONs = DBLogger.GetAverageONDuration(fqdn, kind, logSet)
            avgONs.TotalMilliseconds === 1500.0


            // log 하나 추가 후, 동일 test
            let lastOn = createLog cyl1Error (nextSecond()) true
            let logSet = createTestLoggerInfoSetForReader(storages, lastOn::logs)

            3 === DBLogger.CountLog(fqdn, kind, logSet)
            true === DBLogger.GetLastValue(fqdn, kind, logSet)

            let onsTimeSpans = DBLogger.CollectONDurations(fqdn, kind, logSet)
            onsTimeSpans |> SeqEq [TimeSpan.FromSeconds(2.0); TimeSpan.FromSeconds(1.0)]

            let avgONs = DBLogger.GetAverageONDuration(fqdn, kind, logSet)
            avgONs.TotalMilliseconds === 1500.0
            ()
