namespace T.IOHub

open Dual.UnitTest.Common.FS
open NUnit.Framework
open System
open System.Diagnostics
open IO.Core
open Xunit
open Dual.Common.Core.FS
open System.IO

[<AutoOpen>]
module TestLockModule =
    
    let lockedExe (locker:obj) f =
        if isNull(locker) then
            Trace.WriteLine("Locked")
            f()
        else
            Trace.WriteLine("Unocked")
            lock locker f

    let testSizeof<'T> () = 
        sizeof<'T>
        

    let readTBytes<'T> (tOffset:int): byte[] =
        let size = sizeof<'T>
        let stream:FileStream = null
        let byteOffset = tOffset * size
        let buffer = Array.zeroCreate<byte> (size)
        stream.Seek(int64 byteOffset, SeekOrigin.Begin) |> ignore
        stream.Read(buffer, 0, size) |> ignore
        buffer


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

        [<Test>]
        member x.SizeOf() =
            testSizeof<byte>() === 1
            testSizeof<uint16>() === 2
