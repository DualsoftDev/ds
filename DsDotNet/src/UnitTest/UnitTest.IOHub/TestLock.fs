namespace T.IOHub

open Dual.UnitTest.Common.FS
open NUnit.Framework
open System
open System.Diagnostics
open IO.Core
open Xunit
open Dual.Common.Core.FS

[<AutoOpen>]
module TestLockModule =
    
    let lockedExe (locker:obj) f =
        if isNull(locker) then
            Trace.WriteLine("Locked")
            f()
        else
            Trace.WriteLine("Unocked")
            lock locker f

    [<Collection("ZmqTesting")>]
    [<TestFixture>]
    type TestLock() =
        inherit TestBaseClass("IOHubLogger")


        [<Test>]
        member x.LockedFunctionTest() =
            let a = lockedExe x.Locker (fun () -> Trace.WriteLine("A"); 1)
            let b = lockedExe null (fun () -> Trace.WriteLine("B"); true)
            noop()
        [<Test>]
        member x.LockedActionTest() =
            let a1 = lockedExe x.Locker (fun () -> Trace.WriteLine("A"))
            Trace.WriteLine($"Unit Result={a1}")
